using System.Buffers;
using System.Net.WebSockets;

namespace BinaryMatrixPlayer;

public sealed class ClientSocket : IDisposable {
	private readonly WebSocket socket;
	private readonly byte[] receiveBuf = new byte[256];
	private readonly byte[] sendBuf = new byte[256];
	private readonly CancellationTokenSource cancelSource;
	private readonly AsyncMutex sendMutex = new();
	private readonly TaskCompletionSource completeTaskSource = new();

	public Task CompleteTask => this.completeTaskSource.Task;

	public ClientSocket(WebSocket socket, CancellationToken cancel) {
		this.socket = socket;
		this.cancelSource = CancellationTokenSource.CreateLinkedTokenSource(cancel);
	}

	public async Task Run() {
		try {
			while(true) {
				var res = await this.socket.ReceiveAsync(this.receiveBuf, this.cancelSource.Token);
				if(res.MessageType == WebSocketMessageType.Close) {
					await this.socket.CloseAsync(res.CloseStatus!.Value, "Mirroring peer close.", this.cancelSource.Token);
					break;
				}

				await this.sendMutex.EnterAsync(this.cancelSource.Token);
				try {
					await this.RawSend(this.receiveBuf.AsSpan(..res.Count), res.MessageType, res.EndOfMessage, this.cancelSource.Token);
				} finally {
					this.sendMutex.Exit();
				}
			}

			this.completeTaskSource.SetResult();
		} catch(OperationCanceledException e) {
			this.completeTaskSource.SetCanceled(e.CancellationToken);
			throw;
		} catch(Exception e) {
			Console.Error.Write("EXCEPTION DURING SOCKET HANDLING: ");
			Console.Error.WriteLine(e);
			this.completeTaskSource.SetException(e);
			throw;
		}
	}

	private Task RawSend(ReadOnlySpan<byte> data, WebSocketMessageType type, bool endOfMessage, CancellationToken cancel) {
		data.CopyTo(this.sendBuf);
		return this.socket.SendAsync(this.sendBuf[..data.Length], type, endOfMessage, cancel);
	}

	public Task Send(ReadOnlySpan<byte> data, WebSocketMessageType type, CancellationToken cancel) {
		CancellationToken innerCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel, this.cancelSource.Token).Token;
		if(data.Length > this.sendBuf.Length)
			throw new ArgumentException("Your data is too long.", nameof(data));

		byte[] buffer = ArrayPool<byte>.Shared.Rent(data.Length);
		data.CopyTo(buffer);

		return this.sendMutex.EnterAsync(innerCancel)
			.ContinueWith(x => this.RawSend(buffer, type, true, innerCancel), TaskContinuationOptions.OnlyOnRanToCompletion)
			.ContinueWith(_ => {
				this.sendMutex.Exit();
				ArrayPool<byte>.Shared.Return(buffer);
			}, CancellationToken.None)
		;
	}

	public void Cancel() {
		this.cancelSource.Cancel();
	}

	public void Dispose() {
		this.cancelSource.Cancel();
		this.socket.Dispose();
	}
}

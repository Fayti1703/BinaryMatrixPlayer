using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace BinaryMatrixPlayer;

public struct Nothing { }

public static class BackgroundRunner {

	private static readonly CancellationTokenSource GlobalCancel = new();
	private static readonly ConcurrentDictionary<ClientSocket, Nothing> sockets = new();

	public static void Run() {
		MainLoop().Wait();

		foreach(ClientSocket socket in sockets.Keys)
			socket.Dispose();
	}

	private static async Task MainLoop() {
		try {
			while(!GlobalCancel.IsCancellationRequested) {
				foreach(ClientSocket socket in sockets.Keys) {
					await socket.Send("-- HEARTBEAT --"u8, WebSocketMessageType.Text, GlobalCancel.Token);
				}
				await Task.Delay(5000, GlobalCancel.Token);
			}
		} catch(OperationCanceledException) {}

	}

	public static void Stop() {
		GlobalCancel.Cancel();
	}

	public static ClientSocket AddSocket(WebSocket socket, CancellationToken cancel) {
		ClientSocket wrappedSocket = new(socket, CancellationTokenSource.CreateLinkedTokenSource(GlobalCancel.Token, cancel).Token);
		sockets.AddOrUpdate(wrappedSocket, default(Nothing), (_, _) => default);
		wrappedSocket.Run().ContinueWith(x => {
			sockets.TryRemove(wrappedSocket, out _);
		}, CancellationToken.None).ContinueWith(x => {
			Console.Error.Write("EXCEPTION DURING SOCKET HANDLING: ");
			Console.Error.WriteLine(x.Exception);
		}, TaskContinuationOptions.OnlyOnFaulted);

		return wrappedSocket;
	}
}

using System.Net.WebSockets;
using System.Reflection;
using Fayti1703.CommonLib.FluentAccess;

namespace BinaryMatrixPlayer;

/* wrapper for System.Threading.AsyncMutex, which for some fucking reason is not exposed publicly */
public class AsyncMutex {

	/* HACK: This is not portable at all. */
	private static readonly TypeInfo mutexType = typeof(WebSocket).Assembly.GetType("System.Threading.AsyncMutex", true)!.Rd();
	private static readonly ConstructorInfo mutexConstructor = mutexType.Ctor();

	private static readonly MethodInfo EnterAsyncMethod = mutexType.Mth("EnterAsync", typeof(CancellationToken));
	private static readonly MethodInfo ExitMethod = mutexType.Mth("Exit", Array.Empty<Type>());

	private readonly object mutex = mutexConstructor.Invoke(Array.Empty<object>());

	public Task EnterAsync(CancellationToken cancellationToken) {
		return (Task) EnterAsyncMethod.Invoke(this.mutex, new object[] { cancellationToken })!;
	}

	public void Exit() {
		ExitMethod.Invoke(this.mutex, Array.Empty<object>());
	}
}

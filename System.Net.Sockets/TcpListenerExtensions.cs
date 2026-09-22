using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets;

public static class TcpListenerExtensions
{
	public static async Task<TcpClient> AcceptTcpClientAsync(this TcpListener listener, CancellationToken cancellationToken)
	{
		if (listener == null)
		{
			throw new ArgumentNullException("listener");
		}
		cancellationToken.ThrowIfCancellationRequested();
		using (cancellationToken.Register(listener.Stop))
		{
			try
			{
				return await listener.AcceptTcpClientAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
			{
				throw new OperationCanceledException(cancellationToken);
			}
			catch (SocketException) when (cancellationToken.IsCancellationRequested)
			{
				throw new OperationCanceledException(cancellationToken);
			}
		}
	}
}

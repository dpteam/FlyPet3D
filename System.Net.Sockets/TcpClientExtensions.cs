using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets;

public static class TcpClientExtensions
{
	public static async Task ConnectAsync(this TcpClient client, string host, int port, CancellationToken cancellationToken)
	{
		if (client == null)
		{
			throw new ArgumentNullException("client");
		}
		cancellationToken.ThrowIfCancellationRequested();
		Task connectTask = client.ConnectAsync(host, port);
		Task cancelTask = Task.Delay(-1, cancellationToken);
		if (await Task.WhenAny(connectTask, cancelTask).ConfigureAwait(continueOnCapturedContext: false) == cancelTask)
		{
			client.Close();
			throw new OperationCanceledException(cancellationToken);
		}
		await connectTask.ConfigureAwait(continueOnCapturedContext: false);
	}
}

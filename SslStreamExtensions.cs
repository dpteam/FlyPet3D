using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

public static class SslStreamExtensions
{
	public static Task AuthenticateAsClientAsync(this SslStream sslStream, SslClientAuthenticationOptions options, CancellationToken cancellationToken)
	{
		if (sslStream == null)
		{
			throw new ArgumentNullException("sslStream");
		}
		if (options == null)
		{
			throw new ArgumentNullException("options");
		}
		Task task = sslStream.AuthenticateAsClientAsync(options.TargetHost, options.ClientCertificates, options.EnabledSslProtocols, options.CertificateRevocationCheckMode != X509RevocationMode.NoCheck);
		return WithCancellation(task, cancellationToken);
	}

	public static Task AuthenticateAsServerAsync(this SslStream sslStream, SslServerAuthenticationOptions options, CancellationToken cancellationToken)
	{
		if (sslStream == null)
		{
			throw new ArgumentNullException("sslStream");
		}
		if (options == null)
		{
			throw new ArgumentNullException("options");
		}
		Task task = sslStream.AuthenticateAsServerAsync(options.ServerCertificate, options.ClientCertificateRequired, options.EnabledSslProtocols, options.CertificateRevocationCheckMode != X509RevocationMode.NoCheck);
		return WithCancellation(task, cancellationToken);
	}

	private static async Task WithCancellation(Task task, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		Task cancelTask = Task.Delay(-1, cancellationToken);
		if (await Task.WhenAny(task, cancelTask).ConfigureAwait(continueOnCapturedContext: false) == cancelTask)
		{
			throw new OperationCanceledException(cancellationToken);
		}
		await task.ConfigureAwait(continueOnCapturedContext: false);
	}
}

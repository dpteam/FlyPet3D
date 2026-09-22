using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security;

public class SslServerAuthenticationOptions
{
	public X509Certificate ServerCertificate { get; set; }

	public bool ClientCertificateRequired { get; set; }

	public SslProtocols EnabledSslProtocols { get; set; }

	public X509RevocationMode CertificateRevocationCheckMode { get; set; }
}

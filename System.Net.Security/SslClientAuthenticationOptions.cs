using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security;

public class SslClientAuthenticationOptions
{
	public string TargetHost { get; set; }

	public X509CertificateCollection ClientCertificates { get; set; }

	public SslProtocols EnabledSslProtocols { get; set; }

	public X509RevocationMode CertificateRevocationCheckMode { get; set; }
}

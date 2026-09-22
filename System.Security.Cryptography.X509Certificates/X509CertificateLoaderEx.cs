namespace System.Security.Cryptography.X509Certificates;

public static class X509CertificateLoaderEx
{
	public static X509Certificate2 LoadPkcs12(byte[] rawData, string password)
	{
		return new X509Certificate2(rawData, password);
	}
}

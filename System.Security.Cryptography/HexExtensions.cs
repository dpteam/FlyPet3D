namespace System.Security.Cryptography;

public static class HexExtensions
{
	public static string ToHexString(this byte[] bytes)
	{
		return ConvertEx.ToHexString(bytes);
	}
}

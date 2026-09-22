namespace System.Security.Cryptography;

public static class ConvertEx
{
	public static string ToHexString(byte[] bytes)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes");
		}
		char[] chars = new char[bytes.Length * 2];
		int i = 0;
		foreach (byte b in bytes)
		{
			chars[i++] = ToHexChar(b >> 4);
			chars[i++] = ToHexChar(b & 0xF);
		}
		return new string(chars);
	}

	private static char ToHexChar(int value)
	{
		return (char)((value < 10) ? (48 + value) : (65 + (value - 10)));
	}
}

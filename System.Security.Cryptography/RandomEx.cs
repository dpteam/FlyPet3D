namespace System.Security.Cryptography;

public static class RandomEx
{
	public static byte[] GetBytes(int length)
	{
		byte[] bytes = new byte[length];
		using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
		{
			rng.GetBytes(bytes);
		}
		return bytes;
	}
}

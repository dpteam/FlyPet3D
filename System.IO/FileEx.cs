namespace System.IO;

public static class FileEx
{
	public static void Move(string sourceFileName, string destFileName, bool overwrite)
	{
		if (!overwrite)
		{
			File.Move(sourceFileName, destFileName);
			return;
		}
		if (File.Exists(destFileName))
		{
			try
			{
				File.Replace(sourceFileName, destFileName, null);
				return;
			}
			catch (IOException)
			{
			}
			catch (PlatformNotSupportedException)
			{
			}
			File.Delete(destFileName);
		}
		File.Move(sourceFileName, destFileName);
	}
}

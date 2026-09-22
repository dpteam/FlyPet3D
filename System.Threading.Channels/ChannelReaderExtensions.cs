namespace System.Threading.Channels;

public static class ChannelReaderExtensions
{
	public static AsyncEnumerable<T> ReadAllAsync<T>(this ChannelReader<T> reader, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		return new AsyncEnumerable<T>(reader, cancellationToken);
	}
}

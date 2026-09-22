using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

public static class StreamReadExtensions
{
	public static Task ReadExactlyAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		return stream.ReadExactlyAsync(buffer, 0, buffer.Length, cancellationToken);
	}

	public static async Task ReadExactlyAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (buffer.Length - offset < count)
		{
			throw new ArgumentException("Некорректный offset/count.");
		}
		while (count > 0)
		{
			int read = await stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (read == 0)
			{
				throw new EndOfStreamException();
			}
			offset += read;
			count -= read;
		}
	}
}

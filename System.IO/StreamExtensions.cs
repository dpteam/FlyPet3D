using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

public static class StreamExtensions
{
	public static Task WriteAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		return stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
	}
}

using System.Threading.Tasks;

namespace System.Threading.Channels;

public abstract class ChannelWriter<T>
{
	public abstract bool TryWrite(T item);

	public abstract Task WriteAsync(T item, CancellationToken cancellationToken = default(CancellationToken));

	public abstract bool TryComplete(Exception error = null);

	public abstract void Complete(Exception error = null);
}

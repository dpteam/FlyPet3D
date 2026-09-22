using System.Threading.Tasks;

namespace System.Threading.Channels;

public abstract class ChannelReader<T>
{
	public abstract bool TryRead(out T item);

	public abstract Task<T> ReadAsync(CancellationToken cancellationToken = default(CancellationToken));

	public abstract Task<bool> WaitToReadAsync(CancellationToken cancellationToken = default(CancellationToken));

	public virtual ValueTask<T> ReadAsyncValue(CancellationToken cancellationToken = default(CancellationToken))
	{
		return new ValueTask<T>(ReadAsync(cancellationToken));
	}
}

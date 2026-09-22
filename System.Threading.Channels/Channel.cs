namespace System.Threading.Channels;

public static class Channel
{
	public static Channel<T> CreateUnbounded<T>()
	{
		return Channel<T>.CreateUnbounded();
	}

	public static Channel<T> CreateUnbounded<T>(UnboundedChannelOptions options)
	{
		return Channel<T>.CreateUnbounded(options);
	}

	public static Channel<T> CreateBounded<T>(int capacity)
	{
		return Channel<T>.CreateBounded(capacity);
	}

	public static Channel<T> CreateBounded<T>(BoundedChannelOptions options)
	{
		return Channel<T>.CreateBounded(options);
	}
}
public abstract class Channel<T>
{
	public abstract ChannelReader<T> Reader { get; }

	public abstract ChannelWriter<T> Writer { get; }

	public static Channel<T> CreateUnbounded()
	{
		return CreateUnbounded(new UnboundedChannelOptions());
	}

	public static Channel<T> CreateUnbounded(UnboundedChannelOptions options)
	{
		return new UnboundedChannel<T>(options ?? new UnboundedChannelOptions());
	}

	public static Channel<T> CreateBounded(int capacity)
	{
		return CreateBounded(new BoundedChannelOptions(capacity));
	}

	public static Channel<T> CreateBounded(BoundedChannelOptions options)
	{
		if (options == null)
		{
			throw new ArgumentNullException("options");
		}
		return new BoundedChannel<T>(options);
	}
}

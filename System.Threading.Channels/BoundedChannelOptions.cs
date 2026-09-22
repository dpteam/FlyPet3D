namespace System.Threading.Channels;

public sealed class BoundedChannelOptions : ChannelOptions
{
	public int Capacity { get; }

	public BoundedChannelFullMode FullMode { get; set; }

	public BoundedChannelOptions(int capacity)
	{
		if (capacity <= 0)
		{
			throw new ArgumentOutOfRangeException("capacity");
		}
		Capacity = capacity;
		FullMode = BoundedChannelFullMode.Wait;
	}
}

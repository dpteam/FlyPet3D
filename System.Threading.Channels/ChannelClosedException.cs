namespace System.Threading.Channels;

public class ChannelClosedException : InvalidOperationException
{
	public ChannelClosedException()
		: base("Канал закрыт.")
	{
	}

	public ChannelClosedException(string message)
		: base(message)
	{
	}

	public ChannelClosedException(string message, Exception inner)
		: base(message, inner)
	{
	}
}

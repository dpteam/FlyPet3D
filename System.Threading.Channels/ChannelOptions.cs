namespace System.Threading.Channels;

public class ChannelOptions
{
	public bool SingleReader { get; set; }

	public bool SingleWriter { get; set; }

	public bool AllowSynchronousContinuations { get; set; }
}

using System.Collections.Generic;

namespace System.Threading.Channels;

public readonly struct AsyncEnumerable<T>(ChannelReader<T> reader, CancellationToken token) : IAsyncEnumerable<T>
{
	private readonly ChannelReader<T> _reader = reader;

	private readonly CancellationToken _token = token;

	public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default(CancellationToken))
	{
		CancellationToken t = (cancellationToken.CanBeCanceled ? cancellationToken : _token);
		return new AsyncEnumerator<T>(_reader, t);
	}
}

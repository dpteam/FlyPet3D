using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Threading.Channels;

internal sealed class AsyncEnumerator<T> : IAsyncEnumerator<T>, IAsyncDisposable
{
	private readonly ChannelReader<T> _reader;

	private readonly CancellationToken _token;

	private T _current;

	private bool _done;

	public T Current => _current;

	public AsyncEnumerator(ChannelReader<T> reader, CancellationToken token)
	{
		_reader = reader;
		_token = token;
	}

	public System.Threading.Tasks.ValueTask<bool> MoveNextAsync()
	{
		if (_done)
		{
			return new System.Threading.Tasks.ValueTask<bool>(result: false);
		}
		return new System.Threading.Tasks.ValueTask<bool>(MoveNextCore());
	}

	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}

	private async Task<bool> MoveNextCore()
	{
		while (await _reader.WaitToReadAsync(_token).ConfigureAwait(continueOnCapturedContext: false))
		{
			if (_reader.TryRead(out var item))
			{
				_current = item;
				return true;
			}
			item = default(T);
		}
		_done = true;
		return false;
	}
}

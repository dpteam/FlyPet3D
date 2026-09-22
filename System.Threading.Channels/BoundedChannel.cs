using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Threading.Channels;

internal sealed class BoundedChannel<T> : Channel<T>
{
	private sealed class BoundedWriter : ChannelWriter<T>
	{
		private readonly BoundedChannel<T> _parent;

		public BoundedWriter(BoundedChannel<T> parent)
		{
			_parent = parent;
		}

		public override bool TryWrite(T item)
		{
			return _parent.TryWriteInternal(item);
		}

		public override Task WriteAsync(T item, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _parent.WriteAsyncInternal(item, cancellationToken);
		}

		public override bool TryComplete(Exception error = null)
		{
			return _parent.TryCompleteInternal(error);
		}

		public override void Complete(Exception error = null)
		{
			_parent.CompleteInternal(error);
		}
	}

	private sealed class BoundedReader : ChannelReader<T>
	{
		private readonly BoundedChannel<T> _parent;

		public BoundedReader(BoundedChannel<T> parent)
		{
			_parent = parent;
		}

		public override bool TryRead(out T item)
		{
			return _parent.TryReadInternal(out item);
		}

		public override Task<T> ReadAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return _parent.ReadAsyncInternal(cancellationToken);
		}

		public override Task<bool> WaitToReadAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return _parent.WaitToReadAsyncInternal(cancellationToken);
		}
	}

	private readonly object _gate = new object();

	private readonly Queue<T> _items = new Queue<T>();

	private readonly SemaphoreSlim _available = new SemaphoreSlim(0);

	private readonly SemaphoreSlim _space;

	private readonly BoundedChannelOptions _options;

	private Exception _completionError;

	private bool _completed;

	public override ChannelReader<T> Reader { get; }

	public override ChannelWriter<T> Writer { get; }

	public BoundedChannel(BoundedChannelOptions options)
	{
		_options = options;
		_space = new SemaphoreSlim(options.Capacity, options.Capacity);
		Reader = new BoundedReader(this);
		Writer = new BoundedWriter(this);
	}

	private bool TryWriteInternal(T item)
	{
		if (!_space.Wait(0))
		{
			switch (_options.FullMode)
			{
			case BoundedChannelFullMode.DropNewest:
				return true;
			case BoundedChannelFullMode.DropWrite:
				return true;
			case BoundedChannelFullMode.DropOldest:
				break;
			default:
				return false;
			}
			lock (_gate)
			{
				if (_items.Count > 0)
				{
					_items.Dequeue();
				}
			}
		}
		lock (_gate)
		{
			if (_completed)
			{
				return false;
			}
			_items.Enqueue(item);
		}
		_available.Release();
		return true;
	}

	private async Task WriteAsyncInternal(T item, CancellationToken token)
	{
		if (TryWriteInternal(item))
		{
			return;
		}
		await _space.WaitAsync(token).ConfigureAwait(continueOnCapturedContext: false);
		lock (_gate)
		{
			if (_completed)
			{
				throw new ChannelClosedException();
			}
			_items.Enqueue(item);
		}
		_available.Release();
	}

	private bool TryReadInternal(out T item)
	{
		lock (_gate)
		{
			if (_items.Count == 0)
			{
				item = default(T);
				return false;
			}
			item = _items.Dequeue();
		}
		_space.Release();
		return true;
	}

	private async Task<T> ReadAsyncInternal(CancellationToken token)
	{
		T item;
		while (!TryReadInternal(out item))
		{
			lock (_gate)
			{
				if (_completed)
				{
					if (_completionError != null)
					{
						throw _completionError;
					}
					throw new ChannelClosedException();
				}
			}
			await _available.WaitAsync(token).ConfigureAwait(continueOnCapturedContext: false);
			item = default(T);
		}
		return item;
	}

	private async Task<bool> WaitToReadAsyncInternal(CancellationToken token)
	{
		lock (_gate)
		{
			if (_items.Count > 0)
			{
				return true;
			}
			if (_completed)
			{
				if (_completionError != null)
				{
					throw _completionError;
				}
				return false;
			}
		}
		await _available.WaitAsync(token).ConfigureAwait(continueOnCapturedContext: false);
		return true;
	}

	private bool TryCompleteInternal(Exception error)
	{
		lock (_gate)
		{
			if (_completed)
			{
				return false;
			}
			_completed = true;
			_completionError = error;
		}
		_available.Release();
		return true;
	}

	private void CompleteInternal(Exception error)
	{
		TryCompleteInternal(error);
	}
}

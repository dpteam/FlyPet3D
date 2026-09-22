using System.Runtime.CompilerServices;

namespace System.Threading.Tasks;

public struct ValueTask(Task task)
{
	private static readonly Task _completed = Task.FromResult(0);

	private readonly Task _task = task ?? _completed;

	public static ValueTask CompletedTask => new ValueTask(_completed);

	public Task AsTask()
	{
		return _task ?? _completed;
	}

	public TaskAwaiter GetAwaiter()
	{
		return AsTask().GetAwaiter();
	}
}
public struct ValueTask<T>
{
	private readonly Task<T> _task;

	private readonly T _result;

	public ValueTask(Task<T> task)
	{
		_task = task;
		_result = default(T);
	}

	public ValueTask(T result)
	{
		_task = null;
		_result = result;
	}

	public Task<T> AsTask()
	{
		return _task ?? Task.FromResult(_result);
	}

	public TaskAwaiter<T> GetAwaiter()
	{
		return AsTask().GetAwaiter();
	}
}

using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Threading.Channels;

public struct ValueTask<T>(Task<T> task)
{
	private readonly Task<T> _task = task;

	public Task<T> AsTask()
	{
		return _task;
	}

	public TaskAwaiter<T> GetAwaiter()
	{
		return _task.GetAwaiter();
	}
}

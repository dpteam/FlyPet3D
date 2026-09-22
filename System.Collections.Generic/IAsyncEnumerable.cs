using System.Threading;

namespace System.Collections.Generic;

public interface IAsyncEnumerable<T>
{
	IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default(CancellationToken));
}

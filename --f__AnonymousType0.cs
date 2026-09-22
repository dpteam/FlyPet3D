using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[CompilerGenerated]
[DebuggerDisplay("\\{ ok = {ok}, status = {status}, snapshot = {snapshot} }", Type = "<Anonymous Type>")]
internal sealed class _003C_003Ef__AnonymousType0<_003Cok_003Ej__TPar, _003Cstatus_003Ej__TPar, _003Csnapshot_003Ej__TPar>
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly _003Cok_003Ej__TPar _003Cok_003Ei__Field;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly _003Cstatus_003Ej__TPar _003Cstatus_003Ei__Field;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly _003Csnapshot_003Ej__TPar _003Csnapshot_003Ei__Field;

	public _003Cok_003Ej__TPar ok
	{
		get
		{
			return _003Cok_003Ei__Field;
		}
	}

	public _003Cstatus_003Ej__TPar status
	{
		get
		{
			return _003Cstatus_003Ei__Field;
		}
	}

	public _003Csnapshot_003Ej__TPar snapshot
	{
		get
		{
			return _003Csnapshot_003Ei__Field;
		}
	}

	[DebuggerHidden]
	public _003C_003Ef__AnonymousType0(_003Cok_003Ej__TPar ok, _003Cstatus_003Ej__TPar status, _003Csnapshot_003Ej__TPar snapshot)
	{
		_003Cok_003Ei__Field = ok;
		_003Cstatus_003Ei__Field = status;
		_003Csnapshot_003Ei__Field = snapshot;
	}

	[DebuggerHidden]
	public override bool Equals(object value)
	{
		_003C_003Ef__AnonymousType0<_003Cok_003Ej__TPar, _003Cstatus_003Ej__TPar, _003Csnapshot_003Ej__TPar> anon = value as _003C_003Ef__AnonymousType0<_003Cok_003Ej__TPar, _003Cstatus_003Ej__TPar, _003Csnapshot_003Ej__TPar>;
		return this == anon || (anon != null && EqualityComparer<_003Cok_003Ej__TPar>.Default.Equals(_003Cok_003Ei__Field, anon._003Cok_003Ei__Field) && EqualityComparer<_003Cstatus_003Ej__TPar>.Default.Equals(_003Cstatus_003Ei__Field, anon._003Cstatus_003Ei__Field) && EqualityComparer<_003Csnapshot_003Ej__TPar>.Default.Equals(_003Csnapshot_003Ei__Field, anon._003Csnapshot_003Ei__Field));
	}

	[DebuggerHidden]
	public override int GetHashCode()
	{
		return ((unchecked(-882901888 * -1521134295) + EqualityComparer<_003Cok_003Ej__TPar>.Default.GetHashCode(_003Cok_003Ei__Field)) * -1521134295 + EqualityComparer<_003Cstatus_003Ej__TPar>.Default.GetHashCode(_003Cstatus_003Ei__Field)) * -1521134295 + EqualityComparer<_003Csnapshot_003Ej__TPar>.Default.GetHashCode(_003Csnapshot_003Ei__Field);
	}

	[DebuggerHidden]
	public override string ToString()
	{
		object[] array = new object[3];
		_003Cok_003Ej__TPar val = _003Cok_003Ei__Field;
		array[0] = ((val != null) ? val.ToString() : null);
		_003Cstatus_003Ej__TPar val2 = _003Cstatus_003Ei__Field;
		array[1] = ((val2 != null) ? val2.ToString() : null);
		_003Csnapshot_003Ej__TPar val3 = _003Csnapshot_003Ei__Field;
		array[2] = ((val3 != null) ? val3.ToString() : null);
		return string.Format(null, "{{ ok = {0}, status = {1}, snapshot = {2} }}", array);
	}
}

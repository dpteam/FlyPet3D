namespace System;

internal class ZMath
{
	private static void ThrowMinMax()
	{
		throw new ArgumentOutOfRangeException("min", "min must be less than or equal to max.");
	}

	public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
	{
		if (max.CompareTo(min) < 0)
		{
			ThrowMinMax();
		}
		if (value.CompareTo(min) < 0)
		{
			return min;
		}
		T other = max;
		if (value.CompareTo(other) > 0)
		{
			return max;
		}
		return value;
	}

	public static float Clamp(float value, float min, float max)
	{
		if (min > max)
		{
			ThrowMinMax();
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static double Clamp(double value, double min, double max)
	{
		if (min > max)
		{
			ThrowMinMax();
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static decimal Clamp(decimal value, decimal min, decimal max)
	{
		if (min > max)
		{
			ThrowMinMax();
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static byte Clamp(byte value, byte min, byte max)
	{
		if (min > max)
		{
			ThrowMinMax();
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static sbyte Clamp(sbyte value, sbyte min, sbyte max)
	{
		if (min > max)
		{
			ThrowMinMax();
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static short Clamp(short value, short min, short max)
	{
		if (min > max)
		{
			ThrowMinMax();
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static ushort Clamp(ushort value, ushort min, ushort max)
	{
		if (min > max)
		{
			ThrowMinMax();
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static int Clamp(int value, int min, int max)
	{
		if (min > max)
		{
			ThrowMinMax();
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static uint Clamp(uint value, uint min, uint max)
	{
		if (min > max)
		{
			ThrowMinMax();
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static long Clamp(long value, long min, long max)
	{
		if (min > max)
		{
			ThrowMinMax();
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static ulong Clamp(ulong value, ulong min, ulong max)
	{
		if (min > max)
		{
			ThrowMinMax();
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}
}

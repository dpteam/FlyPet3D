namespace System;

public static class ArrayEx
{
	public static void Fill<T>(T[] array, T value)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = value;
		}
	}

	public static void Clear<T>(T[] array)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		Array.Clear(array, 0, array.Length);
	}

	public static void Clear(Array array)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		Array.Clear(array, 0, array.Length);
	}
}

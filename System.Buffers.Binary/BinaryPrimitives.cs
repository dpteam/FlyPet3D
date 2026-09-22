namespace System.Buffers.Binary;

public static class BinaryPrimitives
{
	public static short ReadInt16BigEndian(byte[] buffer, int offset = 0)
	{
		return (short)((buffer[offset] << 8) | buffer[offset + 1]);
	}

	public static short ReadInt16LittleEndian(byte[] buffer, int offset = 0)
	{
		return (short)(buffer[offset] | (buffer[offset + 1] << 8));
	}

	public static ushort ReadUInt16BigEndian(byte[] buffer, int offset = 0)
	{
		return (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
	}

	public static ushort ReadUInt16LittleEndian(byte[] buffer, int offset = 0)
	{
		return (ushort)(buffer[offset] | (buffer[offset + 1] << 8));
	}

	public static int ReadInt32BigEndian(byte[] buffer, int offset = 0)
	{
		return (buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3];
	}

	public static int ReadInt32LittleEndian(byte[] buffer, int offset = 0)
	{
		return buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24);
	}

	public static uint ReadUInt32BigEndian(byte[] buffer, int offset = 0)
	{
		return (uint)((buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3]);
	}

	public static uint ReadUInt32LittleEndian(byte[] buffer, int offset = 0)
	{
		return (uint)(buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24));
	}

	public static long ReadInt64BigEndian(byte[] buffer, int offset = 0)
	{
		long hi = ReadInt32BigEndian(buffer, offset);
		long lo = ReadUInt32BigEndian(buffer, offset + 4);
		return (hi << 32) | lo;
	}

	public static long ReadInt64LittleEndian(byte[] buffer, int offset = 0)
	{
		long lo = ReadUInt32LittleEndian(buffer, offset);
		long hi = ReadUInt32LittleEndian(buffer, offset + 4);
		return (hi << 32) | lo;
	}

	public static void WriteInt16BigEndian(byte[] buffer, short value, int offset = 0)
	{
		buffer[offset] = (byte)(value >> 8);
		buffer[offset + 1] = (byte)value;
	}

	public static void WriteInt16LittleEndian(byte[] buffer, short value, int offset = 0)
	{
		buffer[offset] = (byte)value;
		buffer[offset + 1] = (byte)(value >> 8);
	}

	public static void WriteInt32BigEndian(byte[] buffer, int value, int offset = 0)
	{
		buffer[offset] = (byte)(value >> 24);
		buffer[offset + 1] = (byte)(value >> 16);
		buffer[offset + 2] = (byte)(value >> 8);
		buffer[offset + 3] = (byte)value;
	}

	public static void WriteInt32LittleEndian(byte[] buffer, int value, int offset = 0)
	{
		buffer[offset] = (byte)value;
		buffer[offset + 1] = (byte)(value >> 8);
		buffer[offset + 2] = (byte)(value >> 16);
		buffer[offset + 3] = (byte)(value >> 24);
	}

	public static void WriteInt64BigEndian(byte[] buffer, long value, int offset = 0)
	{
		WriteInt32BigEndian(buffer, (int)(value >> 32), offset);
		WriteInt32BigEndian(buffer, (int)value, offset + 4);
	}

	public static void WriteInt64LittleEndian(byte[] buffer, long value, int offset = 0)
	{
		WriteInt32LittleEndian(buffer, (int)value, offset);
		WriteInt32LittleEndian(buffer, (int)(value >> 32), offset + 4);
	}
}

using System;
using System.Drawing;
using System.Numerics;

namespace FlyPet;

public sealed class TableCamera
{
	public float Yaw = 0.42f;

	public float Elevation = 0.72f;

	public float Distance = 1150f;

	public Vector3 Target = new Vector3(0f, -85f, 0f);

	public Vector3 Position => Target + new Vector3(MathF.Sin(Yaw) * MathF.Cos(Elevation), MathF.Sin(Elevation), MathF.Cos(Yaw) * MathF.Cos(Elevation)) * Distance;

	public void Reset()
	{
		Yaw = 0.42f;
		Elevation = 0.72f;
		Distance = 1150f;
		Target = new Vector3(0f, -85f, 0f);
	}

	public void Orbit(float dx, float dy)
	{
		Yaw = World.Wrap(Yaw - dx * 0.008f);
		Elevation = ZMath.Clamp(Elevation + dy * 0.006f, 0.3f, 1.35f);
	}

	public void Zoom(int delta)
	{
		Distance = ZMath.Clamp(Distance * MathF.Exp((0f - (float)delta / 120f) * 0.1f), 700f, 1600f);
	}

	public (Vector3 Right, Vector3 Up, Vector3 Forward) Basis()
	{
		Vector3 vector = Vector3.Normalize(Target - Position);
		Vector3 vector2 = Vector3.Normalize(Vector3.Cross(vector, Vector3.UnitY));
		return (Right: vector2, Up: Vector3.Cross(vector2, vector), Forward: vector);
	}

	public static float Focal(int height)
	{
		return (float)height * 1.4f;
	}

	public PointF Project(Vector3 point, int width, int height)
	{
		(Vector3, Vector3, Vector3) tuple = Basis();
		Vector3 item = tuple.Item1;
		Vector3 item2 = tuple.Item2;
		Vector3 item3 = tuple.Item3;
		Vector3 vector = point - Position;
		float num = Vector3.Dot(vector, item3);
		return new PointF((float)width * 0.5f + Vector3.Dot(vector, item) * Focal(height) / num, (float)height * 0.5f - Vector3.Dot(vector, item2) * Focal(height) / num);
	}

	public PointF? PickTable(PointF point, int width, int height)
	{
		(Vector3, Vector3, Vector3) tuple = Basis();
		Vector3 item = tuple.Item1;
		Vector3 item2 = tuple.Item2;
		Vector3 vector = tuple.Item3 + item * ((point.X - (float)width * 0.5f) / Focal(height)) - item2 * ((point.Y - (float)height * 0.5f) / Focal(height));
		if (Math.Abs(vector.Y) < 1E-05f)
		{
			return null;
		}
		float num = (0f - Position.Y) / vector.Y;
		if (num <= 0f)
		{
			return null;
		}
		Vector3 vector2 = Position + vector * num;
		float num2 = vector2.X + 400f;
		float num3 = vector2.Z + 250f;
		if (!(num2 >= -0.01f) || !(num2 <= 800.01f) || !(num3 >= -0.01f) || !(num3 <= 500.01f))
		{
			return null;
		}
		return new PointF(ZMath.Clamp(num2, 0f, 800f), ZMath.Clamp(num3, 0f, 500f));
	}
}

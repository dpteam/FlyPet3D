using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;

namespace FlyPet;

public sealed class TableScene : IDisposable
{
	private readonly record struct Vertex(float X, float Y, float Z, Vector3 Color, float Alpha);

	public readonly TableCamera Camera = new TableCamera();

	private readonly int _width;

	private readonly int _height;

	private readonly Bitmap _bitmap;

	private readonly int[] _pixels;

	private readonly float[] _depth;

	private Vector3 _eye;

	private Vector3 _right;

	private Vector3 _up;

	private Vector3 _forward;

	private static readonly Vector3 Light = Vector3.Normalize(new Vector3(-0.5f, 1f, -0.3f));

	private readonly List<(int A, int B, int C)> _sphere = new List<(int, int, int)>();

	private readonly Vector3[] _spherePoints = new Vector3[209];

	private readonly Vertex[] _sphereVertices = new Vertex[209];

	private readonly bool[] _sphereFront = new bool[209];

	public double LastFrameMs { get; private set; }

	public int Triangles { get; private set; }

	public TableScene(int width = 800, int height = 500)
	{
		_width = width;
		_height = height;
		_bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
		_pixels = new int[width * height];
		_depth = new float[width * height];
		for (int i = 0; i <= 10; i++)
		{
			for (int j = 0; j <= 18; j++)
			{
				_spherePoints[i * 19 + j] = Unit(i, j);
				static Vector3 Unit(int ring, int side)
				{
					float x = (float)ring * (float)Math.PI / 10f;
					float x2 = (float)side * ((float)Math.PI * 2f) / 18f;
					return new Vector3(MathF.Sin(x) * MathF.Cos(x2), MathF.Cos(x), MathF.Sin(x) * MathF.Sin(x2));
				}
			}
		}
		for (int k = 0; k < 10; k++)
		{
			for (int l = 0; l < 18; l++)
			{
				int num = k * 19 + l;
				int num2 = (k + 1) * 19 + l;
				int num3 = num2 + 1;
				int item = num + 1;
				if (k > 0)
				{
					_sphere.Add((num, num3, item));
				}
				if (k < 9)
				{
					_sphere.Add((num, num2, num3));
				}
			}
		}
	}

	private Vertex Project(Vector3 p, Vector3 color, float alpha = 1f)
	{
		Vector3 vector = p - _eye;
		float num = Vector3.Dot(vector, _forward);
		return new Vertex((float)_width * 0.5f + Vector3.Dot(vector, _right) * TableCamera.Focal(_height) / num, (float)_height * 0.5f - Vector3.Dot(vector, _up) * TableCamera.Focal(_height) / num, num, color, alpha);
	}

	private Vector3 Shade(Vector3 p, Vector3 normal, Color color, float shine = 0.1f)
	{
		Vector3 vector = Vector3.Normalize(normal);
		float num = Math.Max(0f, Vector3.Dot(vector, Light));
		Vector3 vector2 = Vector3.Normalize(Light + Vector3.Normalize(_eye - p));
		float value = MathF.Pow(Math.Max(0f, Vector3.Dot(vector, vector2)), 32f) * shine * 255f;
		return Vector3.Min(new Vector3(255f), new Vector3((int)color.R, (int)color.G, (int)color.B) * (0.46f + 0.54f * num) + new Vector3(value));
	}

	private static float Edge(Vertex a, Vertex b, float x, float y)
	{
		return (x - a.X) * (b.Y - a.Y) - (y - a.Y) * (b.X - a.X);
	}

	private void Triangle(Vertex a, Vertex b, Vertex c)
	{
		if (a.Z <= 4f || b.Z <= 4f || c.Z <= 4f)
		{
			return;
		}
		float num = Edge(a, b, c.X, c.Y);
		if (Math.Abs(num) < 0.0001f)
		{
			return;
		}
		int num2 = Math.Max(0, (int)MathF.Floor(Math.Min(a.X, Math.Min(b.X, c.X))));
		int num3 = Math.Min(_width - 1, (int)MathF.Ceiling(Math.Max(a.X, Math.Max(b.X, c.X))));
		int num4 = Math.Max(0, (int)MathF.Floor(Math.Min(a.Y, Math.Min(b.Y, c.Y))));
		int num5 = Math.Min(_height - 1, (int)MathF.Ceiling(Math.Max(a.Y, Math.Max(b.Y, c.Y))));
		if (num2 > num3 || num4 > num5)
		{
			return;
		}
		Triangles++;
		float num6 = 1f / num;
		float num7 = 1f / a.Z;
		float num8 = 1f / b.Z;
		float num9 = 1f / c.Z;
		float num10 = (c.Y - b.Y) * num6;
		float num11 = (a.Y - c.Y) * num6;
		for (int i = num4; i <= num5; i++)
		{
			float num12 = Edge(b, c, (float)num2 + 0.5f, (float)i + 0.5f) * num6;
			float num13 = Edge(c, a, (float)num2 + 0.5f, (float)i + 0.5f) * num6;
			int num14 = num2;
			while (num14 <= num3)
			{
				float num15 = 1f - num12 - num13;
				if (!(num12 < -1E-05f) && !(num13 < -1E-05f) && !(num15 < -1E-05f))
				{
					float num16 = num12 * num7 + num13 * num8 + num15 * num9;
					int num17 = i * _width + num14;
					if (!(num16 <= _depth[num17]))
					{
						Vector3 vector = a.Color * num12 + b.Color * num13 + c.Color * num15;
						float num18 = a.Alpha * num12 + b.Alpha * num13 + c.Alpha * num15;
						if (num18 < 0.999f)
						{
							int num19 = _pixels[num17];
							vector = vector * num18 + new Vector3((num19 >> 16) & 0xFF, (num19 >> 8) & 0xFF, num19 & 0xFF) * (1f - num18);
						}
						_pixels[num17] = -16777216 | (ZMath.Clamp((int)vector.X, 0, 255) << 16) | (ZMath.Clamp((int)vector.Y, 0, 255) << 8) | ZMath.Clamp((int)vector.Z, 0, 255);
						_depth[num17] = num16;
					}
				}
				num14++;
				num12 += num10;
				num13 += num11;
			}
		}
	}

	private void Flat(Vector3 a, Vector3 b, Vector3 c, Color color)
	{
		Vector3 vector = Vector3.Normalize(Vector3.Cross(b - a, c - a));
		if (Vector3.Dot(vector, _eye - a) < 0f)
		{
			vector = -vector;
		}
		Triangle(Project(a, Shade(a, vector, color, 0f)), Project(b, Shade(b, vector, color, 0f)), Project(c, Shade(c, vector, color, 0f)));
	}

	private void Box(Vector3 center, Vector3 size, Color color, Matrix4x4 root)
	{
		Vector3[] array = new Vector3[8];
		for (int i = 0; i < 8; i++)
		{
			array[i] = Vector3.Transform(center + size * new Vector3(((i & 1) == 0) ? (-0.5f) : 0.5f, ((i & 2) == 0) ? (-0.5f) : 0.5f, ((i & 4) == 0) ? (-0.5f) : 0.5f), root);
		}
		int[] array2 = new int[24]
		{
			0, 1, 3, 2, 4, 6, 7, 5, 0, 4,
			5, 1, 2, 3, 7, 6, 0, 2, 6, 4,
			1, 5, 7, 3
		};
		Vector3 vector = Vector3.Transform(center, root);
		for (int j = 0; j < array2.Length; j += 4)
		{
			Vector3 vector2 = (array[array2[j]] + array[array2[j + 2]]) * 0.5f;
			if (!(Vector3.Dot(vector2 - vector, _eye - vector2) <= 0f))
			{
				Flat(array[array2[j]], array[array2[j + 1]], array[array2[j + 2]], color);
				Flat(array[array2[j]], array[array2[j + 2]], array[array2[j + 3]], color);
			}
		}
	}

	private void Sphere(Vector3 center, Vector3 size, Color color, Matrix4x4 root, float yaw = 0f, float roll = 0f, float shine = 0.12f)
	{
		Matrix4x4 matrix = Matrix4x4.CreateScale(size) * Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateRotationX(roll) * Matrix4x4.CreateTranslation(center) * root;
		Matrix4x4.Invert(matrix, out var result);
		Matrix4x4 matrix2 = Matrix4x4.Transpose(result);
		for (int i = 0; i < _spherePoints.Length; i++)
		{
			Vector3 vector = Vector3.Transform(_spherePoints[i], matrix);
			Vector3 vector2 = Vector3.TransformNormal(_spherePoints[i], matrix2);
			_sphereFront[i] = Vector3.Dot(vector2, _eye - vector) > 0f;
			_sphereVertices[i] = Project(vector, Shade(vector, vector2, color, shine));
		}
		foreach (var (num, num2, num3) in _sphere)
		{
			if (_sphereFront[num] || _sphereFront[num2] || _sphereFront[num3])
			{
				Triangle(_sphereVertices[num], _sphereVertices[num2], _sphereVertices[num3]);
			}
		}
	}

	private void Rod(Vector3 a, Vector3 b, float radius, Color color, Matrix4x4 root)
	{
		a = Vector3.Transform(a, root);
		b = Vector3.Transform(b, root);
		Vector3 vector = Vector3.Normalize(b - a);
		Vector3 vector2 = vector;
		Vector3 vector3 = ((Math.Abs(vector2.Y) > 0.9f) ? Vector3.UnitX : Vector3.UnitY);
		Vector3 vector4 = Vector3.Normalize(Vector3.Cross(vector2, vector3)) * radius;
		Vector3 vector5 = Vector3.Cross(vector, vector4);
		for (int i = 0; i < 7; i++)
		{
			float x = (float)i * ((float)Math.PI * 2f) / 7f;
			float x2 = (float)(i + 1) * ((float)Math.PI * 2f) / 7f;
			Vector3 vector6 = vector4 * MathF.Cos(x) + vector5 * MathF.Sin(x);
			Vector3 vector7 = vector4 * MathF.Cos(x2) + vector5 * MathF.Sin(x2);
			Flat(a + vector6, b + vector6, b + vector7, color);
			Flat(a + vector6, b + vector7, a + vector7, color);
		}
	}

	private void Shadow(float x, float z, float rx, float rz, float alpha, float height = 0.8f)
	{
		Vector3 color = new Vector3(75f, 59f, 37f);
		Vertex a = Project(new Vector3(x, height, z), color, alpha);
		for (int i = 0; i < 32; i++)
		{
			float x2 = (float)i * ((float)Math.PI * 2f) / 32f;
			float x3 = (float)(i + 1) * ((float)Math.PI * 2f) / 32f;
			Triangle(a, Project(new Vector3(x + MathF.Cos(x2) * rx, height, z + MathF.Sin(x2) * rz), color, 0f), Project(new Vector3(x + MathF.Cos(x3) * rx, height, z + MathF.Sin(x3) * rz), color, 0f));
		}
	}

	private void Fly(FlyFrame frame, float time, bool local, bool paused)
	{
		float num = frame.X - 400f;
		float num2 = frame.Y - 250f;
		Shadow(num + 6f, num2 + 5f, 47f, 35f, 0.38f);
		Matrix4x4 root = Matrix4x4.CreateRotationY(0f - frame.Angle) * Matrix4x4.CreateTranslation(num, 0f, num2);
		Color color = Color.FromArgb(58, 60, 48);
		bool flag = frame.Sleeping | paused;
		if (!flag)
		{
			bool flag2;
			switch (frame.Activity)
			{
			case "Ест фрукт":
			case "Пьёт воду":
			case "Чистит лапки":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		float num3 = (flag ? 0f : (MathF.Sin(time * 17f) * 4f));
		for (int i = -1; i <= 1; i += 2)
		{
			for (int j = 0; j < 3; j++)
			{
				Vector3 a = new Vector3(-9 + j * 9, 12f, i * 6);
				Vector3 vector = new Vector3((float)(-17 + j * 14) + num3 * (float)((j % 2 == 0) ? 1 : (-1)), 11f, i * 23);
				Vector3 b = new Vector3((float)(-29 + j * 25) + num3, 1.3f, i * 33);
				Rod(a, vector, 1.5f, color, root);
				Rod(vector, b, 1.1f, color, root);
			}
		}
		Sphere(new Vector3(-17f, 14f, 0f), new Vector3(20f, 12f, 11f), Color.FromArgb(94, 101, 61), root);
		for (int k = 0; k < 3; k++)
		{
			Sphere(new Vector3(-25 + k * 7, 15f, 0f), new Vector3(2.5f, 11f, 10.5f), Color.FromArgb(67, 75, 49), root, 0f, 0f, 0.02f);
		}
		Sphere(new Vector3(0f, 17f, 0f), new Vector3(13f, 13f, 11f), local ? Color.FromArgb(76, 108, 85) : Color.FromArgb(175, 115, 84), root);
		Sphere(new Vector3(15f, 18f, 0f), new Vector3(11f, 10f, 10f), color, root);
		Sphere(new Vector3(19f, 20f, -7f), new Vector3(7f, 8f, 6f), Color.FromArgb(172, 63, 40), root, 0f, 0f, 0.7f);
		Sphere(new Vector3(19f, 20f, 7f), new Vector3(7f, 8f, 6f), Color.FromArgb(172, 63, 40), root, 0f, 0f, 0.7f);
		Rod(new Vector3(22f, 21f, -3f), new Vector3(32f, 27f, -8f), 0.8f, color, root);
		Rod(new Vector3(22f, 21f, 3f), new Vector3(32f, 27f, 8f), 0.8f, color, root);
		float num4 = (frame.Sleeping ? 0f : (MathF.Sin(time * 11f) * 0.05f));
		Sphere(new Vector3(-12f, 27f, -14f), new Vector3(29f, 1.25f, 10f), Color.FromArgb(223, 228, 203), root, -0.28f, num4, 0.45f);
		Sphere(new Vector3(-12f, 27.4f, 14f), new Vector3(29f, 1.25f, 10f), Color.FromArgb(227, 231, 207), root, 0.28f, 0f - num4, 0.45f);
		for (int l = -1; l <= 1; l += 2)
		{
			Rod(new Vector3(5f, 28.3f, l * 7), new Vector3(-34f, 28.3f, l * 19), 0.45f, Color.FromArgb(152, 171, 140), root);
		}
	}

	private void Item(TableItem item, bool local)
	{
		float num = item.X - 400f;
		float num2 = item.Y - 250f;
		float scale = 0.65f + item.Amount / 100f;
		Shadow(num + 5f, num2 + 3f, 28f, 22f, 0.23f);
		Matrix4x4 root = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(num, 0f, num2);
		if (item.Kind == "water")
		{
			Sphere(new Vector3(0f, 3f, 0f), new Vector3(22f, 4f, 17f), Color.FromArgb(125, 183, 191), root, 0f, 0f, 0.85f);
			Sphere(new Vector3(-6f, 6.3f, -4f), new Vector3(7f, 0.4f, 3f), Color.FromArgb(224, 244, 235), root, 0f, 0f, 0f);
		}
		else
		{
			Sphere(new Vector3(0f, 6f, 0f), new Vector3(23f, 7f, 23f), Color.FromArgb(225, 148, 51), root);
			Sphere(new Vector3(0f, 10f, 0f), new Vector3(19f, 3.3f, 19f), Color.FromArgb(248, 193, 95), root);
			for (int i = 0; i < 7; i++)
			{
				float x = (float)i * ((float)Math.PI * 2f) / 7f;
				Rod(new Vector3(0f, 13.4f, 0f), new Vector3(MathF.Cos(x) * 17f, 11.5f, MathF.Sin(x) * 17f), 0.65f, Color.FromArgb(255, 226, 161), root);
			}
			Rod(new Vector3(17f, 8f, 6f), new Vector3(24f, 17f, 10f), 1.2f, Color.FromArgb(91, 115, 59), root);
		}
		Sphere(new Vector3(0f, 1.5f, 29f), new Vector3(2.4f, 1f, 2.4f), local ? Color.FromArgb(68, 108, 82) : Color.FromArgb(183, 108, 80), root, 0f, 0f, 0f);
	}

	private void Swatter(float x, float z, float lift, float tilt)
	{
		Shadow(x + lift * 0.3f, z + 10f, 42f + lift * 0.15f, 44f, 0.26f);
		Matrix4x4 root = Matrix4x4.CreateRotationX(tilt) * Matrix4x4.CreateRotationY(-0.25f) * Matrix4x4.CreateTranslation(x, lift, z);
		Color color = Color.FromArgb(100, 136, 105);
		Box(new Vector3(-31f, 0f, 0f), new Vector3(4f, 4f, 51f), color, root);
		Box(new Vector3(31f, 0f, 0f), new Vector3(4f, 4f, 51f), color, root);
		Box(new Vector3(0f, 0f, -25f), new Vector3(64f, 4f, 4f), color, root);
		Box(new Vector3(0f, 0f, 25f), new Vector3(64f, 4f, 4f), color, root);
		for (int i = -24; i <= 24; i += 8)
		{
			Rod(new Vector3(i, 0f, -24f), new Vector3(i, 0f, 24f), 0.8f, color, root);
		}
		for (int j = -18; j <= 18; j += 6)
		{
			Rod(new Vector3(-29f, 0f, j), new Vector3(29f, 0f, j), 0.8f, color, root);
		}
		Rod(new Vector3(0f, 0f, 25f), new Vector3(0f, 0f, 120f), 2.5f, color, root);
		Box(new Vector3(0f, 0f, 112f), new Vector3(9f, 7f, 37f), Color.FromArgb(61, 89, 68), root);
	}

	private void Brush(float x, float z, float time)
	{
		Matrix4x4 root = Matrix4x4.CreateRotationY(-0.45f) * Matrix4x4.CreateTranslation(x, 3f, z);
		Shadow(x, z, 25f, 23f, 0.18f);
		Box(new Vector3(0f, 5f, 0f), new Vector3(28f, 13f, 17f), Color.FromArgb(204, 179, 122), root);
		for (int i = -12; i <= 12; i += 3)
		{
			Rod(new Vector3(i, 9f, -6f), new Vector3(i, 0f, -11f + MathF.Sin(time * 9f)), 0.6f, Color.FromArgb(159, 138, 91), root);
		}
		Rod(new Vector3(0f, 11f, 4f), new Vector3(0f, 54f, 45f), 4f, Color.FromArgb(129, 91, 58), root);
	}

	public Bitmap Render(World world, FlyFrame? remote, float time, IReadOnlyList<PointF> dust, IReadOnlyList<SceneEffect> effects, TableTool tool, PointF? cursor, float sinceSwat)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		Triangles = 0;
		_eye = Camera.Position;
		(_right, _up, _forward) = Camera.Basis();
		ArrayEx.Fill(_pixels, Color.FromArgb(226, 226, 214).ToArgb());
		ArrayEx.Clear(_depth);
		Matrix4x4 identity = Matrix4x4.Identity;
		Box(new Vector3(0f, -234f, 0f), new Vector3(2100f, 10f, 1800f), Color.FromArgb(245, 244, 232), identity);
		Shadow(20f, 15f, 550f, 360f, 0.27f, -228.8f);
		float[] array = new float[2] { -347f, 347f };
		float[] array2 = array;
		foreach (float x in array2)
		{
			float[] array3 = new float[2] { -201f, 201f };
			float[] array4 = array3;
			foreach (float z in array4)
			{
				Box(new Vector3(x, -122f, z), new Vector3(28f, 214f, 28f), Color.FromArgb(149, 119, 80), identity);
			}
		}
		Box(new Vector3(0f, -16.6f, 0f), new Vector3(808f, 30f, 508f), Color.FromArgb(174, 136, 88), identity);
		for (int k = 0; k < 5; k++)
		{
			Box(new Vector3(0f, -0.8f, -200 + k * 100), new Vector3(800f, 1.6f, 99f), Color.FromArgb(216 + k % 2 * 4, 188 + k % 2 * 3, 143 + k % 2 * 3), identity);
		}
		for (int l = 0; l < 25; l++)
		{
			float num = -244 + l * 20;
			Color color = Color.FromArgb(210, 182, 137);
			Vector3 a = new Vector3(-395f, 0.45f, num);
			Vector3 b = new Vector3(395f, 0.45f, num);
			Vector3 vector = new Vector3(395f, 0.45f, num + 2.6f);
			Vector3 c = new Vector3(-395f, 0.45f, num + 2.6f);
			Flat(a, b, vector, color);
			Flat(a, vector, c, color);
		}
		foreach (PointF item2 in dust)
		{
			Box(new Vector3(item2.X - 400f, 0.4f, item2.Y - 250f), new Vector3(2.6f, 0.8f, 2f), Color.FromArgb(165, 130, 83), identity);
		}
		foreach (TableItem item3 in world.Items)
		{
			Item(item3, local: true);
		}
		if (remote != null)
		{
			TableItem[] items = remote.Items;
			TableItem[] array5 = items;
			foreach (TableItem item4 in array5)
			{
				Item(item4, local: false);
			}
		}
		if (remote != null)
		{
			Fly(remote, time, local: false, remote.Activity == "На паузе");
		}
		Fly(world.Frame(), time, local: true, world.Paused);
		foreach (SceneEffect effect in effects)
		{
			float num2 = effect.Action.X - 400f;
			float num3 = effect.Action.Y - 250f;
			if (effect.Action.Kind == "swat")
			{
				float num4 = 15f + effect.Age * 160f;
				for (int n = 0; n < 28; n++)
				{
					float x2 = (float)n * ((float)Math.PI * 2f) / 28f;
					float x3 = (float)(n + 1) * ((float)Math.PI * 2f) / 28f;
					Rod(new Vector3(num2 + MathF.Cos(x2) * num4, 0.4f, num3 + MathF.Sin(x2) * num4), new Vector3(num2 + MathF.Cos(x3) * num4, 0.4f, num3 + MathF.Sin(x3) * num4), 0.65f, Color.FromArgb(180, 155, 115), identity);
				}
				if (effect.Age < 0.45f)
				{
					Swatter(num2, num3, 3f + MathF.Pow(Math.Max(0f, effect.Age - 0.09f) * 5f, 2f) * 45f, -0.05f - effect.Age * 0.2f);
				}
			}
			else if (effect.Remote)
			{
				Brush(num2, num3, time);
			}
		}
		if (tool != TableTool.Observe && cursor.HasValue)
		{
			PointF valueOrDefault = cursor.GetValueOrDefault();
			if (World.InBounds(valueOrDefault.X, valueOrDefault.Y))
			{
				float x4 = valueOrDefault.X - 400f;
				float z2 = valueOrDefault.Y - 250f;
				if (tool == TableTool.Swatter && sinceSwat > 0.45f)
				{
					Swatter(x4, z2, 62f, -0.18f);
				}
				else
				{
					switch (tool)
					{
					case TableTool.Brush:
						Brush(x4, z2, time);
						break;
					case TableTool.Fruit:
					case TableTool.Water:
						Item(new TableItem((tool == TableTool.Fruit) ? "fruit" : "water", valueOrDefault.X, valueOrDefault.Y, 25f), local: true);
						break;
					}
				}
				Shadow(x4, z2, 6f, 6f, 0.65f, 0.25f);
			}
		}
		if (world.Sleeping)
		{
			for (int num5 = 0; num5 < _pixels.Length; num5++)
			{
				int num6 = _pixels[num5];
				_pixels[num5] = -16777216 | (((num6 >> 16) & 0xFF) * 3 / 5 << 16) | (((num6 >> 8) & 0xFF) * 2 / 3 << 8) | Math.Min(255, (num6 & 0xFF) * 3 / 4 + 15);
			}
		}
		BitmapData bitmapData = _bitmap.LockBits(new Rectangle(0, 0, _width, _height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
		try
		{
			Marshal.Copy(_pixels, 0, bitmapData.Scan0, _pixels.Length);
		}
		finally
		{
			_bitmap.UnlockBits(bitmapData);
		}
		LastFrameMs = stopwatch.Elapsed.TotalMilliseconds;
		return _bitmap;
	}

	public void Dispose()
	{
		_bitmap.Dispose();
	}
}

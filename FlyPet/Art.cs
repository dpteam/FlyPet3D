using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace FlyPet;

internal static class Art
{
	public static readonly Color Ink = Color.FromArgb(55, 58, 51);

	public static readonly Color Muted = Color.FromArgb(132, 134, 122);

	public static readonly Color Paper = Color.FromArgb(245, 244, 238);

	public static readonly Color Green = Color.FromArgb(78, 113, 92);

	public static readonly Color Coral = Color.FromArgb(191, 124, 98);

	public static GraphicsPath Round(RectangleF r, float radius = 16f)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		float num = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
		graphicsPath.AddArc(r.X, r.Y, num, num, 180f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Y, num, num, 270f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Bottom - num, num, num, 0f, 90f);
		graphicsPath.AddArc(r.X, r.Bottom - num, num, num, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}

	public static void Box(Graphics g, Color c, RectangleF r, float radius = 16f)
	{
		using GraphicsPath path = Round(r, radius);
		using SolidBrush brush = new SolidBrush(c);
		g.FillPath(brush, path);
	}

	public static void Oval(Graphics g, Color c, float x, float y, float w, float h)
	{
		using SolidBrush brush = new SolidBrush(c);
		g.FillEllipse(brush, x, y, w, h);
	}

	public static void Line(Graphics g, Color c, float width, params PointF[] points)
	{
		using Pen pen = new Pen(c, width)
		{
			StartCap = LineCap.Round,
			EndCap = LineCap.Round,
			LineJoin = LineJoin.Round
		};
		g.DrawLines(pen, points);
	}

	public static void Text(Graphics g, string text, float x, float y, float size = 11f, Color? c = null, bool bold = false)
	{
		using Font font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
		using SolidBrush brush = new SolidBrush(c ?? Ink);
		g.DrawString(text, font, brush, x, y);
	}

	public static void ClippedText(Graphics g, string text, RectangleF rect, float size, Color color, bool bold = false)
	{
		using Font font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
		using SolidBrush brush = new SolidBrush(color);
		using StringFormat format = new StringFormat
		{
			Trimming = StringTrimming.EllipsisCharacter,
			FormatFlags = StringFormatFlags.NoWrap
		};
		g.DrawString(text, font, brush, rect, format);
	}

	public static void Icon(Graphics g, TableTool tool, float x, float y, float scale = 1f, bool selected = false)
	{
		GraphicsState gstate = g.Save();
		g.TranslateTransform(x, y);
		g.ScaleTransform(scale, scale);
		Color color = (selected ? Color.FromArgb(239, 241, 224) : Green);
		switch (tool)
		{
		case TableTool.Observe:
		{
			using (Pen pen = new Pen(color, 2f))
			{
				g.DrawEllipse(pen, -12, -7, 24, 14);
			}
			Oval(g, color, -3f, -3f, 6f, 6f);
			break;
		}
		case TableTool.Fruit:
		{
			Oval(g, Color.FromArgb(220, 156, 82), -12f, -11f, 24f, 23f);
			Oval(g, Color.FromArgb(245, 205, 137), -8f, -8f, 16f, 17f);
			PointF[] array = new PointF[3]
			{
				new PointF(0f, -7f),
				new PointF(6f, 4f),
				new PointF(-6f, 4f)
			};
			PointF[] array2 = array;
			foreach (PointF pointF in array2)
			{
				Line(g, Color.FromArgb(250, 230, 186), 1.5f, PointF.Empty, pointF);
			}
			Line(g, Green, 2f, new PointF(1f, -10f), new PointF(5f, -15f));
			break;
		}
		case TableTool.Water:
		{
			using (GraphicsPath graphicsPath = new GraphicsPath())
			{
				graphicsPath.AddBezier(0, -15, -4, -7, -13, 1, -10, 7);
				graphicsPath.AddBezier(-10, 7, -6, 17, 12, 14, 11, 3);
				graphicsPath.AddBezier(11, 3, 10, -2, 3, -10, 0, -15);
				using SolidBrush brush2 = new SolidBrush(Color.FromArgb(116, 157, 169));
				g.FillPath(brush2, graphicsPath);
			}
			Line(g, Color.FromArgb(218, 236, 232), 2f, new PointF(-5f, 2f), new PointF(-5f, 6f));
			break;
		}
		case TableTool.Brush:
		{
			Line(g, Color.FromArgb(157, 125, 88), 7f, new PointF(8f, -14f), new PointF(-1f, 1f));
			using (SolidBrush brush = new SolidBrush(Color.FromArgb(195, 178, 140)))
			{
				g.FillPolygon(brush, new PointF[4]
				{
					new PointF(-7f, -3f),
					new PointF(5f, 4f),
					new PointF(0f, 16f),
					new PointF(-17f, 6f)
				});
			}
			for (int i = 0; i < 4; i++)
			{
				Line(g, Color.FromArgb(154, 139, 108), 1f, new PointF(-6 + i * 3, 3 + i), new PointF(-11 + i * 3, 10 + i));
			}
			break;
		}
		case TableTool.Swatter:
			Swatter(g, PointF.Empty, 0.45f, -25f);
			break;
		}
		g.Restore(gstate);
	}

	public static void Swatter(Graphics g, PointF point, float scale, float angle, bool shadow = false)
	{
		GraphicsState gstate = g.Save();
		g.TranslateTransform(point.X, point.Y);
		g.RotateTransform(angle);
		g.ScaleTransform(scale, scale);
		if (shadow)
		{
			Box(g, Color.FromArgb(24, 50, 43, 30), new RectangleF(-21f, -15f, 54f, 44f), 9f);
			Line(g, Color.FromArgb(20, 50, 43, 30), 8f, new PointF(6f, 27f), new PointF(6f, 108f));
		}
		Color c = Color.FromArgb(119, 147, 120);
		Line(g, Color.FromArgb(90, 118, 94), 5f, new PointF(0f, 19f), new PointF(0f, 94f));
		Box(g, Color.FromArgb(66, 91, 74), new RectangleF(-5f, 78f, 10f, 28f), 4f);
		Box(g, c, new RectangleF(-28f, -24f, 56f, 46f), 10f);
		Box(g, Color.FromArgb(221, 223, 191), new RectangleF(-23f, -19f, 46f, 36f), 6f);
		for (int i = -18; i <= 18; i += 6)
		{
			Line(g, c, 1.5f, new PointF(i, -17f), new PointF(i, 15f));
		}
		for (int j = -13; j <= 13; j += 6)
		{
			Line(g, c, 1.5f, new PointF(-21f, j), new PointF(21f, j));
		}
		g.Restore(gstate);
	}

	public static void Fly(Graphics g, PointF at, float angle, bool sleeping, float phase, bool local)
	{
		Oval(g, Color.FromArgb(30, 65, 48, 24), at.X - 14f, at.Y + 12f, 36f, 12f);
		GraphicsState gstate = g.Save();
		g.TranslateTransform(at.X, at.Y);
		g.RotateTransform(angle * 180f / (float)Math.PI);
		g.ScaleTransform(0.83f, 0.83f);
		for (int i = -1; i <= 1; i += 2)
		{
			for (int j = 0; j < 3; j++)
			{
				Line(g, Color.FromArgb(67, 64, 49), 1.7f, new PointF(-8 + j * 8, i * 5), new PointF(-15 + j * 13, (float)i * (17f + phase)), new PointF(-20 + j * 17, i * 24));
			}
		}
		Oval(g, Color.FromArgb(88, 89, 63), -22f, -10f, 29f, 20f);
		int[] array = new int[2] { -14, -7 };
		int[] array2 = array;
		foreach (int num in array2)
		{
			Line(g, Color.FromArgb(142, 143, 101), 2f, new PointF(num, -7f), new PointF(num, 7f));
		}
		Oval(g, Color.FromArgb(176, 242, 240, 215), -22f, -20f, 37f, 16f);
		Oval(g, Color.FromArgb(176, 242, 240, 215), -22f, 4f, 37f, 16f);
		Line(g, Color.FromArgb(95, 145, 151, 123), 0.8f, new PointF(6f, -5f), new PointF(-17f, -13f));
		Line(g, Color.FromArgb(95, 145, 151, 123), 0.8f, new PointF(6f, 5f), new PointF(-17f, 13f));
		Oval(g, local ? Green : Coral, -5f, -9f, 20f, 18f);
		Oval(g, Color.FromArgb(69, 66, 48), 9f, -9f, 15f, 18f);
		Oval(g, Color.FromArgb(172, 91, 58), 14f, -10f, 9f, 9f);
		Oval(g, Color.FromArgb(172, 91, 58), 14f, 1f, 9f, 9f);
		Line(g, Color.FromArgb(67, 64, 49), 1.5f, new PointF(22f, -4f), new PointF(29f, -10f));
		Line(g, Color.FromArgb(67, 64, 49), 1.5f, new PointF(22f, 4f), new PointF(29f, 10f));
		g.Restore(gstate);
		if (sleeping)
		{
			Text(g, "z z", at.X + 19f, at.Y - 38f, 13f, Color.FromArgb(233, 236, 220));
		}
	}
}

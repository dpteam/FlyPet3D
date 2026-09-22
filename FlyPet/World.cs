using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NeuroFlyEngine;

namespace FlyPet;

public sealed class World
{
	public const float Width = 800f;

	public const float Height = 500f;

	public readonly PlayerProfile Profile;

	public float X = 360f;

	public float Y = 245f;

	public float Angle = -0.3f;

	public float Time;

	public float Threat;

	public bool Sleeping;

	public bool Paused;

	public string Activity = "Осматривается";

	public readonly List<TableItem> Items = new List<TableItem>();

	private PointF _impact;

	public Pet Pet => Profile.Pet;

	public World(PlayerProfile profile)
	{
		Profile = profile;
		X += (float)(profile.Id.ToByteArray()[0] - 128) * 0.6f;
	}

	public static bool InBounds(float x, float y)
	{
		if (ZFloat.IsFinite(x) && ZFloat.IsFinite(y) && x >= 0f && x <= 800f && y >= 0f)
		{
			return y <= 500f;
		}
		return false;
	}

	public static float Wrap(float angle)
	{
		return MathF.Atan2(MathF.Sin(angle), MathF.Cos(angle));
	}

	public static float Distance(float x, float y, float a, float b)
	{
		return MathF.Sqrt((x - a) * (x - a) + (y - b) * (y - b));
	}

	public bool Place(string kind, PointF point)
	{
		bool flag = Paused;
		if (!flag)
		{
			bool flag2 = ((kind == "fruit" || kind == "water") ? true : false);
			flag = !flag2;
		}
		if (flag || !InBounds(point.X, point.Y))
		{
			return false;
		}
		if (Items.Count == 6)
		{
			Items.RemoveAt(0);
		}
		Items.Add(new TableItem(kind, ZMath.Clamp(point.X, 28f, 772f), ZMath.Clamp(point.Y, 28f, 472f), 40f));
		return true;
	}

	public void Apply(TableAction action)
	{
		if (!action.IsValid || Paused)
		{
			return;
		}
		float num = Distance(X, Y, action.X, action.Y);
		if (action.Kind == "swat")
		{
			float num2 = ZMath.Clamp(1f - num / 300f, 0f, 1f);
			if (!(num2 <= 0f))
			{
				Threat = Math.Max(Threat, num2 * 2.4f);
				_impact = new PointF(action.X, action.Y);
				Sleeping = false;
				Angle = MathF.Atan2(Y - action.Y, X - action.X);
			}
		}
		else if (num < 85f)
		{
			Pet.Clean = Math.Min(100f, Pet.Clean + 4f);
		}
	}

	private TableItem? Target()
	{
		return (from i in Items
			where (!(i.Kind == "fruit")) ? (Pet.Water < 97f) : (Pet.Satiety < 97f)
			orderby Distance(X, Y, i.X, i.Y) / ((i.Kind == "fruit") ? (101f - Pet.Satiety) : (101f - Pet.Water))
			select i).FirstOrDefault();
	}

	public (float Left, float Right, float Forward, float ThreatLeft, float ThreatRight) Inputs()
	{
		TableItem tableItem = Target();
		float num = 0f;
		float num2 = 0f;
		if (tableItem != null)
		{
			num2 = Wrap(MathF.Atan2(tableItem.Y - Y, tableItem.X - X) - Angle);
			num = ZMath.Clamp(1f - Distance(X, Y, tableItem.X, tableItem.Y) / 800f, 0.1f, 1f);
		}
		float num3 = Wrap(MathF.Atan2(_impact.Y - Y, _impact.X - X) - Angle);
		float num4 = ZMath.Clamp(Threat, 0f, 1f);
		return (Left: num * ZMath.Clamp(0.5f - num2 / (float)Math.PI, 0f, 1f), Right: num * ZMath.Clamp(0.5f + num2 / (float)Math.PI, 0f, 1f), Forward: num, ThreatLeft: num4 * ((num3 < 0f) ? 1f : 0.25f), ThreatRight: num4 * ((num3 >= 0f) ? 1f : 0.25f));
	}

	public void Tick(float dt, BrainSignals signals)
	{
		if (Paused || !ZFloat.IsFinite(dt) || dt <= 0f)
		{
			return;
		}
		dt = Math.Min(dt, 0.1f);
		Time += dt;
		Pet.AgeSeconds += dt;
		Threat = Math.Max(0f, Threat - dt);
		Pet.Satiety -= dt * 0.045f;
		Pet.Water -= dt * 0.06f;
		Pet.Clean -= dt * 0.025f;
		Pet.Energy += dt * (Sleeping ? 1.3f : (-0.065f));
		TableItem tableItem = Target();
		float num = ((tableItem == null) ? 1000f : Distance(X, Y, tableItem.X, tableItem.Y));
		if (Sleeping)
		{
			Activity = "Спит";
		}
		else if (Threat > 0f || signals.Escape)
		{
			Activity = "Убегает";
		}
		else if (tableItem != null && num < 26f)
		{
			Activity = ((tableItem.Kind == "fruit") ? "Ест фрукт" : "Пьёт воду");
		}
		else if (Pet.Clean < 65f && signals.GroomDrive > 0.15f)
		{
			Activity = "Чистит лапки";
		}
		else
		{
			Activity = ((tableItem == null) ? "Исследует стол" : ((tableItem.Kind == "fruit") ? "Ищет фрукт" : "Ищет воду"));
		}
		if (!Sleeping)
		{
			string activity = Activity;
			if (activity == "Ест фрукт" || activity == "Пьёт воду")
			{
				float num2 = Math.Min(tableItem.Amount, dt * 5f);
				if (tableItem.Kind == "fruit")
				{
					Pet.Satiety += num2;
					Pet.Energy += num2 * 0.3f;
				}
				else
				{
					Pet.Water += num2;
				}
				int index = Items.IndexOf(tableItem);
				if (tableItem.Amount <= num2)
				{
					Items.RemoveAt(index);
					if (tableItem.Kind == "fruit")
					{
						Pet.Meals++;
					}
				}
				else
				{
					Items[index] = tableItem with
					{
						Amount = tableItem.Amount - num2
					};
				}
			}
			else if (Activity == "Чистит лапки")
			{
				Pet.Clean += dt * 2f;
			}
			else
			{
				float num3 = ((tableItem == null) ? 0f : Wrap(MathF.Atan2(tableItem.Y - Y, tableItem.X - X) - Angle));
				float num4 = ((Threat > 0f) ? (Wrap(MathF.Atan2(Y - _impact.Y, X - _impact.X) - Angle) * 4f) : ((tableItem != null) ? (num3 * 2.4f) : (MathF.Sin(Time * 0.6f) * 0.6f)));
				Angle = Wrap(Angle + (num4 - signals.TurnBias * 0.8f) * dt);
				float num5 = (15f + 26f * signals.WalkDrive + 8f * signals.Arousal) * (0.25f + Pet.Energy / 130f);
				if (tableItem != null && Threat <= 0f)
				{
					num5 *= ZMath.Clamp(num / 90f, 0.25f, 1f);
				}
				if (Threat > 0f || signals.Escape)
				{
					num5 += 85f * (0.4f + signals.Nervous);
				}
				if (signals.Backward && Threat <= 0f)
				{
					num5 *= -0.45f;
				}
				X += MathF.Cos(Angle) * num5 * dt;
				Y += MathF.Sin(Angle) * num5 * dt;
				if (X < 24f || X > 776f || Y < 24f || Y > 476f)
				{
					Angle = Wrap(Angle + 2.1991148f);
				}
				X = ZMath.Clamp(X, 24f, 776f);
				Y = ZMath.Clamp(Y, 24f, 476f);
			}
		}
		Pet.Clamp();
	}

	public FlyFrame Frame()
	{
		return new FlyFrame(Profile.Id, Profile.Name, X, Y, Angle, Sleeping, Paused ? "На паузе" : Activity, Pet.Satiety, Pet.Water, Pet.Energy, Pet.Clean, Items.ToArray());
	}
}

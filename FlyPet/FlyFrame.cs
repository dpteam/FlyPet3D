using System;
using System.Linq;

namespace FlyPet;

public sealed record FlyFrame(Guid Owner, string Name, float X, float Y, float Angle, bool Sleeping, string Activity, float Satiety, float Water, float Energy, float Clean, TableItem[] Items)
{
	public bool IsValid(Guid owner)
	{
		if (Owner == owner)
		{
			string name = Name;
			if (name != null)
			{
				int length = name.Length;
				if (length > 0 && length <= 18)
				{
					name = Activity;
					if (name != null && name.Length <= 40 && World.InBounds(X, Y) && ZFloat.IsFinite(Angle) && (double)Math.Abs(Angle) <= Math.PI * 4.0 && new float[4] { Satiety, Water, Energy, Clean }.All((float v) => ZFloat.IsFinite(v) && v >= 0f && v <= 100f))
					{
						TableItem[] items = Items;
						if (items != null && items.Length <= 6)
						{
							return Items.All(delegate(TableItem i)
							{
								bool flag = (object)i != null;
								if (flag)
								{
									string kind = i.Kind;
									bool flag2 = ((kind == "fruit" || kind == "water") ? true : false);
									flag = flag2;
								}
								return flag && World.InBounds(i.X, i.Y) && ZFloat.IsFinite(i.Amount) && i.Amount > 0f && i.Amount <= 40f;
							});
						}
					}
				}
			}
		}
		return false;
	}
}

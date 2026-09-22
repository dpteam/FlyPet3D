using System;

namespace FlyPet;

public sealed class Pet
{
	public float Satiety { get; set; } = 75f;

	public float Water { get; set; } = 85f;

	public float Energy { get; set; } = 90f;

	public float Clean { get; set; } = 90f;

	public float AgeSeconds { get; set; }

	public int Meals { get; set; }

	public void Clamp()
	{
		Satiety = Safe(Satiety);
		Water = Safe(Water);
		Energy = Safe(Energy);
		Clean = Safe(Clean);
		if (!ZFloat.IsFinite(AgeSeconds) || AgeSeconds < 0f)
		{
			AgeSeconds = 0f;
		}
		Meals = Math.Max(0, Meals);
	}

	private static float Safe(float x)
	{
		if (!ZFloat.IsFinite(x))
		{
			return 75f;
		}
		return ZMath.Clamp(x, 0f, 100f);
	}
}

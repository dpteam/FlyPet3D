namespace FlyPet;

public sealed record TableAction(string Kind, float X, float Y)
{
	public bool IsValid
	{
		get
		{
			string kind = Kind;
			if (kind == "swat" || kind == "brush")
			{
				return World.InBounds(X, Y);
			}
			return false;
		}
	}
}

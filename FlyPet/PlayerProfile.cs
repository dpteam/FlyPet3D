using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FlyPet;

public sealed class PlayerProfile
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public string Name { get; set; } = "Пиксель";

	public Pet Pet { get; set; } = new Pet();

	public static string CleanName(string? name)
	{
		string text = new string((name ?? "").Where((char c) => !char.IsControl(c)).Take(18).ToArray()).Trim();
		if (text.Length != 0)
		{
			return text;
		}
		return "Муха";
	}

	public static PlayerProfile Load(string folder)
	{
		try
		{
			PlayerProfile playerProfile = JsonSerializer.Deserialize<PlayerProfile>(File.ReadAllText(Path.Combine(folder, "profile.json"))) ?? new PlayerProfile();
			if (playerProfile.Id == Guid.Empty)
			{
				playerProfile.Id = Guid.NewGuid();
			}
			playerProfile.Name = CleanName(playerProfile.Name);
			PlayerProfile playerProfile2 = playerProfile;
			if (playerProfile2.Pet == null)
			{
				Pet pet = (playerProfile2.Pet = new Pet());
				Pet pet3 = pet;
			}
			playerProfile.Pet.Clamp();
			return playerProfile;
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException || ex is JsonException) ? 1 : 0) != 0)
		{
			PlayerProfile playerProfile3 = new PlayerProfile();
			try
			{
				playerProfile3.Pet = JsonSerializer.Deserialize<Pet>(File.ReadAllText(Path.Combine(folder, "pet.json"))) ?? new Pet();
			}
			catch (Exception ex2) when (((ex2 is IOException || ex2 is UnauthorizedAccessException || ex2 is JsonException) ? 1 : 0) != 0)
			{
			}
			playerProfile3.Pet.Clamp();
			return playerProfile3;
		}
	}

	public void Save(string folder)
	{
		Directory.CreateDirectory(folder);
		string text = Path.Combine(folder, "profile.json");
		File.WriteAllText(text + ".tmp", JsonSerializer.Serialize(this));
		FileEx.Move(text + ".tmp", text, overwrite: true);
	}
}

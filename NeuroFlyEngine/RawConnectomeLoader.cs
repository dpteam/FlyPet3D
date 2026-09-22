using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace NeuroFlyEngine;

public static class RawConnectomeLoader
{
	public static bool HasData(string path)
	{
		if (!File.Exists(path))
		{
			return typeof(RawConnectomeLoader).Assembly.GetManifestResourceInfo("FlyPet.Data." + Path.GetFileName(path)) != null;
		}
		return true;
	}

	private static Stream OpenData(string path)
	{
		return File.Exists(path) ? File.OpenRead(path) : (typeof(RawConnectomeLoader).Assembly.GetManifestResourceStream("FlyPet.Data." + Path.GetFileName(path)) ?? throw new FileNotFoundException("Connectome data not found", path));
	}

	private static string[] ParseCsv(string line)
	{
		List<string> list = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		for (int i = 0; i < line.Length; i++)
		{
			char c = line[i];
			switch (c)
			{
			case '"':
				if (flag && i + 1 < line.Length && line[i + 1] == '"')
				{
					stringBuilder.Append('"');
					i++;
				}
				else
				{
					flag = !flag;
				}
				continue;
			case ',':
				if (!flag)
				{
					list.Add(stringBuilder.ToString());
					stringBuilder.Clear();
					continue;
				}
				break;
			}
			stringBuilder.Append(c);
		}
		list.Add(stringBuilder.ToString());
		return list.ToArray();
	}

	public static (List<Neuron> neurons, List<Edge> edges) LoadFromGz(string classificationGzPath, string connectionsGzPath, string? coordinatesGzPath = null, Action<string, int>? onProgress = null, string? cellTypesGzPath = null)
	{
		List<Neuron> list = new List<Neuron>();
		List<Edge> list2 = new List<Edge>();
		Dictionary<ulong, int> dictionary = new Dictionary<ulong, int>(140000);
		Dictionary<ulong, string> dictionary2 = new Dictionary<ulong, string>();
		if (cellTypesGzPath == null)
		{
			cellTypesGzPath = Path.Combine(Path.GetDirectoryName(classificationGzPath) ?? ".", "consolidated_cell_types.csv.gz");
		}
		if (HasData(cellTypesGzPath))
		{
			using Stream stream = OpenData(cellTypesGzPath);
			using GZipStream stream2 = new GZipStream(stream, CompressionMode.Decompress);
			using StreamReader streamReader = new StreamReader(stream2);
			streamReader.ReadLine();
			string line;
			while ((line = streamReader.ReadLine()) != null)
			{
				string[] array = ParseCsv(line);
				if (array.Length >= 2 && ulong.TryParse(array[0], out var result))
				{
					dictionary2[result] = array[1].Trim().ToLowerInvariant();
				}
			}
		}
		onProgress?.Invoke("Парсинг типов нейронов", 0);
		using (Stream stream3 = OpenData(classificationGzPath))
		{
			using GZipStream stream4 = new GZipStream(stream3, CompressionMode.Decompress);
			using StreamReader streamReader2 = new StreamReader(stream4, Encoding.UTF8);
			streamReader2.ReadLine();
			int num = 0;
			string text;
			while ((text = streamReader2.ReadLine()) != null)
			{
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				string[] array2 = ParseCsv(text);
				if (array2.Length >= 7 && ulong.TryParse(array2[0], out var result2))
				{
					array2[1].Trim().ToLowerInvariant();
					string text2 = array2[4].Trim().ToLowerInvariant();
					string text3 = array2[3].Trim().ToLowerInvariant();
					string side = array2[6].Trim().ToLowerInvariant();
					string valueOrDefault = dictionary2.GetValueOrDefault(result2, "");
					string text4;
					switch (valueOrDefault)
					{
					case "lc4":
					case "mdn":
					case "lplc2":
					case "dnp09":
					case "dna01":
					case "dna02":
					case "dng11":
					case "escw":
						text4 = valueOrDefault;
						break;
					case "gf":
					case "giant_fiber":
						text4 = "gf";
						break;
					default:
						text4 = "other";
						break;
					}
					string role = text4;
					if (text3 == "giant_fiber" || text2 == "giant_fiber")
					{
						role = "gf";
					}
					string text5 = array2[2].Trim().ToLowerInvariant();
					bool flag;
					switch (text5)
					{
					case "sensory":
					case "ascending":
					case "descending":
						flag = true;
						break;
					default:
						flag = false;
						break;
					}
					string type = (flag ? text5 : "inter");
					int count = list.Count;
					dictionary[result2] = count;
					list.Add(new Neuron
					{
						Id = result2.ToString(),
						Role = role,
						Type = type,
						Side = side
					});
					num++;
					if (num % 25000 == 0)
					{
						onProgress?.Invoke("Парсинг типов нейронов", (int)((double)num / 140000.0 * 100.0));
					}
				}
			}
		}
		onProgress?.Invoke("Парсинг типов нейронов", 100);
		if (!string.IsNullOrEmpty(coordinatesGzPath) && File.Exists(coordinatesGzPath))
		{
			onProgress?.Invoke("Парсинг 3D координат", 0);
			using FileStream stream5 = File.OpenRead(coordinatesGzPath);
			using GZipStream stream6 = new GZipStream(stream5, CompressionMode.Decompress);
			using StreamReader streamReader3 = new StreamReader(stream6, Encoding.UTF8);
			streamReader3.ReadLine();
			string text6;
			while ((text6 = streamReader3.ReadLine()) != null)
			{
				int num2 = text6.IndexOf(',');
				if (num2 <= 0 || !ulong.TryParse(text6.Substring(0, num2), out var result3) || !dictionary.TryGetValue(result3, out var value) || list[value].X != 0f || list[value].Y != 0f)
				{
					continue;
				}
				int num3 = text6.IndexOf('(', num2);
				int num4 = text6.IndexOf(')', (num3 > 0) ? num3 : num2);
				if (num3 >= 0 && num4 > num3)
				{
					string[] array3 = text6.Substring(num3 + 1, num4 - num3 - 1).Split(new char[3] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
					if (array3.Length >= 3)
					{
						float.TryParse(array3[0], NumberStyles.Any, CultureInfo.InvariantCulture, out list[value].X);
						float.TryParse(array3[1], NumberStyles.Any, CultureInfo.InvariantCulture, out list[value].Y);
						float.TryParse(array3[2], NumberStyles.Any, CultureInfo.InvariantCulture, out list[value].Z);
					}
				}
			}
			onProgress?.Invoke("Парсинг 3D координат", 100);
		}
		onProgress?.Invoke("Построение графа синапсов", 0);
		using (Stream stream7 = OpenData(connectionsGzPath))
		{
			using GZipStream stream8 = new GZipStream(stream7, CompressionMode.Decompress);
			using StreamReader streamReader4 = new StreamReader(stream8, Encoding.UTF8);
			streamReader4.ReadLine();
			int num5 = 0;
			Dictionary<long, float> dictionary3 = new Dictionary<long, float>();
			string text7;
			while ((text7 = streamReader4.ReadLine()) != null)
			{
				if (string.IsNullOrWhiteSpace(text7))
				{
					continue;
				}
				string[] array4 = ParseCsv(text7);
				if (array4.Length >= 5 && ulong.TryParse(array4[0], out var result4) && ulong.TryParse(array4[1], out var result5) && dictionary.TryGetValue(result4, out var value2) && dictionary.TryGetValue(result5, out var value3) && float.TryParse(array4[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var result6))
				{
					string text8 = array4[4].Trim().ToLowerInvariant();
					float num6 = ((text8 == "gaba" || text8 == "glut") ? (-1f) : 1f);
					float num7 = result6 * num6;
					long key = ((long)value2 << 32) | (uint)value3;
					if (dictionary3.TryGetValue(key, out var value4))
					{
						dictionary3[key] = value4 + num7;
					}
					else
					{
						dictionary3[key] = num7;
					}
					num5++;
					if (num5 % 300000 == 0)
					{
						onProgress?.Invoke("Построение графа синапсов", (int)((double)num5 / 5342446.0 * 100.0));
					}
				}
			}
			list2.Capacity = dictionary3.Count;
			foreach (KeyValuePair<long, float> item in dictionary3)
			{
				int pre = (int)(item.Key >> 32);
				int post = (int)(item.Key & 0xFFFFFFFFu);
				list2.Add(new Edge(pre, post, item.Value));
			}
		}
		onProgress?.Invoke("Построение графа синапсов", 100);
		return (neurons: list, edges: list2);
	}
}

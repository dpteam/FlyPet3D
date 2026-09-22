using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace NeuroFlyEngine;

public class LIFSim
{
	private struct Stim
	{
		public int[] Idx;

		public float Strength;

		public int DurationMs;

		public int UntilMs;
	}

	public int N;

	public string[] Ids = Array.Empty<string>();

	public string[] Roles = Array.Empty<string>();

	public string[] Types = Array.Empty<string>();

	public string[] Sides = Array.Empty<string>();

	public readonly List<int> LoomLeft = new List<int>();

	public readonly List<int> LoomRight = new List<int>();

	public readonly List<int> Gf = new List<int>();

	public readonly List<int> DnaL = new List<int>();

	public readonly List<int> DnaR = new List<int>();

	public readonly List<int> Mdn = new List<int>();

	public readonly List<int> Fwd = new List<int>();

	public readonly List<int> Groom = new List<int>();

	public readonly List<int> Escw = new List<int>();

	public readonly List<int> Ascend = new List<int>();

	public readonly List<int> Sens = new List<int>();

	public readonly List<int> FoodOSNs = new List<int>();

	public readonly List<int> SocialOSNs = new List<int>();

	public List<int> DnaDriveL = new List<int>();

	public List<int> DnaDriveR = new List<int>();

	public List<int> FwdDrive = new List<int>();

	public List<int> ArousalPool = new List<int>();

	public float LoomL;

	public float LoomR;

	public float GaitDrive;

	public float GaitPhase;

	public float AttractL;

	public float AttractR;

	public float AttractFwd;

	public float AttractArousal;

	public float AirPuff;

	public float ActivityScale = 1f;

	public float SensoryGate = 1f;

	public float LoomOverride;

	public float RateLoom;

	public float RateDNaL;

	public float RateDNaR;

	public float RateMDN;

	public float RateFwd;

	public float RateGroom;

	public float RateEscW;

	public float RatePop;

	private float[] _v = Array.Empty<float>();

	private float[] _refr = Array.Empty<float>();

	private float[] _baseline = Array.Empty<float>();

	private int[] _spikeBuffer = Array.Empty<int>();

	private float[] _ascendPhase = Array.Empty<float>();

	private int[] _rowStart = Array.Empty<int>();

	private int[] _colIdx = Array.Empty<int>();

	private float[] _w = Array.Empty<float>();

	private int[] _edgePre = Array.Empty<int>();

	private int[] _edgePost = Array.Empty<int>();

	private long[] _edgeTraffic = Array.Empty<long>();

	private long[] _neuronSpikeCounts = Array.Empty<long>();

	private float[][] _inhQueue = Array.Empty<float[]>();

	private int _qHead;

	private const int InhDelayMs = 4;

	private const float Decay = 0.9512f;

	private const float Threshold = 1f;

	private const float RefractoryMs = 2f;

	private const float WeightScale = 0.0008f;

	private const float PNoise = 0.0022f;

	private const float NoiseKick = 0.42f;

	private const float LoomGain = 0.3f;

	private const float AttractGain = 0.07f;

	private const float AttractFwdGain = 0.3f;

	private const float AttractArousalGain = 0.08f;

	private const float RateAlpha = 1f / 120f;

	private readonly Random _rng = new Random();

	private int _simMs;

	private long _totalSpikes;

	private bool _gfLatch;

	private readonly object _stimLock = new object();

	private readonly List<Stim> _pendingStims = new List<Stim>();

	private readonly List<Stim> _activeStims = new List<Stim>();

	public int SimMs => _simMs;

	public long TotalSpikes => _totalSpikes;

	public int SynapseCount => _colIdx.Length;

	public bool ConsumeGF()
	{
		bool gfLatch = _gfLatch;
		_gfLatch = false;
		return gfLatch;
	}

	public void Stimulate(List<int> indices, float strength, int durationMs)
	{
		if (indices == null || indices.Count == 0)
		{
			return;
		}
		lock (_stimLock)
		{
			_pendingStims.Add(new Stim
			{
				Idx = indices.ToArray(),
				Strength = strength,
				DurationMs = durationMs,
				UntilMs = 0
			});
			if (_pendingStims.Count > 16)
			{
				_pendingStims.RemoveAt(0);
			}
		}
	}

	public void Initialize(List<Neuron> neurons, List<Edge> edges)
	{
		N = neurons.Count;
		_simMs = 0;
		_totalSpikes = 0L;
		_gfLatch = false;
		RateLoom = (RateDNaL = (RateDNaR = (RateMDN = (RateFwd = (RateGroom = (RateEscW = (RatePop = 0f)))))));
		lock (_stimLock)
		{
			_pendingStims.Clear();
			_activeStims.Clear();
		}
		Ids = new string[N];
		Roles = new string[N];
		Types = new string[N];
		Sides = new string[N];
		_v = new float[N];
		_refr = new float[N];
		_baseline = new float[N];
		_spikeBuffer = new int[N];
		_neuronSpikeCounts = new long[N];
		LoomLeft.Clear();
		LoomRight.Clear();
		Gf.Clear();
		DnaL.Clear();
		DnaR.Clear();
		Mdn.Clear();
		Fwd.Clear();
		Groom.Clear();
		Escw.Clear();
		Ascend.Clear();
		Sens.Clear();
		FoodOSNs.Clear();
		SocialOSNs.Clear();
		for (int i = 0; i < N; i++)
		{
			Neuron neuron = neurons[i];
			Ids[i] = neuron.Id ?? "";
			Roles[i] = neuron.Role ?? "";
			Types[i] = neuron.Type ?? "";
			Sides[i] = neuron.Side ?? "";
			string text = Roles[i];
			string text2 = Sides[i];
			if (text == "lc4" || text == "lplc2")
			{
				if (text2 == "left")
				{
					LoomLeft.Add(i);
				}
				else
				{
					LoomRight.Add(i);
				}
				continue;
			}
			bool flag;
			switch (text)
			{
			case "gf":
				Gf.Add(i);
				continue;
			case "dna01":
			case "dna02":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				if (text2 == "left")
				{
					DnaL.Add(i);
				}
				else
				{
					DnaR.Add(i);
				}
				continue;
			}
			switch (text)
			{
			case "mdn":
				Mdn.Add(i);
				break;
			case "dnp09":
				Fwd.Add(i);
				break;
			case "dng11":
				Groom.Add(i);
				break;
			case "escw":
				Escw.Add(i);
				break;
			case "other":
				if (Types[i] == "ascending")
				{
					Ascend.Add(i);
				}
				else if (Types[i] == "sensory")
				{
					Sens.Add(i);
				}
				break;
			}
		}
		_ascendPhase = new float[Ascend.Count];
		for (int j = 0; j < Ascend.Count; j++)
		{
			_ascendPhase[j] = (float)(_rng.NextDouble() * Math.PI * 2.0);
		}
		for (int k = 0; k < N; k++)
		{
			string text3 = Roles[k];
			bool flag2;
			switch (text3)
			{
			case "other":
				_baseline[k] = 0.01f + 0.06f * (float)_rng.NextDouble();
				continue;
			case "lc4":
			case "lplc2":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			if (flag2)
			{
				_baseline[k] = 0.004f;
				continue;
			}
			switch (text3)
			{
			case "dna01":
			case "dna02":
			case "mdn":
			case "dng11":
			case "escw":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			if (flag2)
			{
				_baseline[k] = 0.036f;
			}
			else if (text3 == "dnp09")
			{
				_baseline[k] = 0.038f;
			}
			else
			{
				_baseline[k] = 0.002f;
			}
		}
		int[] array = new int[N];
		for (int l = 0; l < edges.Count; l++)
		{
			Edge edge = edges[l];
			if (edge.Pre >= 0 && edge.Pre < N && edge.Post >= 0 && edge.Post < N)
			{
				array[edge.Pre]++;
			}
		}
		_rowStart = new int[N + 1];
		for (int m = 0; m < N; m++)
		{
			_rowStart[m + 1] = _rowStart[m] + array[m];
		}
		int num = _rowStart[N];
		_edgeTraffic = new long[num];
		_edgePre = new int[num];
		_edgePost = new int[num];
		_colIdx = new int[num];
		_w = new float[num];
		int[] array2 = new int[N];
		Array.Copy(_rowStart, array2, N);
		for (int n = 0; n < edges.Count; n++)
		{
			Edge edge2 = edges[n];
			if (edge2.Pre >= 0 && edge2.Pre < N && edge2.Post >= 0 && edge2.Post < N)
			{
				float num2 = edge2.Weight * 0.0008f;
				string text4 = Roles[edge2.Pre];
				if ((text4 == "lc4" || text4 == "lplc2" || false || (Roles[edge2.Pre] == "other" && Types[edge2.Pre] == "sensory")) && Roles[edge2.Post] == "gf")
				{
					num2 *= 6f;
				}
				int num3 = array2[edge2.Pre];
				_colIdx[num3] = edge2.Post;
				_w[num3] = num2;
				_edgePre[num3] = edge2.Pre;
				_edgePost[num3] = edge2.Post;
				array2[edge2.Pre]++;
			}
		}
		_inhQueue = new float[5][];
		for (int num4 = 0; num4 < _inhQueue.Length; num4++)
		{
			_inhQueue[num4] = new float[N];
		}
		_qHead = 0;
		BuildAttractPathways(edges);
		for (int num5 = 0; num5 < Sens.Count; num5++)
		{
			if (num5 < 25)
			{
				FoodOSNs.Add(Sens[num5]);
			}
			else if (num5 < 40)
			{
				SocialOSNs.Add(Sens[num5]);
			}
		}
	}

	private void BuildAttractPathways(List<Edge> edges)
	{
		Dictionary<int, float> dictionary = new Dictionary<int, float>();
		Dictionary<int, float> dictionary2 = new Dictionary<int, float>();
		Dictionary<int, float> dictionary3 = new Dictionary<int, float>();
		HashSet<int> hashSet = new HashSet<int>(DnaL);
		HashSet<int> hashSet2 = new HashSet<int>(DnaR);
		HashSet<int> hashSet3 = new HashSet<int>(Fwd);
		for (int i = 0; i < edges.Count; i++)
		{
			Edge edge = edges[i];
			if (edge.Pre >= 0 && edge.Pre < N && edge.Post >= 0 && edge.Post < N && !(edge.Weight <= 0f) && !(Roles[edge.Pre] != "other"))
			{
				if (hashSet.Contains(edge.Post))
				{
					dictionary[edge.Pre] = dictionary.GetValueOrDefault(edge.Pre) + edge.Weight;
				}
				else if (hashSet2.Contains(edge.Post))
				{
					dictionary2[edge.Pre] = dictionary2.GetValueOrDefault(edge.Pre) + edge.Weight;
				}
				else if (hashSet3.Contains(edge.Post))
				{
					dictionary3[edge.Pre] = dictionary3.GetValueOrDefault(edge.Pre) + edge.Weight;
				}
			}
		}
		DnaDriveL = TopDriveKeys(dictionary, dictionary2, 12);
		DnaDriveR = TopDriveKeys(dictionary2, dictionary, 12);
		FwdDrive = TopDriveKeys(dictionary3, null, 6);
		HashSet<int> hashSet4 = new HashSet<int>();
		Dictionary<int, float> dictionary4 = new Dictionary<int, float>();
		for (int j = 0; j < edges.Count; j++)
		{
			Edge edge2 = edges[j];
			if (edge2.Pre >= 0 && edge2.Pre < N && edge2.Post >= 0 && edge2.Post < N && !(edge2.Weight <= 0f))
			{
				string text = Roles[edge2.Post];
				if (text == "gf" || text == "mdn")
				{
					hashSet4.Add(edge2.Pre);
				}
				if (Roles[edge2.Pre] == "other" && Types[edge2.Pre] != "sensory")
				{
					dictionary4[edge2.Pre] = dictionary4.GetValueOrDefault(edge2.Pre) + edge2.Weight;
				}
			}
		}
		List<KeyValuePair<int, float>> list = new List<KeyValuePair<int, float>>();
		foreach (KeyValuePair<int, float> item in dictionary4)
		{
			if (!hashSet4.Contains(item.Key))
			{
				list.Add(item);
			}
		}
		list.Sort((KeyValuePair<int, float> a, KeyValuePair<int, float> b) => b.Value.CompareTo(a.Value));
		ArousalPool.Clear();
		for (int num = 0; num < list.Count; num++)
		{
			if (ArousalPool.Count >= 50)
			{
				break;
			}
			ArousalPool.Add(list[num].Key);
		}
	}

	private static List<int> TopDriveKeys(Dictionary<int, float> primary, Dictionary<int, float>? other, int count)
	{
		List<KeyValuePair<int, float>> list = new List<KeyValuePair<int, float>>();
		foreach (KeyValuePair<int, float> item in primary)
		{
			if (other == null || !other.TryGetValue(item.Key, out var value) || !(value >= item.Value))
			{
				list.Add(item);
			}
		}
		list.Sort((KeyValuePair<int, float> a, KeyValuePair<int, float> b) => b.Value.CompareTo(a.Value));
		List<int> list2 = new List<int>();
		for (int num = 0; num < list.Count; num++)
		{
			if (list2.Count >= count)
			{
				break;
			}
			list2.Add(list[num].Key);
		}
		return list2;
	}

	public void Step(int ms)
	{
		if (ms <= 0 || N == 0)
		{
			return;
		}
		lock (_stimLock)
		{
			for (int i = 0; i < _pendingStims.Count; i++)
			{
				Stim item = _pendingStims[i];
				item.UntilMs = _simMs + item.DurationMs;
				_activeStims.Add(item);
			}
			_pendingStims.Clear();
			for (int num = _activeStims.Count - 1; num >= 0; num--)
			{
				if (_simMs >= _activeStims[num].UntilMs)
				{
					_activeStims.RemoveAt(num);
				}
			}
		}
		for (int j = 0; j < ms; j++)
		{
			_simMs++;
			float num2 = 0.0022f * ActivityScale;
			for (int k = 0; k < N; k++)
			{
				if (_refr[k] > 0f)
				{
					_refr[k]--;
					_v[k] *= 0.9512f;
					continue;
				}
				float num3 = _v[k] * 0.9512f + _baseline[k] * ActivityScale;
				if (_rng.NextDouble() < (double)num2)
				{
					num3 += 0.42f;
				}
				_v[k] = num3;
			}
			if (LoomL > 0.001f)
			{
				float num4 = LoomL * 0.3f * SensoryGate;
				for (int l = 0; l < LoomLeft.Count; l++)
				{
					_v[LoomLeft[l]] += num4;
				}
			}
			if (LoomR > 0.001f)
			{
				float num5 = LoomR * 0.3f * SensoryGate;
				for (int m = 0; m < LoomRight.Count; m++)
				{
					_v[LoomRight[m]] += num5;
				}
			}
			if (GaitDrive > 0.001f && _ascendPhase != null)
			{
				float num6 = GaitPhase * 2f * (float)Math.PI;
				for (int n = 0; n < Ascend.Count; n++)
				{
					_v[Ascend[n]] += GaitDrive * 0.09f * (0.5f + 0.5f * (float)Math.Sin(num6 + _ascendPhase[n]));
				}
			}
			if (AirPuff > 0.001f)
			{
				float num7 = AirPuff * 0.12f * SensoryGate;
				for (int num8 = 0; num8 < Sens.Count; num8++)
				{
					_v[Sens[num8]] += num7;
				}
			}
			if (AttractL > 0.001f)
			{
				for (int num9 = 0; num9 < DnaDriveL.Count; num9++)
				{
					_v[DnaDriveL[num9]] += AttractL * 0.07f;
				}
			}
			if (AttractR > 0.001f)
			{
				for (int num10 = 0; num10 < DnaDriveR.Count; num10++)
				{
					_v[DnaDriveR[num10]] += AttractR * 0.07f;
				}
			}
			if (AttractFwd > 0.001f)
			{
				for (int num11 = 0; num11 < FwdDrive.Count; num11++)
				{
					_v[FwdDrive[num11]] += AttractFwd * 0.3f;
				}
			}
			if (AttractArousal > 0.001f)
			{
				for (int num12 = 0; num12 < ArousalPool.Count; num12++)
				{
					_v[ArousalPool[num12]] += AttractArousal * 0.08f;
				}
			}
			for (int num13 = 0; num13 < _activeStims.Count; num13++)
			{
				Stim stim = _activeStims[num13];
				if (_simMs > stim.UntilMs)
				{
					continue;
				}
				for (int num14 = 0; num14 < stim.Idx.Length; num14++)
				{
					int num15 = stim.Idx[num14];
					if (num15 >= 0 && num15 < N)
					{
						_v[num15] += stim.Strength;
					}
				}
			}
			float[] array = _inhQueue[_qHead];
			for (int num16 = 0; num16 < N; num16++)
			{
				float num17 = array[num16];
				if (num17 != 0f)
				{
					_v[num16] = Math.Max(-2f, _v[num16] + num17);
					array[num16] = 0f;
				}
			}
			int num18 = 0;
			for (int num19 = 0; num19 < N; num19++)
			{
				if (_refr[num19] <= 0f && _v[num19] >= 1f)
				{
					_v[num19] = 0f;
					_refr[num19] = 2f;
					_neuronSpikeCounts[num19]++;
					if (num18 < _spikeBuffer.Length)
					{
						_spikeBuffer[num18++] = num19;
					}
				}
			}
			_totalSpikes += num18;
			int num20 = (_qHead + 4) % _inhQueue.Length;
			float[] array2 = _inhQueue[num20];
			for (int num21 = 0; num21 < num18; num21++)
			{
				int num22 = _spikeBuffer[num21];
				int num23 = _rowStart[num22];
				int num24 = _rowStart[num22 + 1];
				for (int num25 = num23; num25 < num24; num25++)
				{
					int num26 = _colIdx[num25];
					float num27 = _w[num25];
					_edgeTraffic[num25]++;
					if (num27 >= 0f)
					{
						_v[num26] = Math.Max(-2f, _v[num26] + num27);
					}
					else
					{
						array2[num26] += num27;
					}
				}
			}
			_qHead = (_qHead + 1) % _inhQueue.Length;
			int num28 = 0;
			int num29 = 0;
			int num30 = 0;
			int num31 = 0;
			int num32 = 0;
			int num33 = 0;
			int num34 = 0;
			for (int num35 = 0; num35 < num18; num35++)
			{
				int num36 = _spikeBuffer[num35];
				string text = Roles[num36];
				if (text == "lc4" || text == "lplc2")
				{
					num28++;
					continue;
				}
				if (text == "dna01" || text == "dna02")
				{
					if (Sides[num36] == "left")
					{
						num29++;
					}
					else
					{
						num30++;
					}
					continue;
				}
				switch (text)
				{
				case "mdn":
					num31++;
					break;
				case "dnp09":
					num32++;
					break;
				case "dng11":
					num33++;
					break;
				case "escw":
					num34++;
					break;
				case "gf":
					_gfLatch = true;
					break;
				}
			}
			float num37 = Math.Max(1, LoomLeft.Count + LoomRight.Count);
			RateLoom += ((float)num28 * 1000f / num37 - RateLoom) * (1f / 120f);
			RateDNaL += ((float)num29 * 1000f / (float)Math.Max(1, DnaL.Count) - RateDNaL) * (1f / 120f);
			RateDNaR += ((float)num30 * 1000f / (float)Math.Max(1, DnaR.Count) - RateDNaR) * (1f / 120f);
			RateMDN += ((float)num31 * 1000f / (float)Math.Max(1, Mdn.Count) - RateMDN) * (1f / 120f);
			RateFwd += ((float)num32 * 1000f / (float)Math.Max(1, Fwd.Count) - RateFwd) * (1f / 120f);
			RateGroom += ((float)num33 * 1000f / (float)Math.Max(1, Groom.Count) - RateGroom) * (1f / 120f);
			RateEscW += ((float)num34 * 1000f / (float)Math.Max(1, Escw.Count) - RateEscW) * (1f / 120f);
			RatePop += ((float)num18 * 1000f / (float)Math.Max(1, N) - RatePop) * (1f / 120f);
		}
	}

	public void SaveState(string path)
	{
		using StreamWriter streamWriter = new StreamWriter(path, append: false, Encoding.UTF8);
		streamWriter.WriteLine($"simMs={_simMs}");
		streamWriter.WriteLine($"totalSpikes={_totalSpikes}");
		streamWriter.WriteLine("rateLoom=" + RateLoom.ToString(CultureInfo.InvariantCulture));
		streamWriter.WriteLine("rateDNaL=" + RateDNaL.ToString(CultureInfo.InvariantCulture));
		streamWriter.WriteLine("rateDNaR=" + RateDNaR.ToString(CultureInfo.InvariantCulture));
		streamWriter.WriteLine("rateMDN=" + RateMDN.ToString(CultureInfo.InvariantCulture));
		streamWriter.WriteLine("rateFwd=" + RateFwd.ToString(CultureInfo.InvariantCulture));
		streamWriter.WriteLine("rateGroom=" + RateGroom.ToString(CultureInfo.InvariantCulture));
		streamWriter.WriteLine("rateEscW=" + RateEscW.ToString(CultureInfo.InvariantCulture));
		streamWriter.WriteLine("ratePop=" + RatePop.ToString(CultureInfo.InvariantCulture));
	}

	public void DumpAnalytics(string path)
	{
		long num = 0L;
		int num2 = 0;
		for (int i = 0; i < N; i++)
		{
			num += _neuronSpikeCounts[i];
			if (_neuronSpikeCounts[i] > 0)
			{
				num2++;
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("{\n");
		stringBuilder.Append("  \"sim_ms\": ").Append(_simMs).Append(",\n");
		stringBuilder.Append("  \"total_neurons\": ").Append(N).Append(",\n");
		stringBuilder.Append("  \"total_edges\": ").Append(_colIdx.Length).Append(",\n");
		stringBuilder.Append("  \"total_spikes\": ").Append(num).Append(",\n");
		stringBuilder.Append("  \"active_neurons\": ").Append(num2).Append(",\n");
		float num3 = ((N > 0) ? ((float)num2 * 100f / (float)N) : 0f);
		stringBuilder.Append("  \"active_percent\": ").Append(num3.ToString("F2", CultureInfo.InvariantCulture)).Append(",\n");
		double num4 = (double)_simMs / 1000.0;
		double num5 = ((N > 0 && num4 > 0.0) ? ((double)num / num4 / (double)N) : 0.0);
		stringBuilder.Append("  \"avg_hz_per_neuron\": ").Append(num5.ToString("F4", CultureInfo.InvariantCulture)).Append("\n");
		stringBuilder.Append("}\n");
		File.WriteAllText(path, stringBuilder.ToString(), Encoding.UTF8);
	}
}

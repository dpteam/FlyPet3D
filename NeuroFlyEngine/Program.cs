using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NeuroFlyEngine;

internal static class Program
{
	private static void Main()
	{
		Console.OutputEncoding = Encoding.UTF8;
		Console.WriteLine("===============================================================");
		Console.WriteLine("        NEUROFLY BRAIN ENGINE — PERFORMANCE BENCHMARK          ");
		Console.WriteLine("===============================================================");
		FlyBrainEngine flyBrainEngine = new FlyBrainEngine();
		string text = "classification.csv.gz";
		string text2 = "connections_princeton.csv.gz";
		string text3 = "coordinates.csv.gz";
		if (File.Exists(text) && File.Exists(text2))
		{
			Console.WriteLine("[DATA] Обнаружены оригинальные датасеты FlyWire (.csv.gz). Загрузка...");
			var (list, list2) = RawConnectomeLoader.LoadFromGz(text, text2, File.Exists(text3) ? text3 : null, delegate(string stage, int pct)
			{
				Console.Write($"\r--> {stage}: {pct,3}%");
			});
			Console.WriteLine($"\n[INIT] Инициализация LIF-матриц ({list.Count} нейронов, {list2.Count} синапсов)...");
			flyBrainEngine.Sim.Initialize(list, list2);
		}
		else
		{
			Console.WriteLine("[WARN] Файлы classification.csv.gz / connections_princeton.csv.gz не найдены.");
			Console.WriteLine("[SYNTH] Генерация синтетической кортикальной колонки (5,000 нейронов, 120,000 связей)...");
			GenerateBenchmarkCircuit(flyBrainEngine.Sim, 5000, 24);
		}
		Console.WriteLine($"[READY] Сеть готова. Топология: {flyBrainEngine.Sim.N:N0} нейронов | {flyBrainEngine.Sim.SynapseCount:N0} синапсов.");
		double num = 1.9200000762939453;
		Console.WriteLine($"\n[BENCHMARK] Запуск симуляции {120} кадров ({num:F2} с биол. времени)...");
		Console.WriteLine(new string('-', 75));
		Console.WriteLine("Кадр | Биол.время | Спайки |  TurnBias | WalkDrive | Nervous | Escape | FPS шага");
		Console.WriteLine(new string('-', 75));
		Stopwatch stopwatch = Stopwatch.StartNew();
		for (int num2 = 1; num2 <= 120; num2++)
		{
			if (num2 == 20)
			{
				flyBrainEngine.SetLooming(1f, 0f);
			}
			if (num2 == 40)
			{
				flyBrainEngine.SetLooming(0f, 0f);
			}
			if (num2 == 60)
			{
				flyBrainEngine.SetAttraction(0f, 0f, 1f, 0.4f);
			}
			if (num2 == 90)
			{
				flyBrainEngine.SetAttraction(0f, 0f, 0f, 0f);
			}
			Stopwatch stopwatch2 = Stopwatch.StartNew();
			BrainSignals brainSignals = flyBrainEngine.Update(0.016f);
			stopwatch2.Stop();
			double value = ((stopwatch2.Elapsed.TotalSeconds > 0.0) ? (1.0 / stopwatch2.Elapsed.TotalSeconds) : 9999.0);
			if (num2 % 10 == 0 || num2 == 20 || num2 == 60)
			{
				Console.WriteLine($"{num2,4} | {(float)num2 * 0.016f,8:F2} с | {flyBrainEngine.Sim.TotalSpikes,6} | {brainSignals.TurnBias:+0.00;-0.00} | {brainSignals.WalkDrive,9:F2} | {brainSignals.Nervous,7:F2} | {brainSignals.Escape,6} | {value,7:F0}");
			}
		}
		stopwatch.Stop();
		double totalSeconds = stopwatch.Elapsed.TotalSeconds;
		double num3 = num / totalSeconds;
		Console.WriteLine(new string('-', 75));
		Console.WriteLine(" РЕЗУЛЬТАТЫ БЕНЧМАРКА:");
		Console.WriteLine($" * Реальное время теста (Wall time):       {totalSeconds * 1000.0:F2} мс");
		Console.WriteLine($" * Просимулировано в мозге (Bio time):     {num * 1000.0:F2} мс");
		Console.WriteLine(string.Format(" * Множитель скорости (Real-time factor):  {0:F2}x {1}", num3, (num3 >= 1.0) ? "(Быстрее реального времени)" : "(Медленнее реального времени)"));
		Console.WriteLine($" * Суммарно спайков сгенерировано:         {flyBrainEngine.Sim.TotalSpikes:N0}");
		Console.WriteLine($" * Средняя спайковая частота:              {(double)flyBrainEngine.Sim.TotalSpikes / num / (double)Math.Max(1, flyBrainEngine.Sim.N):F2} Гц/нейрон");
		Console.WriteLine($" * Пропускная способность синапсов:        {(double)flyBrainEngine.Sim.SynapseCount * (num * 1000.0) / totalSeconds:N0} синаптических обновлений/сек");
		flyBrainEngine.Sim.SaveState("benchmark_state.dat");
		flyBrainEngine.Sim.DumpAnalytics("benchmark_analytics.json");
		Console.WriteLine("\n[EXPORT] Чекпоинты сохранены в benchmark_state.dat и benchmark_analytics.json.");
	}

	private static void GenerateBenchmarkCircuit(LIFSim sim, int neuronCount, int synapsesPerNeuron)
	{
		Random random = new Random(42);
		List<Neuron> list = new List<Neuron>(neuronCount);
		List<Edge> list2 = new List<Edge>(neuronCount * synapsesPerNeuron);
		string[] array = new string[6] { "lc4", "gf", "dna01", "dnp09", "mdn", "other" };
		string[] array2 = new string[3] { "left", "right", "center" };
		for (int i = 0; i < neuronCount; i++)
		{
			list.Add(new Neuron
			{
				Id = i.ToString(),
				Role = ((i < 100) ? array[i % array.Length] : "other"),
				Type = ((i % 5 == 0) ? "sensory" : ((i % 7 == 0) ? "descending" : "inter")),
				Side = array2[i % array2.Length],
				X = (float)random.NextDouble() * 1000f,
				Y = (float)random.NextDouble() * 1000f,
				Z = (float)random.NextDouble() * 1000f
			});
		}
		for (int j = 0; j < neuronCount; j++)
		{
			for (int k = 0; k < synapsesPerNeuron; k++)
			{
				int num = random.Next(neuronCount);
				if (num != j)
				{
					float weight = (float)(random.NextDouble() * 12.0 - 3.0);
					list2.Add(new Edge(j, num, weight));
				}
			}
		}
		sim.Initialize(list, list2);
	}
}

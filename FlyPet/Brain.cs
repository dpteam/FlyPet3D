using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeuroFlyEngine;

namespace FlyPet;

internal sealed class Brain : IDisposable
{
	public sealed record Input(float Left, float Right, float Forward, bool Sleep, float Threat, bool Paused = false, float ThreatRight = -1f);

	public sealed record Output(BrainSignals Signals, int Neurons, int Edges, long Spikes, int BioMs, float Hz, double StepMs, int LoomCount, int SteeringCount, int ForwardCount);

	private Input _input = new Input(0f, 0f, 0f, Sleep: false, 0f);

	private Output _output = new Output(default(BrainSignals), 0, 0, 0L, 0, 0f, 0.0, 0, 0, 0);

	private string _status = "Открываем карту мозга…";

	private readonly CancellationTokenSource _stop = new CancellationTokenSource();

	public Output Snapshot => Volatile.Read(ref _output);

	public string Status => Volatile.Read(ref _status);

	public Task Completion { get; }

	public void Send(Input value)
	{
		Volatile.Write(ref _input, value);
	}

	public Brain(string folder)
	{
		Completion = Task.Run(async delegate
		{
			try
			{
				string[] array = new string[3] { "classification.csv.gz", "connections_princeton.csv.gz", "consolidated_cell_types.csv.gz" };
				string[] array2 = array;
				foreach (string text in array2)
				{
					if (!RawConnectomeLoader.HasData(PathOf(text)))
					{
						throw new FileNotFoundException("Не найден " + text + ". Положите файл рядом с проектом.");
					}
				}
				var (list, list2) = RawConnectomeLoader.LoadFromGz(PathOf("classification.csv.gz"), PathOf("connections_princeton.csv.gz"), null, delegate(string s, int p)
				{
					Volatile.Write(ref _status, $"{s} · {p}%");
				});
				if (!_stop.IsCancellationRequested)
				{
					FlyBrainEngine engine = new FlyBrainEngine();
					engine.Sim.Initialize(list, list2);
					list.Clear();
					list2.Clear();
					Volatile.Write(ref _status, "Подключено к FlyWire");
					while (!_stop.IsCancellationRequested)
					{
						Input input = Volatile.Read(ref _input);
						if (input.Paused)
						{
							await Task.Delay(40, _stop.Token);
						}
						else
						{
							engine.Sim.ActivityScale = (input.Sleep ? 0.18f : 0.8f);
							engine.Sim.SensoryGate = (input.Sleep ? 0.2f : 1f);
							engine.SetLooming(input.Threat, (input.ThreatRight < 0f) ? (input.Threat * 0.35f) : input.ThreatRight);
							engine.SetAttraction(input.Left, input.Right, input.Forward, input.Sleep ? 0f : 0.15f);
							Stopwatch stopwatch = Stopwatch.StartNew();
							BrainSignals signals = engine.Update(0.016f);
							stopwatch.Stop();
							LIFSim sim = engine.Sim;
							Volatile.Write(ref _output, new Output(signals, sim.N, sim.SynapseCount, sim.TotalSpikes, sim.SimMs, sim.RatePop, stopwatch.Elapsed.TotalMilliseconds, sim.LoomLeft.Count + sim.LoomRight.Count, sim.DnaL.Count + sim.DnaR.Count, sim.Fwd.Count));
							await Task.Delay(Math.Max(1, 40 - (int)stopwatch.ElapsedMilliseconds), _stop.Token);
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex2)
			{
				Exception ex3 = ex2;
				Volatile.Write(ref _status, "Ошибка: " + ex3.Message);
			}
		});
		string PathOf(string name)
		{
			return Path.Combine(folder, name);
		}
	}

	public void Dispose()
	{
		_stop.Cancel();
	}
}

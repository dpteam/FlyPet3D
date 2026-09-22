using System;

namespace NeuroFlyEngine;

public class FlyBrainEngine
{
	private double _remainingMs;

	public LIFSim Sim { get; } = new LIFSim();

	public SignalBuilder SignalsBuilder { get; } = new SignalBuilder();

	public BrainSignals CurrentSignals { get; private set; }

	public bool IsLoaded => Sim.N > 0;

	public BrainSignals Update(float dt, bool holdingSteeringTarget = false)
	{
		if (!IsLoaded)
		{
			return default(BrainSignals);
		}
		if (!ZFloat.IsFinite(dt) || dt <= 0f)
		{
			return CurrentSignals;
		}
		_remainingMs += (double)dt * 1000.0;
		int num = (int)Math.Min(Math.Floor(_remainingMs + 1E-05), 50.0);
		_remainingMs -= num;
		if (num == 0)
		{
			return CurrentSignals;
		}
		Sim.Step(num);
		CurrentSignals = SignalsBuilder.Make(Sim, (float)num / 1000f, holdingSteeringTarget);
		return CurrentSignals;
	}

	public void SetLooming(float left, float right)
	{
		Sim.LoomL = left;
		Sim.LoomR = right;
	}

	public void SetAttraction(float left, float right, float forward, float arousal)
	{
		Sim.AttractL = left;
		Sim.AttractR = right;
		Sim.AttractFwd = forward;
		Sim.AttractArousal = arousal;
	}
}

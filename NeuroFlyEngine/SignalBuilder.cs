using System;

namespace NeuroFlyEngine;

public class SignalBuilder
{
	private float _dnaBaseline;

	private bool _primed;

	public BrainSignals Make(LIFSim sim, float dt, bool holdSteering)
	{
		BrainSignals result = default(BrainSignals);
		if (sim == null)
		{
			return result;
		}
		float num = sim.RateDNaL - sim.RateDNaR;
		if (!_primed)
		{
			_dnaBaseline = num;
			_primed = true;
		}
		float num2 = (holdSteering ? 20f : 8f);
		_dnaBaseline += (num - _dnaBaseline) * Math.Min(1f, dt / num2);
		result.Escape = sim.ConsumeGF();
		result.Nervous = ZMath.Clamp(sim.RateLoom / 80f, 0f, 1f);
		result.TurnBias = ZMath.Clamp((num - _dnaBaseline) * 0.04f, -1f, 1f);
		result.Backward = sim.RateMDN > 8f;
		result.WalkDrive = ZMath.Clamp(sim.RateFwd / 10f, 0f, 1.3f);
		result.GroomDrive = sim.RateGroom / 8f;
		result.WingDrive = ZMath.Clamp(sim.RateEscW / 10f, 0f, 1.3f);
		result.Arousal = ZMath.Clamp(sim.RatePop / 20f, 0f, 1f);
		result.Tempo = 1f;
		result.Sleep = false;
		return result;
	}
}

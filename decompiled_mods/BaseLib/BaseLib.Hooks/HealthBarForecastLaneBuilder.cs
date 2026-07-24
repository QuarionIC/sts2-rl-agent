using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Combat;

namespace BaseLib.Hooks;

public sealed class HealthBarForecastLaneBuilder
{
	public HealthBarForecastSequenceBuilder Sequence { get; }

	public HealthBarForecastLaneBuilder(HealthBarForecastSequenceBuilder sequence, Color color, HealthBarForecastDirection direction, Color? overlaySelfModulate = null, bool affectsHpLabel = true)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		_003Ccolor_003EP = color;
		_003Cdirection_003EP = direction;
		_003CoverlaySelfModulate_003EP = overlaySelfModulate;
		_003CaffectsHpLabel_003EP = affectsHpLabel;
		Sequence = sequence;
		base._002Ector();
	}

	public HealthBarForecastLaneBuilder Add(int amount, int order, Material? overlayMaterial)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		Sequence.Add(amount, _003Ccolor_003EP, _003Cdirection_003EP, order, overlayMaterial, _003CoverlaySelfModulate_003EP, _003CaffectsHpLabel_003EP);
		return this;
	}

	public HealthBarForecastLaneBuilder Add(int amount, int order = 0)
	{
		return Add(amount, order, null);
	}

	public HealthBarForecastLaneBuilder AddRange(IEnumerable<int> amounts, int order, Material? overlayMaterial)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		Sequence.AddRange(amounts, _003Ccolor_003EP, _003Cdirection_003EP, order, overlayMaterial, _003CoverlaySelfModulate_003EP, _003CaffectsHpLabel_003EP);
		return this;
	}

	public HealthBarForecastLaneBuilder AddRange(IEnumerable<int> amounts, int order = 0)
	{
		return AddRange(amounts, order, null);
	}

	public HealthBarForecastLaneBuilder AtSideTurnStart(CombatSide triggerSide, params int[] amounts)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		int order = HealthBarForecastOrder.ForSideTurnStart(Sequence.Context.Creature, triggerSide);
		Sequence.AddRange(amounts, _003Ccolor_003EP, _003Cdirection_003EP, order, null, _003CoverlaySelfModulate_003EP, _003CaffectsHpLabel_003EP);
		return this;
	}

	public HealthBarForecastLaneBuilder AtSideTurnEnd(CombatSide triggerSide, params int[] amounts)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		int order = HealthBarForecastOrder.ForSideTurnEnd(Sequence.Context.Creature, triggerSide);
		Sequence.AddRange(amounts, _003Ccolor_003EP, _003Cdirection_003EP, order, null, _003CoverlaySelfModulate_003EP, _003CaffectsHpLabel_003EP);
		return this;
	}

	public HealthBarForecastLaneBuilder ThenFromRight(Color nextColor)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return Sequence.FromRight(nextColor, null);
	}

	public HealthBarForecastLaneBuilder ThenFromLeft(Color nextColor)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return Sequence.FromLeft(nextColor, null);
	}

	public IReadOnlyList<HealthBarForecastSegment> Build()
	{
		return Sequence.Build();
	}
}

using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Combat;

namespace BaseLib.Hooks;

public sealed class HealthBarForecastSequenceBuilder
{
	private readonly List<HealthBarForecastSegment> _segments = new List<HealthBarForecastSegment>();

	public HealthBarForecastContext Context { get; }

	public HealthBarForecastSequenceBuilder(HealthBarForecastContext context)
	{
		Context = context;
		base._002Ector();
	}

	public HealthBarForecastSequenceBuilder Add(int amount, Color color, HealthBarForecastDirection direction, int order, Material? overlayMaterial)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return Add(amount, color, direction, order, overlayMaterial, null);
	}

	public HealthBarForecastSequenceBuilder Add(int amount, Color color, HealthBarForecastDirection direction, int order, Material? overlayMaterial, Color? overlaySelfModulate)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return Add(amount, color, direction, order, overlayMaterial, overlaySelfModulate, affectsHpLabel: true);
	}

	public HealthBarForecastSequenceBuilder Add(int amount, Color color, HealthBarForecastDirection direction, int order, Material? overlayMaterial, Color? overlaySelfModulate, bool affectsHpLabel)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		if (amount <= 0)
		{
			return this;
		}
		HealthBarForecastSegment healthBarForecastSegment = new HealthBarForecastSegment(amount, color, direction, order, overlayMaterial, overlaySelfModulate, HealthBarForecastLeftOriginLayout.Chained, 0, affectsHpLabel);
		if (_segments.Count > 0)
		{
			List<HealthBarForecastSegment> segments = _segments;
			HealthBarForecastSegment healthBarForecastSegment2 = segments[segments.Count - 1];
			if (CanMerge(healthBarForecastSegment2, healthBarForecastSegment))
			{
				List<HealthBarForecastSegment> segments2 = _segments;
				segments2[segments2.Count - 1] = healthBarForecastSegment2 with
				{
					Amount = healthBarForecastSegment2.Amount + healthBarForecastSegment.Amount
				};
				return this;
			}
		}
		_segments.Add(healthBarForecastSegment);
		return this;
	}

	public HealthBarForecastSequenceBuilder Add(int amount, Color color, HealthBarForecastDirection direction, int order = 0)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return Add(amount, color, direction, order, null, null);
	}

	public HealthBarForecastSequenceBuilder AddRange(IEnumerable<int> amounts, Color color, HealthBarForecastDirection direction, int order, Material? overlayMaterial)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return AddRange(amounts, color, direction, order, overlayMaterial, null);
	}

	public HealthBarForecastSequenceBuilder AddRange(IEnumerable<int> amounts, Color color, HealthBarForecastDirection direction, int order, Material? overlayMaterial, Color? overlaySelfModulate)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return AddRange(amounts, color, direction, order, overlayMaterial, overlaySelfModulate, affectsHpLabel: true);
	}

	public HealthBarForecastSequenceBuilder AddRange(IEnumerable<int> amounts, Color color, HealthBarForecastDirection direction, int order, Material? overlayMaterial, Color? overlaySelfModulate, bool affectsHpLabel)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		ArgumentNullException.ThrowIfNull(amounts, "amounts");
		foreach (int amount in amounts)
		{
			Add(amount, color, direction, order, overlayMaterial, overlaySelfModulate, affectsHpLabel);
		}
		return this;
	}

	public HealthBarForecastSequenceBuilder AddRange(IEnumerable<int> amounts, Color color, HealthBarForecastDirection direction, int order = 0)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return AddRange(amounts, color, direction, order, null, null);
	}

	public HealthBarForecastSequenceBuilder AddSideTurnStart(CombatSide triggerSide, Color color, HealthBarForecastDirection direction, params int[] amounts)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		return AddRange(amounts, color, direction, HealthBarForecastOrder.ForSideTurnStart(Context.Creature, triggerSide));
	}

	public HealthBarForecastSequenceBuilder AddSideTurnEnd(CombatSide triggerSide, Color color, HealthBarForecastDirection direction, params int[] amounts)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		return AddRange(amounts, color, direction, HealthBarForecastOrder.ForSideTurnEnd(Context.Creature, triggerSide));
	}

	public HealthBarForecastLaneBuilder FromRight(Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return FromRight(color, null);
	}

	public HealthBarForecastLaneBuilder FromRight(Color color, Color? overlaySelfModulate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return FromRight(color, overlaySelfModulate, affectsHpLabel: true);
	}

	public HealthBarForecastLaneBuilder FromRight(Color color, Color? overlaySelfModulate, bool affectsHpLabel)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return new HealthBarForecastLaneBuilder(this, color, HealthBarForecastDirection.FromRight, overlaySelfModulate, affectsHpLabel);
	}

	public HealthBarForecastLaneBuilder FromLeft(Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return FromLeft(color, null);
	}

	public HealthBarForecastLaneBuilder FromLeft(Color color, Color? overlaySelfModulate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return FromLeft(color, overlaySelfModulate, affectsHpLabel: true);
	}

	public HealthBarForecastLaneBuilder FromLeft(Color color, Color? overlaySelfModulate, bool affectsHpLabel)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return new HealthBarForecastLaneBuilder(this, color, HealthBarForecastDirection.FromLeft, overlaySelfModulate, affectsHpLabel);
	}

	public IReadOnlyList<HealthBarForecastSegment> Build()
	{
		if (_segments.Count != 0)
		{
			return _segments.ToArray();
		}
		return Array.Empty<HealthBarForecastSegment>();
	}

	private static bool CanMerge(HealthBarForecastSegment left, HealthBarForecastSegment right)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		if (left.Color == right.Color && left.Direction == right.Direction && left.Order == right.Order)
		{
			Color? overlaySelfModulate = left.OverlaySelfModulate;
			Color? overlaySelfModulate2 = right.OverlaySelfModulate;
			if (overlaySelfModulate.HasValue == overlaySelfModulate2.HasValue && (!overlaySelfModulate.HasValue || overlaySelfModulate.GetValueOrDefault() == overlaySelfModulate2.GetValueOrDefault()) && left.LeftOriginLayout == right.LeftOriginLayout && left.LeftExclusiveZGroup == right.LeftExclusiveZGroup && left.AffectsHpLabel == right.AffectsHpLabel)
			{
				return left.OverlayMaterial == right.OverlayMaterial;
			}
		}
		return false;
	}
}

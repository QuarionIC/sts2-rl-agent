using System;
using System.Collections.Generic;
using Godot;

namespace BaseLib.Hooks;

public static class HealthBarForecasts
{
	public static HealthBarForecastSequenceBuilder For(HealthBarForecastContext context)
	{
		return new HealthBarForecastSequenceBuilder(context);
	}

	public static HealthBarForecastLaneBuilder FromRight(HealthBarForecastContext context, Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return FromRight(context, color, null);
	}

	public static HealthBarForecastLaneBuilder FromRight(HealthBarForecastContext context, Color color, Color? overlaySelfModulate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return FromRight(context, color, overlaySelfModulate, affectsHpLabel: true);
	}

	public static HealthBarForecastLaneBuilder FromRight(HealthBarForecastContext context, Color color, Color? overlaySelfModulate, bool affectsHpLabel)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return new HealthBarForecastLaneBuilder(For(context), color, HealthBarForecastDirection.FromRight, overlaySelfModulate, affectsHpLabel);
	}

	public static HealthBarForecastLaneBuilder FromLeft(HealthBarForecastContext context, Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return FromLeft(context, color, null);
	}

	public static HealthBarForecastLaneBuilder FromLeft(HealthBarForecastContext context, Color color, Color? overlaySelfModulate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return FromLeft(context, color, overlaySelfModulate, affectsHpLabel: true);
	}

	public static HealthBarForecastLaneBuilder FromLeft(HealthBarForecastContext context, Color color, Color? overlaySelfModulate, bool affectsHpLabel)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return new HealthBarForecastLaneBuilder(For(context), color, HealthBarForecastDirection.FromLeft, overlaySelfModulate, affectsHpLabel);
	}

	public static IEnumerable<HealthBarForecastSegment> Single(int amount, Color color, HealthBarForecastDirection direction, int order, Material? overlayMaterial)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return Single(amount, color, direction, order, overlayMaterial, null);
	}

	public static IEnumerable<HealthBarForecastSegment> Single(int amount, Color color, HealthBarForecastDirection direction, int order, Material? overlayMaterial, Color? overlaySelfModulate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return Single(amount, color, direction, order, overlayMaterial, overlaySelfModulate, affectsHpLabel: true);
	}

	public static IEnumerable<HealthBarForecastSegment> Single(int amount, Color color, HealthBarForecastDirection direction, int order, Material? overlayMaterial, Color? overlaySelfModulate, bool affectsHpLabel)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		if (amount <= 0)
		{
			return Array.Empty<HealthBarForecastSegment>();
		}
		return new _003C_003Ez__ReadOnlySingleElementList<HealthBarForecastSegment>(new HealthBarForecastSegment(amount, color, direction, order, overlayMaterial, overlaySelfModulate, HealthBarForecastLeftOriginLayout.Chained, 0, affectsHpLabel));
	}

	public static IEnumerable<HealthBarForecastSegment> Single(int amount, Color color, HealthBarForecastDirection direction, int order = 0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return Single(amount, color, direction, order, null, null);
	}
}

using Godot;

namespace BaseLib.Hooks;

public readonly record struct HealthBarForecastSegment(int Amount, Color Color, HealthBarForecastDirection Direction, int Order, Material? OverlayMaterial, Color? OverlaySelfModulate = null, HealthBarForecastLeftOriginLayout LeftOriginLayout = HealthBarForecastLeftOriginLayout.Chained, int LeftExclusiveZGroup = 0, bool AffectsHpLabel = true)
{
	public HealthBarForecastSegment(int Amount, Color Color, HealthBarForecastDirection Direction, int Order, Material? OverlayMaterial, Color? OverlaySelfModulate = null, HealthBarForecastLeftOriginLayout LeftOriginLayout = HealthBarForecastLeftOriginLayout.Chained, int LeftExclusiveZGroup = 0, bool AffectsHpLabel = true)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		this.Amount = Amount;
		this.Color = Color;
		this.Direction = Direction;
		this.Order = Order;
		this.OverlayMaterial = OverlayMaterial;
		this.OverlaySelfModulate = OverlaySelfModulate;
		this.LeftOriginLayout = LeftOriginLayout;
		this.LeftExclusiveZGroup = LeftExclusiveZGroup;
		this.AffectsHpLabel = AffectsHpLabel;
	}

	public HealthBarForecastSegment(int amount, Color color, HealthBarForecastDirection direction, int order = 0)
		: this(amount, color, direction, order, null, null)
	{
	}//IL_0002: Unknown result type (might be due to invalid IL or missing references)


	public HealthBarForecastSegment(int amount, Color color, HealthBarForecastDirection direction, int order, Material? overlayMaterial)
		: this(amount, color, direction, order, overlayMaterial, null)
	{
	}//IL_0002: Unknown result type (might be due to invalid IL or missing references)


	public HealthBarForecastSegment(int amount, Color color, HealthBarForecastDirection direction, int order, Material? overlayMaterial, Color? overlaySelfModulate)
		: this(amount, color, direction, order, overlayMaterial, overlaySelfModulate, HealthBarForecastLeftOriginLayout.Chained)
	{
	}//IL_0002: Unknown result type (might be due to invalid IL or missing references)


	public HealthBarForecastSegment(int amount, Color color, HealthBarForecastDirection direction, int order, Material? overlayMaterial, Color? overlaySelfModulate, HealthBarForecastLeftOriginLayout leftOriginLayout)
		: this(amount, color, direction, order, overlayMaterial, overlaySelfModulate, leftOriginLayout, 0)
	{
	}//IL_0002: Unknown result type (might be due to invalid IL or missing references)


	public HealthBarForecastSegment(int amount, Color color, HealthBarForecastDirection direction, int order, Material? overlayMaterial, Color? overlaySelfModulate, HealthBarForecastLeftOriginLayout leftOriginLayout, int leftExclusiveZGroup)
		: this(amount, color, direction, order, overlayMaterial, overlaySelfModulate, leftOriginLayout, leftExclusiveZGroup, AffectsHpLabel: true)
	{
	}//IL_0002: Unknown result type (might be due to invalid IL or missing references)

}

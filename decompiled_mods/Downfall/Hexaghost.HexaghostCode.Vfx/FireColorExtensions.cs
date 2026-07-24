using Godot;

namespace Hexaghost.HexaghostCode.Vfx;

public static class FireColorExtensions
{
	public static Color ToColor(this FireColor fireColor)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		return (Color)(fireColor switch
		{
			FireColor.Red => new Color(4288093183u), 
			FireColor.Green => new Color(2550095871u), 
			FireColor.Blue => new Color(1895825407u), 
			FireColor.Yellow => new Color(1f, 0.9f, 0.1f, 1f), 
			FireColor.Pink => new Color(4285726719u), 
			FireColor.Orange => new Color(1f, 0.5f, 0.1f, 1f), 
			_ => Colors.White, 
		});
	}
}

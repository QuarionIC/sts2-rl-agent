using System;
using Godot;

namespace BaseLib.Utils;

public static class ShaderUtils
{
	private const string HsvShaderPath = "res://shaders/hsv.gdshader";

	private const string DoomBarShaderPath = "res://scenes/combat/doom_bar.gdshader";

	private static NoiseTexture2D? _vanillaDoomBarNoiseTexture;

	private static Shader? GameHsvShader
	{
		get
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Expected O, but got Unknown
			Shader obj = GD.Load<Shader>("res://shaders/hsv.gdshader");
			return (Shader)((obj != null) ? ((Resource)obj).Duplicate(false) : null);
		}
	}

	private static Shader? GameDoomBarShader
	{
		get
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Expected O, but got Unknown
			Shader obj = GD.Load<Shader>("res://scenes/combat/doom_bar.gdshader");
			return (Shader)((obj != null) ? ((Resource)obj).Duplicate(false) : null);
		}
	}

	private static NoiseTexture2D VanillaDoomBarNoiseTexture => _vanillaDoomBarNoiseTexture ?? (_vanillaDoomBarNoiseTexture = CreateVanillaDoomBarNoiseTexture());

	public static ShaderMaterial GenerateHsv(float h, float s, float v)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		Shader gameHsvShader = GameHsvShader;
		if (gameHsvShader == null)
		{
			throw new InvalidOperationException("Failed to load HSV shader (res://shaders/hsv.gdshader).");
		}
		ShaderMaterial val = new ShaderMaterial
		{
			Shader = gameHsvShader
		};
		val.SetShaderParameter(StringName.op_Implicit("h"), Variant.op_Implicit(h));
		val.SetShaderParameter(StringName.op_Implicit("s"), Variant.op_Implicit(s));
		val.SetShaderParameter(StringName.op_Implicit("v"), Variant.op_Implicit(v));
		return val;
	}

	public static ShaderMaterial CreateDoomBarShaderMaterial(GradientTexture1D gradientTexture)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		ArgumentNullException.ThrowIfNull(gradientTexture, "gradientTexture");
		Shader gameDoomBarShader = GameDoomBarShader;
		if (gameDoomBarShader == null)
		{
			throw new InvalidOperationException("Failed to load doom bar shader (res://scenes/combat/doom_bar.gdshader).");
		}
		ShaderMaterial val = new ShaderMaterial
		{
			Shader = gameDoomBarShader
		};
		val.SetShaderParameter(StringName.op_Implicit("noise_tex"), Variant.op_Implicit((GodotObject)(object)VanillaDoomBarNoiseTexture));
		val.SetShaderParameter(StringName.op_Implicit("gradient_tex"), Variant.op_Implicit((GodotObject)(object)gradientTexture));
		return val;
	}

	public static GradientTexture1D CreateVanillaDoomBarGradientTexture()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		Gradient val = new Gradient();
		val.AddPoint(0f, new Color(0.300863f, 0.162626f, 0.528347f, 1f));
		val.AddPoint(0.514583f, new Color(0.513726f, 0.254902f, 0.505882f, 1f));
		val.AddPoint(1f, new Color(0.354657f, 0.0421873f, 0.437114f, 1f));
		return new GradientTexture1D
		{
			Gradient = val
		};
	}

	public static NoiseTexture2D CreateVanillaDoomBarNoiseTexture()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		FastNoiseLite noise = new FastNoiseLite
		{
			NoiseType = (NoiseTypeEnum)3,
			Frequency = 0.0383f
		};
		return new NoiseTexture2D
		{
			Noise = (Noise)(object)noise
		};
	}
}

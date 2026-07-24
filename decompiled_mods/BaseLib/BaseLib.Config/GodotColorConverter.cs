using System;
using System.ComponentModel;
using System.Globalization;
using Godot;

namespace BaseLib.Config;

public class GodotColorConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		if (!(sourceType == typeof(string)))
		{
			return base.CanConvertFrom(context, sourceType);
		}
		return true;
	}

	public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
	{
		if (!(destinationType == typeof(string)))
		{
			return base.CanConvertTo(context, destinationType);
		}
		return true;
	}

	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		if (!(value is string text))
		{
			return base.ConvertFrom(context, culture, value);
		}
		string text2 = text.Trim('[', ']');
		string[] array = text2.Split(',');
		if (array.Length == 4 && float.TryParse(array[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && float.TryParse(array[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result2) && float.TryParse(array[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var result3) && float.TryParse(array[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var result4))
		{
			return (object)new Color(result, result2, result3, result4);
		}
		throw new FormatException($"String '{value}' is not in a valid format. Expected format (as string): [r, g, b, a].");
	}

	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		if (destinationType == typeof(string) && value is Color val)
		{
			return $"[{val.R.ToString(CultureInfo.InvariantCulture)}, {val.G.ToString(CultureInfo.InvariantCulture)}, {val.B.ToString(CultureInfo.InvariantCulture)}, {val.A.ToString(CultureInfo.InvariantCulture)}]";
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}
}

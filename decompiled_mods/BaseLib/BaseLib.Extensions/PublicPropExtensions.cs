using System;
using MegaCrit.Sts2.Core.ValueProps;

namespace BaseLib.Extensions;

public static class PublicPropExtensions
{
	public static bool IsPoweredAttack_(this ValueProp props)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (((Enum)props).HasFlag((Enum)(object)(ValueProp)8))
		{
			return !((Enum)props).HasFlag((Enum)(object)(ValueProp)4);
		}
		return false;
	}

	public static bool IsPoweredCardOrMonsterMoveBlock_(this ValueProp props)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (((Enum)props).HasFlag((Enum)(object)(ValueProp)8))
		{
			return !((Enum)props).HasFlag((Enum)(object)(ValueProp)4);
		}
		return false;
	}

	public static bool IsCardOrMonsterMove_(this ValueProp props)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return ((Enum)props).HasFlag((Enum)(object)(ValueProp)8);
	}
}

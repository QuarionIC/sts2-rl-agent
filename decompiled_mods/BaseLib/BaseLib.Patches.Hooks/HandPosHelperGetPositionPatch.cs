using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;

namespace BaseLib.Patches.Hooks;

[HarmonyPatch]
internal static class HandPosHelperGetPositionPatch
{
	private static float GetInferredHalfSpread(int handSize)
	{
		float num = (float)(Math.Min(handSize, 14) - 10) / 4f;
		num = Mathf.Clamp(num, 0f, 1f);
		return Mathf.Lerp(610f, 690f, num);
	}

	[HarmonyPatch(typeof(HandPosHelper), "GetPosition")]
	[HarmonyPrefix]
	private static bool GetPosition(int handSize, int cardIndex, ref Vector2 __result)
	{
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		if (handSize <= 10)
		{
			return true;
		}
		if (cardIndex < 0 || cardIndex >= handSize)
		{
			throw new ArgumentOutOfRangeException("cardIndex", $"Card index {cardIndex} is outside hand size {handSize}.");
		}
		float inferredHalfSpread = GetInferredHalfSpread(handSize);
		float num = Math.Max(72f, 88f - (float)(handSize - 10) * 1.5f);
		float num2 = ((handSize <= 1) ? 0f : (2f * (float)cardIndex / ((float)handSize - 1f) - 1f));
		float num3 = inferredHalfSpread * num2;
		float num4 = Math.Min(18f, -64f + num * num2 * num2);
		__result = new Vector2(num3, num4);
		return false;
	}

	[HarmonyPatch(typeof(HandPosHelper), "GetAngle")]
	[HarmonyPrefix]
	private static bool GetAngle(int handSize, int cardIndex, ref float __result)
	{
		if (handSize <= 10)
		{
			return true;
		}
		if (cardIndex < 0 || cardIndex >= handSize)
		{
			throw new ArgumentOutOfRangeException("cardIndex", $"Card index {cardIndex} is outside hand size {handSize}.");
		}
		float inferredHalfSpread = GetInferredHalfSpread(handSize);
		float num = Math.Max(72f, 88f - (float)(handSize - 10) * 1.5f);
		float num2 = ((handSize <= 1) ? 0f : (2f * (float)cardIndex / ((float)handSize - 1f) - 1f));
		float num3 = 2f * num * num2;
		float num4 = Math.Max(1f, inferredHalfSpread);
		float num5 = Mathf.RadToDeg(Mathf.Atan2(num3, num4));
		__result = Mathf.Clamp(num5, -18f, 18f);
		return false;
	}

	[HarmonyPatch(typeof(HandPosHelper), "GetScale")]
	[HarmonyPrefix]
	private static bool GetScale(int handSize, ref Vector2 __result)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		if (handSize <= 10)
		{
			return true;
		}
		float val = 0.64f * MathF.Pow(0.95f, handSize - 11);
		__result = Vector2.One * Math.Max(0.48f, val);
		return false;
	}
}

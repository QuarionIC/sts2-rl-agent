using System;
using System.Collections.Generic;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace Hexaghost.HexaghostCode.Ghostflames.Intents;

public class CustomAttackIntent(Func<int> damage, Func<int> repeat) : CustomIntent()
{
	public override IntentType IntentType => (IntentType)0;

	protected override string IntentSpritePath => GetTieredSpritePath(damage());

	private static string GetTieredAnimation(int damage)
	{
		if (damage < 20)
		{
			if (damage >= 5)
			{
				if (damage < 10)
				{
					return "attack_2";
				}
				return "attack_3";
			}
			return "attack_1";
		}
		if (damage < 40)
		{
			return "attack_4";
		}
		return "attack_5";
	}

	private static string GetTieredSpritePath(int damage)
	{
		if (damage < 20)
		{
			if (damage >= 5)
			{
				if (damage < 10)
				{
					return "atlases/intent_atlas.sprites/attack/intent_attack_2.tres";
				}
				return "atlases/intent_atlas.sprites/attack/intent_attack_3.tres";
			}
			return "atlases/intent_atlas.sprites/attack/intent_attack_1.tres";
		}
		if (damage < 40)
		{
			return "atlases/intent_atlas.sprites/attack/intent_attack_4.tres";
		}
		return "atlases/intent_atlas.sprites/attack/intent_attack_5.tres";
	}

	public override string GetAnimation(IEnumerable<Creature> targets, Creature owner)
	{
		return GetTieredAnimation(damage());
	}

	public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		LocString val = new LocString("intents", "FORMAT_DAMAGE_MULTI");
		val.Add("Damage", (decimal)damage());
		val.Add("Repeat", (decimal)repeat());
		return val;
	}
}

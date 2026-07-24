using BaseLib.Abstracts;
using Downfall.DownfallCode.Abstract;
using Hermit.HermitCode.Core;
using Hermit.HermitCode.Patches;
using Hermit.HermitCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Rare;

public class DeadOnReplay : DownfallCardModifier
{
	public bool IsDeadOn
	{
		get
		{
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Invalid comparison between Unknown and I4
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Invalid comparison between Unknown and I4
			CardModel owner = ((CardModifier)this).Owner;
			PileType? obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				CardPile pile = owner.Pile;
				obj = ((pile != null) ? new PileType?(pile.Type) : ((PileType?)null));
			}
			PileType? val = obj;
			bool num = (int)val.GetValueOrDefault() == 2;
			bool flag = (int)val.GetValueOrDefault() == 5;
			bool flag2 = num && IsDeadOnInHand;
			bool flag3 = flag && WasThisPlayedDeadOn;
			if (((CardModifier)this).Owner != null)
			{
				return flag2 || flag3;
			}
			return false;
		}
	}

	private bool IsDeadOnInHand
	{
		get
		{
			if (((CardModifier)this).Owner != null)
			{
				return HermitCmd.IsDeadOnInCurrentHandState(((CardModifier)this).Owner);
			}
			return false;
		}
	}

	private bool WasThisPlayedDeadOn
	{
		get
		{
			if (DeadOnPatch.LastPlayed == ((CardModifier)this).Owner)
			{
				return DeadOnPatch.LastWasDeadOn;
			}
			return false;
		}
	}

	private int ModVal
	{
		get
		{
			int value = Value;
			CardModel owner = ((CardModifier)this).Owner;
			return value * ((owner == null || !owner.Owner.Creature.HasPower<SnipePower>()) ? 1 : 2);
		}
	}

	public override bool ShouldGlowGold => IsDeadOn;

	public int Value { get; set; } = 1;

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (card != ((CardModifier)this).Owner || !IsDeadOn)
		{
			return playCount;
		}
		return playCount + ModVal;
	}

	public override void ModifyDescription(Creature? target, ref string description)
	{
		LocString description2 = base.Description;
		((CardModifier)this).DynamicVars.AddTo(description2);
		description2.Add("Replay", (decimal)ModVal);
		description = description + "\n" + description2.GetFormattedText();
	}
}

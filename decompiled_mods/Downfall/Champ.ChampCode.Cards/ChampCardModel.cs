using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Enchantments;
using Champ.ChampCode.Extensions;
using Champ.ChampCode.Interfaces;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards;

public abstract class ChampCardModel : DownfallCardModel<Champ.ChampCode.Core.Champ>, IFinisherCard
{
	protected override bool ShouldGlowRedInternal
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			if (((CardModel)this).Tags.Contains(ChampTag.Finisher))
			{
				return ((CardModel)this).Owner.ChampStance().HasFinisher;
			}
			return false;
		}
	}

	protected override bool ShouldGlowGoldInternal
	{
		get
		{
			if (!(this is IBerserkerComboCard) || !((CardModel)this).Owner.ShouldBerserkerComboTrigger())
			{
				if (this is IDefensiveComboCard)
				{
					return ((CardModel)this).Owner.ShouldDefensiveComboTrigger();
				}
				return false;
			}
			return true;
		}
	}

	protected override bool IsPlayable
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			if (((CardModel)this).Tags.Contains(ChampTag.Finisher) && !((CardModel)this).Owner.ChampStance().HasFinisher)
			{
				return ((CardModel)this).Enchantment is Signature;
			}
			return true;
		}
	}

	protected ChampCardModel(int cost, CardType type, CardRarity rarity, TargetType targetType, bool showInCardLibrary = true, bool autoAdd = true)
		: base(cost, type, rarity, targetType, showInCardLibrary, autoAdd)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (this is IBerserkerComboCard)
		{
			((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampTip.Combo));
			((ConstructedCardModel)(object)this).WithBerserkerTip();
		}
		if (this is IDefensiveComboCard)
		{
			((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampTip.Combo));
			((ConstructedCardModel)(object)this).WithDefensiveTip();
		}
	}

	public virtual async Task FinisherEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await ChampCmd.PlayFinisher(ctx, cardPlay);
	}
}

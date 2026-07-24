using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using SlimeBoss.SlimeBossCode.Cards.Token;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Cards.Rare;

[Pool(typeof(SlimeBossCardPool))]
public class PrepareCrush : SlimeBossCardModel
{
	public PrepareCrush()
		: base(2, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithUpgradingCardTip<SlimeCrush>((Action<SlimeCrush, CardModel>)null);
		((ConstructedCardModel)(object)this).WithTip<StrengthPower>();
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithEnergy(3, 1);
		((ConstructedCardModel)(object)this).WithPower<StrengthNextTurnPower>(3, 2, showTooltip: false);
		((ConstructedCardModel)(object)this).WithPower<EnergyNextTurnPower>(3, 1, showTooltip: false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		CopyNextTurnPower copyNextTurnPower = await CommonActions.ApplySelf<CopyNextTurnPower>(ctx, (CardModel)(object)this, 1m, false);
		ICombatState combatState = ((CardModel)this).CombatState;
		SlimeCrush slimeCrush = ((combatState != null) ? combatState.CreateCard<SlimeCrush>(((CardModel)this).Owner) : null);
		if (slimeCrush != null && copyNextTurnPower != null)
		{
			if (((CardModel)this).IsUpgraded)
			{
				((CardModel)slimeCrush).UpgradeInternal();
			}
			copyNextTurnPower.Card = (CardModel?)(object)slimeCrush;
			await CommonActions.ApplySelf<EnergyNextTurnPower>(ctx, (CardModel)(object)this, false);
			await CommonActions.ApplySelf<StrengthNextTurnPower>(ctx, (CardModel)(object)this, false);
		}
	}
}

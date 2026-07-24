using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Guardian.GuardianCode.Cards.Token;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Guardian.GuardianCode.Cards.Uncommon;

[Pool(typeof(GuardianCardPool))]
public class SentryBlast : GuardianCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public SentryBlast()
		: base(0, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(5, 2);
		((ConstructedCardModel)this).WithUpgradingCardTip<SentryWave>((Action<SentryWave, CardModel>)null);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Stasis));
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		if (GuardianCmd.CanPutIntoStasis(((CardModel)this).Owner))
		{
			SentryWave card = ((CardModel)this).CombatState.CreateCard<SentryWave>(((CardModel)this).Owner);
			if (((CardModel)this).IsUpgraded)
			{
				CardCmd.Upgrade((CardModel)(object)card, (CardPreviewStyle)1);
			}
			await CardPileCmd.Add((CardModel)(object)card, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
			await GuardianCmd.PutIntoStasis((CardModel)(object)card, ctx, (AbstractModel)(object)this);
		}
	}
}

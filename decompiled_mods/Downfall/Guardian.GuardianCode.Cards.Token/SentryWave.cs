using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Guardian.GuardianCode.Cards.Uncommon;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Guardian.GuardianCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class SentryWave : GuardianCardModel
{
	public SentryWave()
		: base(0, (CardType)2, (CardRarity)7, (TargetType)2)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithPower<WeakPower>(1, 0);
		((ConstructedCardModel)(object)this).WithBrace(0, 2);
		((ConstructedCardModel)this).WithUpgradingCardTip<SentryBlast>((Action<SentryBlast, CardModel>)null);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Stasis));
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Apply<WeakPower>(ctx, (CardModel)(object)this, cardPlay, false);
		if (((CardModel)this).IsUpgraded)
		{
			await GuardianCmd.Brace(ctx, (CardModel)(object)this);
		}
		if (GuardianCmd.CanPutIntoStasis(((CardModel)this).Owner))
		{
			SentryBlast card = ((CardModel)this).CombatState.CreateCard<SentryBlast>(((CardModel)this).Owner);
			if (((CardModel)this).IsUpgraded)
			{
				CardCmd.Upgrade((CardModel)(object)card, (CardPreviewStyle)1);
			}
			await CardPileCmd.Add((CardModel)(object)card, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
			await GuardianCmd.PutIntoStasis((CardModel)(object)card, ctx, (AbstractModel)(object)this);
		}
	}
}

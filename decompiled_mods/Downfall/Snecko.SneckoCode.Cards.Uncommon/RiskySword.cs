using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.Events;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class RiskySword : SneckoCardModel, IAfterCardMuddled
{
	public RiskySword()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(8, 2);
		((ConstructedCardModel)this).WithVar("Increase", 8, 2);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoKeywords.Muddle));
	}

	public Task AfterCardMuddled(PlayerChoiceContext ctx, CardModel card, AbstractModel? source)
	{
		if ((object)card != this)
		{
			return Task.CompletedTask;
		}
		((DynamicVar)((CardModel)this).DynamicVars.Damage).UpgradeValueBy(((CardModel)this).DynamicVars["Increase"].BaseValue);
		return Task.CompletedTask;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}

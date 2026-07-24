using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class Flurry : SneckoCardModel
{
	public Flurry()
		: base(2, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(13, 4);
		((ConstructedCardModel)this).WithCards(3, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		foreach (CardModel item in IEnumerableExtensions.TakeRandom<CardModel>(from c in ((CardModel)this).Owner.GetDraw()
			where c.IsUpgradable
			select c, ((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue, ((CardModel)this).Owner.RunState.Rng.CombatCardSelection).ToList())
		{
			CardCmd.Upgrade(item, (CardPreviewStyle)1);
			CardCmd.Preview(item, 1.2f, (CardPreviewStyle)1);
		}
	}
}

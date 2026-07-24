using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Rare;

[Pool(typeof(ChampCardPool))]
public class Devastate : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	public Devastate()
		: base(5, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(6, 3);
		((ConstructedCardModel)(object)this).WithRepeat(3);
		((ConstructedCardModel)this).WithEnergyTip();
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampTip.Finisher));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, ((DynamicVar)((CardModel)this).DynamicVars.Repeat).IntValue, (string)null, (string)null, (string)null).Execute(ctx);
	}

	public override Task AfterCardEnteredCombat(CardModel card)
	{
		if ((object)card != this || ((CardModel)this).IsClone)
		{
			return Task.CompletedTask;
		}
		ReduceCostBy(CombatManager.Instance.History.CardPlaysFinished.Count((CardPlayFinishedEntry e) => e.CardPlay.Card.Tags.Contains(ChampTag.Finisher) && e.CardPlay.Card.Owner == ((CardModel)this).Owner));
		return Task.CompletedTask;
	}

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		if (cardPlay.Card.Owner != ((CardModel)this).Owner || !cardPlay.Card.Tags.Contains(ChampTag.Finisher))
		{
			return Task.CompletedTask;
		}
		ReduceCostBy(1);
		return Task.CompletedTask;
	}

	private void ReduceCostBy(int amount)
	{
		((CardModel)this).EnergyCost.AddThisCombat(-amount, false);
	}
}

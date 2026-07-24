using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.CustomEnums;
using Hermit.HermitCode.Patches;
using Hermit.HermitCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Uncommon;

public sealed class Cheat : HermitCardModel, IHasDeadOnEffect
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Cheat()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCards(3, 2);
	}

	public Task DeadOnEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		return Task.CompletedTask;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		bool isDeadOn = DeadOnPatch.LastWasDeadOn;
		CardModel lastPlayed = DeadOnPatch.LastPlayed;
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		List<CardModel> list = PileTypeExtensions.GetPile((PileType)1, ((CardModel)this).Owner).Cards.Take(((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue).ToList();
		if (list.Count == 0)
		{
			return;
		}
		CardModel selected = (await CardSelectCmd.FromSimpleGrid(ctx, (IReadOnlyList<CardModel>)list, ((CardModel)this).Owner, new CardSelectorPrefs(DownfallCardSelectorPrefs.PlaySelectionPrompt, 1))).FirstOrDefault();
		if (selected != null)
		{
			if (isDeadOn)
			{
				await PowerCmd.Apply<CheatPower>(ctx, ((CardModel)this).Owner.Creature, 1m, ((CardModel)this).Owner.Creature, (CardModel)(object)this, true);
			}
			await CardCmd.AutoPlay(ctx, selected, (Creature)null, (AutoPlayType)1, false, false);
			DeadOnPatch.LastWasDeadOn = isDeadOn;
			DeadOnPatch.LastPlayed = lastPlayed;
		}
	}
}

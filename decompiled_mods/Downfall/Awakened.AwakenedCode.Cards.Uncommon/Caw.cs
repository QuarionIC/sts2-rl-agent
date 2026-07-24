using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Events;
using Awakened.AwakenedCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace Awakened.AwakenedCode.Cards.Uncommon;

[Pool(typeof(AwakenedCardPool))]
public class Caw : AwakenedCardModel, IChantable, IOnChant
{
	private static readonly LocString CawCawDialogue = new LocString("monsters", "DAMP_CULTIST.moves.INCANTATION.banter");

	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Occultpyromancer>();

	public bool HasChanted { get; set; }

	public Caw()
		: base(0, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(4, 1);
		((ConstructedCardModel)this).WithVar("Caw", 4, 1);
	}

	public async Task PlayChantEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await Task.CompletedTask;
	}

	public Task OnCardChanted(CardModel card, PlayerChoiceContext ctx, CardPlay cardPlay, bool firstTime)
	{
		if (card is Caw && card.Owner == ((CardModel)this).Owner)
		{
			((DynamicVar)((CardModel)this).DynamicVars.Damage).UpgradeValueBy(card.DynamicVars["Caw"].BaseValue);
		}
		return Task.CompletedTask;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, "event:/sfx/enemy/enemy_attacks/cultists/cultists_buff_damp", (string)null).Execute(ctx);
		TalkCmd.Play(CawCawDialogue, ((CardModel)this).Owner.Creature, (VfxColor)2, (VfxDuration)6);
	}
}

using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Extensions;
using Awakened.AwakenedCode.Piles;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Multiplayer;

[Pool(typeof(AwakenedCardPool))]
public class BookOfSecrets : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<GoofballMcgee>();

	public override CardMultiplayerConstraint MultiplayerConstraint => (CardMultiplayerConstraint)1;

	public BookOfSecrets()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithConjure();
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)2);
		((ConstructedCardModel)this).WithBlock(6, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		if (((CardModel)this).CombatState == null)
		{
			return;
		}
		AwakenedPile orInitSpellbook = AwakenedModel.GetOrInitSpellbook(((CardModel)this).Owner);
		CardModel nextSpell = orInitSpellbook.NextSpell;
		if (nextSpell == null)
		{
			return;
		}
		foreach (Creature item in from c in ((CardModel)this).CombatState.GetTeammatesOf(((CardModel)this).Owner.Creature)
			where c != null && c.IsAlive && c.IsPlayer
			select c)
		{
			Player player = item.Player;
			if (player != null && player != ((CardModel)this).Owner)
			{
				CardModel obj = nextSpell.CreateClone();
				obj._owner = player;
				await CardPileCmd.Add(obj, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
			}
		}
	}
}

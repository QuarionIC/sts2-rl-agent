using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Extensions;
using Hexaghost.HexaghostCode.Interfaces;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Ancient;

[Pool(typeof(HexaghostCardPool))]
public class Apocryphra : HexaghostCardModel, IHasAfterlifeEffect
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<GoofballMcgee>();

	public Apocryphra()
		: base(1, (CardType)1, (CardRarity)5, (TargetType)3)
	{
		((ConstructedCardModel)(object)this).WithAfterlife();
		((ConstructedCardModel)this).WithDamage(5, 2);
		((ConstructedCardModel)this).WithPower<SoulBurnPower>(5, 2);
	}

	public async Task AfterlifeEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await PowerCmd.Apply<SoulBurnPower>(ctx, (IEnumerable<Creature>)((CardModel)this).CombatState.HittableEnemies, DynamicVarSetExtensions.Power<SoulBurnPower>(((CardModel)this).DynamicVars).BaseValue, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
		await CardPileCmd.Add((CardModel)(object)this, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await AfterlifeEffect(ctx, cardPlay);
	}
}

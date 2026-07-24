using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Encode;
using Automaton.AutomatonCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class DazingPulse : AutomatonCardModel, IEncodable, ICompilable
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public IEnumerable<Encodable> Encodings => new global::_003C_003Ez__ReadOnlyArray<Encodable>(new Encodable[2]
	{
		new BlockEncode(),
		new DamageEncode()
	});

	public DazingPulse()
		: base(2, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithBlock(7, 2);
		((ConstructedCardModel)this).WithDamage(7, 2);
		((ConstructedCardModel)this).WithCards(2, 0);
		((ConstructedCardModel)(object)this).WithTip<Dazed>();
	}

	public Task OnCompile(PlayerChoiceContext context)
	{
		return DownfallCardCmd.GiveCards<Dazed>(((CardModel)this).Owner, (PileType)1, ((DynamicVar)((CardModel)this).DynamicVars.Cards).BaseValue, (CardPilePosition)3, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Dazed>?)null, (Player?)null);
	}
}

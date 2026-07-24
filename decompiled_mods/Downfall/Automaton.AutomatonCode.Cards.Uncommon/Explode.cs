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
using Downfall.DownfallCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class Explode : AutomatonCardModel, IEncodable, ICompilable
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public IEnumerable<Encodable> Encodings => new _003C_003Ez__ReadOnlySingleElementList<Encodable>(new SoulburnEncode());

	public Explode()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)3)
	{
		((ConstructedCardModel)this).WithCards(2, 0);
		((ConstructedCardModel)this).WithPower<SoulBurnPower>(15, 5);
		((ConstructedCardModel)(object)this).WithTip<Burn>();
	}

	public Task OnCompile(PlayerChoiceContext context)
	{
		return DownfallCardCmd.GiveCards<Burn>(((CardModel)this).Owner, (PileType)1, ((DynamicVar)((CardModel)this).DynamicVars.Cards).BaseValue, (CardPilePosition)3, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Burn>?)null, (Player?)null);
	}
}

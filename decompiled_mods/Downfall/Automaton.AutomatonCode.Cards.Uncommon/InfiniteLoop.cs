using System.Collections.Generic;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Encode;
using Automaton.AutomatonCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class InfiniteLoop : AutomatonCardModel, IEncodable, ICompilable
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public IEnumerable<Encodable> Encodings => new _003C_003Ez__ReadOnlySingleElementList<Encodable>(new DamageEncode());

	public InfiniteLoop()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(6, 0);
		((ConstructedCardModel)this).WithVar("Increase", 2, 2);
	}

	public async Task OnCompile(PlayerChoiceContext context)
	{
		CardModel obj = ((CardModel)this).CreateClone();
		obj.EnergyCost.AfterCardPlayedCleanup();
		obj.EnergyCost.EndOfTurnCleanup();
		((DynamicVar)obj.DynamicVars.Damage).UpgradeValueBy(((CardModel)this).DynamicVars["Increase"].BaseValue);
		obj.DynamicVars.FinalizeUpgrade();
		await CardPileCmd.AddGeneratedCardToCombat(obj, (PileType)2, ((CardModel)this).Owner, (CardPilePosition)1);
	}
}

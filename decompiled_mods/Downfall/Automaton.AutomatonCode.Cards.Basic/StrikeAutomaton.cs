using System.Collections.Generic;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Encode;
using Automaton.AutomatonCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Basic;

[Pool(typeof(AutomatonCardPool))]
public class StrikeAutomaton : AutomatonCardModel, IEncodable
{
	public bool CanPlayerEncode => false;

	public IEnumerable<Encodable> Encodings => new _003C_003Ez__ReadOnlySingleElementList<Encodable>(new DamageEncode());

	public StrikeAutomaton()
		: base(1, (CardType)1, (CardRarity)1, (TargetType)2)
	{
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)1 });
		((ConstructedCardModel)this).WithDamage(6, 3);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null).Execute(ctx);
	}
}

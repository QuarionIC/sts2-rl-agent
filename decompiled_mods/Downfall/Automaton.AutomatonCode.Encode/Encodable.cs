using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Token;
using BaseLib.Extensions;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Encode;

public abstract class Encodable
{
	public static readonly IEnumerable<Encodable> All = new global::_003C_003Ez__ReadOnlyArray<Encodable>(new Encodable[10]
	{
		new PowerEncode(),
		new BlockEncode(),
		new DamageEncode(),
		new StrengthEncode(),
		new WeakEncode(),
		new VulnerableEncode(),
		new PoisonEncode(),
		new SoulburnEncode(),
		new EnergyEncode(),
		new DazedEncode()
	});

	public abstract TargetType Target { get; }

	public abstract CardType Type { get; }

	private string Id => StringHelper.Slugify(GetType().Name);

	private LocString Description => new LocString("encode", TypePrefix.GetPrefix(GetType()) + Id + ".encode");

	public abstract DynamicVar FunctionDynamicVar { get; }

	public abstract Task OnPlay(AbstractModel model, PlayerChoiceContext ctx, Creature? target, CardPlay? cardPlay);

	public abstract DynamicVar DynamicVar(AbstractModel card);

	public virtual IEnumerable<IHoverTip> HoverTips(AbstractModel card)
	{
		return Array.Empty<IHoverTip>();
	}

	public LocString GetDescription(AbstractModel card)
	{
		LocString description = Description;
		CardModel val = (CardModel)(object)((card is CardModel) ? card : null);
		description.Add("IsOnCard", val != null && !(val is FunctionCard));
		description.Add("IsOnFunction", card is FunctionCard);
		description.Add("IsOnPower", card is PowerModel);
		card.GetDynamicVars().AddTo(description);
		return description;
	}

	public void ApplyEncode(FunctionCard functionCard, CardModel sourceCard)
	{
		DynamicVar obj = DynamicVar((AbstractModel)(object)functionCard);
		obj.BaseValue += DynamicVar((AbstractModel)(object)sourceCard).BaseValue;
	}
}

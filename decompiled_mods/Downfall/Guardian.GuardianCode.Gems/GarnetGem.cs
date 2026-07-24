using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Godot;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.DynamicVars;
using Guardian.GuardianCode.Events;
using Guardian.GuardianCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Guardian.GuardianCode.Gems;

public class GarnetGem : GemModel
{
	public override IEnumerable<IHoverTip> ExtraHoverTips => new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.FromPower<VulnerablePower>((int?)null));

	protected override IEnumerable<DynamicVar> CanonicalVars => new _003C_003Ez__ReadOnlySingleElementList<DynamicVar>((DynamicVar)(object)new GemVar(1m));

	public override Color GemColor => new Color(1560349183u);

	public override CardRarity Rarity => (CardRarity)3;

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay? cardPlay)
	{
		decimal num = GuardianHook.ModifyGemEffect(base.CombatState, this, ((DynamicVar)((CardModifier)this).DynamicVars.Gem()).BaseValue, base.Card);
		await PowerCmd.Apply<VulnerablePower>(ctx, (IEnumerable<Creature>)base.CombatState.Enemies, num, base.Player.Creature, (CardModel)null, false);
	}
}

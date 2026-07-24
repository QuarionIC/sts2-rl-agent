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
using MegaCrit.Sts2.Core.Models.Powers;

namespace Guardian.GuardianCode.Gems;

public class AmethystGem : GemModel
{
	public override IEnumerable<IHoverTip> ExtraHoverTips => new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.FromPower<StrengthPower>((int?)null));

	protected override IEnumerable<DynamicVar> CanonicalVars => new _003C_003Ez__ReadOnlySingleElementList<DynamicVar>((DynamicVar)(object)new GemVar(2m));

	public override Color GemColor => new Color(2768292351u);

	public override CardRarity Rarity => (CardRarity)3;

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay? cardPlay)
	{
		decimal num = GuardianHook.ModifyGemEffect(base.CombatState, this, ((DynamicVar)((CardModifier)this).DynamicVars.Gem()).BaseValue, base.Card);
		await PowerCmd.Apply<AmethystGemPower>(ctx, (IEnumerable<Creature>)base.CombatState.Enemies, num, base.Player.Creature, ((cardPlay != null) ? cardPlay.Card : null) ?? base.Card, false);
	}
}

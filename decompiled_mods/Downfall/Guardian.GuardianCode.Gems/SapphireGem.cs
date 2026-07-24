using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Godot;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.DynamicVars;
using Guardian.GuardianCode.Events;
using Guardian.GuardianCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Guardian.GuardianCode.Gems;

public class SapphireGem : GemModel
{
	public override IEnumerable<IHoverTip> ExtraHoverTips => new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.Static(GuardianTip.Brace, Array.Empty<DynamicVar>()));

	protected override IEnumerable<DynamicVar> CanonicalVars => new _003C_003Ez__ReadOnlySingleElementList<DynamicVar>((DynamicVar)(object)new GemVar(4m));

	public override Color GemColor => new Color(103071487u);

	public override CardRarity Rarity => (CardRarity)2;

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay? cardPlay)
	{
		decimal amount = GuardianHook.ModifyGemEffect(base.CombatState, this, ((DynamicVar)((CardModifier)this).DynamicVars.Gem()).BaseValue, base.Card);
		await GuardianCmd.Brace(ctx, base.Player, amount);
	}
}

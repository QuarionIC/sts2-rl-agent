using System;
using System.Collections.Generic;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Utils.Sound;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Cards.Basic;
using SlimeBoss.SlimeBossCode.Relics;

namespace SlimeBoss.SlimeBossCode.Core;

public class SlimeBoss : DownfallCharacterModel
{
	private static readonly Color Color = new Color(425597439u);

	public override Color EnergyLabelOutlineColor => new Color(5702911u);

	public override string CharId => "SlimeBoss";

	public override string ModId => "SlimeBoss";

	public override Color NameColor => Color;

	public override Color LabOutlineColor => Color;

	public override Color DeckEntryCardColor => Color;

	public override float CardColorH => 0.45f;

	public override float CardColorS => 0.5f;

	public override float CardColorV => 1.2f;

	public override Color MapDrawingColor => Color;

	public override CharacterGender Gender => (CharacterGender)0;

	protected override CharacterModel? UnlocksAfterRunAs => null;

	public override int StartingHp => 65;

	public override int StartingGold => 99;

	public override IEnumerable<CardModel> StartingDeck => new global::_003C_003Ez__ReadOnlyArray<CardModel>((CardModel[])(object)new CardModel[10]
	{
		(CardModel)ModelDb.Card<StrikeSlimeBoss>(),
		(CardModel)ModelDb.Card<StrikeSlimeBoss>(),
		(CardModel)ModelDb.Card<StrikeSlimeBoss>(),
		(CardModel)ModelDb.Card<DefendSlimeBoss>(),
		(CardModel)ModelDb.Card<DefendSlimeBoss>(),
		(CardModel)ModelDb.Card<DefendSlimeBoss>(),
		(CardModel)ModelDb.Card<DefendSlimeBoss>(),
		(CardModel)ModelDb.Card<CorrosiveSpit>(),
		(CardModel)ModelDb.Card<Split>(),
		(CardModel)ModelDb.Card<Tackle>()
	});

	public override ModSoundEffect CharacterSelectSfxEntry => new ModSoundEffect(new ModSoundEntry("res://SlimeBoss/audio/character_select/SOTE_SFX_SlimeSplit_v1.ogg", 1f, 0.3f, 1f, 10f));

	public override IReadOnlyList<RelicModel> StartingRelics => new _003C_003Ez__ReadOnlySingleElementList<RelicModel>((RelicModel)(object)ModelDb.Relic<HeartOfGoo>());

	public override float AttackAnimDelay => 0.15f;

	public override float CastAnimDelay => 0.25f;

	public override CardPoolModel CardPool => (CardPoolModel)(object)ModelDb.CardPool<SlimeBossCardPool>();

	public override PotionPoolModel PotionPool => (PotionPoolModel)(object)ModelDb.PotionPool<SlimeBossPotionPool>();

	public override RelicPoolModel RelicPool => (RelicPoolModel)(object)ModelDb.RelicPool<SlimeBossRelicPool>();

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Expected O, but got Unknown
		GD.Print("[Downfall] GenerateAnimator called");
		AnimState val = new AnimState("idle", true);
		AnimState val2 = new AnimState("idle", false);
		AnimState val3 = new AnimState("idle", false);
		AnimState val4 = new AnimState("idle", false);
		AnimState val5 = new AnimState("idle", false);
		AnimState val6 = new AnimState("idle", false);
		val2.NextState = val;
		val3.NextState = val;
		val4.NextState = val;
		val6.NextState = val;
		val6.AddBranch("Idle", val, (Func<bool>)null);
		CreatureAnimator val7 = new CreatureAnimator(val, controller);
		val7.AddAnyState("Idle", val, (Func<bool>)null);
		val7.AddAnyState("Dead", val5, (Func<bool>)null);
		val7.AddAnyState("Hit", val4, (Func<bool>)null);
		val7.AddAnyState("Attack", val3, (Func<bool>)null);
		val7.AddAnyState("Cast", val2, (Func<bool>)null);
		val7.AddAnyState("Relaxed", val6, (Func<bool>)null);
		return val7;
	}
}

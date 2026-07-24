using System;
using System.Collections.Generic;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Utils.Sound;
using Godot;
using Hermit.HermitCode.Cards.Basic;
using Hermit.HermitCode.Relics;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Core;

public class Hermit : DownfallCharacterModel
{
	private static readonly Color Color = new Color(3466885119u);

	public override Color EnergyLabelOutlineColor => new Color(3129540863u);

	public override string CharId => "Hermit";

	public override string ModId => "Hermit";

	public override Color NameColor => Color;

	public override Color LabOutlineColor => Color;

	public override Color DeckEntryCardColor => Color;

	public override float CardColorH => 0.1f;

	public override float CardColorS => 0.4f;

	public override float CardColorV => 1.2f;

	public override Color MapDrawingColor => new Color(2051604735u);

	public override CharacterGender Gender => (CharacterGender)0;

	protected override CharacterModel? UnlocksAfterRunAs => null;

	public override int StartingHp => 70;

	public override int StartingGold => 99;

	public override IEnumerable<CardModel> StartingDeck => new global::_003C_003Ez__ReadOnlyArray<CardModel>((CardModel[])(object)new CardModel[10]
	{
		(CardModel)ModelDb.Card<StrikeHermit>(),
		(CardModel)ModelDb.Card<StrikeHermit>(),
		(CardModel)ModelDb.Card<StrikeHermit>(),
		(CardModel)ModelDb.Card<StrikeHermit>(),
		(CardModel)ModelDb.Card<DefendHermit>(),
		(CardModel)ModelDb.Card<DefendHermit>(),
		(CardModel)ModelDb.Card<DefendHermit>(),
		(CardModel)ModelDb.Card<DefendHermit>(),
		(CardModel)ModelDb.Card<Covet>(),
		(CardModel)ModelDb.Card<Snapshot>()
	});

	public override ModSoundEffect CharacterSelectSfxEntry => new ModSoundEffect(new ModSoundEntry("res://Hermit/audio/hermit_gun.ogg", 10f, 0.3f, 1f, 8f), new ModSoundEntry("res://Hermit/audio/hermit_gun2.ogg", 3f, 0.3f, 1f, 8f), new ModSoundEntry("res://Hermit/audio/hermit_gun3.ogg", 1f, 0.3f, 1f, 8f), new ModSoundEntry("res://Hermit/audio/hermit_reload.ogg", 6f, 0.3f, 1f, 8f), new ModSoundEntry("res://Hermit/audio/hermit_spin.ogg", 4f, 0.3f, 1f, 8f));

	public override IReadOnlyList<RelicModel> StartingRelics => new _003C_003Ez__ReadOnlySingleElementList<RelicModel>((RelicModel)(object)ModelDb.Relic<OldLocket>());

	public override float AttackAnimDelay => 0.15f;

	public override float CastAnimDelay => 0.25f;

	public override CardPoolModel CardPool => (CardPoolModel)(object)ModelDb.CardPool<HermitCardPool>();

	public override PotionPoolModel PotionPool => (PotionPoolModel)(object)ModelDb.PotionPool<HermitPotionPool>();

	public override RelicPoolModel RelicPool => (RelicPoolModel)(object)ModelDb.RelicPool<HermitRelicPool>();

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
		AnimState val = new AnimState("Idle", true);
		AnimState val2 = new AnimState("Idle", false);
		AnimState val3 = new AnimState("Attack", false);
		AnimState val4 = new AnimState("Hit", false);
		AnimState val5 = new AnimState("Idle", false);
		AnimState val6 = new AnimState("Idle", false);
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

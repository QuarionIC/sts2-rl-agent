using System.Collections.Generic;
using Automaton.AutomatonCode.Cards.Basic;
using Automaton.AutomatonCode.Relics;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Utils.Sound;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Core;

public class Automaton : DownfallCharacterModel
{
	private static readonly Color Color = new Color(3569982975u);

	public override Color EnergyLabelOutlineColor => new Color("4e3e01FF");

	public override string CharId => "Automaton";

	public override string ModId => "Automaton";

	public override Color NameColor => Color;

	public override Color LabOutlineColor => Color;

	public override Color DeckEntryCardColor => Color;

	public override float CardColorH => 0.16f;

	public override float CardColorS => 0.45f;

	public override float CardColorV => 1.2f;

	public override Color MapDrawingColor => new Color(4294902015u);

	public override CharacterGender Gender => (CharacterGender)1;

	protected override CharacterModel? UnlocksAfterRunAs => null;

	public override int StartingHp => 70;

	public override int StartingGold => 99;

	public override IReadOnlyList<RelicModel> StartingRelics => new _003C_003Ez__ReadOnlySingleElementList<RelicModel>((RelicModel)(object)ModelDb.Relic<BronzeCore>());

	public override float AttackAnimDelay => 0.15f;

	public override float CastAnimDelay => 0.25f;

	public override CardPoolModel CardPool => (CardPoolModel)(object)ModelDb.CardPool<AutomatonCardPool>();

	public override PotionPoolModel PotionPool => (PotionPoolModel)(object)ModelDb.PotionPool<AutomatonPotionPool>();

	public override RelicPoolModel RelicPool => (RelicPoolModel)(object)ModelDb.RelicPool<AutomatonRelicPool>();

	public override ModSoundEffect CharacterSelectSfxEntry => new ModSoundEffect(new ModSoundEntry("res://Automaton/audio/character_select/STS_SFX_AutomatonOrbSpawn_v1.ogg", 1f, 0.1f, 1f, 7f), new ModSoundEntry("res://Automaton/audio/character_select/STS_SFX_BronzeAutomatonSummon_v2.ogg", 1f, 0.1f, 1f, 7f));

	public override IEnumerable<CardModel> StartingDeck => new global::_003C_003Ez__ReadOnlyArray<CardModel>((CardModel[])(object)new CardModel[10]
	{
		(CardModel)ModelDb.Card<StrikeAutomaton>(),
		(CardModel)ModelDb.Card<StrikeAutomaton>(),
		(CardModel)ModelDb.Card<StrikeAutomaton>(),
		(CardModel)ModelDb.Card<StrikeAutomaton>(),
		(CardModel)ModelDb.Card<DefendAutomaton>(),
		(CardModel)ModelDb.Card<DefendAutomaton>(),
		(CardModel)ModelDb.Card<DefendAutomaton>(),
		(CardModel)ModelDb.Card<DefendAutomaton>(),
		(CardModel)ModelDb.Card<Postpone>(),
		(CardModel)ModelDb.Card<Branch>()
	});
}

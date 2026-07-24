using System.Collections.Generic;
using Champ.ChampCode.Cards.Basic;
using Champ.ChampCode.Relics;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Utils.Sound;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Core;

public class Champ : DownfallCharacterModel
{
	private static readonly Color Color = new Color(1582911487u);

	public override Color EnergyLabelOutlineColor => new Color(1178731519u);

	public override string CharId => "Champ";

	public override string ModId => "Champ";

	public override Color NameColor => Color;

	public override Color LabOutlineColor => Color;

	public override Color DeckEntryCardColor => Color;

	public override float CardColorH => 0.6f;

	public override float CardColorS => 0.5f;

	public override float CardColorV => 1.2f;

	public override Color MapDrawingColor => Color;

	public override CharacterGender Gender => (CharacterGender)2;

	protected override CharacterModel? UnlocksAfterRunAs => null;

	public override int StartingHp => 80;

	public override int StartingGold => 99;

	public override IEnumerable<CardModel> StartingDeck => new global::_003C_003Ez__ReadOnlyArray<CardModel>((CardModel[])(object)new CardModel[10]
	{
		(CardModel)ModelDb.Card<StrikeChamp>(),
		(CardModel)ModelDb.Card<StrikeChamp>(),
		(CardModel)ModelDb.Card<StrikeChamp>(),
		(CardModel)ModelDb.Card<StrikeChamp>(),
		(CardModel)ModelDb.Card<DefendChamp>(),
		(CardModel)ModelDb.Card<DefendChamp>(),
		(CardModel)ModelDb.Card<DefendChamp>(),
		(CardModel)ModelDb.Card<BerserkersShout>(),
		(CardModel)ModelDb.Card<DefensiveShout>(),
		(CardModel)ModelDb.Card<Execute>()
	});

	public override IReadOnlyList<RelicModel> StartingRelics => new _003C_003Ez__ReadOnlySingleElementList<RelicModel>((RelicModel)(object)ModelDb.Relic<ChampionsCrown>());

	public override float AttackAnimDelay => 0.15f;

	public override float CastAnimDelay => 0.25f;

	public override CardPoolModel CardPool => (CardPoolModel)(object)ModelDb.CardPool<ChampCardPool>();

	public override PotionPoolModel PotionPool => (PotionPoolModel)(object)ModelDb.PotionPool<ChampPotionPool>();

	public override RelicPoolModel RelicPool => (RelicPoolModel)(object)ModelDb.RelicPool<ChampRelicPool>();

	public override ModSoundEffect CharacterSelectSfxEntry => new ModSoundEffect(new ModSoundEntry("res://Champ/audio/character_select/STS_VO_TheChamp_3a.ogg", 1f, 0.1f, 1f, 10f), new ModSoundEntry("res://Champ/audio/character_select/STS_VO_TheChamp_3b.ogg", 1f, 0.1f, 1f, 10f));
}

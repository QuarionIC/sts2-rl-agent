using System.Collections.Generic;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Utils.Sound;
using Godot;
using Hexaghost.HexaghostCode.Cards.Basic;
using Hexaghost.HexaghostCode.Relics;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Core;

public class Hexaghost : DownfallCharacterModel
{
	private static readonly Color Color = new Color(1916694015u);

	public override Color EnergyLabelOutlineColor => Color;

	public override string ModId => "Hexaghost";

	public override string CharId => "Hexaghost";

	public override Color NameColor => Color;

	public override Color LabOutlineColor => Color;

	public override Color DeckEntryCardColor => Color;

	public override float CardColorH => 0.85f;

	public override float CardColorS => 0.4f;

	public override float CardColorV => 0.8f;

	public override Color MapDrawingColor => Color;

	public override CharacterGender Gender => (CharacterGender)0;

	protected override CharacterModel? UnlocksAfterRunAs => null;

	public override int StartingHp => 66;

	public override int StartingGold => 99;

	public override IEnumerable<CardModel> StartingDeck => new global::_003C_003Ez__ReadOnlyArray<CardModel>((CardModel[])(object)new CardModel[11]
	{
		(CardModel)ModelDb.Card<StrikeHexaghost>(),
		(CardModel)ModelDb.Card<StrikeHexaghost>(),
		(CardModel)ModelDb.Card<StrikeHexaghost>(),
		(CardModel)ModelDb.Card<StrikeHexaghost>(),
		(CardModel)ModelDb.Card<DefendHexaghost>(),
		(CardModel)ModelDb.Card<DefendHexaghost>(),
		(CardModel)ModelDb.Card<DefendHexaghost>(),
		(CardModel)ModelDb.Card<DefendHexaghost>(),
		(CardModel)ModelDb.Card<Sear>(),
		(CardModel)ModelDb.Card<Float>(),
		(CardModel)ModelDb.Card<Kindle>()
	});

	public override ModSoundEffect CharacterSelectSfxEntry => new ModSoundEffect(new ModSoundEntry("res://Hexaghost/audio/character_select/SOTE_SFX_BossOrbIgnite1_v2.ogg", 1f, 0.1f, 1f, 5f), new ModSoundEntry("res://Hexaghost/audio/character_select/SOTE_SFX_BossOrbIgnite2_v2.ogg", 1f, 0.1f, 1f, 5f));

	public override IReadOnlyList<RelicModel> StartingRelics => new _003C_003Ez__ReadOnlySingleElementList<RelicModel>((RelicModel)(object)ModelDb.Relic<SpiritBrand>());

	public override float AttackAnimDelay => 0.15f;

	public override float CastAnimDelay => 0.25f;

	public override CardPoolModel CardPool => (CardPoolModel)(object)ModelDb.CardPool<HexaghostCardPool>();

	public override PotionPoolModel PotionPool => (PotionPoolModel)(object)ModelDb.PotionPool<HexaghostPotionPool>();

	public override RelicPoolModel RelicPool => (RelicPoolModel)(object)ModelDb.RelicPool<HexaghostRelicPool>();
}

using System.Collections.Generic;
using Awakened.AwakenedCode.Cards.Basic;
using Awakened.AwakenedCode.Relics;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Utils.Sound;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Core;

public class Awakened : DownfallCharacterModel
{
	private static readonly Color Color = new Color(318435583u);

	public override Color EnergyLabelOutlineColor => new Color(4806399u);

	public override string CharId => "Awakened";

	public override string ModId => "Awakened";

	public override Color NameColor => Color;

	public override Color LabOutlineColor => Color;

	public override Color DeckEntryCardColor => Color;

	public override float CardColorH => 0.6f;

	public override float CardColorS => 0.5f;

	public override float CardColorV => 1f;

	public override Color MapDrawingColor => Color;

	public override CharacterGender Gender => (CharacterGender)0;

	protected override CharacterModel? UnlocksAfterRunAs => null;

	public override int StartingHp => 70;

	public override int StartingGold => 99;

	public override IEnumerable<CardModel> StartingDeck => new global::_003C_003Ez__ReadOnlyArray<CardModel>((CardModel[])(object)new CardModel[10]
	{
		(CardModel)ModelDb.Card<StrikeAwakened>(),
		(CardModel)ModelDb.Card<StrikeAwakened>(),
		(CardModel)ModelDb.Card<StrikeAwakened>(),
		(CardModel)ModelDb.Card<StrikeAwakened>(),
		(CardModel)ModelDb.Card<DefendAwakened>(),
		(CardModel)ModelDb.Card<DefendAwakened>(),
		(CardModel)ModelDb.Card<DefendAwakened>(),
		(CardModel)ModelDb.Card<DefendAwakened>(),
		(CardModel)ModelDb.Card<Hymn>(),
		(CardModel)ModelDb.Card<TalonRake>()
	});

	public override IReadOnlyList<RelicModel> StartingRelics => new _003C_003Ez__ReadOnlySingleElementList<RelicModel>((RelicModel)(object)ModelDb.Relic<RippedDoll>());

	public override float AttackAnimDelay => 0.15f;

	public override float CastAnimDelay => 0.25f;

	public override CardPoolModel CardPool => (CardPoolModel)(object)ModelDb.CardPool<AwakenedCardPool>();

	public override PotionPoolModel PotionPool => (PotionPoolModel)(object)ModelDb.PotionPool<AwakenedPotionPool>();

	public override RelicPoolModel RelicPool => (RelicPoolModel)(object)ModelDb.RelicPool<AwakenedRelicPool>();

	public override ModSoundEffect CharacterSelectSfxEntry => new ModSoundEffect(new ModSoundEntry("res://Awakened/audio/chant_activatev2.ogg", 1f, 0.1f, 1f, 10f));
}

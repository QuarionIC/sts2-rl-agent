using System.Collections.Generic;
using System.Linq;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Utils.Sound;
using Godot;
using Guardian.GuardianCode.Cards.Basic;
using Guardian.GuardianCode.Relics;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Core;

public class Guardian : DownfallCharacterModel
{
	private static readonly Color Color = new Color(3394984959u);

	public override Color EnergyLabelOutlineColor => new Color(1464878335u);

	public override string CharId => "Guardian";

	public override string ModId => "Guardian";

	public override Color NameColor => Color;

	public override Color LabOutlineColor => Color;

	public override Color DeckEntryCardColor => Color;

	public override float CardColorH => 0.17f;

	public override float CardColorS => 1.5f;

	public override float CardColorV => 1.2f;

	public override Color MapDrawingColor => Color;

	public override CharacterGender Gender => (CharacterGender)2;

	protected override CharacterModel? UnlocksAfterRunAs => null;

	public override int StartingHp => 80;

	public override int StartingGold => 99;

	public override IEnumerable<CardModel> StartingDeck => new global::_003C_003Ez__ReadOnlyArray<CardModel>((CardModel[])(object)new CardModel[10]
	{
		(CardModel)ModelDb.Card<StrikeGuardian>(),
		(CardModel)ModelDb.Card<StrikeGuardian>(),
		(CardModel)ModelDb.Card<StrikeGuardian>(),
		(CardModel)ModelDb.Card<StrikeGuardian>(),
		(CardModel)ModelDb.Card<DefendGuardian>(),
		(CardModel)ModelDb.Card<DefendGuardian>(),
		(CardModel)ModelDb.Card<DefendGuardian>(),
		(CardModel)ModelDb.Card<DefendGuardian>(),
		(CardModel)ModelDb.Card<CurlUp>(),
		(CardModel)ModelDb.Card<TwinSlam>()
	});

	public override ModSoundEffect CharacterSelectSfxEntry => new ModSoundEffect(new ModSoundEntry("res://Guardian/audio/character_select/STS_SFX_Guardian3Destroy_v2.ogg", 1f, 0.1f, 1f, 7f));

	protected override IEnumerable<string> ExtraAssetPaths => GuardianModelDb.AllGems.Select((GemModel g) => g.IconPath);

	public override IReadOnlyList<RelicModel> StartingRelics => new _003C_003Ez__ReadOnlySingleElementList<RelicModel>((RelicModel)(object)ModelDb.Relic<BronzeGear>());

	public override float AttackAnimDelay => 0.15f;

	public override float CastAnimDelay => 0.25f;

	public override CardPoolModel CardPool => (CardPoolModel)(object)ModelDb.CardPool<GuardianCardPool>();

	public override PotionPoolModel PotionPool => (PotionPoolModel)(object)ModelDb.PotionPool<GuardianPotionPool>();

	public override RelicPoolModel RelicPool => (RelicPoolModel)(object)ModelDb.RelicPool<GuardianRelicPool>();
}

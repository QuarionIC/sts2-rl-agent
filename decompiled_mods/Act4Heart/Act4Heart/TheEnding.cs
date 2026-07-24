using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;

namespace Act4Heart;

internal class TheEnding : ActModel
{
	public override Color MapTraveledColor => new Color("1D1E2F");

	public override Color MapUntraveledColor => new Color("60717C");

	public override Color MapBgColor => new Color("819A97");

	public override string[] BgMusicOptions => new string[1] { "event:/music/STS_Act4_BGM_v2" };

	public override string[] MusicBankPaths => new string[1] { "res://banks/desktop/Act4Heart/Act4Heart.bank" };

	public override string AmbientSfx => "event:/sfx/ambience/act3_ambience";

	public override int BaseNumberOfRooms => 2;

	public override string ChestSpineResourcePath => "res://animations/backgrounds/treasure_room/chest_room_act_3_skel_data.tres";

	public override string ChestSpineSkinNameNormal => "act3";

	public override string ChestSpineSkinNameStroke => "act3_stroke";

	public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act3";

	public override IEnumerable<EncounterModel> BossDiscoveryOrder => new _003C_003Ez__ReadOnlySingleElementList<EncounterModel>((EncounterModel)(object)ModelDb.Encounter<CorruptHeartBoss>());

	public override IEnumerable<AncientEventModel> AllAncients => Array.Empty<AncientEventModel>();

	public override IEnumerable<EventModel> AllEvents => Array.Empty<EventModel>();

	public override int Index => 3;

	public override bool IsDefault => true;

	public override bool IsUnlocked(UnlockState unlockState)
	{
		return true;
	}

	public override void ApplyActDiscoveryOrderModifications(UnlockState unlockState)
	{
	}

	public override IEnumerable<EncounterModel> GenerateAllEncounters()
	{
		return new _003C_003Ez__ReadOnlyArray<EncounterModel>((EncounterModel[])(object)new EncounterModel[3]
		{
			ModelDb.Encounter<CorruptHeartBoss>(),
			ModelDb.Encounter<SpireShieldAndSpireSpearElite>(),
			ModelDb.Encounter<EmptyFightAct4Weak>()
		});
	}

	public override MapPointTypeCounts GetMapPointTypes(Rng rng)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		return new MapPointTypeCounts(0, 1);
	}

	public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState _)
	{
		return Array.Empty<AncientEventModel>();
	}

	internal ActMap CreateMap()
	{
		return (ActMap)(object)new TheEndingMap();
	}

	internal string get_identifier()
	{
		return "glory";
	}
}

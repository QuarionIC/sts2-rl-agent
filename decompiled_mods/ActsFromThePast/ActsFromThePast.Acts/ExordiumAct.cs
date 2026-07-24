using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Unlocks;

namespace ActsFromThePast.Acts;

public sealed class ExordiumAct : CustomActModel
{
	public override int Index => 0;

	public override bool IsDefault => false;

	public override IEnumerable<EventModel> AllEvents => (IEnumerable<EventModel>)(object)new EventModel[1] { (EventModel)ModelDb.Event<TrashHeap>() };

	public override IEnumerable<AncientEventModel> AllAncients => CustomActModel.Act1Ancients;

	public override Color MapTraveledColor => new Color("28231D");

	public override Color MapUntraveledColor => new Color("877256");

	public override Color MapBgColor => new Color("A78A67");

	public override string[] BgMusicOptions => Array.Empty<string>();

	public override string[] MusicBankPaths => Array.Empty<string>();

	public override string AmbientSfx => "";

	public override string ChestSpineResourcePath => "res://animations/backgrounds/treasure_room/chest_room_act_1_skel_data.tres";

	public override string ChestSpineSkinNameNormal => "act1";

	public override string ChestSpineSkinNameStroke => "act1_stroke";

	public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act1";

	protected override string CustomMapTopBgPath => "res://images/packed/map/map_bgs/exordium_act/map_top_exordium_act.png";

	protected override string CustomMapMidBgPath => "res://images/packed/map/map_bgs/exordium_act/map_middle_exordium_act.png";

	protected override string CustomMapBotBgPath => "res://images/packed/map/map_bgs/exordium_act/map_middle_exordium_act.png";

	protected override string CustomRestSiteBackgroundPath => "res://scenes/rest_site/overgrowth_rest_site.tscn";

	public ExordiumAct()
		: base(1, true)
	{
	}

	public override IEnumerable<EncounterModel> GenerateAllEncounters()
	{
		return (IEnumerable<EncounterModel>)(object)new EncounterModel[0];
	}

	public override bool IsUnlocked(UnlockState unlockState)
	{
		return true;
	}

	public override bool Equals(object? obj)
	{
		return obj is ExordiumAct;
	}

	public override int GetHashCode()
	{
		return typeof(ExordiumAct).GetHashCode();
	}
}

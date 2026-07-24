using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Unlocks;

namespace ActsFromThePast.Acts.TheCity;

public sealed class TheCityAct : CustomActModel
{
	public override int Index => 1;

	public override bool IsDefault => false;

	public override IEnumerable<EventModel> AllEvents => (IEnumerable<EventModel>)(object)new EventModel[1] { (EventModel)ModelDb.Event<TrashHeap>() };

	public override IEnumerable<AncientEventModel> AllAncients => CustomActModel.Act2Ancients;

	public override string[] BgMusicOptions => Array.Empty<string>();

	public override string[] MusicBankPaths => Array.Empty<string>();

	public override string AmbientSfx => "";

	public override string ChestSpineResourcePath => "res://animations/backgrounds/treasure_room/chest_room_act_2_skel_data.tres";

	public override string ChestSpineSkinNameNormal => "act2";

	public override string ChestSpineSkinNameStroke => "act2_stroke";

	public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act2";

	public override Color MapTraveledColor => new Color("27221C");

	public override Color MapUntraveledColor => new Color("6E7750");

	public override Color MapBgColor => new Color("9B9562");

	protected override int NumberOfWeakEncounters => 2;

	protected override string CustomMapTopBgPath => "res://images/packed/map/map_bgs/the_city_act/map_top_the_city_act.png";

	protected override string CustomMapMidBgPath => "res://images/packed/map/map_bgs/the_city_act/map_middle_the_city_act.png";

	protected override string CustomMapBotBgPath => "res://images/packed/map/map_bgs/the_city_act/map_middle_the_city_act.png";

	protected override string CustomRestSiteBackgroundPath => "res://scenes/rest_site/hive_rest_site.tscn";

	public TheCityAct()
		: base(2, true)
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
		return obj is TheCityAct;
	}

	public override int GetHashCode()
	{
		return typeof(TheCityAct).GetHashCode();
	}
}

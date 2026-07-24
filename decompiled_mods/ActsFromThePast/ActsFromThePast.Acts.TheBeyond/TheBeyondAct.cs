using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Unlocks;

namespace ActsFromThePast.Acts.TheBeyond;

public sealed class TheBeyondAct : CustomActModel
{
	public override int Index => 2;

	public override bool IsDefault => false;

	public override IEnumerable<EventModel> AllEvents => (IEnumerable<EventModel>)(object)new EventModel[1] { (EventModel)ModelDb.Event<TrashHeap>() };

	public override IEnumerable<AncientEventModel> AllAncients => CustomActModel.Act3Ancients;

	public override string[] BgMusicOptions => Array.Empty<string>();

	public override string[] MusicBankPaths => Array.Empty<string>();

	public override string AmbientSfx => "";

	public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act2";

	public override Color MapTraveledColor => new Color("1D1E2F");

	public override Color MapUntraveledColor => new Color("60717C");

	public override Color MapBgColor => new Color("819A97");

	protected override int NumberOfWeakEncounters => 2;

	protected override string CustomMapTopBgPath => "res://images/packed/map/map_bgs/the_beyond_act/map_top_the_beyond_act.png";

	protected override string CustomMapMidBgPath => "res://images/packed/map/map_bgs/the_beyond_act/map_middle_the_beyond_act.png";

	protected override string CustomMapBotBgPath => "res://images/packed/map/map_bgs/the_beyond_act/map_middle_the_beyond_act.png";

	protected override string CustomRestSiteBackgroundPath => "res://scenes/rest_site/glory_rest_site.tscn";

	public TheBeyondAct()
		: base(3, true)
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
		return obj is TheBeyondAct;
	}

	public override int GetHashCode()
	{
		return typeof(TheBeyondAct).GetHashCode();
	}
}

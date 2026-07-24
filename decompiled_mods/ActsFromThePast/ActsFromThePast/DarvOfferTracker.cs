using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;

namespace ActsFromThePast;

public static class DarvOfferTracker
{
	public static HashSet<string> GetPreviouslyOfferedTitles(Player owner)
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (IReadOnlyList<MapPointHistoryEntry> item in owner.RunState.MapPointHistory)
		{
			foreach (MapPointHistoryEntry item2 in item)
			{
				PlayerMapPointHistoryEntry val = ((IEnumerable<PlayerMapPointHistoryEntry>)item2.PlayerStats).FirstOrDefault((Func<PlayerMapPointHistoryEntry, bool>)((PlayerMapPointHistoryEntry p) => p.PlayerId == owner.NetId));
				if (((val != null) ? val.AncientChoices : null) == null || val.AncientChoices.Count == 0)
				{
					continue;
				}
				foreach (AncientChoiceHistoryEntry ancientChoice in val.AncientChoices)
				{
					hashSet.Add(ancientChoice.Title.GetFormattedText());
				}
			}
		}
		return hashSet;
	}
}

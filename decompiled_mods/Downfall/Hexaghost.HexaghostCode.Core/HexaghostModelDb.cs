using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Core;

public static class HexaghostModelDb
{
	private static IEnumerable<GhostflameModel>? _allGhostflames;

	public static IEnumerable<GhostflameModel> AllGhostflames
	{
		get
		{
			if (_allGhostflames != null)
			{
				return _allGhostflames;
			}
			return _allGhostflames = (from t in ModelDb.AllAbstractModelSubtypes
				where t.IsSubclassOf(typeof(GhostflameModel))
				select (GhostflameModel)(object)ModelDb.Get(t)).ToList();
		}
	}

	public static T Ghostflame<T>() where T : GhostflameModel
	{
		return ModelDb.Get<T>();
	}
}

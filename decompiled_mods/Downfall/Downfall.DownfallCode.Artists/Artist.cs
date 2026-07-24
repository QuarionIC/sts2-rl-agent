using System;
using System.Collections.Generic;
using BaseLib.Extensions;
using Downfall.DownfallCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace Downfall.DownfallCode.Artists;

public abstract class Artist
{
	private static readonly Dictionary<Type, Artist> Instances = new Dictionary<Type, Artist>();

	private string Id => TypePrefix.GetPrefix(GetType()) + StringHelper.Slugify(GetType().Name);

	private static LocString ArtByLocString => new LocString("artists", "ART_BY");

	private LocString Name => new LocString("artists", Id + ".name");

	private LocString ArtByName
	{
		get
		{
			LocString artByLocString = ArtByLocString;
			artByLocString.Add("name", Name.GetFormattedText());
			return artByLocString;
		}
	}

	private Texture2D? Icon
	{
		get
		{
			if (IconPath != null)
			{
				return ResourceLoader.Load<Texture2D>(IconPath, (string)null, (CacheMode)1);
			}
			return null;
		}
	}

	protected virtual string? IconPath => (Id + ".png").ArtistImagePath();

	public IHoverTip HoverTip => (IHoverTip)(object)new ArtistHoverTip(ArtByName, Icon);

	public static T Get<T>() where T : Artist, new()
	{
		Artist artist3;
		if (!Instances.TryGetValue(typeof(T), out Artist value))
		{
			Artist artist = (Instances[typeof(T)] = new T());
			artist3 = artist;
		}
		else
		{
			artist3 = value;
		}
		return (T)artist3;
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace BaseLib.Utils;

public class CustomBackgroundAssets : BackgroundAssets
{
	private static readonly Action<BackgroundAssets, string> BackgroundScenePathSetter = ReflectionUtils.GetSetterForProperty<BackgroundAssets, string>("BackgroundScenePath");

	private static readonly Action<BackgroundAssets, string?> FgLayerSetter = ReflectionUtils.GetSetterForProperty<BackgroundAssets, string>("FgLayer");

	private const string FakeKey = "glory";

	public CustomBackgroundAssets()
		: base("glory", Rng.Chaotic)
	{
		((BackgroundAssets)this).BgLayers.Clear();
	}

	public CustomBackgroundAssets(string layersPath, Rng rng)
		: this(layersPath, "res://BaseLib/scenes/dynamic_background.tscn", rng)
	{
	}

	public CustomBackgroundAssets(string layersPath, string bgScenePath, Rng rng)
		: this()
	{
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
		List<string> list = new List<string>();
		string[] array = ResourceLoader.ListDirectory(layersPath);
		foreach (string text in array)
		{
			if (text == null)
			{
				continue;
			}
			if (text.Contains("_fg_"))
			{
				list.Add(layersPath + "/" + text);
				continue;
			}
			if (!text.Contains("_bg_"))
			{
				throw new InvalidOperationException("files must either contain '_fg_' or '_bg_'");
			}
			string key = text.Split("_bg_")[1].Split("_")[0];
			if (!dictionary.ContainsKey(key))
			{
				dictionary.Add(key, new List<string>());
			}
			dictionary[key].Add(layersPath + "/" + text);
		}
		BackgroundScenePathSetter((BackgroundAssets)(object)this, bgScenePath);
		((BackgroundAssets)this).BgLayers.AddRange(BackgroundAssets.SelectRandomBackgroundAssetLayers(rng, dictionary));
		FgLayerSetter((BackgroundAssets)(object)this, BackgroundAssets.SelectRandomForegroundAssetLayer(rng, (IEnumerable<string>)list));
	}

	public CustomBackgroundAssets(string backgroundScenePath, List<string> backgroundLayers, string foregroundLayer)
		: this()
	{
		BackgroundScenePathSetter((BackgroundAssets)(object)this, backgroundScenePath);
		((BackgroundAssets)this).BgLayers.AddRange(backgroundLayers);
		FgLayerSetter((BackgroundAssets)(object)this, foregroundLayer);
	}

	public CustomBackgroundAssets(string backgroundScenePath, IEnumerable<IEnumerable<string>> backgroundLayers, IEnumerable<string> foregroundLayers, Rng rng)
		: this()
	{
		BackgroundScenePathSetter((BackgroundAssets)(object)this, backgroundScenePath);
		((BackgroundAssets)this).BgLayers.AddRange(backgroundLayers.Select((IEnumerable<string> layer) => rng.NextItem<string>(layer)).ToList());
		FgLayerSetter((BackgroundAssets)(object)this, BackgroundAssets.SelectRandomForegroundAssetLayer(rng, foregroundLayers));
	}
}

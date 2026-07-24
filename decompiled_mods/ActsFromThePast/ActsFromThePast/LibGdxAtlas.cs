using System;
using System.Collections.Generic;
using Godot;

namespace ActsFromThePast;

public static class LibGdxAtlas
{
	public struct TextureRegion
	{
		public Texture2D Texture;

		public Rect2 Region;
	}

	public struct RegionInfo
	{
		public int X;

		public int Y;

		public int Width;

		public int Height;

		public int OrigWidth;

		public int OrigHeight;

		public int OffsetX;

		public int OffsetY;

		public bool Rotate;
	}

	private class AtlasData
	{
		public Dictionary<string, RegionData> Regions = new Dictionary<string, RegionData>();
	}

	private class RegionData
	{
		public string TexturePath;

		public int X;

		public int Y;

		public int Width;

		public int Height;

		public int OrigWidth;

		public int OrigHeight;

		public int OffsetX;

		public int OffsetY;

		public bool Rotate;
	}

	private static readonly Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();

	private static readonly Dictionary<string, AtlasData> _atlasCache = new Dictionary<string, AtlasData>();

	public static RegionInfo? GetRegionData(string atlasPath, string regionName)
	{
		AtlasData atlasData = LoadAtlasData(atlasPath);
		if (!atlasData.Regions.TryGetValue(regionName, out RegionData value))
		{
			return null;
		}
		return new RegionInfo
		{
			X = value.X,
			Y = value.Y,
			Width = value.Width,
			Height = value.Height,
			OrigWidth = value.OrigWidth,
			OrigHeight = value.OrigHeight,
			OffsetX = value.OffsetX,
			OffsetY = value.OffsetY,
			Rotate = value.Rotate
		};
	}

	public static TextureRegion? GetRegion(string atlasPath, string regionName)
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		AtlasData atlasData = LoadAtlasData(atlasPath);
		if (!atlasData.Regions.TryGetValue(regionName, out RegionData value))
		{
			return null;
		}
		Texture2D val = LoadTexture(value.TexturePath);
		if (val == null)
		{
			return null;
		}
		return new TextureRegion
		{
			Texture = val,
			Region = new Rect2((float)value.X, (float)value.Y, (float)value.Width, (float)value.Height)
		};
	}

	private static Texture2D LoadTexture(string path)
	{
		if (_textureCache.TryGetValue(path, out Texture2D value))
		{
			return value;
		}
		Texture2D val = GD.Load<Texture2D>(path);
		if (val == null)
		{
			return null;
		}
		_textureCache[path] = val;
		return val;
	}

	private static AtlasData LoadAtlasData(string atlasPath)
	{
		if (_atlasCache.TryGetValue(atlasPath, out AtlasData value))
		{
			return value;
		}
		AtlasData atlasData = ParseAtlasFile(atlasPath);
		_atlasCache[atlasPath] = atlasData;
		return atlasData;
	}

	private static AtlasData ParseAtlasFile(string atlasPath)
	{
		AtlasData atlasData = new AtlasData();
		FileAccess val = FileAccess.Open(atlasPath, (ModeFlags)1);
		try
		{
			if (val == null)
			{
				return atlasData;
			}
			string asText = val.GetAsText(true);
			string[] array = asText.Split('\n');
			string baseDir = StringExtensions.GetBaseDir(atlasPath);
			string texturePath = null;
			string text = null;
			RegionData regionData = new RegionData();
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i].Trim('\r');
				if (string.IsNullOrWhiteSpace(text2))
				{
					if (text != null)
					{
						regionData.TexturePath = texturePath;
						atlasData.Regions[text] = regionData;
					}
					text = null;
				}
				else if (text2.EndsWith(".png") || text2.EndsWith(".jpg") || text2.EndsWith(".jpeg"))
				{
					if (text != null)
					{
						regionData.TexturePath = texturePath;
						atlasData.Regions[text] = regionData;
						text = null;
					}
					texturePath = baseDir + "/" + text2;
				}
				else
				{
					if (text2.StartsWith("size:") || text2.StartsWith("format:") || text2.StartsWith("filter:") || text2.StartsWith("repeat:"))
					{
						continue;
					}
					if (array[i].StartsWith("  ") || array[i].StartsWith("\t"))
					{
						if (text == null)
						{
							continue;
						}
						int num = text2.IndexOf(':');
						if (num != -1)
						{
							string text3 = text2.Substring(0, num).Trim();
							string text4 = text2.Substring(num + 1).Trim();
							switch (text3)
							{
							case "xy":
							{
								string[] array5 = text4.Split(',');
								regionData.X = int.Parse(array5[0].Trim());
								regionData.Y = int.Parse(array5[1].Trim());
								break;
							}
							case "size":
							{
								string[] array4 = text4.Split(',');
								regionData.Width = int.Parse(array4[0].Trim());
								regionData.Height = int.Parse(array4[1].Trim());
								break;
							}
							case "orig":
							{
								string[] array3 = text4.Split(',');
								regionData.OrigWidth = int.Parse(array3[0].Trim());
								regionData.OrigHeight = int.Parse(array3[1].Trim());
								break;
							}
							case "offset":
							{
								string[] array2 = text4.Split(',');
								regionData.OffsetX = int.Parse(array2[0].Trim());
								regionData.OffsetY = int.Parse(array2[1].Trim());
								break;
							}
							case "rotate":
								regionData.Rotate = text4 == "true";
								break;
							}
						}
					}
					else
					{
						if (text != null)
						{
							regionData.TexturePath = texturePath;
							atlasData.Regions[text] = regionData;
						}
						text = text2;
						regionData = new RegionData();
					}
				}
			}
			if (text != null)
			{
				regionData.TexturePath = texturePath;
				atlasData.Regions[text] = regionData;
			}
			return atlasData;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}
}

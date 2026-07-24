using System;
using System.IO;
using Downfall.DownfallCode.Abstract;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Extensions;

public static class StringExtensions
{
	private static string ModId<T>() where T : DownfallCharacterModel
	{
		return ModelDb.Character<T>().ModId;
	}

	private static string ImgPath(string modId, string subfolder, string file)
	{
		return Path.Join(modId, "images", subfolder, file);
	}

	public static string ScenePath(string modId, string subfolder, string file)
	{
		return Path.Join(modId, "scenes", subfolder, file);
	}

	private static string WithFallback(string path, Func<string> fallbackProvider)
	{
		if (!ResourceLoader.Exists(path, ""))
		{
			return fallbackProvider();
		}
		return path;
	}

	private static string? WithNullFallback(string path)
	{
		if (!ResourceLoader.Exists(path, ""))
		{
			return null;
		}
		return path;
	}

	private static string FallbackImg(string missingPath, string subfolder, string file)
	{
		DownfallMainFile.Logger.Warn($"File not found at: '{missingPath}'. Falling back to: '{subfolder}/{file}'", 1);
		return ImgPath("Downfall", subfolder, file);
	}

	private static string FallbackScene(string missingPath, string subfolder, string file)
	{
		DownfallMainFile.Logger.Warn($"File not found at: '{missingPath}'. Falling back to: '{subfolder}/{file}'", 1);
		return ScenePath("Downfall", subfolder, file);
	}

	public static string CardImageAtlasPath<T>(this string path) where T : DownfallCharacterModel
	{
		string primaryPath = ImgPath(ModId<T>(), "atlases/card_atlas.sprites", path);
		return WithFallback(primaryPath, () => FallbackImg(primaryPath, "atlases/card_atlas.sprites", "todo.tres"));
	}

	public static string RestSitePath<T>(this string path) where T : DownfallCharacterModel
	{
		return ImgPath(ModId<T>(), "ui/restsite", path);
	}

	public static string EnchantmentPath<T>(this string path) where T : DownfallCharacterModel
	{
		string primaryPath = ImgPath(ModId<T>(), "enchantments", path);
		return WithFallback(primaryPath, () => FallbackImg(primaryPath, "enchantments", "todo.png"));
	}

	public static string DownfallPowerSpriteImagePath(this string path)
	{
		string primaryPath = ImgPath("Downfall", "atlases/power_sprite_atlas.sprites", path);
		return WithFallback(primaryPath, () => FallbackImg(primaryPath, "atlases/power_sprite_atlas.sprites", "todo_power.tres"));
	}

	public static string DownfallPowerImagePath(this string path)
	{
		string primaryPath = ImgPath("Downfall", "atlases/power_atlas.sprites", path);
		return WithFallback(primaryPath, () => FallbackImg(primaryPath, "atlases/power_atlas.sprites", "todo_power.tres"));
	}

	public static string DownfallBigPowerImagePath(this string path)
	{
		string primaryPath = ImgPath("Downfall", "powers", path);
		return WithFallback(primaryPath, () => FallbackImg(primaryPath, "powers", "todo_power.png"));
	}

	public static string PowerImagePath<T>(this string path) where T : DownfallCharacterModel
	{
		string primaryPath = ImgPath(ModId<T>(), "atlases/power_atlas.sprites", path);
		return WithFallback(primaryPath, () => FallbackImg(primaryPath, "atlases/power_atlas.sprites", "todo_power.tres"));
	}

	public static string BigPowerImagePath<T>(this string path) where T : DownfallCharacterModel
	{
		string primaryPath = ImgPath(ModId<T>(), "powers", path);
		return WithFallback(primaryPath, () => FallbackImg(primaryPath, "powers", "todo_power.png"));
	}

	public static string PowerSpriteImagePath<T>(this string path) where T : DownfallCharacterModel
	{
		string primaryPath = ImgPath(ModId<T>(), "atlases/power_sprite_atlas.sprites", path);
		return WithFallback(primaryPath, () => FallbackImg(primaryPath, "atlases/power_sprite_atlas.sprites", "todo_power.tres"));
	}

	public static string BigRelicImagePath<T>(this string path) where T : DownfallCharacterModel
	{
		string primaryPath = ImgPath(ModId<T>(), "relics", path);
		return WithFallback(primaryPath, () => FallbackImg(primaryPath, "relics", "todo.png"));
	}

	public static string TresRelicImagePath<T>(this string path) where T : DownfallCharacterModel
	{
		string primaryPath = ImgPath(ModId<T>(), "atlases/relic_atlas.sprites", path);
		string fallbackFile = (path.Contains("outline") ? "todo_outline.tres" : "todo.tres");
		return WithFallback(primaryPath, () => FallbackImg(primaryPath, "atlases/relic_atlas.sprites", fallbackFile));
	}

	public static string TresPotionImagePath<T>(this string path) where T : DownfallCharacterModel
	{
		string primaryPath = ImgPath(ModId<T>(), "atlases/potion_atlas.sprites", path);
		string fallbackFile = (path.Contains("outline") ? "todo_potion_outline.tres" : "todo_potion.tres");
		return WithFallback(primaryPath, () => FallbackImg(primaryPath, "atlases/potion_atlas.sprites", fallbackFile));
	}

	public static string DownfallTresPotionImagePath(this string path)
	{
		string primaryPath = ImgPath("Downfall", "atlases/potion_atlas.sprites", path);
		string fallbackFile = (path.Contains("outline") ? "todo_potion_outline.tres" : "todo_potion.tres");
		return WithFallback(primaryPath, () => FallbackImg(primaryPath, "atlases/potion_atlas.sprites", fallbackFile));
	}

	public static string AfflictionScenePath<T>(this string path) where T : DownfallCharacterModel
	{
		string primaryPath = ScenePath(ModId<T>(), "afflictions", path);
		return WithFallback(primaryPath, () => FallbackScene(primaryPath, "afflictions", "todo_affliction.tscn"));
	}

	public static string? ArtistImagePath(this string path)
	{
		return WithNullFallback(ImgPath("Downfall", "artists", path));
	}
}

using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Saves;

namespace Dolso;

internal sealed class ConfigReader
{
	internal Config local_config = new Config();

	private FileSystemWatcher? config_watcher;

	private Action? config_updated_handler;

	private bool notify_combat_of_change;

	internal event Action? on_config_updated;

	internal static ConfigReader Startup(bool notify_combat_of_change, Action? config_updated_handler = null)
	{
		Assembly assembly = typeof(Config).Assembly;
		string text = ProjectSettings.GlobalizePath(UserDataPathProvider.GetAccountScopedBasePath("mod_configs/", (PlatformType?)null, (ulong?)null));
		string text2 = "dolso." + StringHelper.SnakeCase(assembly.GetName().Name) + ".config";
		string path = Path.Combine(text, text2);
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		ConfigReader reader = new ConfigReader();
		reader.UpdateConfig(path);
		reader.notify_combat_of_change = notify_combat_of_change;
		reader.config_updated_handler = config_updated_handler;
		reader.config_watcher = new FileSystemWatcher(text, text2);
		reader.config_watcher.NotifyFilter = NotifyFilters.LastWrite;
		reader.config_watcher.Changed += delegate(object sender, FileSystemEventArgs args)
		{
			reader.UpdateConfig(args.FullPath);
		};
		reader.config_watcher.EnableRaisingEvents = true;
		return reader;
	}

	internal void UpdateConfig(string path)
	{
		FileSystemWatcher? fileSystemWatcher = config_watcher;
		if (fileSystemWatcher != null)
		{
			fileSystemWatcher.EnableRaisingEvents = false;
		}
		try
		{
			bool flag = File.Exists(path);
			using FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
			byte[] array = new byte[fileStream.Length];
			fileStream.ReadExactly(array);
			fileStream.Seek(0L, SeekOrigin.Begin);
			try
			{
				local_config = (flag ? (JsonSerializer.Deserialize(fileStream, Config.JsonSerializer.Default.Config) ?? local_config) : local_config);
			}
			catch (Exception data)
			{
				log.warning(data);
			}
			using MemoryStream memoryStream = new MemoryStream();
			JsonSerializer.Serialize(memoryStream, local_config, Config.JsonSerializer.Default.Config);
			memoryStream.TryGetBuffer(out var buffer);
			if (!((ReadOnlySpan<byte>)array).SequenceEqual((ReadOnlySpan<byte>)buffer))
			{
				fileStream.SetLength(0L);
				fileStream.Write(buffer);
			}
			fileStream.Dispose();
			if (config_watcher != null)
			{
				log.info("Read " + config_watcher.Filter);
			}
			if (config_updated_handler != null)
			{
				config_updated_handler();
			}
			else
			{
				OnConfigChanged();
			}
		}
		catch (Exception data2)
		{
			log.error(data2);
		}
		FileSystemWatcher? fileSystemWatcher2 = config_watcher;
		if (fileSystemWatcher2 != null)
		{
			fileSystemWatcher2.EnableRaisingEvents = true;
		}
	}

	internal void OnConfigChanged()
	{
		this.on_config_updated?.Invoke();
		if (notify_combat_of_change)
		{
			CombatManager.Instance.StateTracker.NotifyCombatStateChanged(typeof(Config).Assembly.GetName().Name);
		}
	}
}

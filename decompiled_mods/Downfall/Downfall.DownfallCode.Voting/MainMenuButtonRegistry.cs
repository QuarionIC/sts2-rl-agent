using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace Downfall.DownfallCode.Voting;

public static class MainMenuButtonRegistry
{
	public class Entry
	{
		public Func<NSubmenu?>? CreateSubmenu;

		public Func<bool> IsVisible = () => true;

		public required string Label;

		public Action<NMainMenuSubmenuStack?>? OnPress;

		public Type? SubmenuType;
	}

	private static readonly List<Entry> entries = new List<Entry>();

	public static IReadOnlyList<Entry> Entries => entries;

	public static void Register(Entry entry)
	{
		entries.Add(entry);
	}

	internal static Entry? FindBySubmenuType(Type type)
	{
		return entries.FirstOrDefault((Entry e) => e.SubmenuType == type);
	}
}

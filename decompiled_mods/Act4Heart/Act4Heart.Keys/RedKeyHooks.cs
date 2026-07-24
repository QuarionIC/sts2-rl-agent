using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Dolso;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace Act4Heart.Keys;

[Hook]
internal class RedKeyHooks : ModelHook
{
	[Hook]
	private static void Init()
	{
		ModelHook.Register<RedKeyHooks>();
	}

	public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
	{
		if (!ModMain.current_config.keys_enable)
		{
			return false;
		}
		try
		{
			return AddRecallOption(player, options);
		}
		catch (Exception data)
		{
			log.error(data);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool AddRecallOption(Player player, ICollection<RestSiteOption> options)
	{
		if (KeyRelicModel.EveryoneHasKey<RubyKey>(player.RunState))
		{
			return false;
		}
		options.Add((RestSiteOption)(object)new RecallSiteOption(player));
		return true;
	}
}

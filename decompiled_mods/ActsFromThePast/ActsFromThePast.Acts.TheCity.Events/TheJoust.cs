using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class TheJoust : CustomEventModel, IShrineEvent
{
	private const int BetAmount = 50;

	private const int WinMurderer = 100;

	private const int WinOwner = 250;

	private const float OwnerWinChance = 0.3f;

	private bool _betForOwner;

	private bool _ownerWins;

	public override ActModel[] Acts => (ActModel[])(object)new TheCityAct[1] { ModelDb.Act<TheCityAct>() };

	bool IShrineEvent.IsOneTimeEvent => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[3]
	{
		(DynamicVar)new IntVar("BetAmount", 50m),
		(DynamicVar)new IntVar("WinMurderer", 100m),
		(DynamicVar)new IntVar("WinOwner", 250m)
	};

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return (IReadOnlyList<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Continue, "INITIAL", Array.Empty<IHoverTip>()) };
	}

	private Task Continue()
	{
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("EXPLANATION"), (IEnumerable<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)BetMurderer, "EXPLANATION", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)BetOwner, "EXPLANATION", Array.Empty<IHoverTip>())
		});
		return Task.CompletedTask;
	}

	private async Task BetMurderer()
	{
		_betForOwner = false;
		await PlayerCmd.LoseGold(50m, ((EventModel)this).Owner, (GoldLossType)2);
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("BET_MURDERER"), (IEnumerable<EventOption>)(object)new EventOption[1]
		{
			new EventOption((EventModel)(object)this, (Func<Task>)WatchJoust, ((AbstractModel)this).Id.Entry + ".pages.BET_MURDERER.options.WATCH", Array.Empty<IHoverTip>())
		});
	}

	private async Task BetOwner()
	{
		_betForOwner = true;
		await PlayerCmd.LoseGold(50m, ((EventModel)this).Owner, (GoldLossType)2);
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("BET_OWNER"), (IEnumerable<EventOption>)(object)new EventOption[1]
		{
			new EventOption((EventModel)(object)this, (Func<Task>)WatchJoust, ((AbstractModel)this).Id.Entry + ".pages.BET_OWNER.options.WATCH", Array.Empty<IHoverTip>())
		});
	}

	private async Task WatchJoust()
	{
		_ownerWins = ((EventModel)this).Rng.NextFloat(1f) < 0.3f;
		NGame instance = NGame.Instance;
		if (instance != null)
		{
			instance.ScreenShake((ShakeStrength)2, (ShakeDuration)1, -1f);
		}
		SfxCmd.Play("event:/sfx/enemy/enemy_attacks/cultists/cultists_attack", 1f);
		await Cmd.Wait(1f, false);
		NGame instance2 = NGame.Instance;
		if (instance2 != null)
		{
			instance2.ScreenShake((ShakeStrength)2, (ShakeDuration)1, -1f);
		}
		SfxCmd.Play("event:/sfx/enemy/enemy_attacks/assassin_ruby_raider/assassin_ruby_raider_attack", 1f);
		await Cmd.Wait(0.25f, false);
		NGame instance3 = NGame.Instance;
		if (instance3 != null)
		{
			instance3.ScreenShake((ShakeStrength)2, (ShakeDuration)1, -1f);
		}
		SfxCmd.Play("event:/sfx/enemy/enemy_attacks/cultists/cultists_attack", 1f);
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("COMBAT"), (IEnumerable<EventOption>)(object)new EventOption[1]
		{
			new EventOption((EventModel)(object)this, (Func<Task>)ResolveJoust, ((AbstractModel)this).Id.Entry + ".pages.COMBAT.options.CONTINUE", Array.Empty<IHoverTip>())
		});
	}

	private async Task ResolveJoust()
	{
		if (_ownerWins)
		{
			if (_betForOwner)
			{
				await PlayerCmd.GainGold(250m, ((EventModel)this).Owner, false);
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("OWNER_WINS_BET_WON"));
			}
			else
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("OWNER_WINS_BET_LOST"));
			}
		}
		else if (_betForOwner)
		{
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("MURDERER_WINS_BET_LOST"));
		}
		else
		{
			await PlayerCmd.GainGold(100m, ((EventModel)this).Owner, false);
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("MURDERER_WINS_BET_WON"));
		}
	}

	public TheJoust()
		: base(true)
	{
	}
}

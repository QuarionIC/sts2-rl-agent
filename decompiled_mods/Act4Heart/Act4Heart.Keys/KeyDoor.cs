using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;

namespace Act4Heart.Keys;

internal class KeyDoor : EventModel
{
	internal byte state;

	public override bool IsShared => true;

	public override EventLayoutType LayoutType => (EventLayoutType)1;

	public override EncounterModel CanonicalEncounter => (EncounterModel)(object)ModelDb.Encounter<CorruptHeartBoss>();

	public override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Expected O, but got Unknown
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		EventOption[] array = (EventOption[])(object)new EventOption[3];
		if (EveryoneHasAllKeys())
		{
			array[0] = new EventOption((EventModel)(object)this, (Func<Task>)Succeed, "KEY_DOOR.pages.INITIAL.options.SUCCEED", Array.Empty<IHoverTip>());
		}
		else
		{
			array[0] = new EventOption((EventModel)(object)this, (Func<Task>)null, "KEY_DOOR.pages.INITIAL.options.SUCCEED_LOCKED", Array.Empty<IHoverTip>());
		}
		array[1] = new EventOption((EventModel)(object)this, (Func<Task>)Delusion, "KEY_DOOR.pages.INITIAL.options.DELUSION", Array.Empty<IHoverTip>());
		if (RunManager.Instance.State.Players.Count == 1)
		{
			array[2] = new EventOption((EventModel)(object)this, (Func<Task>)Failure, "KEY_DOOR.pages.INITIAL.options.FAILURE", false, true, Array.Empty<IHoverTip>()).ThatWillKillPlayerIf((Func<Player, bool>)((Player a) => true));
		}
		else
		{
			array[2] = new EventOption((EventModel)(object)this, (Func<Task>)Failure, "KEY_DOOR.pages.INITIAL.options.FAILURE", Array.Empty<IHoverTip>());
		}
		return array;
	}

	internal static bool EveryoneHasAllKeys()
	{
		foreach (Player player in RunManager.Instance.State.Players)
		{
			if (player.GetRelic<EmeraldKey>() == null || player.GetRelic<RubyKey>() == null || player.GetRelic<SapphireKey>() == null)
			{
				return false;
			}
		}
		return true;
	}

	private Task Succeed()
	{
		state = 1;
		((EventModel)this).SetEventFinished((LocString)null);
		ProceedToNextAct();
		return Task.CompletedTask;
	}

	private Task Delusion()
	{
		state = 2;
		((EventModel)this).SetEventFinished((LocString)null);
		ProceedToNextAct();
		return Task.CompletedTask;
	}

	private Task Failure()
	{
		state = 4;
		if (RunManager.Instance.State.Players.Count == 1)
		{
			NModalContainer.Instance.Add((Node)(object)NAbandonRunConfirmPopup.Create((NMainMenu)null), true);
			return Task.CompletedTask;
		}
		return CreatureCmd.Kill(((EventModel)this).Owner.Creature, true);
	}

	private void ProceedToNextAct()
	{
		if (LocalContext.IsMe(((EventModel)this).Owner))
		{
			RunManager.Instance.ActChangeSynchronizer.SetLocalPlayerReady();
		}
	}
}

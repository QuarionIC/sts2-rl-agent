using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using ActsFromThePast.Minigames;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class SecretPortal : CustomEventModel, IShrineEvent
{
	private const int MinRunTimeSeconds = 800;

	public override bool IsShared => true;

	public override ActModel[] Acts => (ActModel[])(object)new TheBeyondAct[1] { ModelDb.Act<TheBeyondAct>() };

	bool IShrineEvent.IsOneTimeEvent => true;

	public override bool IsAllowed(IRunState runState)
	{
		if (RunManager.Instance.RunTime <= 800)
		{
			return false;
		}
		if (ActsFromThePastConfig.RebalancedMode && ((IPlayerCollection)runState).Players.Count > 1)
		{
			return false;
		}
		return true;
	}

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "secret_portal");
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Enter, "INITIAL_REBALANCED", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)ReachIn, "INITIAL_REBALANCED", Array.Empty<IHoverTip>())
			};
		}
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Enter, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	private async Task Enter()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			RunState concreteState = (RunState)((EventModel)this).Owner.RunState;
			ActMap map = ((EventModel)this).Owner.RunState.Map;
			int currentRow = ((EventModel)this).Owner.RunState.CurrentMapCoord?.row ?? 0;
			PortalMapBuilderMinigame minigame = new PortalMapBuilderMinigame(availableNodeCount: map.GetRowCount() - 1 - currentRow, owner: ((EventModel)this).Owner, rng: ((EventModel)this).Rng);
			await minigame.PlayMinigame();
			RunManager.Instance.MapSelectionSynchronizer.BeforeMapGenerated();
			PortalBuilderActMap newMap = new PortalBuilderActMap(concreteState, minigame.Nodes, minigame.AvailableNodeCount);
			((EventModel)this).Owner.RunState.Map = (ActMap)(object)newMap;
			concreteState.RemoveStaleVisitedMapCoords((ActMap)(object)newMap);
			foreach (MapCoord coord in newMap.NewVisitedCoords)
			{
				concreteState.AddVisitedMapCoord(coord);
			}
			RunManager.Instance.MapSelectionSynchronizer.OnLocationChanged(((EventModel)this).Owner.RunState.MapLocation);
			NMapScreen instance = NMapScreen.Instance;
			if (instance != null)
			{
				instance.SetMap((ActMap)(object)newMap, ((EventModel)this).Owner.RunState.Rng.Seed, true);
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("ENTER_REBALANCED"));
		}
		else
		{
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("ENTER"), (IEnumerable<EventOption>)(object)new EventOption[1]
			{
				new EventOption((EventModel)(object)this, (Func<Task>)TeleportToBoss, ((AbstractModel)this).Id.Entry + ".pages.ENTER.options.CONTINUE", Array.Empty<IHoverTip>())
			});
		}
	}

	private Task TeleportToBoss()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		MapCoord coord = ((EventModel)this).Owner.RunState.Map.BossMapPoint.coord;
		TaskHelper.RunSafely(RunManager.Instance.EnterMapCoord(coord));
		return Task.CompletedTask;
	}

	private Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
		return Task.CompletedTask;
	}

	private async Task ReachIn()
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1);
		foreach (CardModel original in (await CardSelectCmd.FromDeckForTransformation(((EventModel)this).Owner, prefs, (Func<CardModel, CardTransformation>)null)).ToList())
		{
			CardModel transformed = CardFactory.CreateRandomCardForTransform(original, false, ((EventModel)this).Owner.RunState.Rng.Niche);
			CardCmd.Upgrade(transformed, (CardPreviewStyle)1);
			await CardCmd.Transform(original, transformed, (CardPreviewStyle)1);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("REACH_IN"));
	}

	public SecretPortal()
		: base(true)
	{
	}
}

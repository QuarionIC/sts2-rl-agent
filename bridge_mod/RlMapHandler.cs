// RlMapHandler.cs -- RL-agent-driven map navigation handler.
//
// Replaces AutoSlay's MapScreenHandler. Instead of picking the first child
// of the current map point, this handler:
//   1. Enumerates available map nodes (the reachable next nodes)
//   2. Sends them to Python with their types (Monster, Elite, Shop, etc.)
//   3. Waits for the agent to choose a node index
//   4. Clicks the chosen node
//
// Falls back to random selection if Python is disconnected or times out.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.AutoSlay.Handlers;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace STS2BridgeMod;

public class RlMapHandler : IScreenHandler, IHandler
{
    private static readonly TimeSpan AgentTimeout = TimeSpan.FromSeconds(30);

    private TaskCompletionSource? _roomEnteredTcs;

    public Type ScreenType => typeof(NMapScreen);
    public TimeSpan Timeout => TimeSpan.FromSeconds(60);

    public async Task HandleAsync(Rng random, CancellationToken ct)
    {
        Logger.Log("[RlMap] Handling map screen");
        Node root = ((SceneTree)Engine.GetMainLoop()).Root;

        // GetNode<T> THROWS when the path is absent, and the Run node is absent
        // in two very different situations: the run has genuinely ended, or the
        // scene is still loading. The hard lookup threw NullReferenceException
        // and killed the run ("Run N/8 FAILED: Object reference not set to an
        // instance of an object" on every run of the first multi-run sessions).
        //
        // But returning immediately on null is equally wrong, and measurably
        // worse: it silently SKIPS the map choice, so no node is selected, the
        // next room never starts, and the run dies with "Combat not started" --
        // observed at Act 1 Floor 9 while the log showed asset loading in
        // progress, i.e. the run was alive and the node was merely late.
        //
        // So poll for it, and only conclude the run has ended if it never
        // arrives. The watchdog is reset each iteration because this wait can
        // legitimately outlast its 30s no-progress window during asset loads.
        NRun? runNode = null;
        DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            runNode = root.GetNodeOrNull<NRun>("/root/Game/RootSceneContainer/Run");
            if (runNode?.GlobalUi?.MapScreen != null)
                break;
            RlAutoSlayer.CurrentWatchdog?.Reset("Waiting for the map screen to load");
            await Task.Delay(250, ct);
        }
        if (runNode?.GlobalUi?.MapScreen == null)
        {
            Logger.Log("[RlMap] Run node / map screen never appeared after 30s "
                       + "-- treating the run as ended; nothing to choose");
            return;
        }

        await WaitHelper.Until(
            () => runNode.GlobalUi.MapScreen.IsVisibleInTree(), ct,
            AutoSlayConfig.mapScreenTimeout, "Map screen not visible");

        List<NMapPoint> allPoints = UiHelper.FindAll<NMapPoint>(runNode.GlobalUi.MapScreen);
        RunState runState = RunManager.Instance.DebugOnlyGetState();

        // Determine available next nodes
        List<NMapPoint> availableNodes;
        if (runState.VisitedMapCoords.Count == 0)
        {
            // First room selection: all nodes in row 0
            availableNodes = allPoints
                .Where(mp => mp.Point.coord.row == 0)
                .ToList();
        }
        else
        {
            // Get the children of the last visited node.
            //
            // IMPORTANT: order these by MapPoint.Children (the same order
            // RunManager._actions_map_choice()/get_available_next_coords()
            // uses on the Python simulation side) rather than by re-scanning
            // allPoints, whose iteration order reflects scene-tree/visual
            // (row, col) layout and can diverge from Children's insertion
            // order (path generation can add children out of column order).
            // A trained full-run model indexes into this list positionally,
            // so the two orderings must match exactly.
            IReadOnlyList<MapCoord> visited = runState.VisitedMapCoords;
            MapCoord lastCoord = visited[visited.Count - 1];
            NMapPoint lastNode = allPoints.First(
                mp => mp.Point.coord.Equals(lastCoord));
            Dictionary<MapCoord, NMapPoint> pointsByCoord = allPoints
                .ToDictionary(mp => mp.Point.coord);
            availableNodes = lastNode.Point.Children
                .Select(child => pointsByCoord.TryGetValue(child.coord, out NMapPoint mp) ? mp : null)
                .OfType<NMapPoint>()
                .ToList();
        }

        if (availableNodes.Count == 0)
        {
            Logger.Log("[RlMap] No available nodes found!");
            return;
        }

        // OFFER ONLY NODES THE GAME WILL ACTUALLY LET US ENTER.
        //
        // Not every node reachable in the graph is enterable right now. The
        // first map choice enumerates every row-0 point, and in the legacy
        // acts some of those (an ANCIENT node, for one) are never enabled.
        // Offering them let the agent pick one, after which the click wait
        // timed out with "Map point not enabled" and the whole run FAILED --
        // observed live on run 6 of an Ironclad batch, choosing "Ancient at
        // (0,3)".
        //
        // Filtering here rather than at the click keeps the agent's action
        // indices meaningful: it only ever chooses among real options.
        List<NMapPoint> enabledNodes = availableNodes.Where(mp => mp.IsEnabled).ToList();
        if (enabledNodes.Count > 0 && enabledNodes.Count != availableNodes.Count)
        {
            Logger.Log($"[RlMap] {availableNodes.Count - enabledNodes.Count} of "
                       + $"{availableNodes.Count} reachable node(s) are not "
                       + $"enterable; offering the {enabledNodes.Count} that are");
            availableNodes = enabledNodes;
        }
        else if (enabledNodes.Count == 0)
        {
            // Nothing enabled yet -- usually the map is still animating in.
            // Wait briefly rather than either failing or offering junk.
            Logger.Log("[RlMap] No node is enabled yet; waiting for the map to settle");
            DateTime settle = DateTime.UtcNow + TimeSpan.FromSeconds(10);
            while (DateTime.UtcNow < settle && enabledNodes.Count == 0)
            {
                RlAutoSlayer.CurrentWatchdog?.Reset("Waiting for map nodes to enable");
                await Task.Delay(250, ct);
                enabledNodes = availableNodes.Where(mp => mp.IsEnabled).ToList();
            }
            if (enabledNodes.Count > 0)
                availableNodes = enabledNodes;
        }

        // Build the state message for Python
        var nodes = new List<Dictionary<string, object>>();
        for (int i = 0; i < availableNodes.Count; i++)
        {
            NMapPoint mp = availableNodes[i];
            nodes.Add(new Dictionary<string, object>
            {
                ["index"] = i,
                ["type"] = mp.Point.PointType.ToString(),
                ["row"] = mp.Point.coord.row,
                ["col"] = mp.Point.coord.col,
            });
        }

        var stateMsg = RunStateBridgeFields.Apply(new Dictionary<string, object>
        {
            ["type"] = "map_select",
            ["nodes"] = nodes,
        });

        NMapPoint chosenNode;

        if (BridgeServer.Instance.IsClientConnected)
        {
            try
            {
                string stateJson = JsonSerializer.Serialize(stateMsg);
                string responseJson = await BridgeServer.Instance.SendStateAndWaitForActionAsync(
                    stateJson,
                    AgentTimeout, ct);

                if (responseJson != null)
                {
                    using var doc = JsonDocument.Parse(responseJson);
                    var rRoot = doc.RootElement;
                    int chosenIndex = rRoot.GetProperty("index").GetInt32();

                    if (chosenIndex >= 0 && chosenIndex < availableNodes.Count)
                    {
                        chosenNode = availableNodes[chosenIndex];
                        Logger.Log(
                            $"[RlMap] Agent chose node {chosenIndex}: {chosenNode.Point.PointType} at ({chosenNode.Point.coord.row},{chosenNode.Point.coord.col})");
                    }
                    else
                    {
                        Logger.Log($"[RlMap] Invalid index {chosenIndex}, falling back to random");
                        chosenNode = random.NextItem(availableNodes);
                    }
                }
                else
                {
                    Logger.Log("[RlMap] No response from agent, falling back to random");
                    chosenNode = random.NextItem(availableNodes);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[RlMap] Agent error: {ex.Message}, falling back to random");
                chosenNode = random.NextItem(availableNodes);
            }
        }
        else
        {
            Logger.Log("[RlMap] No agent connected, selecting random node");
            chosenNode = random.NextItem(availableNodes);
        }

        // Wait for the node to be enabled, and if it never is, fall back to
        // one that is rather than throwing away the run. The filter above
        // makes this rare, but a node can still disable between the offer and
        // the click, and losing an entire run to an unclickable map point is
        // never the right trade.
        if (!chosenNode.IsEnabled)
        {
            try
            {
                await WaitHelper.Until(() => chosenNode.IsEnabled, ct,
                    TimeSpan.FromSeconds(10), "Map point not enabled");
            }
            catch (Exception)
            {
                NMapPoint fallback = availableNodes.FirstOrDefault(mp => mp.IsEnabled);
                if (fallback == null)
                    throw;
                Logger.Log($"[RlMap] Chosen node never enabled; falling back to "
                           + $"{fallback.Point.PointType} at "
                           + $"({fallback.Point.coord.row},{fallback.Point.coord.col})");
                chosenNode = fallback;
            }
        }

        _roomEnteredTcs = new TaskCompletionSource();
        RunManager.Instance.RoomEntered += OnRoomEntered;
        try
        {
            await UiHelper.Click(chosenNode);
            await WaitHelper.ForTask(_roomEnteredTcs.Task, ct,
                AutoSlayConfig.mapScreenTimeout, "Room not entered after map click");
        }
        finally
        {
            RunManager.Instance.RoomEntered -= OnRoomEntered;
            _roomEnteredTcs = null;
        }

        Logger.Log("[RlMap] Map navigation complete");
    }

    private void OnRoomEntered()
    {
        _roomEnteredTcs?.TrySetResult();
    }
}

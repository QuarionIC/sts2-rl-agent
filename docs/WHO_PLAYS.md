# Who plays when you launch Slay the Spire 2

## Current behaviour

The mod launches AutoSlay **unconditionally** at startup — `bridge_mod/MainFile.cs`,
"Phase 3":

```csharp
// Phase 3: Launch AutoSlay with RL handlers on Godot main thread.
TaskHelper.RunSafely(LaunchRlAutoSlayAsync());
```

There is no condition on that line, which is why opening the game hands the run
straight to the bot. Taking the controller back currently means removing the mod.

## The toggle

`scripts/who_plays.py` writes a flag file (`sts2_who_plays.txt`, containing
`human` or `agent`) next to the mod. Absence of the file means **agent**, so
today's behaviour is preserved for anyone who never runs the script.

```bash
python scripts/who_plays.py human   # you play
python scripts/who_plays.py agent   # the agent plays
python scripts/who_plays.py         # show current setting
```

## Required C# change (one rebuild)

The Python side is done; the mod must be taught to read the flag. In
`MainFile.cs`, replace the unconditional Phase 3 launch with:

```csharp
// Phase 3: Launch AutoSlay with RL handlers on Godot main thread,
// unless the player has taken the controller back.
if (ShouldAgentPlay())
{
    TaskHelper.RunSafely(LaunchRlAutoSlayAsync());
}
else
{
    Logger.Log("sts2_who_plays = human -- AutoSlay disabled, you have the controller.");
}
```

and add:

```csharp
/// <summary>
/// Read the sts2_who_plays flag written by scripts/who_plays.py.
/// Absent or unreadable => agent plays, preserving prior behaviour.
/// </summary>
private static bool ShouldAgentPlay()
{
    try
    {
        string dir = System.IO.Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".";
        string path = System.IO.Path.Combine(dir, "sts2_who_plays.txt");
        if (!System.IO.File.Exists(path))
            return true;
        string value = System.IO.File.ReadAllText(path).Trim().ToLowerInvariant();
        return !value.StartsWith("human");
    }
    catch
    {
        return true;
    }
}
```

The string `sts2_who_plays` in `MainFile.cs` is what `who_plays.py` looks for to
report whether the deployed mod honours the flag, so keep that literal.

Rebuild and redeploy per `docs/MOD_BUILD_GUIDE.md`.

## Related: unlocking the combat planner

The same rebuild is a good moment to emit the fields the deterministic combat
planner needs. See `sts2_env/bridge/combat_reconstruct.py` — currently the mod
sends `draw_pile_count` (an integer) but not the ordered pile, so the planner
cannot be used against the live game and combat falls back to the LLM.

In `RlCombatHandler.SerializeCombatState`, add:

```csharp
["draw_pile"]    = pcs.DrawPile.Cards.Select(SerializeCard).ToList(),
["discard_pile"] = pcs.DiscardPile.Cards.Select(SerializeCard).ToList(),
["exhaust_pile"] = pcs.ExhaustPile.Cards.Select(SerializeCard).ToList(),
["deck"]         = runState.Player.Deck.Cards.Select(SerializeCard).ToList(),
```

`CardPile.Cards` is confirmed present in the decompiled v0.109.0 source
(`CardPileCmd.cs` uses `targetPile.Cards.Count`, and `Shuffle` reorders that
same list), so the accessor is real. **Unverified:** whether index 0 is the top
of the draw pile or the bottom. If the planner's first live plan diverges
immediately, reversing `draw_pile` is the first thing to try.

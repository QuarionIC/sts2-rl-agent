using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Utils;

[Obsolete("No longer differs between main and beta.")]
public class CombatStateWrapper(object combatState)
{
	private static readonly VariableReference<IRunState> RunStateRef;

	private static readonly VariableReference<IReadOnlyList<Creature>> AlliesRef;

	private static readonly VariableReference<IReadOnlyList<Creature>> EnemiesRef;

	private static readonly VariableReference<IReadOnlyList<ModifierModel>> ModifiersRef;

	private static readonly VariableReference<MultiplayerScalingModel?> MultiplayerScalingModelRef;

	private static readonly VariableReference<int> RoundNumberRef;

	private static readonly VariableReference<CombatSide> CurrentSideRef;

	private static readonly VariableReference<List<Creature>> EscapedCreaturesRef;

	private static readonly VariableReference<IReadOnlyList<Creature>> HittableEnemiesRef;

	public object WrappedState => combatState;

	public IRunState RunState => RunStateRef.Get(combatState);

	public IReadOnlyList<Creature> Allies => AlliesRef.Get(combatState);

	public IReadOnlyList<Creature> Enemies => EnemiesRef.Get(combatState);

	public IReadOnlyList<Creature> Creatures => Allies.Concat(Enemies).ToList();

	public IReadOnlyList<Creature> PlayerCreatures => Creatures.Where((Creature c) => c.IsPlayer).ToList();

	public IReadOnlyList<Player> Players => PlayerCreatures.Select((Creature c) => c.Player).ToList();

	public IReadOnlyList<ModifierModel> Modifiers => ModifiersRef.Get(combatState);

	public MultiplayerScalingModel? MultiplayerScalingModel => MultiplayerScalingModelRef.Get(combatState);

	public int RoundNumber => RoundNumberRef.Get(combatState);

	public CombatSide CurrentSide => CurrentSideRef.Get(combatState);

	public List<Creature> EscapedCreatures => EscapedCreaturesRef.Get(combatState);

	public IReadOnlyList<Creature> HittableEnemies => HittableEnemiesRef.Get(combatState);

	public IReadOnlyList<Creature> CreaturesOnCurrentSide => GetCreaturesOnSide(CurrentSide);

	static CombatStateWrapper()
	{
		Type type = null;
		try
		{
			type = Type.GetType("MegaCrit.Sts2.Core.Combat.ICombatState, sts2");
		}
		catch (Exception)
		{
		}
		if (type == null)
		{
			type = Type.GetType("MegaCrit.Sts2.Core.Combat.CombatState, sts2");
		}
		if (type == null)
		{
			throw new Exception("Failed to get combat state type in CombatStateWrapper for compatibility");
		}
		RunStateRef = new VariableReference<IRunState>(type, "RunState");
		AlliesRef = new VariableReference<IReadOnlyList<Creature>>(type, "Allies");
		EnemiesRef = new VariableReference<IReadOnlyList<Creature>>(type, "Enemies");
		ModifiersRef = new VariableReference<IReadOnlyList<ModifierModel>>(type, "Modifiers");
		MultiplayerScalingModelRef = new VariableReference<MultiplayerScalingModel>(type, "MultiplayerScalingModel");
		RoundNumberRef = new VariableReference<int>(type, "RoundNumber");
		CurrentSideRef = new VariableReference<CombatSide>(type, "CurrentSide");
		EscapedCreaturesRef = new VariableReference<List<Creature>>(type, "EscapedCreatures");
		HittableEnemiesRef = new VariableReference<IReadOnlyList<Creature>>(type, "HittableEnemies");
	}

	public IReadOnlyList<Creature> GetCreaturesOnSide(CombatSide side)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Invalid comparison between Unknown and I4
		if ((int)side != 2)
		{
			return Allies;
		}
		return Enemies;
	}

	public IReadOnlyList<Creature> GetOpponentsOf(Creature creature)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return GetCreaturesOnSide(CombatSideExtensions.GetOppositeSide(creature.Side));
	}

	public IReadOnlyList<Creature> GetTeammatesOf(Creature creature)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return GetCreaturesOnSide(creature.Side);
	}

	public Player? GetPlayer(ulong playerId)
	{
		return ((IEnumerable<Player>)Players).FirstOrDefault((Func<Player, bool>)((Player p) => p.NetId == playerId));
	}

	public bool HappenedThisTurn(CombatHistoryEntry entry)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (RoundNumber == entry.RoundNumber)
		{
			return CurrentSide == entry.CurrentSide;
		}
		return false;
	}
}

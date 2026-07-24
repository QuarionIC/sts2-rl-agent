using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace BaseLib.Abstracts;

public interface ICustomResourceCost
{
	int Canonical { get; }

	bool CostsX { get; }

	bool WasJustUpgraded { get; set; }

	bool HasLocalModifiers { get; }

	int CapturedXValue { get; set; }

	bool IsOptional(Player? p);

	int GetWithModifiers(CostModifiers modifiers);

	int ResolveXValue();

	int GetAmountToSpend();

	int GetResolved();

	UnplayableReason ResourceCheck(PlayerCombatState combatState, CardModel card);

	void SetUntilPlayed(int cost, bool reduceOnly = false);

	void SetThisTurnOrUntilPlayed(int cost, bool reduceOnly = false);

	void SetThisTurn(int cost, bool reduceOnly = false);

	void SetThisCombat(int cost, bool reduceOnly = false);

	void AddUntilPlayed(int amount, bool reduceOnly = false);

	void AddThisTurnOrUntilPlayed(int amount, bool reduceOnly = false);

	void AddThisTurn(int amount, bool reduceOnly = false);

	void AddThisCombat(int amount, bool reduceOnly = false);

	bool EndOfTurnCleanup();

	bool AfterCardPlayedCleanup();

	void UpgradeCostBy(int addend);

	void FinalizeUpgrade();

	void ResetForDowngrade();

	void SetCustomBaseCost(int newBaseCost);

	void UpdateCostVisuals(NCard nCard, PileType pileType);
}

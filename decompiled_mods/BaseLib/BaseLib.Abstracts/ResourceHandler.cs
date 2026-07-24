using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

internal class ResourceHandler(string id, Func<PlayerCombatState, CustomResource> getResource, Func<CardModel, ICustomResourceCost?> getCost, Action<PlayerCombatState> prep, Action<PlayerCombatState> cleanup, Func<PlayerCombatState, CardModel, UnplayableReason> resourceCheck, Func<CardModel, Task> spend, Action<CardPlay> recordSpend, Action<CardModel> afterCardPlayedCleanup, Action<CardModel> endOfTurnCleanup, Action<CardModel> setToFreeThisCombat, Action<CardModel> setToFreeThisTurn, Action<CardModel> finalizeUpgrade, Action<CardModel> resetForDowngrade, Func<CardModel, bool, bool> costsMoreThanZero) : IComparable<ResourceHandler>
{
	public string Id { get; } = id;

	public Func<PlayerCombatState, CustomResource> GetResource { get; } = getResource;

	public Func<CardModel, ICustomResourceCost?> GetCost { get; } = getCost;

	public Action<PlayerCombatState> Prep { get; } = prep;

	public Action<PlayerCombatState> Cleanup { get; } = cleanup;

	public Func<PlayerCombatState, CardModel, UnplayableReason> ResourceCheck { get; } = resourceCheck;

	public Func<CardModel, Task> Spend { get; } = spend;

	public Action<CardPlay> RecordSpend { get; } = recordSpend;

	public Action<CardModel> AfterCardPlayedCleanup { get; } = afterCardPlayedCleanup;

	public Action<CardModel> EndOfTurnCleanup { get; } = endOfTurnCleanup;

	public Action<CardModel> SetToFreeThisCombat { get; } = setToFreeThisCombat;

	public Action<CardModel> SetToFreeThisTurn { get; } = setToFreeThisTurn;

	public Action<CardModel> FinalizeUpgrade { get; } = finalizeUpgrade;

	public Action<CardModel> ResetForDowngrade { get; } = resetForDowngrade;

	public Func<CardModel, bool, bool> CostsMoreThanZero { get; } = costsMoreThanZero;

	public int CompareTo(ResourceHandler? other)
	{
		return string.Compare(Id, other?.Id, StringComparison.Ordinal);
	}
}

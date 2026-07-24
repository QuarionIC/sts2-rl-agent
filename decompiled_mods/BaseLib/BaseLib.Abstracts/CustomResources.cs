using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public static class CustomResources<T> where T : CustomResource, new()
{
	private static bool _registered;

	private static NotNullSpireField<PlayerCombatState, T>? _resource;

	private static SpireField<CardModel, int>? _canonicalCost;

	private static SpireField<CardModel, CustomResourceCost<T>?>? _cost;

	private static SpireField<CardModel, int>? _lastSpend;

	private static SpireField<CardPlay, int>? _recordedSpend;

	private static SpireField<CardModel, int> CanonicalCostField => _canonicalCost ?? (_canonicalCost = new SpireField<CardModel, int>(() => -1).CopyOnClone());

	private static NotNullSpireField<PlayerCombatState, T> Resource => _resource ?? (_resource = new NotNullSpireField<PlayerCombatState, T>((Func<T>)delegate
	{
		BaseLibMain.Logger.Debug("Initializing resource " + typeof(T).Name + " for combat", 1);
		T val = new T();
		val.PrepForCombat();
		val.AmountChanged += CombatManager.Instance.StateTracker.OnPlayerCombatStateValueChanged;
		return val;
	}));

	private static SpireField<CardModel, CustomResourceCost<T>?> CostField => _cost ?? (_cost = new SpireField<CardModel, CustomResourceCost<T>>(() => (CustomResourceCost<T>?)null).CopyOnClone(delegate(CardModel source, CardModel dest, CustomResourceCost<T>? original)
	{
		CostField[dest] = original?.Clone(dest);
	}));

	private static SpireField<CardModel, int> LastSpend => _lastSpend ?? (_lastSpend = new SpireField<CardModel, int>(() => -1));

	private static SpireField<CardPlay, int> RecordedSpend => _recordedSpend ?? (_recordedSpend = new SpireField<CardPlay, int>(() => 0));

	internal static void Register(T resourceInstance)
	{
		if (!_registered)
		{
			_registered = true;
			CustomResourcePatches.RegisteredResources.InsertSorted(new ResourceHandler(resourceInstance.Id, Get, Cost, PrepForCombat, CleanupAfterCombat, ResourceCheck, Spend, RecordSpend, AfterCardPlayedCleanup, EndOfTurnCleanup, SetToFreeThisCombat, SetToFreeThisTurn, FinalizeUpgrade, ResetForDowngrade, CostsMoreThanZero));
		}
	}

	private static void PrepForCombat(PlayerCombatState combatState)
	{
		Resource.Get(combatState);
	}

	private static void CleanupAfterCombat(PlayerCombatState combatState)
	{
		Resource.Get(combatState).AmountChanged -= CombatManager.Instance.StateTracker.OnPlayerCombatStateValueChanged;
	}

	private static UnplayableReason ResourceCheck(PlayerCombatState combatState, CardModel card)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		return (UnplayableReason)(((_003F?)Cost(card)?.ResourceCheck(combatState, card)) ?? 0);
	}

	private static async Task Spend(CardModel card)
	{
		CustomResourceCost<T> customResourceCost = Cost(card);
		LastSpend[card] = -1;
		if (customResourceCost != null)
		{
			int spend = customResourceCost.GetAmountToSpend();
			if (await Resource.Get(card.Owner.PlayerCombatState).Spend<T>(card.CombatState, (AbstractModel?)(object)card, spend, customResourceCost.IsOptional(card.Owner)))
			{
				LastSpend[card] = spend;
			}
		}
	}

	private static void RecordSpend(CardPlay cardPlay)
	{
		RecordedSpend[cardPlay] = LastSpend[cardPlay.Card];
		BaseLibMain.Logger.Debug($"Recorded spend: {LastSpend[cardPlay.Card]}", 1);
	}

	private static void AfterCardPlayedCleanup(CardModel card)
	{
		Cost(card)?.AfterCardPlayedCleanup();
	}

	private static void EndOfTurnCleanup(CardModel card)
	{
		CustomResourceCost<T>? customResourceCost = Cost(card);
		if (customResourceCost != null && customResourceCost.EndOfTurnCleanup())
		{
			card.InvokeEnergyCostChanged();
		}
	}

	private static void SetToFreeThisCombat(CardModel card)
	{
		CustomResourceCost<T> customResourceCost = Cost(card);
		if (customResourceCost != null && Resource.Get(card.Owner.PlayerCombatState).ApplySharedModification)
		{
			customResourceCost.SetThisCombat(0);
		}
	}

	private static void SetToFreeThisTurn(CardModel card)
	{
		CustomResourceCost<T> customResourceCost = Cost(card);
		if (customResourceCost != null && Resource.Get(card.Owner.PlayerCombatState).ApplySharedModification)
		{
			customResourceCost.SetThisTurnOrUntilPlayed(0);
		}
	}

	private static void FinalizeUpgrade(CardModel card)
	{
		Cost(card)?.FinalizeUpgrade();
	}

	private static void ResetForDowngrade(CardModel card)
	{
		Cost(card)?.ResetForDowngrade();
	}

	private static bool CostsMoreThanZero(CardModel card, bool includeGlobalModifiers)
	{
		CustomResourceCost<T> customResourceCost = Cost(card);
		if (customResourceCost == null || customResourceCost.CostsX)
		{
			return false;
		}
		return customResourceCost.GetWithModifiers((CostModifiers)(includeGlobalModifiers ? (-1) : 2)) > 0;
	}

	public static T Get(PlayerCombatState combatState)
	{
		return Resource[combatState];
	}

	public static bool TryGet(PlayerCombatState? combatState, [NotNullWhen(true)] out T? result)
	{
		result = null;
		if (combatState == null)
		{
			return false;
		}
		result = Resource[combatState];
		return true;
	}

	public static void SetCanonicalCost(CardModel card, int canonicalCost)
	{
		CanonicalCostField[card] = canonicalCost;
	}

	public static void SetXCost(CardModel card)
	{
		CanonicalCostField[card] = int.MinValue;
	}

	public static int CanonicalCost(CardModel card)
	{
		return CanonicalCostField[card];
	}

	public static CustomResourceCost<T>? Cost(CardModel card)
	{
		CustomResourceCost<T> customResourceCost = CostField[card];
		if (customResourceCost != null)
		{
			return customResourceCost;
		}
		int num = CanonicalCostField[card];
		if (num == -1)
		{
			return null;
		}
		bool flag = num == int.MinValue;
		return CostField[card] = new CustomResourceCost<T>(card, (!flag) ? num : 0, flag);
	}

	public static int AmountSpent(CardPlay play)
	{
		return RecordedSpend[play];
	}

	public static bool WasSpent(CardPlay play)
	{
		return RecordedSpend[play] >= 0;
	}
}

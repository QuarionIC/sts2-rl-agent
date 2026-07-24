using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Downfall.DownfallCode.Events;
using Downfall.DownfallCode.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace Downfall.DownfallCode.Commands;

public class DownfallCardCmd
{
	public static readonly Func<CardModel, PlayerChoiceContext, CardPlay, Task> OnPlay = BuildOnPlayDelegate();

	public static async Task<T> GiveCard<T>(Player player, PileType pileType, CardPilePosition position = (CardPilePosition)1, bool upgraded = false, float animationTime = 0.6f, CardPreviewStyle animationStyle = (CardPreviewStyle)1, bool skipAnimation = false, Action<T>? action = null, Player? creator = null) where T : CardModel
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (creator == null)
		{
			creator = player;
		}
		T val = (T)(object)player.Creature.CombatState.CreateCard((CardModel)(object)ModelDb.Card<T>(), player);
		if (upgraded)
		{
			((CardModel)val).UpgradeInternal();
		}
		action?.Invoke(val);
		CardPileAddResult val2 = await CardPileCmd.AddGeneratedCardToCombat((CardModel)(object)val, pileType, creator, position);
		if (val2.success && !skipAnimation && (int)pileType != 2)
		{
			CardCmd.PreviewCardPileAdd(val2, animationTime, animationStyle);
		}
		return (T)(object)val2.cardAdded;
	}

	public static async Task<IEnumerable<T>> GiveCards<T>(Player player, PileType pileType, decimal count, CardPilePosition position = (CardPilePosition)1, bool upgraded = false, float animationTime = 0.6f, CardPreviewStyle animationStyle = (CardPreviewStyle)1, bool skipAnimation = false, Action<T>? action = null, Player? creator = null) where T : CardModel
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (creator == null)
		{
			creator = player;
		}
		if (count <= 0m)
		{
			return Array.Empty<T>();
		}
		List<CardModel> list = new List<CardModel>();
		T val = ModelDb.Card<T>();
		for (int i = 0; (decimal)i < count; i++)
		{
			T val2 = (T)(object)player.Creature.CombatState.CreateCard((CardModel)(object)val, player);
			if (upgraded)
			{
				((CardModel)val2).UpgradeInternal();
			}
			action?.Invoke(val2);
			list.Add((CardModel)(object)val2);
		}
		IReadOnlyList<CardPileAddResult> readOnlyList = await CardPileCmd.AddGeneratedCardsToCombat((IEnumerable<CardModel>)list, pileType, creator, position);
		if (!skipAnimation && (int)pileType != 2)
		{
			CardCmd.PreviewCardPileAdd(readOnlyList, animationTime, animationStyle);
		}
		return readOnlyList.Select((CardPileAddResult e) => (T)(object)e.cardAdded);
	}

	public static async Task AutoPlayFromDrawPile(PlayerChoiceContext choiceContext, Player player, int count, AutoPlayType autoPlayType = (AutoPlayType)1, bool skipXCapture = false)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return;
		}
		List<CardModel> cards = new List<CardModel>(count);
		CardPile drawPile = PileTypeExtensions.GetPile((PileType)1, player);
		int i = 0;
		while (i < count)
		{
			await CardPileCmd.ShuffleIfNecessary(choiceContext, player);
			if (drawPile.Cards.Count == 0)
			{
				break;
			}
			cards.Add(drawPile.Cards[0]);
			int num = i + 1;
			i = num;
		}
		foreach (CardModel item in cards.TakeWhile((CardModel card) => !card.Owner.Creature.IsDead))
		{
			await CardCmd.AutoPlay(choiceContext, item, (Creature)null, autoPlayType, skipXCapture, false);
		}
	}

	public static async Task AnimateCardFromRewardScreen(PileType pile, CardModel card, Player player)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		NCard node = NCard.Create(card, (ModelVisibility)1);
		if (node == null)
		{
			return;
		}
		NRun instance = NRun.Instance;
		Control val = ((instance != null) ? instance.GlobalUi.CardPreviewContainer : null);
		NRun instance2 = NRun.Instance;
		Node trailContainer = ((instance2 != null) ? instance2.GlobalUi.TopBar.TrailContainer : null);
		if (val != null && trailContainer != null)
		{
			GodotTreeExtensions.AddChildSafely((Node)(object)val, (Node)(object)node);
			Tween val2 = ((Node)node).CreateTween();
			val2.TweenProperty((GodotObject)(object)node, NodePath.op_Implicit("scale"), Variant.op_Implicit(Vector2.One), 0.25).From(Variant.op_Implicit(Vector2.Zero)).SetEase((EaseType)1)
				.SetTrans((TransitionType)7);
			await ((GodotObject)node).ToSignal((GodotObject)(object)val2, SignalName.Finished);
			NCardFlyVfx val3 = NCardFlyVfx.Create(node, pile, true, player.Character.TrailPath);
			GodotTreeExtensions.AddChildSafely(trailContainer, (Node)(object)val3);
			if (val3 != null)
			{
				await ((GodotObject)val3).ToSignal((GodotObject)(object)val3, SignalName.TreeExited);
			}
		}
	}

	public static async Task<CardPileAddResult> DrawFromCustomPile(PlayerChoiceContext ctx, Player player, PileType pileType)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (player.Creature.CombatState == null)
		{
			return default(CardPileAddResult);
		}
		CustomPile customPile = CustomPiles.GetCustomPile(player.PlayerCombatState, pileType);
		CardPileAddResult result = (CardPileAddResult)((customPile != null && ((CardPile)customPile).Cards.Count != 0) ? (await CardPileCmd.Add(((CardPile)customPile).Cards[0], (PileType)2, (CardPilePosition)1, (AbstractModel)null, false)) : default(CardPileAddResult));
		await DownfallHook.AfterCustomDraw(player.Creature.CombatState, ctx, player, pileType, result);
		return result;
	}

	public static async Task<IReadOnlyList<CardPileAddResult>> DrawFromCustomPile(PlayerChoiceContext ctx, Player player, PileType pileType, int amount)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		List<CardPileAddResult> result = new List<CardPileAddResult>();
		for (int i = 0; i < amount; i++)
		{
			List<CardPileAddResult> list = result;
			list.Add(await DrawFromCustomPile(ctx, player, pileType));
		}
		return result;
	}

	public static async Task<IEnumerable<CardModel>> SelectFromCards(PlayerChoiceContext ctx, IReadOnlyList<CardModel> cards, LocString prompt, int count, CardModel cardSource, bool optional = false)
	{
		return await CardSelectCmd.FromSimpleGrid(ctx, cards, cardSource.Owner, new CardSelectorPrefs(prompt, (!optional) ? count : 0, count));
	}

	public static async Task<IEnumerable<CardModel>> SelectFromCards(PlayerChoiceContext ctx, IReadOnlyList<CardModel> cards, LocString prompt, CardModel cardSource, bool optional = false)
	{
		int count = ((!cardSource.DynamicVars.ContainsKey("Cards")) ? 1 : ((DynamicVar)cardSource.DynamicVars.Cards).IntValue);
		return await SelectFromCards(ctx, cards, prompt, count, cardSource, optional);
	}

	public static async Task<IEnumerable<CardModel>> SelectFromHand(PlayerChoiceContext ctx, LocString prompt, int count, CardModel cardSource, Func<CardModel, bool>? filter = null, bool optional = false)
	{
		return await CardSelectCmd.FromHand(ctx, cardSource.Owner, new CardSelectorPrefs(prompt, (!optional) ? count : 0, count), filter, (AbstractModel)(object)cardSource);
	}

	public static async Task<IEnumerable<CardModel>> SelectFromHand(PlayerChoiceContext ctx, LocString prompt, CardModel cardSource, Func<CardModel, bool>? filter = null, bool optional = false)
	{
		int count = ((!cardSource.DynamicVars.ContainsKey("Cards")) ? 1 : ((DynamicVar)cardSource.DynamicVars.Cards).IntValue);
		return await SelectFromHand(ctx, prompt, count, cardSource, filter, optional);
	}

	public static async Task<IEnumerable<CardModel>> SelectFromHand(PlayerChoiceContext ctx, LocString prompt, int count, PowerModel powerSource, Func<CardModel, bool>? filter = null, bool optional = false)
	{
		return await CardSelectCmd.FromHand(ctx, powerSource.Owner.Player, new CardSelectorPrefs(prompt, (!optional) ? count : 0, count), filter, (AbstractModel)(object)powerSource);
	}

	public static async Task<IEnumerable<CardModel>> SelectFromHand(PlayerChoiceContext ctx, LocString prompt, PowerModel powerSource, Func<CardModel, bool>? filter = null, bool optional = false)
	{
		int amount = powerSource.Amount;
		return await SelectFromHand(ctx, prompt, amount, powerSource, filter, optional);
	}

	public static void ForceUpgrade(CardModel card, int upgrade = 1)
	{
		ForceUpgradeHelper.ForceUpgrade(card, upgrade);
	}

	public static async Task AddWithIndex(CardModel card, CardPile cardPile, int index)
	{
		cardPile.AddInternal(card, index, false);
		cardPile.InvokeCardAddFinished();
		await Hook.AfterCardChangedPiles(card.Owner.RunState, card.Owner.Creature.CombatState, card, (PileType)0, (AbstractModel)null);
		CardCmd.PreviewCardPileAdd(new CardPileAddResult
		{
			cardAdded = card,
			success = true,
			oldPile = null,
			modifyingModels = null
		}, 0.6f, (CardPreviewStyle)1);
	}

	private static Func<CardModel, PlayerChoiceContext, CardPlay, Task> BuildOnPlayDelegate()
	{
		MethodInfo method = typeof(CardModel).GetMethod("OnPlay", BindingFlags.Instance | BindingFlags.NonPublic);
		ParameterExpression parameterExpression = Expression.Parameter(typeof(CardModel), "instance");
		ParameterExpression parameterExpression2 = Expression.Parameter(typeof(PlayerChoiceContext), "ctx");
		ParameterExpression parameterExpression3 = Expression.Parameter(typeof(CardPlay), "cardPlay");
		return Expression.Lambda<Func<CardModel, PlayerChoiceContext, CardPlay, Task>>(Expression.Call(parameterExpression, method, parameterExpression2, parameterExpression3), new ParameterExpression[3] { parameterExpression, parameterExpression2, parameterExpression3 }).Compile();
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using BaseLib.Utils.Patching;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.addons.mega_text;

namespace BaseLib.Commands;

public static class MultiPileCardSelect
{
	[HarmonyPatch(typeof(NCardGrid), "SetCards")]
	private static class SortCardsPatch
	{
		private static void Postfix(NCardGrid __instance)
		{
			PileType[] pileTypes;
			if (IsGridMultiPile.Get(__instance))
			{
				pileTypes = PileTypes.Get(__instance);
				((List<CardModel>)AccessTools.Field(typeof(NCardGrid), "_cards").GetValue(__instance)).Sort((CardModel a, CardModel b) => PileOrder(a).CompareTo(PileOrder(b)));
				PileTypes.Set(__instance, null);
			}
			int PileOrder(CardModel c)
			{
				//IL_0014: Unknown result type (might be due to invalid IL or missing references)
				//IL_002c: Unknown result type (might be due to invalid IL or missing references)
				if (c.Pile != null && pileTypes.Contains(c.Pile.Type))
				{
					return ListExtensions.IndexOf<PileType>((IReadOnlyList<PileType>)pileTypes, c.Pile.Type);
				}
				return 2147483646;
			}
		}
	}

	[HarmonyPatch(typeof(NCardGrid), "InitGrid", new Type[] { })]
	private static class AddPileIndicatorNodePatch
	{
		private static List<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return new InstructionPatcher(instructions).Match(new InstructionMatcher().call(AccessTools.Method(typeof(NGridCardHolder), "Create", (Type[])null, (Type[])null)).stloc_s()).Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[3]
			{
				CodeInstruction.LoadArgument(0, false),
				CodeInstruction.LoadLocal(10, false),
				CodeInstruction.Call(typeof(MultiPileCardSelect), "AddIndicatorNode", (Type[])null, (Type[])null)
			}));
		}
	}

	[HarmonyPatch(typeof(NCardGrid), "AssignCardsToRow")]
	private static class RefreshPileIndicatorNodePatch
	{
		private static List<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return new InstructionPatcher(instructions).Match(new InstructionMatcher().callvirt(AccessTools.Method(typeof(NCardHolder), "ReassignToCard", (Type[])null, (Type[])null))).Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[3]
			{
				CodeInstruction.LoadArgument(0, false),
				CodeInstruction.LoadLocal(1, false),
				CodeInstruction.Call(typeof(MultiPileCardSelect), "AddIndicatorNode", (Type[])null, (Type[])null)
			}));
		}
	}

	[HarmonyPatch(typeof(NGridCardHolder), "OnFocus")]
	private static class ShowTipPatch
	{
		private static void Postfix(NGridCardHolder __instance)
		{
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			MegaLabel val = PileNameLabel.Get(__instance);
			if (val != null)
			{
				Tween obj = ((Node)__instance).CreateTween();
				obj.SetParallel(true);
				obj.SetEase((EaseType)1);
				obj.SetTrans((TransitionType)4);
				obj.TweenProperty((GodotObject)(object)val, NodePath.op_Implicit("position:x"), Variant.op_Implicit(-262f), 0.20000000298023224);
				obj.TweenProperty((GodotObject)(object)val, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(1f), 0.20000000298023224);
			}
		}
	}

	[HarmonyPatch(typeof(NCardHolder), "OnUnfocus")]
	private static class HideTipPatch
	{
		private static void Postfix(NCardHolder __instance)
		{
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			NGridCardHolder val = (NGridCardHolder)(object)((__instance is NGridCardHolder) ? __instance : null);
			if (val != null)
			{
				MegaLabel val2 = PileNameLabel.Get(val);
				if (val2 != null)
				{
					Tween obj = ((Node)__instance).CreateTween();
					obj.SetParallel(true);
					obj.SetEase((EaseType)1);
					obj.SetTrans((TransitionType)4);
					obj.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("position:x"), Variant.op_Implicit(-185f), 0.20000000298023224);
					obj.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), 0.20000000298023224);
				}
			}
		}
	}

	private const float IconSize = 65f;

	private const float LabelMargin = 12f;

	private const float LabelSize = 250f;

	private const float LabelTweenTime = 0.2f;

	private static readonly SpireField<NCardGrid, bool> IsGridMultiPile = new SpireField<NCardGrid, bool>((NCardGrid _) => false);

	private static readonly SpireField<NCardGrid, PileType[]> PileTypes = new SpireField<NCardGrid, PileType[]>((NCardGrid _) => (PileType[]?)null);

	private static readonly SpireField<NGridCardHolder, TextureRect> PileIndicator = new SpireField<NGridCardHolder, TextureRect>((NGridCardHolder _) => (TextureRect?)null);

	private static readonly SpireField<NGridCardHolder, MegaLabel> PileNameLabel = new SpireField<NGridCardHolder, MegaLabel>((NGridCardHolder _) => (MegaLabel?)null);

	private static readonly Dictionary<PileType, (LocString Title, string TexturePath)> RegisteredPileIndicators = new Dictionary<PileType, (LocString, string)>
	{
		[(PileType)1] = (new LocString("card_selection", "BASELIB-DRAW_PILE"), "res://images/packed/combat_ui/draw_pile.png"),
		[(PileType)3] = (new LocString("card_selection", "BASELIB-DISCARD_PILE"), "res://images/packed/combat_ui/discard_pile.png"),
		[(PileType)4] = (new LocString("card_selection", "BASELIB-EXHAUST_PILE"), "res://images/packed/combat_ui/exhaust_pile.png"),
		[(PileType)2] = (new LocString("card_selection", "BASELIB-HAND"), "res://images/powers/hello_world_power.png"),
		[(PileType)6] = (new LocString("card_selection", "BASELIB-DECK"), "res://images/atlases/ui_atlas.sprites/top_bar/top_bar_deck.tres")
	};

	internal static void RegisterPileIndicator(PileType pileType, string texturePath, LocString name)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		RegisteredPileIndicators[pileType] = (name, texturePath);
	}

	public static async Task<IEnumerable<CardModel>> Select(PlayerChoiceContext context, Player player, CardSelectorPrefs prefs, Func<CardModel, bool>? filter = null, params PileType[] pileTypes)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (CombatManager.Instance.IsEnding)
		{
			return Array.Empty<CardModel>();
		}
		IEnumerable<CardModel> source = pileTypes.SelectMany(delegate(PileType p)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Invalid comparison between Unknown and I4
			IEnumerable<CardModel> enumerable = PileTypeExtensions.GetPile(p, player).Cards;
			if ((int)p == 1)
			{
				enumerable = from c in enumerable
					orderby c.Rarity, ((AbstractModel)c).Id
					select c;
			}
			return enumerable;
		});
		if (filter != null)
		{
			source = source.Where(filter);
		}
		List<CardModel> cards = source.ToList();
		return await Select(context, player, prefs, cards, pileTypes);
	}

	public static async Task<IEnumerable<CardModel>> Select(PlayerChoiceContext context, Player player, CardSelectorPrefs prefs, List<CardModel> cards, PileType[]? pileTypes = null)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (CombatManager.Instance.IsEnding || cards.Count == 0)
		{
			return Array.Empty<CardModel>();
		}
		List<CardModel> result;
		if (!((CardSelectorPrefs)(ref prefs)).RequireManualConfirmation && cards.Count <= ((CardSelectorPrefs)(ref prefs)).MinSelect)
		{
			result = cards;
		}
		else
		{
			uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
			await context.SignalPlayerChoiceBegunCompatibility(player, (PlayerChoiceOptions)0);
			if ((bool)AccessTools.Method(typeof(CardSelectCmd), "ShouldSelectLocalCard", (Type[])null, (Type[])null).Invoke(null, new object[1] { player }))
			{
				NPlayerHand instance = NPlayerHand.Instance;
				if (instance != null)
				{
					instance.CancelAllCardPlay();
				}
				NSimpleCardSelectScreen nSimpleCardSelectScreen = NSimpleCardSelectScreen.Create((IReadOnlyList<CardModel>)cards, prefs);
				NCardGrid node = ((Node)nSimpleCardSelectScreen).GetNode<NCardGrid>(NodePath.op_Implicit("%CardGrid"));
				IsGridMultiPile.Set(node, val: true);
				PileTypes.Set(node, pileTypes ?? Array.Empty<PileType>());
				NOverlayStack.Instance.Push((IOverlayScreen)(object)nSimpleCardSelectScreen);
				result = (await ((NCardGridSelectionScreen)nSimpleCardSelectScreen).CardsSelected()).ToList();
				((NCardGridSelectionScreen)nSimpleCardSelectScreen)._grid._cardRows.ForEach(delegate(List<NGridCardHolder> r)
				{
					r.ForEach(delegate(NGridCardHolder n)
					{
						ClearHolderIndicatorNodes(n);
					});
				});
				List<int> list = result.Select((CardModel c) => cards.IndexOf(c)).ToList();
				RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(player, choiceId, PlayerChoiceResult.FromIndexes(list));
			}
			else
			{
				result = (from i in (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(player, choiceId)).AsIndexes()
					select cards[i]).ToList();
			}
			await context.SignalPlayerChoiceEnded();
		}
		AccessTools.Method(typeof(CardSelectCmd), "LogChoice", (Type[])null, (Type[])null).Invoke(null, new object[2] { player, result });
		return result;
	}

	private static void ClearHolderIndicatorNodes(NGridCardHolder holder)
	{
		TextureRect? obj = PileIndicator.Get(holder);
		if (obj != null)
		{
			((Node)obj).QueueFree();
		}
		PileIndicator.Set(holder, null);
		PileNameLabel.Set(holder, null);
	}

	private static void AddIndicatorNode(NCardGrid grid, NGridCardHolder holder)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Expected O, but got Unknown
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Expected O, but got Unknown
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		ClearHolderIndicatorNodes(holder);
		if (!IsGridMultiPile.Get(grid))
		{
			return;
		}
		NCard cardNode = ((NCardHolder)holder).CardNode;
		PileType? obj;
		if (cardNode == null)
		{
			obj = null;
		}
		else
		{
			CardModel model = cardNode.Model;
			if (model == null)
			{
				obj = null;
			}
			else
			{
				CardPile pile = model.Pile;
				obj = ((pile != null) ? new PileType?(pile.Type) : ((PileType?)null));
			}
		}
		PileType? val = obj;
		if (val.HasValue)
		{
			PileType valueOrDefault = val.GetValueOrDefault();
			if (RegisteredPileIndicators.TryGetValue(valueOrDefault, out (LocString, string) value))
			{
				TextureRect val2 = new TextureRect
				{
					Position = new Vector2(110f, -230f),
					Size = Vector2.One * 65f,
					MouseFilter = (MouseFilterEnum)1,
					ExpandMode = (ExpandModeEnum)1,
					StretchMode = (StretchModeEnum)5,
					Texture = ResourceLoader.Load<Texture2D>(value.Item2, (string)null, (CacheMode)1)
				};
				PileIndicator.Set(holder, val2);
				((Node)holder).AddChild((Node)(object)val2, false, (InternalMode)0);
				MegaLabel val3 = new MegaLabel();
				Color white = Colors.White;
				white.A = 0f;
				((CanvasItem)val3).Modulate = white;
				((Control)val3).Size = new Vector2(250f, 65f);
				((Control)val3).Position = new Vector2(-185f, 0f);
				((Label)val3).VerticalAlignment = (VerticalAlignment)0;
				((Label)val3).HorizontalAlignment = (HorizontalAlignment)2;
				val3.MinFontSize = 16;
				val3.MaxFontSize = 24;
				((Control)val3).MouseFilter = (MouseFilterEnum)2;
				((CanvasItem)val3).ShowBehindParent = true;
				MegaLabel val4 = val3;
				((Control)val4).AddThemeFontOverride(Label.Font, (Font)(object)ResourceLoader.Load<FontVariation>("res://themes/kreon_regular_shared.tres", (string)null, (CacheMode)1));
				StringName fontColor = Label.FontColor;
				white = Colors.White;
				white.A = 0.75f;
				((Control)val4).AddThemeColorOverride(fontColor, white);
				StringName fontOutlineColor = Label.FontOutlineColor;
				white = Colors.Black;
				white.A = 0.75f;
				((Control)val4).AddThemeColorOverride(fontOutlineColor, white);
				StringName fontShadowColor = Label.FontShadowColor;
				white = Colors.Black;
				white.A = 0.2f;
				((Control)val4).AddThemeColorOverride(fontShadowColor, white);
				((Control)val4).AddThemeConstantOverride(Label.OutlineSize, 10);
				((Control)val4).AddThemeConstantOverride(StringName.op_Implicit("shadow_offset_x"), 3);
				((Control)val4).AddThemeConstantOverride(StringName.op_Implicit("shadow_offset_y"), 3);
				val4.SetTextAutoSize(value.Item1.GetFormattedText());
				((Node)val2).AddChild((Node)(object)val4, false, (InternalMode)0);
				PileNameLabel.Set(holder, val4);
			}
		}
	}
}

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Events;
using Champ.ChampCode.Extensions;
using Champ.ChampCode.Stance;
using Champ.ChampCode.Vfx;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Champ.ChampCode.Core;

public class ChampModel : CustomSingletonModel
{
	private static readonly SpireField<Player, ChampStanceModel> ActiveStance = new SpireField<Player, ChampStanceModel>((Func<ChampStanceModel>)ChampModelDb.ChampStance<ChampNoStance>);

	private static readonly ConditionalWeakTable<Player, NChampStanceDisplay> StanceDisplays = new ConditionalWeakTable<Player, NChampStanceDisplay>();

	public ChampModel()
		: base((HookType)1)
	{
	}

	public override async Task BeforeCardPlayed(CardPlay cardPlay)
	{
		CardModel card = cardPlay.Card;
		Player owner = card.Owner;
		ChampStanceModel champStanceModel = owner.ChampStance();
		bool flag = ChampHook.IgnoreChargeCap(owner.Creature.CombatState, owner);
		if ((int)card.Type == 2 && (flag || champStanceModel.Charges > 0))
		{
			if (!flag)
			{
				champStanceModel.Charges--;
				RefreshDisplay(owner);
			}
			await champStanceModel.SkillBonus((PlayerChoiceContext)new BlockingPlayerChoiceContext());
		}
	}

	public static T GetStanceAs<T>(Player player) where T : ChampStanceModel
	{
		return ActiveStance[player] as T;
	}

	public static ChampStanceModel GetStanceModel(Player player)
	{
		return ActiveStance[player] ?? ChampModelDb.ChampStance<ChampNoStance>();
	}

	public static bool IsInStance<T>(Player player) where T : ChampStanceModel
	{
		return ActiveStance[player] is T;
	}

	private static NChampStanceDisplay? GetDisplay(Player player)
	{
		if (!StanceDisplays.TryGetValue(player, out NChampStanceDisplay value))
		{
			return null;
		}
		return value;
	}

	private static void RegisterDisplay(Player player, NChampStanceDisplay display)
	{
		StanceDisplays.AddOrUpdate(player, display);
	}

	public static void RefreshDisplay(Player player)
	{
		GetDisplay(player)?.Refresh();
	}

	public static async Task SetStance<T>(PlayerChoiceContext ctx, Player player) where T : ChampStanceModel
	{
		await SetStance(ctx, player, ChampModelDb.ChampStance<T>());
	}

	private static async Task SetStance(PlayerChoiceContext ctx, Player player, ChampStanceModel newCanonical)
	{
		ChampStanceModel current = ActiveStance[player];
		if (!(((object)current)?.GetType() == ((object)newCanonical).GetType()))
		{
			if (current != null)
			{
				await current.OnExit(ctx);
			}
			ChampStanceModel champStanceModel = newCanonical.ToMutable(player);
			ActiveStance[player] = champStanceModel;
			await champStanceModel.OnEnter(ctx);
			TriggerStanceAnimation(player);
			await ChampHook.OnChampStanceChange(player.Creature.CombatState, ctx, player, current, ActiveStance[player]);
			RefreshStanceDisplay(player, newCanonical);
		}
	}

	public override Task BeforeCombatStart()
	{
		CombatState val = CombatManager.Instance.DebugOnlyGetState();
		if (val == null)
		{
			return Task.CompletedTask;
		}
		foreach (Player player in val.Players)
		{
			ActiveStance[player] = ChampModelDb.ChampStance<ChampNoStance>();
		}
		return Task.CompletedTask;
	}

	private static void TriggerStanceAnimation(Player player)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		Callable val = Callable.From((Action)delegate
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature obj = ((instance != null) ? instance.GetCreatureNode(player.Creature) : null);
			if (((obj != null) ? obj.Visuals : null) is NChampCreatureVisuals nChampCreatureVisuals)
			{
				ChampStanceModel champStanceModel = ActiveStance[player];
				NChampCreatureVisuals.Stance currentStance = ((champStanceModel is ChampBerserkerStance) ? NChampCreatureVisuals.Stance.Berserker : ((champStanceModel is ChampDefensiveStance) ? NChampCreatureVisuals.Stance.Defensive : ((champStanceModel is ChampUltimateStance) ? NChampCreatureVisuals.Stance.Ultimate : NChampCreatureVisuals.Stance.Normal)));
				nChampCreatureVisuals.CurrentStance = currentStance;
				nChampCreatureVisuals.OnAnimationTrigger("Idle");
			}
		});
		((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
	}

	private static void RemoveDisplay(Player player)
	{
		StanceDisplays.Remove(player);
	}

	private static void RefreshStanceDisplay(Player player, ChampStanceModel newCanonical)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		Callable val = Callable.From((Action)delegate
		{
			NChampStanceDisplay display = GetDisplay(player);
			if (newCanonical is ChampNoStance)
			{
				if (display != null && GodotObject.IsInstanceValid((GodotObject)(object)display))
				{
					((Node)display).QueueFree();
				}
				RemoveDisplay(player);
			}
			else if (display == null || !GodotObject.IsInstanceValid((GodotObject)(object)display))
			{
				NChampStanceDisplay nChampStanceDisplay = NChampStanceDisplay.Show(player);
				if (nChampStanceDisplay != null)
				{
					RegisterDisplay(player, nChampStanceDisplay);
				}
			}
			else
			{
				display.Refresh();
			}
		});
		((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
	}
}

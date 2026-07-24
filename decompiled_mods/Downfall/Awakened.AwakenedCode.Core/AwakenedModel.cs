using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Displays;
using Awakened.AwakenedCode.Events;
using Awakened.AwakenedCode.Piles;
using Awakened.AwakenedCode.Vfx;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Vfx;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Awakened.AwakenedCode.Core;

public class AwakenedModel : CustomSingletonModel
{
	private static readonly ConditionalWeakTable<Player, StrongBox<int>> AwakenMeter = new ConditionalWeakTable<Player, StrongBox<int>>();

	private static readonly ConditionalWeakTable<Player, StrongBox<bool>> AwakenDispatched = new ConditionalWeakTable<Player, StrongBox<bool>>();

	private static readonly ConditionalWeakTable<CombatState, StrongBox<bool>> InitializedCombats = new ConditionalWeakTable<CombatState, StrongBox<bool>>();

	private static readonly ConditionalWeakTable<Player, StrongBox<bool>> InitializedSpellbooks = new ConditionalWeakTable<Player, StrongBox<bool>>();

	public AwakenedModel()
		: base((HookType)1)
	{
	}

	public static bool IsAwakened(Player? player)
	{
		if (player != null)
		{
			return AwakenMeter.GetOrCreateValue(player).Value >= 7;
		}
		return false;
	}

	public static bool MarkAwakened(Player player)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		StrongBox<bool> orCreateValue = AwakenDispatched.GetOrCreateValue(player);
		if (orCreateValue.Value)
		{
			return false;
		}
		StrongBox<int> orCreateValue2 = AwakenMeter.GetOrCreateValue(player);
		orCreateValue2.Value = 7;
		orCreateValue.Value = true;
		StatusBarHelper.SetStatus(player, orCreateValue2.Value, 7, (Color?)new Color(1442840575u));
		return true;
	}

	public override Task BeforeCombatStart()
	{
		AwakenMeter.Clear();
		AwakenDispatched.Clear();
		InitializedCombats.Clear();
		InitializedSpellbooks.Clear();
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		Player owner = cardPlay.Card.Owner;
		if (owner.Character is Awakened && !IsAwakened(owner) && (int)cardPlay.Card.Type == 3)
		{
			StrongBox<int> orCreateValue = AwakenMeter.GetOrCreateValue(owner);
			orCreateValue.Value++;
			StatusBarHelper.SetStatus(cardPlay.Card.Owner, orCreateValue.Value, 7, (Color?)new Color(1442840575u));
			if (IsAwakened(owner))
			{
				await AwakenedCmd.Awaken(owner, ctx);
			}
		}
	}

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (card is Void)
		{
			ICombatState val = card.CombatState ?? card.Owner.Creature.CombatState;
			if (val != null)
			{
				await AwakenedHook.OnDrained(val, choiceContext, card.Owner, 1);
			}
		}
	}

	internal static void SetupAwakenedCombatUi(CombatState state)
	{
		if (NCombatRoom.Instance == null)
		{
			return;
		}
		StrongBox<bool> orCreateValue = InitializedCombats.GetOrCreateValue(state);
		if (orCreateValue.Value)
		{
			return;
		}
		orCreateValue.Value = true;
		foreach (Player player in state.Players)
		{
			AwakenMeter.Remove(player);
			AwakenDispatched.Remove(player);
		}
		foreach (Player player2 in state.Players)
		{
			if (player2.Character is Awakened)
			{
				GetOrInitSpellbook(player2);
			}
		}
	}

	internal static AwakenedPile GetOrInitSpellbook(Player player)
	{
		AwakenedPile spellbookOrThrow = AwakenedCmd.GetSpellbookOrThrow(player);
		StrongBox<bool> orCreateValue = InitializedSpellbooks.GetOrCreateValue(player);
		if (!orCreateValue.Value)
		{
			spellbookOrThrow.Refresh(player);
			orCreateValue.Value = true;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null)
		{
			SetupAwakenedUi(instance, player);
		}
		return spellbookOrThrow;
	}

	private static void SetupAwakenedUi(NCombatRoom combatRoom, Player player)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if (!AwakenedDisplay.HasDisplay(player))
		{
			NSpellbookDisplay nSpellbookDisplay = NSpellbookDisplay.Create(player);
			Control combatVfxContainer = combatRoom.CombatVfxContainer;
			GodotTreeExtensions.AddChildSafely((Node)(object)combatVfxContainer, (Node)(object)nSpellbookDisplay);
			NCreature creatureNode = combatRoom.GetCreatureNode(player.Creature);
			if (creatureNode != null)
			{
				Vector2 topOfHitbox = creatureNode.GetTopOfHitbox();
				Transform2D globalTransform = ((CanvasItem)combatVfxContainer).GetGlobalTransform();
				((Control)nSpellbookDisplay).Position = ((Transform2D)(ref globalTransform)).AffineInverse() * topOfHitbox;
				((Control)nSpellbookDisplay).Position = ((Control)nSpellbookDisplay).Position + new Vector2(-120f, -80f);
			}
			AwakenedDisplay.Register(player, nSpellbookDisplay);
			nSpellbookDisplay.Refresh();
		}
	}
}

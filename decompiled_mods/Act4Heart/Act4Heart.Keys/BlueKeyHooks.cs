using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dolso;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.TreasureRelicPicking;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Act4Heart.Keys;

[Hook]
internal static class BlueKeyHooks
{
	[HookAfter(typeof(NTreasureRoom), "OpenChest")]
	private static void EnableSkip_On_OpenChest(NTreasureRoom __instance)
	{
		if (!ModMain.current_config.keys_enable)
		{
			return;
		}
		try
		{
			EnableTreasureSkip(__instance);
		}
		catch (Exception data)
		{
			log.error(data);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void EnableTreasureSkip(NTreasureRoom self)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected O, but got Unknown
		if (self._relicCollection._isEmptyChest)
		{
			return;
		}
		NProceedButton proceedButton = self._proceedButton;
		Player me = LocalContext.GetMe((IEnumerable<Player>)((IPlayerCollection)self._runState).Players);
		bool num = ((me != null) ? me.GetRelic<SapphireKey>() : null) != null;
		if (!num || !KeyRelicModel.EveryoneHasKey<SapphireKey>(self._runState))
		{
			proceedButton.UpdateText(NProceedButton.SkipLoc);
			((NClickableControl)proceedButton).Enable();
		}
		if (!num)
		{
			TextureRect key = new TextureRect
			{
				Texture = ((RelicModel)ModelDb.Relic<SapphireKey>()).BigIcon,
				ExpandMode = (ExpandModeEnum)1,
				Size = new Vector2(80f, 80f),
				Position = new Vector2(-100f, 0f)
			};
			GodotTreeExtensions.AddChildSafely((Node)(object)proceedButton, (Node)(object)key);
			Action<List<RelicPickingResult>> del = delegate
			{
				GodotTreeExtensions.QueueFreeSafely((Node)(object)key);
			};
			RunManager.Instance.TreasureRoomRelicSynchronizer.RelicsAwarded += del;
			((Node)key).TreeExited += delegate
			{
				RunManager.Instance.TreasureRoomRelicSynchronizer.RelicsAwarded -= del;
			};
		}
	}

	[Hook(typeof(TreasureRoomRelicSynchronizer), "OnPicked")]
	private static void ForceSingleplayerSkip_IL_OnPicked(ILContext il)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		ILCursor val = new ILCursor(il);
		ILLabel label = null;
		if (!val.TryGotoNext((MoveType)2, new Func<Instruction, bool>[3]
		{
			(Instruction a) => ILPatternMatchingExt.MatchCallvirt<IReadOnlyCollection<Player>>(a, "get_Count"),
			(Instruction a) => ILPatternMatchingExt.MatchLdcI4(a, 1),
			(Instruction a) => ILPatternMatchingExt.MatchBneUn(a, ref label)
		}))
		{
			val.LogErrorCaller("single player");
			return;
		}
		val.MoveAfterLabels();
		val.Emit(OpCodes.Ldarg_0);
		val.EmitDelegate<Func<TreasureRoomRelicSynchronizer, bool>>((Func<TreasureRoomRelicSynchronizer, bool>)func);
		val.Emit(OpCodes.Brtrue, (object)label);
		static bool func(TreasureRoomRelicSynchronizer self)
		{
			if (ModMain.current_config.keys_enable)
			{
				return self.LocalPlayer.GetRelic<SapphireKey>() == null;
			}
			return false;
		}
	}

	[HookAfter(typeof(TreasureRoomRelicSynchronizer), "AwardRelics")]
	private static void GiveKey_On_AwardRelics(TreasureRoomRelicSynchronizer __instance)
	{
		if (!ModMain.current_config.keys_enable)
		{
			return;
		}
		try
		{
			TryGiveKey(__instance);
		}
		catch (Exception data)
		{
			log.error(data);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void TryGiveKey(TreasureRoomRelicSynchronizer self)
	{
		for (int i = 0; i < self._votes.Count; i++)
		{
			if (!self._votes[i].index.HasValue && self._playerCollection.Players[i].GetRelic<SapphireKey>() == null)
			{
				TaskHelper.RunSafely((Task)RelicCmd.Obtain(((RelicModel)ModelDb.Relic<SapphireKey>()).ToMutable(), self._playerCollection.Players[i], -1));
			}
		}
	}
}

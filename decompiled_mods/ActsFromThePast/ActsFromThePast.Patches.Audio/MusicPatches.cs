using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ActsFromThePast.Acts;
using ActsFromThePast.Acts.Exordium.Events;
using ActsFromThePast.Acts.TheBeyond;
using ActsFromThePast.Acts.TheBeyond.Encounters;
using ActsFromThePast.Acts.TheBeyond.Events;
using ActsFromThePast.Acts.TheCity;
using ActsFromThePast.Acts.TheCity.Events;
using ActsFromThePast.SharedEvents;
using Godot;
using Godot.Collections;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Patches.Audio;

public class MusicPatches
{
	[HarmonyPatch(typeof(NRunMusicController))]
	public static class LegacyActMusicPatches
	{
		private enum TrackType
		{
			None,
			Exploration,
			Elite,
			Boss,
			Shrine
		}

		private enum LegacyAct
		{
			None,
			Exordium,
			City,
			Beyond
		}

		[HarmonyPatch(typeof(Hook), "BeforeCombatStart")]
		public static class BeforeCombatStartStingerPatch
		{
			public static void Prefix()
			{
				if (_playingBossStinger)
				{
					_playingBossStinger = false;
					_isPlayingLegacyMusic = false;
					_currentTrackType = TrackType.None;
				}
			}
		}

		private static readonly PropertyInfo StateProperty = typeof(RunManager).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic);

		private static readonly FieldInfo CombatStateField = typeof(CombatManager).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic);

		private static TrackType _currentTrackType = TrackType.None;

		public static bool _isPlayingLegacyMusic = false;

		public static bool _isPlayingBaseGameMusic = false;

		public static bool _hexaghostActivated = false;

		public static bool _playingBossStinger = false;

		private static readonly string[] ExordiumTracks = new string[2] { "exordium_theme_1", "exordium_theme_2" };

		private static readonly string[] ExordiumEliteTracks = new string[1] { "exordium_elite" };

		private static readonly string[] ExordiumBossTracks = new string[1] { "exordium_boss" };

		private static readonly string[] CityTracks = new string[2] { "city_theme_1", "city_theme_2" };

		private static readonly string[] CityEliteTracks = new string[1] { "exordium_elite" };

		private static readonly string[] CityBossTracks = new string[1] { "city_boss" };

		private static readonly string[] BeyondTracks = new string[2] { "beyond_theme_1", "beyond_theme_2" };

		private static readonly string[] BeyondEliteTracks = new string[1] { "mind_bloom" };

		private static readonly string[] BeyondBossTracks = new string[1] { "beyond_boss" };

		private const string BaseGameBankPath = "res://banks/desktop/act1_a1.bank";

		private const string BaseGameTrack = "event:/music/act1_a1_v1";

		public static void SetBossStingerState()
		{
			_playingBossStinger = true;
			_isPlayingLegacyMusic = true;
			_currentTrackType = TrackType.Boss;
		}

		private static LegacyAct GetCurrentLegacyAct()
		{
			object? obj = StateProperty?.GetValue(RunManager.Instance);
			RunState val = (RunState)((obj is RunState) ? obj : null);
			ActModel val2 = ((val != null) ? val.Act : null);
			if (1 == 0)
			{
			}
			LegacyAct result = ((val2 is ExordiumAct) ? LegacyAct.Exordium : ((val2 is TheCityAct) ? LegacyAct.City : ((val2 is TheBeyondAct) ? LegacyAct.Beyond : LegacyAct.None)));
			if (1 == 0)
			{
			}
			return result;
		}

		private static bool IsLegacyAct()
		{
			return GetCurrentLegacyAct() != LegacyAct.None;
		}

		private static string[] GetExplorationTracks()
		{
			LegacyAct currentLegacyAct = GetCurrentLegacyAct();
			if (1 == 0)
			{
			}
			string[] result = currentLegacyAct switch
			{
				LegacyAct.Exordium => ExordiumTracks, 
				LegacyAct.City => CityTracks, 
				LegacyAct.Beyond => BeyondTracks, 
				_ => ExordiumTracks, 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		private static string[] GetEliteTracks()
		{
			LegacyAct currentLegacyAct = GetCurrentLegacyAct();
			if (1 == 0)
			{
			}
			string[] result = currentLegacyAct switch
			{
				LegacyAct.Exordium => ExordiumEliteTracks, 
				LegacyAct.City => CityEliteTracks, 
				LegacyAct.Beyond => BeyondEliteTracks, 
				_ => ExordiumEliteTracks, 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		private static string[] GetBossTracks()
		{
			LegacyAct currentLegacyAct = GetCurrentLegacyAct();
			if (1 == 0)
			{
			}
			string[] result = currentLegacyAct switch
			{
				LegacyAct.Exordium => ExordiumBossTracks, 
				LegacyAct.City => CityBossTracks, 
				LegacyAct.Beyond => BeyondBossTracks, 
				_ => ExordiumBossTracks, 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		private static string GetAmbienceTrack()
		{
			LegacyAct currentLegacyAct = GetCurrentLegacyAct();
			if (1 == 0)
			{
			}
			string result = currentLegacyAct switch
			{
				LegacyAct.Exordium => "exordium_ambience", 
				LegacyAct.City => "city_ambience", 
				LegacyAct.Beyond => "beyond_ambience", 
				_ => "exordium_ambience", 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		private static Node? GetProxy(NRunMusicController controller)
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Expected O, but got Unknown
			return (Node)(GodotObject)((GodotObject)controller).Get(StringName.op_Implicit("_proxy"));
		}

		private static void StartBaseGameMusic(NRunMusicController controller, int progress, float fadeDelay = 1f)
		{
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			Node proxy = GetProxy(controller);
			if (proxy == null)
			{
				return;
			}
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				((GodotObject)val.CreateTimer((double)fadeDelay, true, false, false)).Connect(StringName.op_Implicit("timeout"), Callable.From((Action)delegate
				{
					//IL_0019: Unknown result type (might be due to invalid IL or missing references)
					//IL_001e: Unknown result type (might be due to invalid IL or missing references)
					//IL_0024: Unknown result type (might be due to invalid IL or missing references)
					//IL_0034: Expected O, but got Unknown
					//IL_002f: Unknown result type (might be due to invalid IL or missing references)
					//IL_0034: Unknown result type (might be due to invalid IL or missing references)
					//IL_0039: Unknown result type (might be due to invalid IL or missing references)
					//IL_005c: Unknown result type (might be due to invalid IL or missing references)
					//IL_0061: Unknown result type (might be due to invalid IL or missing references)
					//IL_0066: Unknown result type (might be due to invalid IL or missing references)
					//IL_0089: Unknown result type (might be due to invalid IL or missing references)
					//IL_008e: Unknown result type (might be due to invalid IL or missing references)
					//IL_009b: Unknown result type (might be due to invalid IL or missing references)
					//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
					//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
					Node obj = proxy;
					StringName obj2 = StringName.op_Implicit("load_act_banks");
					Variant[] array = new Variant[1];
					Array val2 = new Array();
					val2.Add(Variant.op_Implicit("res://banks/desktop/act1_a1.bank"));
					array[0] = Variant.op_Implicit(val2);
					((GodotObject)obj).Call(obj2, (Variant[])(object)array);
					((GodotObject)proxy).Call(StringName.op_Implicit("update_music"), (Variant[])(object)new Variant[1] { Variant.op_Implicit("event:/music/act1_a1_v1") });
					((GodotObject)proxy).Call(StringName.op_Implicit("update_global_parameter"), (Variant[])(object)new Variant[2]
					{
						Variant.op_Implicit("Progress"),
						Variant.op_Implicit(progress)
					});
					_isPlayingBaseGameMusic = true;
				}), 0u);
			}
		}

		private static void StopBaseGameMusic(NRunMusicController controller)
		{
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			if (_isPlayingBaseGameMusic)
			{
				Node proxy = GetProxy(controller);
				if (proxy != null)
				{
					((GodotObject)proxy).Call(StringName.op_Implicit("stop_music"), Array.Empty<Variant>());
					((GodotObject)proxy).Call(StringName.op_Implicit("unload_act_banks"), Array.Empty<Variant>());
					_isPlayingBaseGameMusic = false;
				}
			}
		}

		private static bool IsLagavulinEncounter()
		{
			object? obj = StateProperty?.GetValue(RunManager.Instance);
			RunState val = (RunState)((obj is RunState) ? obj : null);
			AbstractRoom obj2 = ((val != null) ? val.CurrentRoom : null);
			CombatRoom val2 = (CombatRoom)(object)((obj2 is CombatRoom) ? obj2 : null);
			return ((val2 != null) ? val2.Encounter : null) is LagavulinElite;
		}

		private static bool IsLagavulinAsleep()
		{
			CombatManager instance = CombatManager.Instance;
			if (instance == null)
			{
				return false;
			}
			object? obj = CombatStateField?.GetValue(instance);
			CombatState val = (CombatState)((obj is CombatState) ? obj : null);
			if (val == null)
			{
				return false;
			}
			IEnumerable<Creature> enemies = val.Enemies;
			foreach (Creature item in enemies ?? Enumerable.Empty<Creature>())
			{
				if (item.Monster is Lagavulin lagavulin)
				{
					return !lagavulin.IsAwake;
				}
			}
			return false;
		}

		private static bool IsMindBloomEncounter()
		{
			object? obj = StateProperty?.GetValue(RunManager.Instance);
			RunState val = (RunState)((obj is RunState) ? obj : null);
			AbstractRoom obj2 = ((val != null) ? val.CurrentRoom : null);
			CombatRoom val2 = (CombatRoom)(object)((obj2 is CombatRoom) ? obj2 : null);
			EncounterModel val3 = ((val2 != null) ? val2.Encounter : null);
			if (val3 is MindBloomGuardian || val3 is MindBloomHexaghost || val3 is MindBloomSlimeBoss)
			{
				return true;
			}
			return false;
		}

		private static bool IsDeadAdventurerCombat()
		{
			return DeadAdventurer.CombatActive;
		}

		private static bool IsMindBloomCombat()
		{
			return MindBloom.CombatActive;
		}

		private static bool IsMaskedBanditsCombat()
		{
			return MaskedBandits.CombatActive;
		}

		private static bool IsDeadAdventurerCombatEnded()
		{
			if (!DeadAdventurer.CombatActive)
			{
				return false;
			}
			CombatManager instance = CombatManager.Instance;
			return instance == null || !instance.IsInProgress;
		}

		private static bool IsMindBloomCombatEnded()
		{
			if (!MindBloom.CombatActive)
			{
				return false;
			}
			CombatManager instance = CombatManager.Instance;
			return instance == null || !instance.IsInProgress;
		}

		private static bool IsMaskedBanditsCombatEnded()
		{
			if (!MaskedBandits.CombatActive)
			{
				return false;
			}
			CombatManager instance = CombatManager.Instance;
			return instance == null || !instance.IsInProgress;
		}

		public static void OnHexaghostActivated()
		{
			_hexaghostActivated = true;
			AFTPModAudio.FadeIn(ExordiumBossTracks);
			_isPlayingLegacyMusic = true;
			_currentTrackType = TrackType.Boss;
		}

		public static void ResetHexaghostState()
		{
			_hexaghostActivated = false;
		}

		private static bool IsHexaghostEncounter()
		{
			object? obj = StateProperty?.GetValue(RunManager.Instance);
			RunState val = (RunState)((obj is RunState) ? obj : null);
			AbstractRoom obj2 = ((val != null) ? val.CurrentRoom : null);
			CombatRoom val2 = (CombatRoom)(object)((obj2 is CombatRoom) ? obj2 : null);
			return ((val2 != null) ? val2.Encounter : null) is HexaghostBoss;
		}

		private static int? GetSpecialRoomProgress()
		{
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Invalid comparison between Unknown and I4
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Invalid comparison between Unknown and I4
			object? obj = StateProperty?.GetValue(RunManager.Instance);
			RunState val = (RunState)((obj is RunState) ? obj : null);
			RoomType? obj2;
			if (val == null)
			{
				obj2 = null;
			}
			else
			{
				AbstractRoom currentRoom = val.CurrentRoom;
				obj2 = ((currentRoom != null) ? new RoomType?(currentRoom.RoomType) : ((RoomType?)null));
			}
			RoomType? val2 = obj2;
			if (1 == 0)
			{
			}
			if (!val2.HasValue)
			{
				goto IL_0080;
			}
			RoomType valueOrDefault = val2.GetValueOrDefault();
			int? result;
			if ((int)valueOrDefault != 5)
			{
				if ((int)valueOrDefault != 7)
				{
					goto IL_0080;
				}
				result = 3;
			}
			else
			{
				result = 2;
			}
			goto IL_008d;
			IL_008d:
			if (1 == 0)
			{
			}
			return result;
			IL_0080:
			result = null;
			goto IL_008d;
		}

		private static bool IsBossRoom()
		{
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Invalid comparison between Unknown and I4
			object? obj = StateProperty?.GetValue(RunManager.Instance);
			RunState val = (RunState)((obj is RunState) ? obj : null);
			int result;
			if (val == null)
			{
				result = 0;
			}
			else
			{
				AbstractRoom currentRoom = val.CurrentRoom;
				result = (((int)((currentRoom != null) ? new RoomType?(currentRoom.RoomType) : ((RoomType?)null)).GetValueOrDefault() == 3) ? 1 : 0);
			}
			return (byte)result != 0;
		}

		private static bool IsEliteRoom()
		{
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Invalid comparison between Unknown and I4
			object? obj = StateProperty?.GetValue(RunManager.Instance);
			RunState val = (RunState)((obj is RunState) ? obj : null);
			int result;
			if (val == null)
			{
				result = 0;
			}
			else
			{
				AbstractRoom currentRoom = val.CurrentRoom;
				result = (((int)((currentRoom != null) ? new RoomType?(currentRoom.RoomType) : ((RoomType?)null)).GetValueOrDefault() == 2) ? 1 : 0);
			}
			return (byte)result != 0;
		}

		private static bool IsArchitectEvent()
		{
			object? obj = StateProperty?.GetValue(RunManager.Instance);
			RunState val = (RunState)((obj is RunState) ? obj : null);
			AbstractRoom obj2 = ((val != null) ? val.CurrentRoom : null);
			EventRoom val2 = (EventRoom)(object)((obj2 is EventRoom) ? obj2 : null);
			return val2 != null && val2.CanonicalEvent is TheArchitect;
		}

		private static bool IsShrineEvent()
		{
			object? obj = StateProperty?.GetValue(RunManager.Instance);
			RunState val = (RunState)((obj is RunState) ? obj : null);
			AbstractRoom obj2 = ((val != null) ? val.CurrentRoom : null);
			EventRoom val2 = (EventRoom)(object)((obj2 is EventRoom) ? obj2 : null);
			bool flag = val2 != null;
			bool flag2 = flag;
			if (flag2)
			{
				EventModel canonicalEvent = val2.CanonicalEvent;
				bool flag3 = ((canonicalEvent is TheDivineFountain || canonicalEvent is Duplicator || canonicalEvent is GoldenShrine || canonicalEvent is Purifier || canonicalEvent is Transmogrifier || canonicalEvent is UpgradeShrine) ? true : false);
				flag2 = flag3;
			}
			return flag2;
		}

		[HarmonyPatch("UpdateMusic")]
		[HarmonyPrefix]
		public static bool UpdateMusic_Prefix(NRunMusicController __instance)
		{
			if (IsArchitectEvent())
			{
				if (_isPlayingLegacyMusic)
				{
					AFTPModAudio.StopMusic();
					AFTPModAudio.StopAmbience();
					_isPlayingLegacyMusic = false;
					_currentTrackType = TrackType.None;
					_playingBossStinger = false;
				}
				return true;
			}
			if (!IsLegacyAct())
			{
				if (_isPlayingLegacyMusic)
				{
					AFTPModAudio.StopMusic();
					AFTPModAudio.StopAmbience();
					_isPlayingLegacyMusic = false;
					_currentTrackType = TrackType.None;
					_playingBossStinger = false;
				}
				return true;
			}
			_playingBossStinger = false;
			ResetHexaghostState();
			__instance.StopMusic();
			AFTPModAudio.FadeIn(GetExplorationTracks());
			_isPlayingLegacyMusic = true;
			_currentTrackType = TrackType.Exploration;
			__instance.UpdateAmbience();
			return false;
		}

		[HarmonyPatch("UpdateTrack", new Type[] { })]
		[HarmonyPrefix]
		public static bool UpdateTrack_Prefix(NRunMusicController __instance)
		{
			//IL_0126: Unknown result type (might be due to invalid IL or missing references)
			//IL_012b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0139: Unknown result type (might be due to invalid IL or missing references)
			//IL_013e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0143: Unknown result type (might be due to invalid IL or missing references)
			if (IsArchitectEvent())
			{
				if (_isPlayingLegacyMusic)
				{
					AFTPModAudio.StopMusic();
					AFTPModAudio.StopAmbience();
					_isPlayingLegacyMusic = false;
					_currentTrackType = TrackType.None;
					_playingBossStinger = false;
				}
				return true;
			}
			if (!IsLegacyAct())
			{
				if (_isPlayingLegacyMusic)
				{
					AFTPModAudio.StopMusic();
					AFTPModAudio.StopAmbience();
					_isPlayingLegacyMusic = false;
					_currentTrackType = TrackType.None;
					_playingBossStinger = false;
				}
				return true;
			}
			CombatManager instance = CombatManager.Instance;
			bool flag = instance != null && instance.IsInProgress;
			int? specialRoomProgress = GetSpecialRoomProgress();
			if (specialRoomProgress.HasValue)
			{
				if (_playingBossStinger || _isPlayingLegacyMusic)
				{
					_playingBossStinger = false;
					AFTPModAudio.FadeOut();
					_isPlayingLegacyMusic = false;
					_currentTrackType = TrackType.None;
					StartBaseGameMusic(__instance, specialRoomProgress.Value);
				}
				else if (_isPlayingBaseGameMusic)
				{
					Node proxy = GetProxy(__instance);
					if (proxy != null)
					{
						((GodotObject)proxy).Call(StringName.op_Implicit("update_global_parameter"), (Variant[])(object)new Variant[2]
						{
							Variant.op_Implicit("Progress"),
							Variant.op_Implicit(specialRoomProgress.Value)
						});
					}
				}
				return false;
			}
			if (_playingBossStinger && (IsBossRoom() || IsMindBloomEncounter()))
			{
				return false;
			}
			if (IsMindBloomEncounter())
			{
				if (!flag)
				{
					StopBaseGameMusic(__instance);
					AFTPModAudio.StopMusic();
					AFTPModAudio.StopAmbience();
					LegacyBossHelper.OnBossVictory();
					return false;
				}
				if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Elite)
				{
					StopBaseGameMusic(__instance);
					AFTPModAudio.FadeIn(new string[1] { "mind_bloom" });
					_isPlayingLegacyMusic = true;
					_currentTrackType = TrackType.Elite;
				}
				return false;
			}
			if (IsBossRoom() && flag)
			{
				if (GetCurrentLegacyAct() == LegacyAct.Exordium && IsHexaghostEncounter() && !_hexaghostActivated)
				{
					if (_isPlayingLegacyMusic)
					{
						AFTPModAudio.FadeOut(0.5f);
						_isPlayingLegacyMusic = false;
						_currentTrackType = TrackType.None;
					}
					StopBaseGameMusic(__instance);
					return false;
				}
				if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Boss)
				{
					StopBaseGameMusic(__instance);
					AFTPModAudio.FadeIn(GetBossTracks());
					_isPlayingLegacyMusic = true;
					_currentTrackType = TrackType.Boss;
				}
				return false;
			}
			if (IsBossRoom() && !flag)
			{
				StopBaseGameMusic(__instance);
				AFTPModAudio.PlayBossStinger();
				SetBossStingerState();
				return false;
			}
			if (IsEliteRoom() && flag)
			{
				if (GetCurrentLegacyAct() == LegacyAct.Exordium && IsLagavulinEncounter() && IsLagavulinAsleep())
				{
					if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Exploration)
					{
						StopBaseGameMusic(__instance);
						AFTPModAudio.FadeIn(GetExplorationTracks());
						_isPlayingLegacyMusic = true;
						_currentTrackType = TrackType.Exploration;
					}
					return false;
				}
				if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Elite)
				{
					StopBaseGameMusic(__instance);
					AFTPModAudio.FadeIn(GetEliteTracks());
					_isPlayingLegacyMusic = true;
					_currentTrackType = TrackType.Elite;
				}
				return false;
			}
			if (IsDeadAdventurerCombat())
			{
				if (IsDeadAdventurerCombatEnded())
				{
					if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Exploration)
					{
						StopBaseGameMusic(__instance);
						AFTPModAudio.FadeIn(GetExplorationTracks());
						_isPlayingLegacyMusic = true;
						_currentTrackType = TrackType.Exploration;
					}
					return false;
				}
				if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Elite)
				{
					StopBaseGameMusic(__instance);
					AFTPModAudio.FadeIn(GetEliteTracks());
					_isPlayingLegacyMusic = true;
					_currentTrackType = TrackType.Elite;
				}
				return false;
			}
			if (IsMaskedBanditsCombat())
			{
				if (IsMaskedBanditsCombatEnded())
				{
					if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Exploration)
					{
						StopBaseGameMusic(__instance);
						AFTPModAudio.FadeIn(GetExplorationTracks());
						_isPlayingLegacyMusic = true;
						_currentTrackType = TrackType.Exploration;
					}
					return false;
				}
				if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Elite)
				{
					StopBaseGameMusic(__instance);
					AFTPModAudio.FadeIn(GetEliteTracks());
					_isPlayingLegacyMusic = true;
					_currentTrackType = TrackType.Elite;
				}
				return false;
			}
			if (IsShrineEvent())
			{
				if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Shrine)
				{
					StopBaseGameMusic(__instance);
					AFTPModAudio.FadeIn(new string[1] { "shrine" });
					_isPlayingLegacyMusic = true;
					_currentTrackType = TrackType.Shrine;
				}
				return false;
			}
			if (!_isPlayingLegacyMusic || _currentTrackType != TrackType.Exploration)
			{
				StopBaseGameMusic(__instance);
				AFTPModAudio.FadeIn(GetExplorationTracks());
				_isPlayingLegacyMusic = true;
				_currentTrackType = TrackType.Exploration;
			}
			return false;
		}

		[HarmonyPatch("ToggleMerchantTrack")]
		[HarmonyPrefix]
		public static bool ToggleMerchantTrack_Prefix(NRunMusicController __instance)
		{
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			if (!IsLegacyAct())
			{
				return true;
			}
			if (_isPlayingLegacyMusic)
			{
				return false;
			}
			Node proxy = GetProxy(__instance);
			if (proxy == null)
			{
				return false;
			}
			NMapScreen instance = NMapScreen.Instance;
			bool flag = instance != null && ((CanvasItem)instance).IsVisible();
			((GodotObject)proxy).Call(StringName.op_Implicit("update_global_parameter"), (Variant[])(object)new Variant[2]
			{
				Variant.op_Implicit("Progress"),
				Variant.op_Implicit(flag ? 9 : 2)
			});
			return false;
		}

		[HarmonyPatch("TriggerEliteSecondPhase")]
		[HarmonyPrefix]
		public static bool TriggerEliteSecondPhase_Prefix(NRunMusicController __instance)
		{
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			if (!IsLegacyAct())
			{
				return true;
			}
			if (_isPlayingLegacyMusic)
			{
				return false;
			}
			Node proxy = GetProxy(__instance);
			if (proxy == null)
			{
				return false;
			}
			((GodotObject)proxy).Call(StringName.op_Implicit("update_global_parameter"), (Variant[])(object)new Variant[2]
			{
				Variant.op_Implicit("Progress"),
				Variant.op_Implicit(8)
			});
			return false;
		}

		[HarmonyPatch("TriggerCampfireGoingOut")]
		[HarmonyPrefix]
		public static bool TriggerCampfireGoingOut_Prefix()
		{
			return !IsLegacyAct();
		}

		[HarmonyPatch("StopMusic")]
		[HarmonyPostfix]
		public static void StopMusic_Postfix(NRunMusicController __instance)
		{
			AFTPModAudio.StopMusic();
			AFTPModAudio.StopAmbience();
			StopBaseGameMusic(__instance);
			_isPlayingLegacyMusic = false;
			_currentTrackType = TrackType.None;
		}

		[HarmonyPatch("UpdateAmbience")]
		[HarmonyPrefix]
		public static bool UpdateAmbience_Prefix()
		{
			if (!IsLegacyAct())
			{
				if (_isPlayingLegacyMusic)
				{
					AFTPModAudio.StopAmbience();
				}
				return true;
			}
			CombatManager instance = CombatManager.Instance;
			bool flag = instance != null && instance.IsInProgress;
			if (IsMindBloomEncounter() && !flag && !_playingBossStinger)
			{
				AFTPModAudio.StopMusic();
				AFTPModAudio.StopAmbience();
				AFTPModAudio.PlayBossStinger(1.5f);
				SetBossStingerState();
				return false;
			}
			if (IsBossRoom() && !flag && !_playingBossStinger)
			{
				AFTPModAudio.StopMusic();
				AFTPModAudio.StopAmbience();
				AFTPModAudio.PlayBossStinger(1.5f);
				SetBossStingerState();
				return false;
			}
			AFTPModAudio.FadeInAmbience(GetAmbienceTrack());
			return false;
		}
	}

	[HarmonyPatch(typeof(NAudioManager), "SetBgmVol")]
	public static class SetBgmVolPatch
	{
		public static void Postfix(float volume)
		{
			AFTPModAudio.SetMusicVolume(volume);
		}
	}

	[HarmonyPatch(typeof(NAudioManager), "SetAmbienceVol")]
	public static class SetAmbienceVolPatch
	{
		public static void Postfix(float volume)
		{
			AFTPModAudio.SetAmbienceVolume(volume);
		}
	}

	[HarmonyPatch(typeof(Hook), "AfterCombatEnd")]
	public static class AfterCombatEndPatch
	{
		public static void Postfix(IRunState runState, CombatState? combatState, CombatRoom room)
		{
			EncounterModel val = ((combatState != null) ? combatState.Encounter : null);
			if ((val is MindBloomGuardian || val is MindBloomHexaghost || val is MindBloomSlimeBoss) ? true : false)
			{
				MindBloom.CombatActive = false;
			}
			val = ((combatState != null) ? combatState.Encounter : null);
			if ((val is SlimeBossBoss || val is CollectorBoss || val is HexaghostBoss || val is GuardianBoss || val is ChampBoss || val is BronzeAutomatonBoss || val is TimeEaterBoss || val is AwakenedOneBoss || val is DonuAndDecaBoss) ? true : false)
			{
				LegacyBossHelper.OnBossVictory();
			}
		}
	}
}

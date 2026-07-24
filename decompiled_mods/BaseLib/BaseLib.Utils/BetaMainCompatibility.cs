using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace BaseLib.Utils;

public static class BetaMainCompatibility
{
	public static class AttackCommand_
	{
		[Obsolete("No longer differs between main and beta.")]
		public static VariableMethod TargetingAllOpponents = new VariableMethod((typeof(AttackCommand), "TargetingAllOpponents", new Type[1], new int[1]));

		[Obsolete("No longer differs between main and beta.")]
		public static VariableMethod TargetingRandomOpponents = new VariableMethod((typeof(AttackCommand), "TargetingRandomOpponents", new Type[2]
		{
			null,
			typeof(bool)
		}, new int[2] { 0, 1 }));
	}

	public static class Hook_
	{
		[Obsolete("No longer differs between main and beta.")]
		public static VariableMethod ModifyBlock = new VariableMethod((typeof(Hook), "ModifyBlock", new Type[7]
		{
			null,
			typeof(Creature),
			typeof(decimal),
			typeof(ValueProp),
			typeof(CardModel),
			typeof(CardPlay),
			typeof(IEnumerable<AbstractModel>)
		}, new int[7] { 0, 1, 2, 3, 4, 5, 6 }));
	}

	public static class Creature_
	{
		private static MethodInfo? OldInfiniteHp = AccessToolsExtensions.PropertyGetter(typeof(Creature), "ShowsInfiniteHp");

		private static MethodInfo? NewInfiniteHp = AccessToolsExtensions.PropertyGetter(typeof(Creature), "HpDisplay");

		[Obsolete("No longer differs between main and beta.")]
		public static VariableReference<object?> CombatState = new VariableReference<object>(typeof(Creature), "CombatState");

		[Obsolete("No longer differs between main and beta.")]
		public static CombatStateWrapper? WrappedCombatState(Creature creature)
		{
			object obj = CombatState.Get(creature);
			if (obj == null)
			{
				return null;
			}
			return new CombatStateWrapper(obj);
		}

		[Obsolete("No longer differs between main and beta.")]
		public static bool ShowsInfiniteHp(Creature creature)
		{
			if (OldInfiniteHp != null)
			{
				return (bool)(OldInfiniteHp.Invoke(creature, Array.Empty<object>()) ?? throw new InvalidOperationException());
			}
			if (NewInfiniteHp != null)
			{
				int num = Convert.ToInt32(NewInfiniteHp.Invoke(creature, Array.Empty<object>()));
				if ((uint)(num - 1) <= 1u)
				{
					return true;
				}
				return false;
			}
			throw new InvalidOperationException("Could not find property for infinite hp check");
		}
	}

	public static class CardModel_
	{
		[Obsolete("No longer differs between main and beta.")]
		public static VariableReference<object?> CombatState = new VariableReference<object>(typeof(CardModel), "CombatState");

		[Obsolete("No longer differs between main and beta.")]
		public static CombatStateWrapper? WrappedCombatState(CardModel card)
		{
			object obj = CombatState.Get(card);
			if (obj == null)
			{
				return null;
			}
			return new CombatStateWrapper(obj);
		}
	}

	public static class PowerCmd_
	{
		[Obsolete("No longer differs between main and beta.")]
		public static VariableMethod Apply = new VariableMethod((typeof(PowerCmd), "Apply", new Type[6]
		{
			typeof(PlayerChoiceContext),
			typeof(Creature),
			typeof(decimal),
			typeof(Creature),
			typeof(CardModel),
			typeof(bool)
		}, new int[6] { 0, 1, 2, 3, 4, 5 }), (typeof(PowerCmd), "Apply", new Type[5]
		{
			typeof(Creature),
			typeof(decimal),
			typeof(Creature),
			typeof(CardModel),
			typeof(bool)
		}, new int[5] { 1, 2, 3, 4, 5 }));

		[Obsolete("No longer differs between main and beta.")]
		public static VariableMethod ApplyMulti = new VariableMethod((typeof(PowerCmd), "Apply", new Type[6]
		{
			typeof(PlayerChoiceContext),
			typeof(IEnumerable<Creature>),
			typeof(decimal),
			typeof(Creature),
			typeof(CardModel),
			typeof(bool)
		}, new int[6] { 0, 1, 2, 3, 4, 5 }), (typeof(PowerCmd), "Apply", new Type[5]
		{
			typeof(IEnumerable<Creature>),
			typeof(decimal),
			typeof(Creature),
			typeof(CardModel),
			typeof(bool)
		}, new int[5] { 1, 2, 3, 4, 5 }));
	}

	public static class RunState
	{
		[Obsolete("No longer differs between main and beta.")]
		public static VariableMethod IterateHookListeners = new VariableMethod((typeof(IRunState), "IterateHookListeners", new Type[1], new int[1]));
	}

	public static class _HoverTipFactory
	{
		[Obsolete("No longer differs between main and beta.")]
		private static VariableMethod FromPowerDef = new VariableMethod((typeof(HoverTipFactory), "FromPower", new Type[1] { typeof(int?) }, new int[1], (MethodInfo m) => m.IsGenericMethod), (typeof(HoverTipFactory), "FromPower", Array.Empty<Type>(), Array.Empty<int>(), (MethodInfo m) => m.IsGenericMethod));

		[Obsolete("No longer differs between main and beta.")]
		private static VariableMethod FromPowerInstanceDef = new VariableMethod((typeof(HoverTipFactory), "FromPower", new Type[2]
		{
			typeof(PowerModel),
			typeof(int?)
		}, new int[2] { 0, 1 }, (MethodInfo m) => !m.IsGenericMethod), (typeof(HoverTipFactory), "FromPower", new Type[1] { typeof(PowerModel) }, new int[1], (MethodInfo m) => !m.IsGenericMethod));

		[Obsolete("No longer differs between main and beta.")]
		public static IHoverTip FromPower<T>() where T : PowerModel
		{
			if (FromPowerDef.ParamCount == 1)
			{
				return FromPowerDef.InvokeGeneric<IHoverTip, T>(null, new object[1]);
			}
			return FromPowerDef.InvokeGeneric<IHoverTip, T>(null, Array.Empty<object>());
		}

		[Obsolete("No longer differs between main and beta.")]
		public static IHoverTip FromPower(PowerModel power, int? amount = null)
		{
			return FromPowerInstanceDef.Invoke<IHoverTip>(null, new object[2] { power, amount });
		}
	}

	public static class _ModManifest
	{
		private static readonly FieldInfo DependencyField = AccessToolsExtensions.DeclaredField(typeof(ModManifest), "dependencies");

		[Obsolete("No longer differs between main and beta.")]
		public static bool HasDependency(ModManifest modManifest, string dependencyId)
		{
			object value = DependencyField.GetValue(modManifest);
			if (value == null)
			{
				return false;
			}
			if (value is List<string> list)
			{
				return list.Contains(dependencyId);
			}
			try
			{
				Type type = value.GetType();
				if (!type.IsConstructedGenericType)
				{
					return false;
				}
				if (!(value is IList list2))
				{
					return false;
				}
				FieldInfo field = type.GenericTypeArguments[0].GetField("id");
				if (field == null)
				{
					return false;
				}
				foreach (object item in list2)
				{
					if (field.GetValue(item) is string text && text == dependencyId)
					{
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				BaseLibMain.Logger.Error(ex.Message, 1);
			}
			return false;
		}
	}

	private static Lazy<SemanticVersion> _versionInfo = new Lazy<SemanticVersion>((Func<SemanticVersion>)GetVersion);

	private static VariableMethod _fromCard = new VariableMethod((typeof(AttackCommand), "FromCard", new Type[2]
	{
		typeof(CardModel),
		typeof(CardPlay)
	}, new int[2] { 0, 1 }), (typeof(AttackCommand), "FromCard", new Type[1] { typeof(CardModel) }, new int[1]));

	private static VariableMethod _signalPlayerChoiceBegun = new VariableMethod((typeof(PlayerChoiceContext), "SignalPlayerChoiceBegun", new Type[2]
	{
		typeof(Player),
		typeof(PlayerChoiceOptions)
	}, new int[2] { 0, 1 }), (typeof(PlayerChoiceContext), "SignalPlayerChoiceBegun", new Type[1] { typeof(PlayerChoiceOptions) }, new int[1] { 1 }));

	private static VariableMethod _injectSavedPropertiesType = new VariableMethod(("MegaCrit.Sts2.Core.Saves.Runs.SavedPropertiesTypeCache", "CachePropertiesForType", new Type[1] { typeof(Type) }, new int[1]), ("MegaCrit.Sts2.Core.Multiplayer.Serialization.ModelIdSerializationCache", "CachePropertiesForType", new Type[3]
	{
		typeof(Type),
		null,
		null
	}, new int[1]));

	public static SemanticVersion Version => _versionInfo.Value;

	private static SemanticVersion GetVersion()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			return (SemanticVersion)(((object)ReleaseInfoManager.Instance.SemVer) ?? ((object)new SemanticVersion(999, 999, 999, (string)null, (List<string>)null)));
		}
		catch (Exception)
		{
			return new SemanticVersion(999, 999, 999, (string)null, (List<string>)null);
		}
	}

	public static AttackCommand FromCardCompatibility(this AttackCommand command, CardModel card, CardPlay? cardPlay)
	{
		return _fromCard.Invoke<AttackCommand>(command, new object[2] { card, cardPlay });
	}

	public static Task SignalPlayerChoiceBegunCompatibility(this PlayerChoiceContext context, Player player, PlayerChoiceOptions options)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		return _signalPlayerChoiceBegun.Invoke<Task>(context, new object[2] { player, options });
	}

	public static void CacheSavedProperties(Type t)
	{
		_injectSavedPropertiesType.Invoke(null, t);
	}
}

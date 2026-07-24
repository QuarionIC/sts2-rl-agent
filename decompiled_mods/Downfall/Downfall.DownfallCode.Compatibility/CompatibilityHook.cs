using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Downfall.DownfallCode.Compatibility;

public static class CompatibilityHook
{
	private delegate decimal ModifyDamageDel(IRunState runState, ICombatState? combatState, Creature? target, Creature? dealer, decimal damage, ValueProp props, CardModel? cardSource, CardPlay? cardPlay, ModifyDamageHookType modifyDamageHookType, CardPreviewMode previewMode, out IEnumerable<AbstractModel> modifiers);

	private static readonly ModifyDamageDel ModifyDamageD = Build();

	public static decimal ModifyDamage(IRunState runState, ICombatState? combatState, Creature? target, Creature? dealer, decimal damage, ValueProp props, CardModel? cardSource, CardPlay? cardPlay, ModifyDamageHookType modifyDamageHookType, CardPreviewMode previewMode, out IEnumerable<AbstractModel> modifiers)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		return ModifyDamageD(runState, combatState, target, dealer, damage, props, cardSource, cardPlay, modifyDamageHookType, previewMode, out modifiers);
	}

	private static ModifyDamageDel Build()
	{
		Type type = typeof(IEnumerable<AbstractModel>).MakeByRefType();
		Type[] array = new Type[11]
		{
			typeof(IRunState),
			typeof(ICombatState),
			typeof(Creature),
			typeof(Creature),
			typeof(decimal),
			typeof(ValueProp),
			typeof(CardModel),
			typeof(CardPlay),
			typeof(ModifyDamageHookType),
			typeof(CardPreviewMode),
			type
		};
		Type[] subArray = array[..7];
		Type[] subArray2 = array[8..];
		int num = 0;
		Type[] array2 = new Type[subArray.Length + subArray2.Length];
		ReadOnlySpan<Type> readOnlySpan = new ReadOnlySpan<Type>(subArray);
		readOnlySpan.CopyTo(new Span<Type>(array2).Slice(num, readOnlySpan.Length));
		num += readOnlySpan.Length;
		ReadOnlySpan<Type> readOnlySpan2 = new ReadOnlySpan<Type>(subArray2);
		readOnlySpan2.CopyTo(new Span<Type>(array2).Slice(num, readOnlySpan2.Length));
		num += readOnlySpan2.Length;
		Type[] types = array2;
		MethodInfo methodInfo = typeof(Hook).GetMethod("ModifyDamage", BindingFlags.Static | BindingFlags.Public, null, array, null) ?? typeof(Hook).GetMethod("ModifyDamage", BindingFlags.Static | BindingFlags.Public, null, types, null) ?? throw new MissingMethodException("Hook.ModifyDamage not found in any known signature.");
		bool flag = methodInfo.GetParameters().Length == array.Length;
		ParameterExpression[] array3 = new ParameterExpression[11]
		{
			Expression.Parameter(typeof(IRunState), "runState"),
			Expression.Parameter(typeof(ICombatState), "combatState"),
			Expression.Parameter(typeof(Creature), "target"),
			Expression.Parameter(typeof(Creature), "dealer"),
			Expression.Parameter(typeof(decimal), "damage"),
			Expression.Parameter(typeof(ValueProp), "props"),
			Expression.Parameter(typeof(CardModel), "cardSource"),
			Expression.Parameter(typeof(CardPlay), "cardPlay"),
			Expression.Parameter(typeof(ModifyDamageHookType), "hookType"),
			Expression.Parameter(typeof(CardPreviewMode), "previewMode"),
			Expression.Parameter(type, "modifiers")
		};
		ParameterExpression[] array5;
		if (!flag)
		{
			ParameterExpression[] subArray3 = array3[..7];
			ParameterExpression[] subArray4 = array3[8..];
			num = 0;
			ParameterExpression[] array4 = new ParameterExpression[subArray3.Length + subArray4.Length];
			ReadOnlySpan<ParameterExpression> readOnlySpan3 = new ReadOnlySpan<ParameterExpression>(subArray3);
			readOnlySpan3.CopyTo(new Span<ParameterExpression>(array4).Slice(num, readOnlySpan3.Length));
			num += readOnlySpan3.Length;
			ReadOnlySpan<ParameterExpression> readOnlySpan4 = new ReadOnlySpan<ParameterExpression>(subArray4);
			readOnlySpan4.CopyTo(new Span<ParameterExpression>(array4).Slice(num, readOnlySpan4.Length));
			num += readOnlySpan4.Length;
			array5 = array4;
		}
		else
		{
			array5 = array3;
		}
		ParameterExpression[] source = array5;
		return Expression.Lambda<ModifyDamageDel>(Expression.Call(methodInfo, source.Cast<Expression>()), array3).Compile();
	}
}

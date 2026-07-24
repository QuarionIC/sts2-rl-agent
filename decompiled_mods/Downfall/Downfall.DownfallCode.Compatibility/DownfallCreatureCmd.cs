using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Downfall.DownfallCode.Compatibility;

public static class DownfallCreatureCmd
{
	private delegate Task<IEnumerable<DamageResult>> SingleDealerDel(PlayerChoiceContext ctx, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? card, CardPlay? play);

	private delegate Task<IEnumerable<DamageResult>> SingleCardDel(PlayerChoiceContext ctx, Creature target, decimal amount, ValueProp props, CardModel card, CardPlay? play);

	private delegate Task<IEnumerable<DamageResult>> MultiDealerDel(PlayerChoiceContext ctx, IEnumerable<Creature> targets, decimal amount, ValueProp props, Creature? dealer, CardModel? card, CardPlay? play);

	private delegate Task LoseBlockDel(PlayerChoiceContext ctx, Creature target, decimal amount, Creature? remover);

	private static readonly SingleDealerDel SingleWithDealer = Build<SingleDealerDel>(new Type[6]
	{
		typeof(PlayerChoiceContext),
		typeof(Creature),
		typeof(decimal),
		typeof(ValueProp),
		typeof(Creature),
		typeof(CardModel)
	});

	private static readonly SingleCardDel SingleCardOnly = Build<SingleCardDel>(new Type[5]
	{
		typeof(PlayerChoiceContext),
		typeof(Creature),
		typeof(decimal),
		typeof(ValueProp),
		typeof(CardModel)
	});

	private static readonly MultiDealerDel MultiWithDealer = Build<MultiDealerDel>(new Type[6]
	{
		typeof(PlayerChoiceContext),
		typeof(IEnumerable<Creature>),
		typeof(decimal),
		typeof(ValueProp),
		typeof(Creature),
		typeof(CardModel)
	});

	private static readonly LoseBlockDel LoseBlockImpl = BuildLoseBlock();

	public static Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return SingleWithDealer(choiceContext, target, amount, props, dealer, cardSource, cardPlay);
	}

	public static Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, CardModel cardSource, CardPlay? cardPlay)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return SingleCardOnly(choiceContext, target, amount, props, cardSource, cardPlay);
	}

	public static Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return MultiWithDealer(choiceContext, targets, amount, props, dealer, cardSource, cardPlay);
	}

	private static TDelegate Build<TDelegate>(params Type[] baseParams) where TDelegate : Delegate
	{
		Type[] array = baseParams.Append<Type>(typeof(CardPlay)).ToArray();
		MethodInfo? obj = typeof(CreatureCmd).GetMethod("Damage", BindingFlags.Static | BindingFlags.Public, null, array, null) ?? typeof(CreatureCmd).GetMethod("Damage", BindingFlags.Static | BindingFlags.Public, null, baseParams, null) ?? throw new MissingMethodException("CreatureCmd.Damage(" + string.Join(", ", baseParams.Select((Type t) => t.Name)) + ") not found");
		bool num = obj.GetParameters().Length == array.Length;
		ParameterExpression[] array2 = array.Select((Type t, int i) => Expression.Parameter(t, $"p{i}")).ToArray();
		ParameterExpression[] source = (num ? array2 : array2[..^1]);
		return Expression.Lambda<TDelegate>(Expression.Call(obj, source.Cast<Expression>()), array2).Compile();
	}

	public static Task LoseBlock(PlayerChoiceContext choiceContext, Creature target, decimal amount, Creature? remover)
	{
		return LoseBlockImpl(choiceContext, target, amount, remover);
	}

	private static LoseBlockDel BuildLoseBlock()
	{
		MethodInfo method = typeof(CreatureCmd).GetMethod("LoseBlock", BindingFlags.Static | BindingFlags.Public, null, new Type[4]
		{
			typeof(PlayerChoiceContext),
			typeof(Creature),
			typeof(decimal),
			typeof(Creature)
		}, null);
		MethodInfo method2 = typeof(CreatureCmd).GetMethod("LoseBlock", BindingFlags.Static | BindingFlags.Public, null, new Type[2]
		{
			typeof(Creature),
			typeof(decimal)
		}, null);
		ParameterExpression parameterExpression = Expression.Parameter(typeof(PlayerChoiceContext), "ctx");
		ParameterExpression parameterExpression2 = Expression.Parameter(typeof(Creature), "target");
		ParameterExpression parameterExpression3 = Expression.Parameter(typeof(decimal), "amount");
		ParameterExpression parameterExpression4 = Expression.Parameter(typeof(Creature), "remover");
		Expression body;
		if (method != null)
		{
			body = Expression.Call(method, parameterExpression, parameterExpression2, parameterExpression3, parameterExpression4);
		}
		else
		{
			if (!(method2 != null))
			{
				throw new MissingMethodException("CreatureCmd.LoseBlock not found in either known signature");
			}
			body = Expression.Call(method2, parameterExpression2, parameterExpression3);
		}
		return Expression.Lambda<LoseBlockDel>(body, new ParameterExpression[4] { parameterExpression, parameterExpression2, parameterExpression3, parameterExpression4 }).Compile();
	}
}

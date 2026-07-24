using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Minigames;

public class MatchAndKeepMinigame
{
	private readonly TaskCompletionSource _completionSource = new TaskCompletionSource();

	public Player Owner { get; }

	public CardModel[] Cards { get; }

	public int[] PairIndices { get; }

	public CardModel[] Canonicals { get; }

	public int MaxAttempts { get; }

	public int ActIndex { get; }

	public MatchAndKeepMinigame(Player owner, Rng rng, int attempts, int actIndex)
	{
		Owner = owner;
		MaxAttempts = attempts;
		ActIndex = actIndex;
		Cards = (CardModel[])(object)new CardModel[12];
		PairIndices = new int[12];
		Canonicals = (CardModel[])(object)new CardModel[6];
		GenerateCards(rng);
		ShuffleCards(rng);
	}

	private void GenerateCards(Rng rng)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < ActIndex; i++)
		{
			rng.NextInt(1);
		}
		List<CardModel> source = Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint).ToList();
		List<CardModel> list = (from c in ((CardPoolModel)ModelDb.CardPool<CurseCardPool>()).GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
			where c.CanBeGeneratedByModifiers
			select c).ToList();
		Canonicals[0] = rng.NextItem<CardModel>(source.Where((CardModel c) => (int)c.Rarity == 4));
		Canonicals[1] = rng.NextItem<CardModel>(source.Where((CardModel c) => (int)c.Rarity == 3));
		Canonicals[2] = rng.NextItem<CardModel>(source.Where((CardModel c) => (int)c.Rarity == 2));
		Canonicals[3] = (CardModel)(ActsFromThePastConfig.RebalancedMode ? ((object)ModelDb.Card<Guilty>()) : ((object)rng.NextItem<CardModel>((IEnumerable<CardModel>)list)));
		Canonicals[4] = (CardModel)(ActsFromThePastConfig.RebalancedMode ? ((object)ModelDb.Card<Guilty>()) : ((object)rng.NextItem<CardModel>((IEnumerable<CardModel>)list)));
		List<CardModel> list2 = source.Where((CardModel c) => (int)c.Rarity == 1 && !c.Tags.Contains((CardTag)1) && !c.Tags.Contains((CardTag)2)).ToList();
		if (list2.Count == 0)
		{
			list2 = Owner.Character.StartingDeck.Where((CardModel c) => (int)c.Rarity == 1 && !c.Tags.Contains((CardTag)1) && !c.Tags.Contains((CardTag)2)).ToList();
		}
		Canonicals[5] = ((list2.Count > 0) ? rng.NextItem<CardModel>((IEnumerable<CardModel>)list2) : rng.NextItem<CardModel>(source.Where((CardModel c) => (int)c.Rarity == 2)));
		for (int num = 0; num < 6; num++)
		{
			Cards[num * 2] = ((ICardScope)Owner.RunState).CreateCard(Canonicals[num], Owner);
			Cards[num * 2 + 1] = ((ICardScope)Owner.RunState).CreateCard(Canonicals[num], Owner);
			PairIndices[num * 2] = num;
			PairIndices[num * 2 + 1] = num;
		}
	}

	private void ShuffleCards(Rng rng)
	{
		for (int num = 11; num > 0; num--)
		{
			int num2 = rng.NextInt(num + 1);
			CardModel[] cards = Cards;
			int num3 = num;
			CardModel[] cards2 = Cards;
			int num4 = num2;
			CardModel val = Cards[num2];
			CardModel val2 = Cards[num];
			cards[num3] = val;
			cards2[num4] = val2;
			ref int reference = ref PairIndices[num];
			ref int reference2 = ref PairIndices[num2];
			num4 = PairIndices[num2];
			num3 = PairIndices[num];
			reference = num4;
			reference2 = num3;
		}
	}

	public void Complete()
	{
		if (!_completionSource.Task.IsCompleted)
		{
			_completionSource.SetResult();
		}
	}

	public void ForceEnd()
	{
		if (!_completionSource.Task.IsCompleted)
		{
			_completionSource.TrySetCanceled();
		}
	}

	public async Task PlayMinigame()
	{
		if (LocalContext.IsMe(Owner))
		{
			NMatchAndKeepScreen.ShowScreen(this);
			await _completionSource.Task;
		}
	}
}

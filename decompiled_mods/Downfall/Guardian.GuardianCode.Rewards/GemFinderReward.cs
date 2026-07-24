using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Downfall.DownfallCode.CustomEnums;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Guardian.GuardianCode.Rewards;

public class GemFinderReward : CustomReward
{
	[CustomEnum(null)]
	public static RewardType GemFinderRewardType;

	private readonly PlayerChoiceSynchronizer _synchronizer;

	private NSimpleCardSelectScreen? _currentlyShownScreen;

	private List<GemModel> Gems { get; }

	protected override string IconPath
	{
		get
		{
			if (Gems.Count <= 0)
			{
				return GuardianModelDb.AllGems.First().IconPath;
			}
			return Gems[0].IconPath;
		}
	}

	protected override RewardType RewardType => GemFinderRewardType;

	public override LocString Description
	{
		get
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Expected O, but got Unknown
			LocString val = new LocString("gameplay_ui", "COMBAT_REWARD_ADD_GEMS");
			val.Add("Amount", (decimal)_003Cchoosable_003EP);
			return val;
		}
	}

	public override bool IsPopulated => Gems.Count > 0;

	public override CreateRewardFromSave<CustomReward> DeserializeMethod => Deserialize;

	public GemFinderReward(int choosable, int choices, Player player)
	{
		_003Cchoosable_003EP = choosable;
		_003Cchoices_003EP = choices;
		_synchronizer = RunManager.Instance.PlayerChoiceSynchronizer;
		Gems = new List<GemModel>();
		((CustomReward)this)._002Ector(player);
	}

	public override void Populate()
	{
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		if (Gems.Count > 0)
		{
			return;
		}
		Dictionary<CardRarity, List<GemModel>> dictionary = (from g in GuardianModelDb.AllGems
			group g by g.Rarity).ToDictionary((IGrouping<CardRarity, GemModel> g) => g.Key, (IGrouping<CardRarity, GemModel> g) => g.ToList());
		int num = Math.Min(_003Cchoices_003EP, GuardianModelDb.AllGems.DistinctBy((GemModel g) => ((AbstractModel)g).Id.Entry).Count());
		Rng rewards = ((Reward)this).Player.PlayerRng.Rewards;
		int num2 = 0;
		while (Gems.Count < num && num2++ < 10000)
		{
			int num3 = rewards.NextInt(100);
			CardRarity key = (CardRarity)((num3 < 55) ? 2 : ((num3 < 85) ? 3 : 4));
			if (dictionary.TryGetValue(key, out var value) && value.Count != 0)
			{
				GemModel gemModel = rewards.NextItem<GemModel>((IEnumerable<GemModel>)value);
				if (gemModel != null)
				{
					value.Remove(gemModel);
					Gems.Add(gemModel);
				}
			}
		}
		if (Gems.Count < num)
		{
			Log.Error($"GemFinderReward only populated {Gems.Count}/{num} gems!", 2);
		}
	}

	protected override async Task<bool> OnSelect()
	{
		List<CardModel> cards = Gems.Select((GemModel e) => e.ToCard).ToList();
		List<int> chosenIndices = new List<int>();
		if (LocalContext.IsMe(((Reward)this).Player))
		{
			CardSelectorPrefs val = default(CardSelectorPrefs);
			((CardSelectorPrefs)(ref val))._002Ector(DownfallCardSelectorPrefs.ToDeckSelectionPrompt, 0, _003Cchoosable_003EP);
			_currentlyShownScreen = NSimpleCardSelectScreen.Create((IReadOnlyList<CardModel>)cards, val);
			NOverlayStack instance = NOverlayStack.Instance;
			if (instance != null)
			{
				instance.Push((IOverlayScreen)(object)_currentlyShownScreen);
			}
			List<CardModel> source = (await ((NCardGridSelectionScreen)_currentlyShownScreen).CardsSelected()).ToList();
			CleanupScreen();
			foreach (int item in source.Select((CardModel card) => cards.IndexOf(card)))
			{
				if (item >= 0)
				{
					chosenIndices.Add(item);
				}
				else
				{
					Log.Error("GemFinderReward: selected card not found in offered list!", 2);
				}
			}
			foreach (int item2 in chosenIndices)
			{
				uint num = _synchronizer.ReserveChoiceId(((Reward)this).Player);
				_synchronizer.SyncLocalChoice(((Reward)this).Player, num, PlayerChoiceResult.FromIndex((int?)item2));
			}
			uint num2 = _synchronizer.ReserveChoiceId(((Reward)this).Player);
			_synchronizer.SyncLocalChoice(((Reward)this).Player, num2, PlayerChoiceResult.FromIndex((int?)null));
		}
		else
		{
			while (true)
			{
				uint num3 = _synchronizer.ReserveChoiceId(((Reward)this).Player);
				int? num4 = (await _synchronizer.WaitForRemoteChoice(((Reward)this).Player, num3)).AsIndexOrNull();
				if (!num4.HasValue)
				{
					break;
				}
				if (num4.Value < 0 || num4.Value >= cards.Count)
				{
					Log.Error($"GemFinderReward: bad remote index {num4.Value} for {cards.Count} gems!", 2);
				}
				else
				{
					chosenIndices.Add(num4.Value);
				}
			}
		}
		if (chosenIndices.Count <= 0)
		{
			return true;
		}
		CardCmd.PreviewCardPileAdd(await CardPileCmd.Add((IEnumerable<CardModel>)chosenIndices.Select((int i) => ((ICardScope)((Reward)this).Player.RunState).CreateCard(cards[i], ((Reward)this).Player)).ToList(), (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
		return true;
	}

	private void CleanupScreen()
	{
		if (_currentlyShownScreen != null)
		{
			NOverlayStack instance = NOverlayStack.Instance;
			if (instance != null)
			{
				instance.Remove((IOverlayScreen)(object)_currentlyShownScreen);
			}
			_currentlyShownScreen = null;
		}
	}

	public override void OnSkipped()
	{
		CleanupScreen();
	}

	public override void MarkContentAsSeen()
	{
	}

	private static CustomReward Deserialize(SerializableReward save, Player player)
	{
		return (CustomReward)(object)new GemFinderReward(save.GoldAmount, save.OptionCount, player);
	}

	public override SerializableReward ToSerializable()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		return new SerializableReward
		{
			RewardType = GemFinderRewardType,
			GoldAmount = _003Cchoosable_003EP,
			OptionCount = _003Cchoices_003EP
		};
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Downfall.DownfallCode.Cards;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using Snecko.SneckoCode.Vfx;

namespace Snecko.SneckoCode.Core;

public static class SneckoPoolSelection
{
	public static async Task<List<CardPoolModel>> DoOffclassSelection(Player snecko, IRunState state, uint[] choiceIds)
	{
		List<CharacterModel> sixCharacters = GetSixCharacters(snecko, state);
		return (await SyncSelections(snecko, sixCharacters, choiceIds)).Select((CharacterModel c) => c.CardPool).ToList();
	}

	private static List<CharacterModel> GetSixCharacters(Player snecko, IRunState state)
	{
		return IEnumerableExtensions.TakeRandom<CharacterModel>(ModelDb.AllCharacters.Where((CharacterModel e) => e != snecko.Character), 6, state.Rng.UpFront).ToList();
	}

	private static async Task<NSneckoCharacterSelect?> TryShowSelectionScreen(Player snecko)
	{
		if (!LocalContext.IsMe(snecko) || NOverlayStack.Instance == null || NGame.Instance == null)
		{
			return null;
		}
		NSneckoCharacterSelect selectScene = new NSneckoCharacterSelect();
		NOverlayStack.Instance.Push((IOverlayScreen)(object)selectScene);
		await ((GodotObject)NGame.Instance).ToSignal((GodotObject)(object)((Node)NGame.Instance).GetTree(), SignalName.ProcessFrame);
		return selectScene;
	}

	private static async Task<List<CharacterModel>> SyncSelections(Player snecko, List<CharacterModel> sixCharacters, uint[] choiceIds)
	{
		List<CharacterModel> chosen = new List<CharacterModel>();
		for (int i = 0; i < 3; i++)
		{
			CharacterModel left = sixCharacters[i * 2];
			CharacterModel right = sixCharacters[i * 2 + 1];
			chosen.Add((await SyncOneChoice(snecko, left, right, choiceIds[i]) == 0) ? left : right);
		}
		return chosen;
	}

	private static async Task<int> SyncOneChoice(Player snecko, CharacterModel left, CharacterModel right, uint choiceId)
	{
		int num;
		if (LocalContext.IsMe(snecko))
		{
			num = await GetLocalChoice(left, right);
			RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(snecko, choiceId, PlayerChoiceResult.FromIndex((int?)num));
		}
		else
		{
			num = (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(snecko, choiceId)).AsIndex();
		}
		return num;
	}

	private static async Task<int> GetLocalChoice(CharacterModel left, CharacterModel right)
	{
		try
		{
			CharacterCard card1 = CharacterCard.Create(left);
			CharacterCard characterCard = CharacterCard.Create(right);
			NChooseACardSelectionScreen val = NChooseACardSelectionScreen.ShowScreen((IReadOnlyList<CardModel>)new global::_003C_003Ez__ReadOnlyArray<CardModel>((CardModel[])(object)new CardModel[2]
			{
				(CardModel)card1,
				(CardModel)characterCard
			}), false);
			if (val == null)
			{
				return 0;
			}
			return (!(await val.CardsSelected()).ToList().Contains((CardModel)(object)card1)) ? 1 : 0;
		}
		catch (Exception ex)
		{
			GD.PrintErr("SelectOne failed: " + ex.Message);
			return 0;
		}
	}

	private static void TearDownSelectionScreen(NSneckoCharacterSelect? selectScene)
	{
		if (selectScene != null)
		{
			NOverlayStack instance = NOverlayStack.Instance;
			if (instance != null)
			{
				instance.Remove((IOverlayScreen)(object)selectScene);
			}
			((Node)selectScene).QueueFree();
		}
	}
}

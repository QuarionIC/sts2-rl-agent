using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.SharedEvents;

public sealed class DesignerInSpire : CustomEventModel, IActRestricted, IShrineEvent
{
	private const int AdjustCost = 50;

	private const int CleanUpCost = 75;

	private const int FullServiceCost = 110;

	private const int HpLoss = 5;

	private bool _adjustmentUpgradesOne;

	private bool _cleanUpRemovesCards;

	public int[] AllowedActIndices => new int[2] { 2, 3 };

	public override ActModel[] Acts => Array.Empty<ActModel>();

	bool IShrineEvent.IsOneTimeEvent => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[4]
	{
		(DynamicVar)new IntVar("AdjustCost", 50m),
		(DynamicVar)new IntVar("CleanUpCost", 75m),
		(DynamicVar)new IntVar("FullServiceCost", 110m),
		(DynamicVar)new IntVar("HpLoss", 5m)
	};

	public override bool IsAllowed(IRunState runState)
	{
		return ((IPlayerCollection)runState).Players.All((Player p) => p.Gold >= 75);
	}

	public override void CalculateVars()
	{
		_adjustmentUpgradesOne = ((EventModel)this).Rng.NextInt(2) == 0;
		_cleanUpRemovesCards = ((EventModel)this).Rng.NextInt(2) == 0;
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return (IReadOnlyList<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Continue, "INITIAL", Array.Empty<IHoverTip>()) };
	}

	private Task Continue()
	{
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Expected O, but got Unknown
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Expected O, but got Unknown
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Expected O, but got Unknown
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Expected O, but got Unknown
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Expected O, but got Unknown
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Expected O, but got Unknown
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Expected O, but got Unknown
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Expected O, but got Unknown
		//IL_0307: Unknown result type (might be due to invalid IL or missing references)
		List<EventOption> list = new List<EventOption>();
		bool flag = ((EventModel)this).Owner.Gold >= 50;
		bool flag2 = ((EventModel)this).Owner.Deck.Cards.Any((CardModel c) => c.IsUpgradable);
		bool flag3 = ((EventModel)this).Owner.Gold >= 75;
		bool flag4 = ((EventModel)this).Owner.Gold >= 110;
		bool flag5 = ((EventModel)this).Owner.Deck.Cards.Any((CardModel c) => c.IsRemovable);
		bool flag6 = ((EventModel)this).Owner.Deck.Cards.Count((CardModel c) => c.IsRemovable) >= 2;
		if (flag && flag2)
		{
			string text = (_adjustmentUpgradesOne ? (((AbstractModel)this).Id.Entry + ".pages.MAIN.options.ADJUST_UPGRADE_ONE") : (((AbstractModel)this).Id.Entry + ".pages.MAIN.options.ADJUST_UPGRADE_TWO"));
			list.Add(new EventOption((EventModel)(object)this, _adjustmentUpgradesOne ? new Func<Task>(AdjustUpgradeOne) : new Func<Task>(AdjustUpgradeTwo), text, Array.Empty<IHoverTip>()));
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.MAIN.options.ADJUST_LOCKED", Array.Empty<IHoverTip>()));
		}
		if (_cleanUpRemovesCards)
		{
			if (flag3 && flag5)
			{
				list.Add(new EventOption((EventModel)(object)this, (Func<Task>)CleanUpRemove, ((AbstractModel)this).Id.Entry + ".pages.MAIN.options.CLEANUP_REMOVE", Array.Empty<IHoverTip>()));
			}
			else
			{
				list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.MAIN.options.CLEANUP_LOCKED", Array.Empty<IHoverTip>()));
			}
		}
		else if (flag3 && flag6)
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)CleanUpTransform, ((AbstractModel)this).Id.Entry + ".pages.MAIN.options.CLEANUP_TRANSFORM", Array.Empty<IHoverTip>()));
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.MAIN.options.CLEANUP_LOCKED", Array.Empty<IHoverTip>()));
		}
		if (flag4 && flag5)
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)FullService, ((AbstractModel)this).Id.Entry + ".pages.MAIN.options.FULL_SERVICE", Array.Empty<IHoverTip>()));
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.MAIN.options.FULL_SERVICE_LOCKED", Array.Empty<IHoverTip>()));
		}
		list.Add(new EventOption((EventModel)(object)this, (Func<Task>)Punch, ((AbstractModel)this).Id.Entry + ".pages.MAIN.options.PUNCH", Array.Empty<IHoverTip>()).ThatDoesDamage(5m));
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("MAIN"), (IEnumerable<EventOption>)list);
		return Task.CompletedTask;
	}

	private async Task AdjustUpgradeOne()
	{
		await PlayerCmd.LoseGold(50m, ((EventModel)this).Owner, (GoldLossType)1);
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
		CardModel card = (await CardSelectCmd.FromDeckForUpgrade(((EventModel)this).Owner, prefs)).FirstOrDefault();
		if (card != null)
		{
			CardCmd.Upgrade(card, (CardPreviewStyle)1);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("SERVICE"));
	}

	private async Task AdjustUpgradeTwo()
	{
		await PlayerCmd.LoseGold(50m, ((EventModel)this).Owner, (GoldLossType)1);
		foreach (CardModel card in ListExtensions.StableShuffle<CardModel>(((EventModel)this).Owner.Deck.Cards.Where((CardModel c) => c.IsUpgradable).ToList(), ((EventModel)this).Owner.RunState.Rng.Niche).Take(2))
		{
			CardCmd.Upgrade(card, (CardPreviewStyle)1);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("SERVICE"));
	}

	private async Task CleanUpRemove()
	{
		await PlayerCmd.LoseGold(75m, ((EventModel)this).Owner, (GoldLossType)1);
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
		await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)(await CardSelectCmd.FromDeckForRemoval(((EventModel)this).Owner, prefs, (Func<CardModel, bool>)null)).ToList(), true);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("SERVICE"));
	}

	private async Task CleanUpTransform()
	{
		await PlayerCmd.LoseGold(75m, ((EventModel)this).Owner, (GoldLossType)1);
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 2);
		foreach (CardModel original in (await CardSelectCmd.FromDeckForTransformation(((EventModel)this).Owner, prefs, (Func<CardModel, CardTransformation>)null)).ToList())
		{
			await CardCmd.TransformToRandom(original, ((EventModel)this).Rng, (CardPreviewStyle)3);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("SERVICE"));
	}

	private async Task FullService()
	{
		await PlayerCmd.LoseGold(110m, ((EventModel)this).Owner, (GoldLossType)1);
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
		await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)(await CardSelectCmd.FromDeckForRemoval(((EventModel)this).Owner, prefs, (Func<CardModel, bool>)null)).ToList(), true);
		IEnumerable<CardModel> upgradable = ListExtensions.StableShuffle<CardModel>(((EventModel)this).Owner.Deck.Cards.Where((CardModel c) => c.IsUpgradable).ToList(), ((EventModel)this).Owner.RunState.Rng.Niche).Take(1);
		foreach (CardModel card in upgradable)
		{
			CardCmd.Upgrade(card, (CardPreviewStyle)1);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("SERVICE"));
	}

	private async Task Punch()
	{
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, 5m, (ValueProp)6, (CardModel)null, (CardPlay)null);
		Control node = ((EventModel)this).Node;
		Node obj = ((node != null) ? ((Node)node).FindChild("Portrait", true, false) : null);
		TextureRect portrait = (TextureRect)(object)((obj is TextureRect) ? obj : null);
		if (portrait != null)
		{
			portrait.Texture = PreloadManager.Cache.GetTexture2D(ImageHelper.GetImagePath("events/actsfromthepast-designer_in_spire_punched.png"));
		}
		NDebugAudioManager.Instance.Play("blunt_attack.mp3", 1f, (PitchVariance)0);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PUNCH"));
	}

	public DesignerInSpire()
		: base(true)
	{
	}
}

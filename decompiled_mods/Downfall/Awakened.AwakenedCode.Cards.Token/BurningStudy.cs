using System.Threading.Tasks;
using Awakened.AwakenedCode.CustomEnums;
using Awakened.AwakenedCode.Events;
using Awakened.AwakenedCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Interfaces;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Awakened.AwakenedCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class BurningStudy : AwakenedCardModel, ISpell, IOnAwaken, ICustomTypePlaque
{
	public LocString GetTypePlaqueName => new LocString("gameplay_ui", "AWAKENED-SPELL");

	public BurningStudy()
		: base(1, (CardType)2, (CardRarity)7, (TargetType)1)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected I4, but got Unknown
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[2]
		{
			(CardKeyword)1,
			(CardKeyword)5
		});
		((ConstructedCardModel)this).WithPower<StrengthPower>(1, 1);
		((ConstructedCardModel)this).WithPower<WeakPower>(1, 1);
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)(int)AwakenedTag.Spell });
	}

	public Task OnAwaken(PlayerChoiceContext ctx, Player player)
	{
		if (player != ((CardModel)this).Owner)
		{
			return Task.CompletedTask;
		}
		CardCmd.Upgrade((CardModel)(object)this, (CardPreviewStyle)0);
		return Task.CompletedTask;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).CombatState == null)
		{
			return;
		}
		await CommonActions.ApplySelf<StrengthPower>(ctx, (CardModel)(object)this, false);
		foreach (Creature enemy in ((CardModel)this).CombatState.Enemies)
		{
			await CommonActions.Apply<WeakPower>(ctx, enemy, (CardModel)(object)this, false);
		}
	}
}

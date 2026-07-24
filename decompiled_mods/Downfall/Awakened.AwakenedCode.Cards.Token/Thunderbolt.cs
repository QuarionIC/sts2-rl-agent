using System.Threading.Tasks;
using Awakened.AwakenedCode.CustomEnums;
using Awakened.AwakenedCode.Events;
using Awakened.AwakenedCode.Interfaces;
using Awakened.AwakenedCode.Relics;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Interfaces;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Awakened.AwakenedCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Thunderbolt : AwakenedCardModel, ISpell, IOnAwaken, ICustomTypePlaque
{
	public override TargetType TargetType
	{
		get
		{
			if (((CardModel)this)._owner != null && ((CardModel)this).Owner.GetRelic<EyeOfTheOccult>() != null)
			{
				return (TargetType)3;
			}
			return (TargetType)2;
		}
	}

	public LocString GetTypePlaqueName => new LocString("gameplay_ui", "AWAKENED-SPELL");

	public Thunderbolt()
		: base(1, (CardType)1, (CardRarity)7, (TargetType)2)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected I4, but got Unknown
		((ConstructedCardModel)this).WithDamage(12, 6);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[2]
		{
			(CardKeyword)1,
			(CardKeyword)5
		});
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
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitFx("vfx/vfx_attack_lightning", (string)null, (string)null).Execute(ctx);
	}
}

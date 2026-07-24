using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Interfaces;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace Guardian.GuardianCode.Cards.Rare;

[Pool(typeof(GuardianCardPool))]
public class RockSlide : GuardianCardModel, IGemSocketCard, IModifyReplayCount, ICardOverlay
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<GoofballMcgee>();

	public int GemSlots => 3;

	public RockSlide()
		: base(3, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(30, 12);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianKeyword.Gem));
	}

	public override void AfterCreated()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		Rng niche = ((CardModel)this).Owner.RunState.Rng.Niche;
		if (this == null)
		{
			return;
		}
		CardRarity[] array = new CardRarity[3];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		CardRarity[] array2 = (CardRarity[])(object)array;
		foreach (CardRarity rarity in array2)
		{
			GemModel gemModel = niche.NextItem<GemModel>(GuardianModelDb.AllGems.Where((GemModel e) => e.Rarity == rarity))?.ToMutable();
			if (gemModel != null)
			{
				((IGemSocketCard)this).AddGem(gemModel);
			}
		}
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}

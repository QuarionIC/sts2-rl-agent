using System.Collections.Generic;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using BaseLib.Abstracts;
using BaseLib.Commands;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Common;

[Pool(typeof(AutomatonCardPool))]
public class CutThrough : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public CutThrough()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(6, 2);
		((ConstructedCardModel)(object)this).WithScry(1, 1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AutomatonTip.Stash));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null).Execute(ctx);
		await ScryCmd.Execute(ctx, (CardModel)(object)this);
		IReadOnlyList<CardModel> draw = ((CardModel)this).Owner.GetDraw();
		if (draw.Count != 0)
		{
			await StashCmd.Stash(draw[0]);
		}
	}
}

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Uncommon;

[Pool(typeof(GuardianCardPool))]
public class OrbSupport : GuardianCardModel
{
	public OrbSupport()
		: base(2, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(9, 3);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Brace));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int num = (await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx)).Results.SelectMany((List<DamageResult> r) => r).Sum((DamageResult x) => x.UnblockedDamage);
		await GuardianCmd.Brace(ctx, ((CardModel)this).Owner, num);
	}
}

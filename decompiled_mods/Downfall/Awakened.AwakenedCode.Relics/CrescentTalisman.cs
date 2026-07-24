using System;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.CustomEnums;
using Awakened.AwakenedCode.Enchantments;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Awakened.AwakenedCode.Relics;

[Pool(typeof(AwakenedRelicPool))]
public class CrescentTalisman : AwakenedRelicModel
{
	public override bool HasUponPickupEffect => true;

	public CrescentTalisman()
		: base((RelicRarity)4)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		WithTip<Conjuration>();
		WithTip(AwakenedTip.Conjure);
	}

	private static bool IsNonConjureSkillOrAttack(CardModel? card)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		CardType? val = ((card != null) ? new CardType?(card.Type) : ((CardType?)null));
		bool flag;
		if (val.HasValue)
		{
			CardType valueOrDefault = val.GetValueOrDefault();
			if (valueOrDefault - 1 <= 1)
			{
				flag = true;
				goto IL_0037;
			}
		}
		flag = false;
		goto IL_0037;
		IL_0037:
		if (flag)
		{
			return !card.Tags.Contains(AwakenedTag.Conjure);
		}
		return false;
	}

	public override async Task AfterObtained()
	{
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(CardSelectorPrefs.EnchantSelectionPrompt, 1);
		CardModel val2 = (await CardSelectCmd.FromDeckForEnchantment(((RelicModel)this).Owner, (EnchantmentModel)(object)ModelDb.Enchantment<Conjuration>(), 1, (Func<CardModel, bool>)IsNonConjureSkillOrAttack, val)).FirstOrDefault();
		if (val2 != null)
		{
			CardCmd.Enchant<Conjuration>(val2, 1m);
			CardCmd.Preview(val2, 1.2f, (CardPreviewStyle)1);
		}
	}
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using Automaton.AutomatonCode.Enchantments;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Automaton.AutomatonCode.Relics;

[Pool(typeof(AutomatonRelicPool))]
public class BottledCode : AutomatonRelicModel
{
	public override bool HasUponPickupEffect => true;

	public BottledCode()
		: base((RelicRarity)4)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		WithTip<Hardcoded>();
		WithTip(AutomatonTip.Encode);
	}

	public override async Task AfterObtained()
	{
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(CardSelectorPrefs.EnchantSelectionPrompt, 1);
		CardModel val2 = (await CardSelectCmd.FromDeckForEnchantment(((RelicModel)this).Owner, (EnchantmentModel)(object)ModelDb.Enchantment<Hardcoded>(), 1, (Func<CardModel, bool>)null, val)).FirstOrDefault();
		if (val2 != null)
		{
			CardCmd.Enchant<Hardcoded>(val2, 1m);
			CardCmd.Preview(val2, 1.2f, (CardPreviewStyle)1);
		}
	}
}

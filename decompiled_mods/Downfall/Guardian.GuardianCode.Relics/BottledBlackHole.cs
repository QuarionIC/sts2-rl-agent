using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Enchantments;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Guardian.GuardianCode.Relics;

[Pool(typeof(GuardianRelicPool))]
public class BottledBlackHole : GuardianRelicModel
{
	public override bool HasUponPickupEffect => true;

	public BottledBlackHole()
		: base((RelicRarity)3)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		WithTip<Temporal>();
		WithTip(GuardianTip.Stasis);
	}

	public override async Task AfterObtained()
	{
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(CardSelectorPrefs.EnchantSelectionPrompt, 1);
		CardModel val2 = (await CardSelectCmd.FromDeckForEnchantment(((RelicModel)this).Owner, (EnchantmentModel)(object)ModelDb.Enchantment<Temporal>(), 1, val)).FirstOrDefault();
		if (val2 != null)
		{
			CardCmd.Enchant<Temporal>(val2, 1m);
			CardCmd.Preview(val2, 1.2f, (CardPreviewStyle)1);
		}
	}
}

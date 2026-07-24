using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Enchantments;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Champ.ChampCode.Relics;

[Pool(typeof(ChampRelicPool))]
public class SignatureFinisher : ChampRelicModel
{
	public override bool HasUponPickupEffect => true;

	public SignatureFinisher()
		: base((RelicRarity)4)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(ChampTip.Finisher);
		WithTip<Signature>();
	}

	public override async Task AfterObtained()
	{
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(CardSelectorPrefs.EnchantSelectionPrompt, 1);
		CardModel val2 = (await CardSelectCmd.FromDeckForEnchantment(((RelicModel)this).Owner, (EnchantmentModel)(object)ModelDb.Enchantment<Signature>(), 1, val)).FirstOrDefault();
		if (val2 != null)
		{
			CardCmd.Enchant<Signature>(val2, 1m);
			CardCmd.Preview(val2, 1.2f, (CardPreviewStyle)1);
		}
	}
}

using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Hooks;

public interface IModifyResourceCostInCombat<T> where T : CustomResource
{
	decimal ModifyResourceCostInCombat(CardModel card, T resource, decimal originalCost);
}

using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace BaseLib.Cards.Variables;

public class RefundVar : DynamicVar
{
	public const string Key = "Refund";

	public RefundVar(decimal refundAmount)
		: base("Refund", refundAmount)
	{
		this.WithTooltip();
	}
}

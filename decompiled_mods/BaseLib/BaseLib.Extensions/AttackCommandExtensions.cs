using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.ValueProps;

namespace BaseLib.Extensions;

public static class AttackCommandExtensions
{
	public static AttackCommand WithValueProp(this AttackCommand attackCommand, ValueProp valueProp)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		attackCommand.DamageProps = valueProp;
		return attackCommand;
	}
}

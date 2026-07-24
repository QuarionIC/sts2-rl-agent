using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Extensions;

public static class ActModelExtensions
{
	public static int ActNumber(this ActModel actModel)
	{
		if (actModel.Index < 0)
		{
			return actModel.Index;
		}
		return actModel.Index + 1;
	}
}

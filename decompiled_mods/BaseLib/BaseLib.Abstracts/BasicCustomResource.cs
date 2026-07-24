using BaseLib.Patches.UI;

namespace BaseLib.Abstracts;

public abstract class BasicCustomResource : CustomResource
{
	private readonly int _setEachTurn;

	protected BasicCustomResource(string resourceId, int setEachTurn = -1)
	{
		_setEachTurn = setEachTurn;
		base._002Ector(resourceId);
	}

	public override void PrepForCombat()
	{
		Amount = 0;
	}

	public override ICustomCostVisualsHandler CostVisualsHandler()
	{
		return new BasicCostVisualsHandler(this);
	}

	public override ICustomResourceVisualsHandler ResourceVisualsHandler()
	{
		return new BasicResourceVisualsHandler(this);
	}
}

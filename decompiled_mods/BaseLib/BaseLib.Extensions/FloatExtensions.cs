using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace BaseLib.Extensions;

public static class FloatExtensions
{
	public static float OrFast(this float time)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		FastModeType fastMode = SaveManager.Instance.PrefsSave.FastMode;
		if ((int)fastMode != 2)
		{
			if ((int)fastMode == 3)
			{
				return 0.01f;
			}
			return time;
		}
		return time * 0.3f;
	}
}

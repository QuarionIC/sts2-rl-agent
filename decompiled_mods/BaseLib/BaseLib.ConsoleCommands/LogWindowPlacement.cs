using BaseLib.BaseLibScenes;
using BaseLib.Config;
using Godot;

namespace BaseLib.ConsoleCommands;

internal static class LogWindowPlacement
{
	internal static void SetupPosition(NLogWindow logWindow, Window host)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		int num = host.CurrentScreen;
		bool flag = TryRestorePosition((Window)(object)logWindow);
		if (!flag)
		{
			int screenCount = DisplayServer.GetScreenCount();
			if (screenCount > 1)
			{
				for (int i = 0; i < screenCount; i++)
				{
					if (i != num)
					{
						num = i;
						break;
					}
				}
			}
			((Window)logWindow).CurrentScreen = num;
		}
		else
		{
			num = ((Window)logWindow).CurrentScreen;
		}
		if (host.ContentScaleFactor > 0f)
		{
			((Window)logWindow).ContentScaleFactor = host.ContentScaleFactor;
		}
		Rect2I val = DisplayServer.ScreenGetUsableRect(num);
		if (BaseLibConfig.LogLastSizeX > 0 && BaseLibConfig.LogLastSizeY > 0 && BaseLibConfig.LogLastSizeX <= ((Rect2I)(ref val)).Size.X && BaseLibConfig.LogLastSizeY <= ((Rect2I)(ref val)).Size.Y)
		{
			((Window)logWindow).Size = new Vector2I(BaseLibConfig.LogLastSizeX, BaseLibConfig.LogLastSizeY);
		}
		else
		{
			((Window)logWindow).Size = ComputeDefaultSize((num == host.CurrentScreen) ? host.Size : ((Rect2I)(ref val)).Size);
		}
		if (!flag)
		{
			((Window)logWindow).Position = ((Rect2I)(ref val)).Position + ((Rect2I)(ref val)).Size / 2 - ((Window)logWindow).Size / 2;
		}
	}

	private static bool TryRestorePosition(Window logWindow)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		int logLastPosX = BaseLibConfig.LogLastPosX;
		int logLastPosY = BaseLibConfig.LogLastPosY;
		if (logLastPosX == int.MinValue && logLastPosY == int.MinValue)
		{
			return false;
		}
		int num = ((BaseLibConfig.LogLastSizeX > 0) ? BaseLibConfig.LogLastSizeX : logWindow.Size.X);
		int num2 = ((BaseLibConfig.LogLastSizeY > 0) ? BaseLibConfig.LogLastSizeY : logWindow.Size.Y);
		Vector2I val = default(Vector2I);
		((Vector2I)(ref val))._002Ector(logLastPosX + num / 2, logLastPosY + num2 / 2);
		for (int i = 0; i < DisplayServer.GetScreenCount(); i++)
		{
			Rect2I val2 = DisplayServer.ScreenGetUsableRect(i);
			if (((Rect2I)(ref val2)).HasPoint(val))
			{
				logWindow.CurrentScreen = i;
				logWindow.Position = new Vector2I(logLastPosX, logLastPosY);
				return true;
			}
		}
		return false;
	}

	internal static Vector2I ComputeDefaultSize(Vector2I hostSize)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		if (hostSize.X <= 0 || hostSize.Y <= 0)
		{
			return new Vector2I(800, 600);
		}
		int num = hostSize.X * 2 / 3;
		int num2 = hostSize.Y * 2 / 3;
		int num3 = Mathf.Clamp((int)((float)num2 * 2.35f), 960, 2048);
		int num4 = Mathf.Min(Mathf.Min(num, num3), Mathf.Max(320, hostSize.X - 32));
		num2 = Mathf.Min(num2, Mathf.Max(200, hostSize.Y - 32));
		return new Vector2I(num4, num2);
	}
}

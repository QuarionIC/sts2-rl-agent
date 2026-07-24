using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public abstract class CustomEventModel : EventModel, ICustomModel, ILocalizationProvider
{
	public virtual ActModel[] Acts => Array.Empty<ActModel>();

	public virtual string? CustomInitialPortraitPath => null;

	public virtual string? CustomBackgroundScenePath => null;

	public virtual string? CustomVfxPath => null;

	public virtual List<(string, string)>? Localization => null;

	public CustomEventModel(bool autoAdd = true)
	{
		if (autoAdd)
		{
			CustomContentDictionary.AddEvent(this);
		}
	}

	protected EventOption Option(Func<Task>? onChosen, LocString title, LocString description, params IHoverTip[] tips)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		return new EventOption((EventModel)(object)this, onChosen, title, description, ((AbstractModel)this).Id.Entry, (IEnumerable<IHoverTip>)tips);
	}

	protected EventOption Option(Func<Task>? onChosen, string pageKey = "INITIAL", params IHoverTip[] tips)
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Expected O, but got Unknown
		MethodInfo methodInfo = onChosen?.Method;
		string text = "UNKNOWN";
		if (methodInfo == null)
		{
			BaseLibMain.Logger.Error("Unable to get delegate method for CustomEventModel.Option; provide an explicit title and description LocString if not passing a method directly.", 1);
		}
		else
		{
			if (methodInfo.IsSpecialName)
			{
				BaseLibMain.Logger.Warn("Method passed as delegate to CustomEventModel.Option has special name; recommended to directly pass declared method or provide an explicit title and description LocString.", 1);
			}
			text = methodInfo.Name;
		}
		return new EventOption((EventModel)(object)this, onChosen, $"{((AbstractModel)this).Id.Entry}.pages.{pageKey}.options.{StringHelper.Slugify(text)}", tips);
	}

	protected EventOption Option(Func<Task>? onChosen, IEnumerable<IHoverTip> tips, string pageKey = "INITIAL")
	{
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Expected O, but got Unknown
		MethodInfo methodInfo = onChosen?.Method;
		string text = "UNKNOWN";
		if (onChosen == null)
		{
			BaseLibMain.Logger.Warn("Defining event option with no effect in event " + ((AbstractModel)this).Id.Entry + ", use LockedOption rather than Option", 1);
		}
		else if (methodInfo == null)
		{
			BaseLibMain.Logger.Error("Unable to get delegate method for CustomEventModel.Option; provide an explicit title and description LocString if not passing a method directly.", 1);
		}
		else
		{
			if (methodInfo.IsSpecialName)
			{
				BaseLibMain.Logger.Warn("Method passed as delegate to CustomEventModel.Option has special name; recommended to directly pass declared method or provide an explicit title and description LocString.", 1);
			}
			text = methodInfo.Name;
		}
		return new EventOption((EventModel)(object)this, onChosen, $"{((AbstractModel)this).Id.Entry}.pages.{pageKey}.options.{StringHelper.Slugify(text)}", tips);
	}

	protected EventOption LockedOption(string locKey, string pageKey = "INITIAL", params IHoverTip[] tips)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		return new EventOption((EventModel)(object)this, (Func<Task>)null, $"{((AbstractModel)this).Id.Entry}.pages.{pageKey}.options.{locKey}", tips);
	}

	protected LocString PageDescription(string pageKey)
	{
		return ((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages." + pageKey + ".description");
	}
}

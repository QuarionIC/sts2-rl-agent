using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.RichTextTags;
using MegaCrit.Sts2.addons.mega_text;

namespace Downfall.DownfallCode.Utils;

public static class RichTextEffectRegistry
{
	private static readonly List<AbstractMegaRichTextEffect> Effects = new List<AbstractMegaRichTextEffect>();

	private static readonly HashSet<Type> RegisteredTypes = new HashSet<Type>();

	private static void Register(AbstractMegaRichTextEffect effect)
	{
		if (RegisteredTypes.Add(((object)effect).GetType()))
		{
			Effects.Add(effect);
		}
	}

	public static void Register<T>() where T : AbstractMegaRichTextEffect, new()
	{
		Register((AbstractMegaRichTextEffect)(object)new T());
	}

	internal static void InstallInto(MegaRichTextLabel label)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		foreach (AbstractMegaRichTextEffect item in Effects.Where((AbstractMegaRichTextEffect effect) => !label.HasEffect(effect)))
		{
			((RichTextLabel)label).CustomEffects.Add(Variant.op_Implicit((GodotObject)(object)item));
		}
	}
}

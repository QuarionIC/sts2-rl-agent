using System;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast;

public static class ClassicSlimedTracker
{
	public static readonly SpireField<CardModel, bool> IsClassicSlimed = new SpireField<CardModel, bool>((Func<bool>)(() => false));

	public static bool CreatingClassicSlimed { get; set; } = false;
}

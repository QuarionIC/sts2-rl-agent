using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public abstract class CustomModifierModel : ModifierModel, ICustomModel
{
	public abstract ModifierAlignment Alignment { get; }

	public virtual IEnumerable<ModifierModel> MutuallyExclusiveGroup => Array.Empty<ModifierModel>();

	public virtual int SortOrder => 0;
}

using System;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Utils;

internal interface ISavedSpireField
{
	bool IsBasegameSupported { get; }

	string Name { get; }

	Type TargetType { get; }

	void Export(object model, SavedProperties props);

	void Import(object model, SavedProperties props);

	bool RegisterCustomSave();
}

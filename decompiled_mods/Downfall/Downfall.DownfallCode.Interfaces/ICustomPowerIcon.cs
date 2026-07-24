using System;
using Godot;

namespace Downfall.DownfallCode.Interfaces;

public interface ICustomPowerIcon
{
	event Action? IconChanged;

	void DecorateIcon(TextureRect icon);
}

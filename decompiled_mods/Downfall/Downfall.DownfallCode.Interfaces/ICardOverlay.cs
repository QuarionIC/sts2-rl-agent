using Godot;

namespace Downfall.DownfallCode.Interfaces;

public interface ICardOverlay
{
	Control CreateCustomOverlay();

	void UpdateOverlay(Control overlay);
}

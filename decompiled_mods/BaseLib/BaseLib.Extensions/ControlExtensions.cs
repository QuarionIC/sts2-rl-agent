using Godot;

namespace BaseLib.Extensions;

public static class ControlExtensions
{
	private static readonly NodePath EmptyNodePath = new NodePath();

	public static void DrawDebug(this Control item)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		((CanvasItem)item).DrawRect(new Rect2(0f, 0f, item.Size), new Color(1f, 1f, 1f, 0.5f), true, -1f, false);
	}

	public static void DrawDebug(this Control artist, Control child)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		((CanvasItem)artist).DrawRect(new Rect2(child.Position, child.Size), new Color(1f, 1f, 1f, 0.5f), true, -1f, false);
	}

	public static void AddThemeFontSizeOverrideAll(this Control control, int fontSize)
	{
		string[] array = new string[6] { "font_size", "normal_font_size", "bold_font_size", "italics_font_size", "bold_italics_font_size", "mono_font_size" };
		foreach (string text in array)
		{
			control.AddThemeFontSizeOverride(StringName.op_Implicit(text), fontSize);
		}
	}

	public static void ClearFocusNeighbors(this Control control)
	{
		control.FocusNeighborTop = EmptyNodePath;
		control.FocusNeighborBottom = EmptyNodePath;
		control.FocusNeighborLeft = EmptyNodePath;
		control.FocusNeighborRight = EmptyNodePath;
	}
}

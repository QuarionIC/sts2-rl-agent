using System.Runtime.CompilerServices;

namespace Automaton.AutomatonCode.Interfaces;

public record EncodeContext(bool IsFromFunction, int SlotIndex = 0)
{
	public static readonly EncodeContext Direct = new EncodeContext(IsFromFunction: false);

	[CompilerGenerated]
	protected EncodeContext(EncodeContext original)
	{
		IsFromFunction = original.IsFromFunction;
		SlotIndex = original.SlotIndex;
	}
}

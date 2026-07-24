using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BaseLib.Abstracts;

public record CardLoc(string Title, string Description, params (string, string)[] ExtraLoc)
{
	public static implicit operator List<(string, string)>(CardLoc loc)
	{
		(string, string) tuple = ("title", loc.Title);
		(string, string) tuple2 = ("description", loc.Description);
		(string, string)[] extraLoc = loc.ExtraLoc;
		int num = 2 + extraLoc.Length;
		List<(string, string)> list = new List<(string, string)>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<(string, string)> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = tuple;
		num2++;
		span[num2] = tuple2;
		num2++;
		ReadOnlySpan<(string, string)> readOnlySpan = new ReadOnlySpan<(string, string)>(extraLoc);
		readOnlySpan.CopyTo(span.Slice(num2, readOnlySpan.Length));
		num2 += readOnlySpan.Length;
		return list;
	}
}

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Downfall.DownfallCode.Voting;

public record ArtEntry
{
	public long Id { get; init; }

	public required string ImagePath { get; init; }

	public required string Author { get; init; }

	public required string Name { get; init; }

	public int Upvotes { get; init; }

	public int Downvotes { get; init; }

	public int MyVote { get; init; }

	public HashSet<string> MyFlags { get; init; } = new HashSet<string>();

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected ArtEntry(ArtEntry original)
	{
		Id = original.Id;
		ImagePath = original.ImagePath;
		Author = original.Author;
		Name = original.Name;
		Upvotes = original.Upvotes;
		Downvotes = original.Downvotes;
		MyVote = original.MyVote;
		MyFlags = original.MyFlags;
	}

	public ArtEntry()
	{
	}
}

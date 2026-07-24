using System.Collections.Generic;
using System.Linq;

namespace Downfall.DownfallCode.Abstract;

public static class CardResourceRegistry
{
	private static readonly List<CardResource> _resources = new List<CardResource>();

	public static void Register(CardResource resource)
	{
		_resources.Add(resource);
	}

	public static IReadOnlyList<CardResource> GetAll()
	{
		return _resources;
	}

	public static T? Get<T>() where T : CardResource
	{
		return _resources.OfType<T>().FirstOrDefault();
	}
}

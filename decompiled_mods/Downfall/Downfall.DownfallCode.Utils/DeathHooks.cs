using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Downfall.DownfallCode.Utils;

public static class DeathHooks
{
	public delegate Task? DeathInterceptor(Creature creature);

	private static readonly List<DeathInterceptor> interceptors = new List<DeathInterceptor>();

	public static void RegisterInterceptor(DeathInterceptor interceptor)
	{
		interceptors.Add(interceptor);
	}

	internal static Task? TryIntercept(Creature creature)
	{
		foreach (DeathInterceptor interceptor in interceptors)
		{
			try
			{
				Task task = interceptor(creature);
				if (task != null)
				{
					return task;
				}
			}
			catch (Exception value)
			{
				DownfallMainFile.Logger.Error($"Death interceptor failed: {value}", 1);
			}
		}
		return null;
	}
}

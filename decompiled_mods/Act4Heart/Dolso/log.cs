using System.Diagnostics;
using MegaCrit.Sts2.Core.Logging;
using MonoMod.Cil;

namespace Dolso;

internal static class log
{
	private static readonly Logger logger = new Logger(typeof(log).Assembly.GetName().Name, (LogType)2);

	[Conditional("DEBUG")]
	internal static void debug(object? data)
	{
	}

	internal static void info(object? data)
	{
		logcore((LogLevel)3, data);
	}

	internal static void warning(object? data)
	{
		logcore((LogLevel)4, data);
	}

	internal static void error(object? data)
	{
		logcore((LogLevel)5, data);
	}

	private static void logcore(LogLevel level, object? data)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		logger.LogMessage(level, data?.ToString(), ((int)level != 5) ? 2 : 1728);
	}

	internal static void LogError(this ILCursor c, object? data)
	{
		logger.LogMessage((LogLevel)5, $"ILCursor failure, skipping: {data}\n{c}", 1);
	}

	internal static void LogErrorCaller(this ILCursor c, object? data)
	{
		logger.LogMessage((LogLevel)5, $"ILCursor failed in {new StackFrame(1).GetMethod().Name}, skipping: {data}\n{c}", 1);
	}
}

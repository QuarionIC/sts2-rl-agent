using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace Downfall.DownfallCode.Compatibility;

public static class CompatibilityAnimation
{
	private const BindingFlags F = BindingFlags.Instance | BindingFlags.Public;

	private static readonly object InitLock = new object();

	private static MethodInfo? _setAnimationM;

	private static MethodInfo? _getCurrentM;

	private static MethodInfo? _addAnimationM;

	private static MethodInfo? _addEmptyAnimationM;

	private static volatile bool _initialized;

	private static bool _initFailed;

	private static readonly ConcurrentDictionary<(Type, string), MethodInfo?> EntryMethodCache = new ConcurrentDictionary<(Type, string), MethodInfo>();

	private static readonly HashSet<string> LoggedFailures = new HashSet<string>();

	private static bool EnsureInitialized()
	{
		if (_initialized)
		{
			return !_initFailed;
		}
		lock (InitLock)
		{
			if (_initialized)
			{
				return !_initFailed;
			}
			try
			{
				Type typeFromHandle = typeof(MegaAnimationState);
				_setAnimationM = FindByName(typeFromHandle, "SetAnimation", typeof(string), typeof(bool));
				_getCurrentM = FindByName(typeFromHandle, "GetCurrent", typeof(int));
				_addAnimationM = FindByName(typeFromHandle, "AddAnimationTracked", typeof(string)) ?? FindByName(typeFromHandle, "AddAnimation", typeof(string));
				_addEmptyAnimationM = FindByName(typeFromHandle, "AddEmptyAnimation");
				if (_setAnimationM == null)
				{
					DownfallMainFile.Logger.Warn("CompatibilityAnimation: SetAnimation not found — animations will be skipped.", 1);
				}
				if (_addAnimationM == null)
				{
					DownfallMainFile.Logger.Warn("CompatibilityAnimation: AddAnimation(Tracked) not found — queued animations will be skipped.", 1);
				}
				_initFailed = _setAnimationM == null && _addAnimationM == null;
			}
			catch (Exception ex)
			{
				_initFailed = true;
				DownfallMainFile.Logger.Warn("CompatibilityAnimation: init failed, animations disabled. " + ex.Message, 1);
			}
			_initialized = true;
		}
		return !_initFailed;
	}

	private static MethodInfo? FindByName(Type type, string name, params Type[] leading)
	{
		return (from m in (from m in type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
				where m.Name == name
				select m).Where(delegate(MethodInfo m)
			{
				ParameterInfo[] parameters = m.GetParameters();
				if (parameters.Length < leading.Length)
				{
					return false;
				}
				for (int i = 0; i < leading.Length; i++)
				{
					if (parameters[i].ParameterType != leading[i])
					{
						return false;
					}
				}
				for (int j = leading.Length; j < parameters.Length; j++)
				{
					if (!parameters[j].IsOptional)
					{
						return false;
					}
				}
				return true;
			})
			orderby m.GetParameters().Length
			select m).FirstOrDefault();
	}

	private static MethodInfo? FindEntryMethod(object entry, string name, params Type[] leading)
	{
		return EntryMethodCache.GetOrAdd((entry.GetType(), name), ((Type, string) _) => FindByName(entry.GetType(), name, leading));
	}

	private static object? Call(MethodInfo m, object target, params object?[] args)
	{
		ParameterInfo[] parameters = m.GetParameters();
		if (parameters.Length == args.Length)
		{
			return m.Invoke(target, args);
		}
		object[] array = new object[parameters.Length];
		Array.Copy(args, array, args.Length);
		for (int i = args.Length; i < parameters.Length; i++)
		{
			object defaultValue = parameters[i].DefaultValue;
			array[i] = ((defaultValue == DBNull.Value) ? Type.Missing : defaultValue);
		}
		return m.Invoke(target, BindingFlags.Instance | BindingFlags.Public | BindingFlags.OptionalParamBinding, null, array, null);
	}

	private static void DisposeEntry(object? entry)
	{
		try
		{
			(entry as IDisposable)?.Dispose();
		}
		catch
		{
		}
	}

	private static void TrySetMixDuration(object entry, float mix)
	{
		MethodInfo methodInfo = FindEntryMethod(entry, "SetMixDuration", typeof(float));
		if (methodInfo == null)
		{
			LogOnce("SetMixDuration", entry.GetType().Name + ".SetMixDuration(float) not found — mix ignored.");
			return;
		}
		Call(methodInfo, entry, mix);
	}

	private static void LogOnce(string key, string message)
	{
		lock (LoggedFailures)
		{
			if (LoggedFailures.Add(key))
			{
				DownfallMainFile.Logger.Warn("CompatibilityAnimation: " + message, 1);
			}
		}
	}

	private static object? SetAnimationGetEntry(MegaAnimationState animState, string anim, bool loop)
	{
		object obj = Call(_setAnimationM, animState, anim, loop);
		if (obj == null && _getCurrentM != null)
		{
			obj = Call(_getCurrentM, animState, 0);
		}
		return obj;
	}

	public static void SetAnimationCompat(this MegaAnimationState animState, string anim, bool loop = true)
	{
		if (!EnsureInitialized() || _setAnimationM == null)
		{
			return;
		}
		object entry = null;
		try
		{
			entry = Call(_setAnimationM, animState, anim, loop);
		}
		catch (Exception ex)
		{
			LogOnce("SetAnimationCompat:" + ex.GetType().Name, "SetAnimation failed: " + (ex.InnerException?.Message ?? ex.Message));
		}
		finally
		{
			DisposeEntry(entry);
		}
	}

	public static void AddAnimationCompat(this MegaAnimationState animState, string anim)
	{
		if (!EnsureInitialized() || _addAnimationM == null)
		{
			return;
		}
		object entry = null;
		try
		{
			entry = Call(_addAnimationM, animState, anim);
		}
		catch (Exception ex)
		{
			LogOnce("AddAnimationCompat:" + ex.GetType().Name, "AddAnimation failed: " + (ex.InnerException?.Message ?? ex.Message));
		}
		finally
		{
			DisposeEntry(entry);
		}
	}

	public static void AddEmptyAnimationCompat(this MegaAnimationState animState)
	{
		if (!EnsureInitialized() || _addEmptyAnimationM == null)
		{
			return;
		}
		object entry = null;
		try
		{
			entry = Call(_addEmptyAnimationM, animState);
		}
		catch (Exception ex)
		{
			LogOnce("AddEmptyAnimationCompat:" + ex.GetType().Name, "AddEmptyAnimation failed: " + (ex.InnerException?.Message ?? ex.Message));
		}
		finally
		{
			DisposeEntry(entry);
		}
	}

	public static void SetAnimationWithMix(this MegaAnimationState animState, string anim, float mix, bool loop = true)
	{
		if (!EnsureInitialized() || _setAnimationM == null)
		{
			return;
		}
		object obj = null;
		try
		{
			obj = SetAnimationGetEntry(animState, anim, loop);
			if (obj != null)
			{
				TrySetMixDuration(obj, mix);
			}
		}
		catch (Exception ex)
		{
			LogOnce("SetAnimationWithMix:" + ex.GetType().Name, "SetAnimationWithMix failed: " + (ex.InnerException?.Message ?? ex.Message));
		}
		finally
		{
			DisposeEntry(obj);
		}
	}

	public static void QueueAnimation(this MegaAnimationState animState, string anim, float mix)
	{
		if (!EnsureInitialized() || _addAnimationM == null)
		{
			return;
		}
		object obj = null;
		try
		{
			obj = Call(_addAnimationM, animState, anim);
			if (obj != null)
			{
				TrySetMixDuration(obj, mix);
			}
		}
		catch (Exception ex)
		{
			LogOnce("QueueAnimation:" + ex.GetType().Name, "QueueAnimation failed: " + (ex.InnerException?.Message ?? ex.Message));
		}
		finally
		{
			DisposeEntry(obj);
		}
	}

	public static void SetAnimationRandomStart(this MegaAnimationState animState, string anim, bool loop, float normalizedTime)
	{
		if (!EnsureInitialized() || _setAnimationM == null)
		{
			return;
		}
		object obj = null;
		try
		{
			obj = SetAnimationGetEntry(animState, anim, loop);
			if (obj == null)
			{
				return;
			}
			MethodInfo methodInfo = FindEntryMethod(obj, "GetAnimationEnd");
			MethodInfo methodInfo2 = FindEntryMethod(obj, "SetTrackTime", typeof(float));
			if (methodInfo == null || methodInfo2 == null)
			{
				LogOnce("RandomStart", obj.GetType().Name + ": GetAnimationEnd/SetTrackTime not found — random start skipped.");
				return;
			}
			object obj2 = Call(methodInfo, obj);
			if (obj2 != null)
			{
				Call(methodInfo2, obj, Convert.ToSingle(obj2) * normalizedTime);
			}
		}
		catch (Exception ex)
		{
			LogOnce("SetAnimationRandomStart:" + ex.GetType().Name, "SetAnimationRandomStart failed: " + (ex.InnerException?.Message ?? ex.Message));
		}
		finally
		{
			DisposeEntry(obj);
		}
	}
}

using HarmonyLib;
using StardewModdingAPI;
using System.Reflection;

namespace EMU.Framework;

public static class HarmonyHelper
{
	private static readonly string[] patchTypeNames = ["prefix", "postfix", "transpiler", "finalizer"];

	public sealed record class PatchData(Harmony harmony, MethodInfo[] targets, Type? targetType, IMonitor monitor);

	private static PatchData WithImpl(PatchData patch, string name, bool all, Type? target = null)
	{
		target ??= patch.targetType;
		if (target is null)
			throw new InvalidOperationException("Must specify target type.");

		MethodInfo[] targs;

		if (all)
		{
			targs = target
				.GetMethods(ModUtilities.AnyDeclared | BindingFlags.Static | BindingFlags.Instance)
				.Where(m => m.Name == name).ToArray();
		}
		else
		{
			var m = target.GetMethod(name, ModUtilities.AnyDeclared | BindingFlags.Static | BindingFlags.Instance);
			targs = m is null ? [] : [m];
		}

		if (targs.Length is 0)
			patch.monitor.Log($"No method with name '{name}' is declared by type '{target}'.", LogLevel.Error);

		return new(patch.harmony, targs, target, patch.monitor);
	}

	public static PatchData Patcher(this Harmony harmony, IMonitor monitor)
	{
		return new(harmony, [], null, monitor);
	}

	public static PatchData Patcher<T>(this Harmony harmony, IMonitor monitor)
	{
		return new(harmony, [], typeof(T), monitor);
	}

	public static PatchData With<T>(this PatchData patch, string name, bool all = false)
	{
		return WithImpl(patch, name, all, typeof(T));
	}

	public static PatchData With(this PatchData patch, string name, bool all = false)
	{
		return WithImpl(patch, name, all);
	}

	private static PatchData WithPropertyImpl(PatchData patch, string name, bool getter, bool all, Type? target = null)
	{
		target ??= patch.targetType;
		if (target is null)
			throw new InvalidOperationException("Must specify target type.");

		PropertyInfo[] targs;

		if (all)
		{
			targs = target
				.GetProperties(ModUtilities.AnyDeclared | BindingFlags.Static | BindingFlags.Instance)
				.Where(m => m.Name == name && ((!getter && m.CanWrite) || (getter && m.CanRead))).ToArray();
		}
		else
		{
			var m = target.GetProperty(name, ModUtilities.AnyDeclared | BindingFlags.Static | BindingFlags.Instance);
			targs = m is null ? [] : [m];
		}

		MethodInfo[] impls = new MethodInfo[targs.Length];
		for (int i = 0; i < targs.Length; i++)
			impls[i] = getter ? targs[i].GetMethod! : targs[i].SetMethod!;

		if (impls.Length is 0)
			patch.monitor.Log($"No property with name '{name}' is declared by type '{target}'.", LogLevel.Error);

		return new(patch.harmony, impls, target, patch.monitor);
	}

	public static PatchData WithProperty<T>(this PatchData patch, string name, bool getter, bool all = false)
	{
		return WithPropertyImpl(patch, name, all, getter, typeof(T));
	}

	public static PatchData WithProperty(this PatchData patch, string name, bool getter, bool all = false)
	{
		return WithPropertyImpl(patch, name, all, getter);
	}

	private static void PatchImpl(PatchData patch, HarmonyMethod use, int type)
	{
		HarmonyMethod?[] applies = new HarmonyMethod?[4];
		applies[type] = use;

		if (patch.targetType is null)
			throw new InvalidOperationException("Target type must be specified before patching.");

		for (int i = 0; i < patch.targets.Length; i++)
		{
			try
			{
				patch.harmony.Patch(patch.targets[i], applies[0], applies[1], applies[2], applies[3]);
			}
			catch (Exception e)
			{
				patch.monitor.Log($"Failed to apply {patchTypeNames[type]} '{use.method}' to '{patch.targets[i]}':\n{e}", LogLevel.Error);
			}
		}
	}

	public static PatchData Prefix(this PatchData patch, Delegate use)
	{
		PatchImpl(patch, new(use.Method), 0);
		return patch;
	}

	public static PatchData Postfix(this PatchData patch, Delegate use)
	{
		PatchImpl(patch, new(use.Method), 1);
		return patch;
	}

	public static PatchData Transpiler(this PatchData patch, Delegate use)
	{
		PatchImpl(patch, new(use.Method), 2);
		return patch;
	}

	public static PatchData Finalizer(this PatchData patch, Delegate use)
	{
		PatchImpl(patch, new(use.Method), 3);
		return patch;
	}
}

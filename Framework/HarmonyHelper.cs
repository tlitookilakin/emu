using HarmonyLib;
using StardewModdingAPI;
using System.Reflection;

namespace EMU.Framework;

public class HarmonyHelper(Harmony Harmony, IMonitor Monitor)
{
	private static readonly string[] patchTypeNames = ["prefix", "postfix", "transpiler", "finalizer"];

	private readonly MethodInfo[] targets = [];
	private readonly Type? targetType;

	private HarmonyHelper(Harmony h, IMonitor m, MethodInfo[] methods, Type type) : this(h, m)
	{
		targets = methods;
		targetType = type;
	}

	public HarmonyHelper With<T>(string name)
		=> WithImpl(name, false, typeof(T));

	public HarmonyHelper With(string name)
		=> WithImpl(name, false);

	public HarmonyHelper WithProperty<T>(string name, bool getter)
		=> WithPropertyImpl(name, false, getter, typeof(T));

	public HarmonyHelper WithProperty(string name, bool getter)
		=> WithPropertyImpl(name, false, getter);

	public HarmonyHelper WithAll<T>(string name, Func<MethodInfo, bool>? predicate = null)
		=> WithImpl(name, true, typeof(T), predicate);

	public HarmonyHelper WithAll(string name, Func<MethodInfo, bool>? predicate = null)
		=> WithImpl(name, true, null, predicate);

	public HarmonyHelper Prefix(Delegate use)
		=> PatchImpl(new(use.Method), 0);

	public HarmonyHelper Postfix(Delegate use)
		=> PatchImpl(new(use.Method), 1);

	public HarmonyHelper Transpiler(Delegate use)
		=> PatchImpl(new(use.Method), 2);

	public HarmonyHelper Finalizer(Delegate use)
		=> PatchImpl(new(use.Method), 3);

	private HarmonyHelper WithImpl(string name, bool all, Type? target = null, Func<MethodInfo, bool>? predicate = null)
	{
		target ??= targetType;
		if (target is null)
			throw new InvalidOperationException("Must specify target type.");

		MethodInfo[] targs;

		if (all)
		{
			targs = target
				.GetMethods(ModUtilities.AnyDeclared | BindingFlags.Static | BindingFlags.Instance)
				.Where(m => m.Name == name && (predicate is null || predicate(m))).ToArray();
		}
		else
		{
			var m = target.GetMethod(name, ModUtilities.AnyDeclared | BindingFlags.Static | BindingFlags.Instance);
			targs = m is null ? [] : [m];
		}

		if (targs.Length is 0)
			Monitor.Log($"No method with name '{name}' is declared by type '{target}'.", LogLevel.Error);

		return new(Harmony, Monitor, targs, target);
	}

	private HarmonyHelper WithPropertyImpl(string name, bool getter, bool all, Type? target = null)
	{
		target ??= targetType;
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
			Monitor.Log($"No property with name '{name}' is declared by type '{target}'.", LogLevel.Error);

		return new(Harmony, Monitor, impls, target);
	}

	private HarmonyHelper PatchImpl(HarmonyMethod use, int type)
	{
		HarmonyMethod?[] applies = new HarmonyMethod?[4];
		applies[type] = use;

		if (targetType is null)
			throw new InvalidOperationException("Target type must be specified before patching.");

		for (int i = 0; i < targets.Length; i++)
		{
			try
			{
				Harmony.Patch(targets[i], applies[0], applies[1], applies[2], applies[3]);
			}
			catch (Exception e)
			{
				Monitor.Log($"Failed to apply {patchTypeNames[type]} '{use.method}' to '{targets[i]}':\n{e}", LogLevel.Error);
			}
		}

		return this;
	}
}

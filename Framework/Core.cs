using EMU.Framework.Attributes;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace EMU.Framework;

internal static class Core
{
	private static readonly List<KeyValuePair<string, object>> features = [];
	private static readonly Dictionary<Type, object> Providers = [];

	internal static void Provide<T>(T dependency) where T : class
	{
		Providers[typeof(T)] = dependency;
	}

	internal static void Init(IMonitor monitor)
	{
		var types = typeof(Core).Assembly.GetTypes();
		foreach (var type in types)
		{
			if (type.GetCustomAttribute<FeatureAttribute>() is not FeatureAttribute attr)
				continue;

			var name = attr.Name;

			if (!TryInitialize(type, out var inst, out var err))
			{
				monitor.Log($"Could not initialize feature '{name}':\n{err}", LogLevel.Error);
				continue;
			}

			if (!TryRegisterActions(type, inst, out err))
			{
				monitor.Log($"Could not register tile action(s) for feature '{name}':\n{err}", LogLevel.Error);
				continue;
			}

			features.Add(new(name, inst));
			monitor.Log($"Initialized feature '{name}'.", LogLevel.Trace);
		}
	}

	private static bool TryRegisterActions(Type type, object instance, [NotNullWhen(false)] out string? error)
	{
		error = null;

		foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
		{
			if (method.GetCustomAttribute<TileActionAttribute>() is TileActionAttribute actionAttr)
			{
				try
				{
					var deleg = method.CreateDelegate<Func<GameLocation, string[], Farmer, Point, bool>>(method.IsStatic ? null : instance);
					GameLocation.RegisterTileAction($"EMU_{actionAttr.Name}", deleg);
				}
				catch (Exception ex)
				{
					error = ex.ToString();
					return false;
				}
			}

			if (method.GetCustomAttribute<TouchActionAttribute>() is TouchActionAttribute touchAttr)
			{
				try
				{
					var deleg = method.CreateDelegate<Action<GameLocation, string[], Farmer, Vector2>>(method.IsStatic ? null : instance);
					GameLocation.RegisterTouchAction($"EMU_{touchAttr.Name}", deleg);
				}
				catch (Exception ex)
				{
					error = ex.ToString();
					return false;
				}
			}
		}

		return true;
	}

	private static bool TryInitialize(Type type, [NotNullWhen(true)] out object? obj, [NotNullWhen(false)] out string? error)
	{
		obj = null;
		error = null;

		var cons = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
		if (cons.Length is 0)
			cons = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

		if (cons.Length is 0)
		{
			error = "No public constructor available.";
			return false;
		}

		ConstructorInfo con = cons.FirstOrDefault(static c => c.GetParameters().Length > 0) ?? cons[0];

		ParameterInfo[] cParams = con.GetParameters();
		var args = new object[cParams.Length];

		for (int i = 0; i < args.Length; i++)
		{
			if (!Providers.TryGetValue(cParams[i].ParameterType, out var arg))
			{
				error = $"No provider for dependency of type {cParams[i].ParameterType.FullName}";
				return false;
			}

			args[i] = arg;
		}

		try
		{
			obj = con.Invoke(args);
		}
		catch (Exception ex)
		{
			error = ex.ToString();
			return false;
		}

		return true;
	}
}

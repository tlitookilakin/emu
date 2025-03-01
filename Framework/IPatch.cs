using HarmonyLib;
using StardewGenTools;
using StardewModdingAPI;
using System.Reflection;

namespace MUMPs.Framework
{
	[Collector("Patchers")]
	// Arrrrrr matey
	internal partial interface IPatch : IFeature
	{
		const BindingFlags AnyDeclared = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic;

		public void Patch(Harmony harmony, out string? Error);

		public string Name { get; }

		public static void PatchAll(string id, Logger Log)
		{
			var harmony = new Harmony(id);

			Log("Beginning patches...", LogLevel.Info);

			for (int i = 0; i < PatcherCount; i++)
			{
				string? error;
				try
				{
					Patchers[i].Patch(harmony, out error);
				}
				catch (Exception ex)
				{
					error = ex.ToString();
				}

				if (error is not null)
					Log($"Patch error in {Patchers[i].Name} feature:\n{error}", LogLevel.Error);
			}
		}
	}
}

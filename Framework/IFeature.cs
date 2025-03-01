using StardewGenTools;
using StardewModdingAPI;

namespace MUMPs.Framework
{
	[Collector("Features")]
	internal partial interface IFeature
	{
		public delegate void Logger(string message, LogLevel level);

		public void Init(Logger log, IModHelper helper);

		public static void InitAll(Logger log, IModHelper helper)
		{
			log(I18n.General_Startup(), LogLevel.Info);

			for (int i = 0; i < FeatureCount; i++)
				Features[i].Init(log, helper);
		}
	}
}

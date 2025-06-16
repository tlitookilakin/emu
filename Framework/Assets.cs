using EMU.Data;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static EMU.ModEntry;

namespace EMU.Framework;

internal class Assets : INotifyPropertyChanged
{
	const string SPRITE_DATA = MOD_ID + "/Sprites";
	const string EXTENDED_DATA = MOD_ID + "/ExtendedLocationData";

	public Dictionary<string, TemporarySpriteData> TempSprites
	{ 
		get => tempSprites ??= content.Load<Dictionary<string, TemporarySpriteData>>(SPRITE_DATA);
		private set => SetAndNotify(ref tempSprites, value);
	}
	private Dictionary<string, TemporarySpriteData>? tempSprites;

	public Dictionary<string, ExtendedLocationData> ExtendedData
	{
		get => extendedData ??= content.Load<Dictionary<string, ExtendedLocationData>>(EXTENDED_DATA);
		private set => SetAndNotify(ref extendedData, value);
	}
	private Dictionary<string, ExtendedLocationData>? extendedData;

	private readonly IGameContentHelper content;
	private readonly IMonitor monitor;

	public event PropertyChangedEventHandler? PropertyChanged;

	public bool TryLoad<T>(string name, out T asset, bool showError = true) where T : notnull
	{
		try
		{
			asset = content.Load<T>(name);
			return true;
		}
		catch (Exception ex)
		{
			monitor.Log($"Failed to load asset '{name}': {ex}", showError ? LogLevel.Warn : LogLevel.Trace);
		}
		asset = default!;
		return false;
	}

	public Assets(IModHelper helper, IMonitor monitor)
	{
		content = helper.GameContent;
		this.monitor = monitor;

		helper.Events.Content.AssetRequested += Requested;
		helper.Events.Content.AssetsInvalidated += Invalidated;
	}

	private void Invalidated(object? sender, AssetsInvalidatedEventArgs e)
	{
		foreach (var name in e.NamesWithoutLocale)
		{
			if (name.IsEquivalentTo(SPRITE_DATA))
				TempSprites = null!;
			else if (name.IsEquivalentTo(EXTENDED_DATA))
				ExtendedData = null!;
		}
	}

	private static void Requested(object? sender, AssetRequestedEventArgs e)
	{
		if (e.NameWithoutLocale.IsEquivalentTo(SPRITE_DATA))
			e.LoadFromModFile<Dictionary<string, TemporarySpriteData>>("assets/sprites.json", AssetLoadPriority.Low);
		else if (e.NameWithoutLocale.IsEquivalentTo(EXTENDED_DATA))
			e.LoadFrom(static () => new Dictionary<string, ExtendedLocationData>(), AssetLoadPriority.Low);
	}

	private void SetAndNotify<T>(ref T field, T value, [CallerMemberName]string? name = null)
	{
		if (name is null)
			return;

		field = value;
		PropertyChanged?.Invoke(this, new(name));
	}
}

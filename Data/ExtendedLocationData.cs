namespace EMU.Data;

public class ExtendedLocationData
{
	public Dictionary<string, Renovation>? Renovations { get; set; }
	public List<ForageRegionData>? ForageRegions { get; set; }
	public List<OverlayData>? Overlays { get; set; }
}

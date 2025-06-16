using Microsoft.Xna.Framework;

namespace EMU.Data;

public class OverlayData
{
	public string ID { get; set; } = "";

	public string? Condition { get; set; }

	public string? Texture { get; set; }

	public Rectangle Source { get; set; }

	public Rectangle Destination { get; set; }

	public Rectangle ActiveRegion { get; set; }

	public int FrameCount
	{
		get => frameCount;
		set => frameCount = Math.Max(value, 1);
	}
	private int frameCount = 1;

	public int FrameTime
	{
		get => frameTime;
		set => frameTime = Math.Max(value, 1);
	}
	private int frameTime = 1;

	public string? UseLayer { get; set; }

	public float Opacity
	{
		get => opacity;
		set => opacity = Math.Clamp(value, 0f, 1f);
	}
	private float opacity = 0f;
}

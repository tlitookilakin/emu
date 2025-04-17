namespace EMU.Framework.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class TileActionAttribute(string name) : Attribute
{
	public string Name { get; init; } = name;
}

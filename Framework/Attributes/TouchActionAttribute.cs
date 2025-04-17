namespace EMU.Framework.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class TouchActionAttribute(string name) : Attribute
{
	public string Name { get; init; } = name;
}

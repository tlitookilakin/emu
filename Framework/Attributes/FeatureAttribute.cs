namespace EMU.Framework.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class FeatureAttribute(string name) : Attribute
{
	public string Name { get; init; } = name;
}

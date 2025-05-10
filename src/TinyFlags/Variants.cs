namespace TinyFlags;

public interface IVariant;

/// <summary>
/// Represents a reference to a shared variant
/// </summary>
/// <param name="Name"></param>
public record VariantReference(string Name) : IVariant;

/// <summary>
/// An inline variant.
/// </summary>
/// <param name="Variant"></param>
public record InlineVariant(Variant Variant) : IVariant;

/// <summary>
/// Represents are colleciton key / value pairs.
/// </summary>
/// <param name="Values"></param>
public record Variant(IDictionary<string, IExpression> Values)
{
    public static readonly Variant Empty = new Variant(new Dictionary<string, IExpression>());
}

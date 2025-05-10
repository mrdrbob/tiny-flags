namespace TinyFlags;

public record Rule(IExpression Expression, IVariant Variant);

public record Flagset(
    IDictionary<string,Variant> Variants,
    IDictionary<string, IExpression> Expressions,
    IDictionary<string, IEnumerable<Rule>> Rules
)
{

    public static readonly Flagset Empty = new Flagset(
        new Dictionary<string, Variant>(),
        new Dictionary<string, IExpression>(),
        new Dictionary<string, IEnumerable<Rule>>()
    );
}


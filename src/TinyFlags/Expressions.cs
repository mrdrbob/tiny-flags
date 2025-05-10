namespace TinyFlags;

public interface IExpression;

public interface ILiteralExpression : IExpression;

public interface IBinaryExpression : IExpression
{
    IExpression Left { get; }
    IExpression Right { get; }
}

/// <summary>
/// Retrives a value from context
/// </summary>
/// <param name="Key"></param>
public record ContextExpression(string Key) : IExpression;

/// <summary>
/// A string literal value
/// </summary>
/// <param name="Value"></param>
public record StringExpression(string Value) : ILiteralExpression;

/// <summary>
/// An integer value
/// </summary>
/// <param name="Value"></param>
public record IntegerExpression(int Value) : ILiteralExpression;

/// <summary>
/// A boolean value
/// </summary>
/// <param name="Value"></param>
public record BooleanExpression(bool Value) : ILiteralExpression
{
    public static readonly BooleanExpression True = new BooleanExpression(true);
    public static readonly BooleanExpression False = new BooleanExpression(false);
}

/// <summary>
/// Returns true if all expressions evaluate to the same value and type. False in all other cases.
/// </summary>
/// <param name="Expressions"></param>
public record EqExpression(IEnumerable<IExpression> Expressions) : IExpression;

/// <summary>
/// Returns true if all expressions evaluate to true. Otherwise falase.
/// </summary>
/// <param name="Expressions"></param>
public record AndExpression(IEnumerable<IExpression> Expressions) : IExpression;

/// <summary>
/// Returns true if any expression evaluates to true. Otherwise falase.
/// </summary>
/// <param name="Expressions"></param>
public record OrExpression(IEnumerable<IExpression> Expressions) : IExpression;

/// <summary>
/// Performs modulus on the resolved `left` and `right` values. If either value is not an integer, returns 0.
/// </summary>
/// <param name="Left"></param>
/// <param name="Right"></param>
public record ModExpression(IExpression Left, IExpression Right) : IBinaryExpression;

/// <summary>
/// Returns true if the `left` expression evaluates to an integer larger than `right`. Returns false if either value is not an integer.
/// </summary>
/// <param name="Left"></param>
/// <param name="Right"></param>
public record GreaterThanExpression(IExpression Left, IExpression Right) : IBinaryExpression;

/// <summary>
/// Returns true if the `left` expression evaluates to an integer smaller than `right`. Returns false if either value is not an integer.
/// </summary>
/// <param name="Left"></param>
/// <param name="Right"></param>
public record LessThanExpression(IExpression Left, IExpression Right) : IBinaryExpression;

/// <summary>
/// Negates the evaluated integer. For non-integers, returns 0.
/// </summary>
/// <param name="Expression"></param>
public record NegateExpression(IExpression Expression) : IExpression;

/// <summary>
/// Returns `false` if the expression evaluates to `true`. Return `false` in all other cases.
/// </summary>
/// <param name="Expression"></param>
public record NotExpression(IExpression Expression) : IExpression;

/// <summary>
/// Returns sum of resolved integer expressions. Non-integer values count as 0.
/// </summary>
/// <param name="Expression"></param>
public record SumExpression(IEnumerable<IExpression> Expressions) : IExpression;

/// <summary>
/// Concats all string values into a single string value. Non-string values are considered empty strings.
/// </summary>
/// <param name="Expressions"></param>
public record ConcatExpression(IEnumerable<IExpression> Expressions) : IExpression;

/// <summary>
/// References a global expression and returns its evaluation
/// </summary>
/// <param name="Key"></param>
public record ExpressionReference(string Key) : IExpression;


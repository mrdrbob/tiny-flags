namespace TinyFlags;

public interface IExpressionResult
{
    bool Matches(IExpressionResult result);
}

public record StringExpressionResult(string Value) : IExpressionResult
{
    public bool Matches(IExpressionResult result)
    {
        return result switch
        {
            StringExpressionResult str => string.Compare(str.Value, Value, true) == 0,
            _ => false
        };
    }
}

public record IntegerExpressionResult(int Value) : IExpressionResult
{
    public bool Matches(IExpressionResult result)
    {
        return result switch
        {
            IntegerExpressionResult expr => expr.Value == Value,
            _ => false
        };
    }

    public static readonly IntegerExpressionResult Zero = new IntegerExpressionResult(0);
}

public record BooleanExpressionResult(bool Value) : IExpressionResult
{
    public static readonly BooleanExpressionResult True = new BooleanExpressionResult(true);
    public static readonly BooleanExpressionResult False = new BooleanExpressionResult(false);

    public bool Matches(IExpressionResult result)
    {
        return result switch
        {
            BooleanExpressionResult expr => expr.Value == Value,
            _ => false
        };
    }
}

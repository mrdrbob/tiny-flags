namespace TinyFlags;

public record Context(Context? ParentContext, IDictionary<string, ILiteralExpression> Data)
{
    public ILiteralExpression Resolve(string key)
    {
        if (Data.TryGetValue(key, out var value))
            return value;

        if (ParentContext is null)
            return BooleanExpression.False;

        return ParentContext.Resolve(key);
    }

    public Context Set(string key, string value)
    {
        Data[key] = new StringExpression(value);
        return this;
    }

    public Context Set(string key, int value)
    {
        Data[key] = new IntegerExpression(value);
        return this;
    }

    public Context Set(string key, bool value)
    {
        Data[key] = new BooleanExpression(value);
        return this;
    }

    public Context ChildContext()
    {
        return new Context(this, new Dictionary<string, ILiteralExpression>());
    }

    public static Context Create() => new Context(null, new Dictionary<string, ILiteralExpression>());

    public ExpressionResolver CreateResolver() => ExpressionResolver.Create(this);

    public ExpressionResolver CreateResolver(IDictionary<string, IExpression> expressionLibrary) => new ExpressionResolver(this, expressionLibrary);
}

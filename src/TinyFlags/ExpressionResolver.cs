
namespace TinyFlags;

public class ExpressionResolver(Context context, IDictionary<string, IExpression> expressionLibrary)
{
    public static ExpressionResolver Create(Context context)
    {
        return new ExpressionResolver(context, new Dictionary<string, IExpression>());
    }


    public bool IsTrue(IExpressionResult result)
    {
        return result switch
        {
            BooleanExpressionResult boolean => boolean.Value,
            _ => false
        };
    }

    public IExpressionResult Evaluate(IExpression expression)
    {
        return expression switch
        {
            // Context
            ContextExpression ctx => Evaluate(context.Resolve(ctx.Key)),

            // Expression reference
            ExpressionReference exp => ResolveExpressionReference(exp),

            // Literals
            StringExpression str => new StringExpressionResult(str.Value),
            BooleanExpression boolean => new BooleanExpressionResult(boolean.Value),
            IntegerExpression integer => new IntegerExpressionResult(integer.Value),

            // And/or
            AndExpression and => and.Expressions.Select(Evaluate).All(IsTrue) ? BooleanExpressionResult.True : BooleanExpressionResult.False,
            OrExpression or => or.Expressions.Select(Evaluate).Any(IsTrue) ? BooleanExpressionResult.True : BooleanExpressionResult.False,

            // Binary
            LessThanExpression lt => EvaluateArithmetic(lt, BooleanExpressionResult.False, (l, r) => new BooleanExpressionResult(l < r)),
            GreaterThanExpression gt => EvaluateArithmetic(gt, BooleanExpressionResult.False, (l, r) => new BooleanExpressionResult(l > r)),
            ModExpression mod => EvaluateArithmetic(mod, IntegerExpressionResult.Zero, (l, r) => new IntegerExpressionResult(l % r)),

            // Oddballs
            NotExpression not => IsTrue(Evaluate(not.Expression)) ? BooleanExpressionResult.False : BooleanExpressionResult.True,
            EqExpression eq => IsEq(eq.Expressions),
            NegateExpression neg => Negate(neg),
            SumExpression sum => Sum(sum),
            ConcatExpression con => Concat(con),

            // Unmatched
            _ => BooleanExpressionResult.False
        };
    }

    private IExpressionResult ResolveExpressionReference(ExpressionReference expressionReference)
    {
        if (!expressionLibrary.TryGetValue(expressionReference.Key, out var expression))
            return BooleanExpressionResult.False;

        return Evaluate(expression);
    }

    private IExpressionResult IsEq(IEnumerable<IExpression> expressions)
    {
        if (!expressions.Any())
            return BooleanExpressionResult.True;

        var first = Evaluate(expressions.First());
        bool allSame = expressions.Skip(1).All(x => Evaluate(x).Matches(first));
        return new BooleanExpressionResult(allSame);
    }

    private IExpressionResult EvaluateArithmetic(
        IBinaryExpression expression,
        IExpressionResult defaultResult,
        Func<int, int, IExpressionResult> convert)
    {
        var leftValue = Evaluate(expression.Left);
        var rightValue = Evaluate(expression.Right);

        return leftValue switch
        {
            IntegerExpressionResult leftResult => rightValue switch
            {
                IntegerExpressionResult rightResult => convert(leftResult.Value, rightResult.Value),
                _ => defaultResult
            },
            _ => defaultResult
        };
    }

    private IntegerExpressionResult Negate(NegateExpression negate)
    {
        var value = Evaluate(negate.Expression);
        return value switch
        {
            IntegerExpressionResult expr => new IntegerExpressionResult(-expr.Value),
            _ => IntegerExpressionResult.Zero
        };
    }

    IExpressionResult Sum(SumExpression expression)
    {
        var total = expression.Expressions.Aggregate(0, (acc, expr) =>
        {
            var value = Evaluate(expr);
            return value switch
            {
                IntegerExpressionResult integer => acc + integer.Value,
                _ => acc
            };
        });

        return new IntegerExpressionResult(total);
    }

    IExpressionResult Concat(ConcatExpression expression)
    {
        var allStrings = expression.Expressions.Select(x =>
        {
            var value = Evaluate(x);
            return value switch
            {
                StringExpressionResult str => str.Value,
                _ => ""
            };
        });

        var fullString = string.Join("", allStrings);
        return new StringExpressionResult(fullString);
    }

}

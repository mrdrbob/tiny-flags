namespace TinyFlags;

public class FlagsService(IFlagsetResolver flagsetResolver)
{
    public FlagsResolver WithContext(Context context)
        => new FlagsResolver(flagsetResolver.GetFlagset(), context);


    public class FlagsResolver
    {
        private Dictionary<string, Variant?> _cache = new Dictionary<string, Variant?>();
        private readonly Flagset flags;
        private readonly ExpressionResolver expressionResolver;

        public FlagsResolver(Flagset flags, Context context)
        {
            this.flags = flags;
            this.expressionResolver = new ExpressionResolver(context, flags.Expressions);
        }

        private Variant? ResolveVariant(IVariant variantReference)
        {
            return variantReference switch
            {
                InlineVariant inline => inline.Variant,
                VariantReference reference => flags.Variants.TryGetValue(reference.Name, out var variant) ? variant : null,
                _ => null
            };
        }

        public Variant? GetVariant(string name)
        {
            if (_cache.TryGetValue(name, out var cachedVariant))
                return cachedVariant;

            if (!this.flags.Rules.TryGetValue(name, out var rules))
                return null;

            foreach (var rule in rules)
            {
                var result = this.expressionResolver.Evaluate(rule.Expression);
                if (this.expressionResolver.IsTrue(result))
                {
                    var variant = ResolveVariant(rule.Variant);
                    _cache[name] = variant;
                    return variant;
                }
            }

            return null;
        }

        public string? Get(string rule, string variable, string? defaultValue)
        {
            return ResolveVariable(rule, variable, defaultValue, ex => ex switch
            {
                StringExpressionResult res => res.Value,
                _ => defaultValue
            });
        }

        public bool? Get(string rule, string variable, bool? defaultValue)
        {
            return ResolveVariable(rule, variable, defaultValue, ex => ex switch
            {
                BooleanExpressionResult res => res.Value,
                _ => defaultValue
            });
        }

        public int? Get(string rule, string variable, int? defaultValue)
        {
            return ResolveVariable(rule, variable, defaultValue, ex => ex switch
            {
                IntegerExpressionResult res => res.Value,
                _ => defaultValue
            });
        }

        private T? ResolveVariable<T>(string rule, string variable, T? defaultValue, Func<IExpressionResult, T> convert)
        {
            var variant = GetVariant(rule);
            if (variant is null)
                return defaultValue;

            if (!variant.Values.TryGetValue(variable, out var expression))
                return defaultValue;

            var resolvedExpression = expressionResolver.Evaluate(expression);
            if (resolvedExpression is null)
                return defaultValue;

            return convert(resolvedExpression);
        }

    }
}

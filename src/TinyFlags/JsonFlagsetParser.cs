using System.Text.Json;
using System.Text.Json.Nodes;

namespace TinyFlags;

public class JsonFlagsetParser : IFlagsetParser
{
    public Flagset Parse(string value)
    {
        var json = JsonSerializer.Deserialize<JsonObject>(value);
        if (json is null)
            return Flagset.Empty;

        var variants = ParseDictionary(json["variants"] as JsonObject, (node) =>
        {
            var expressions = ParseDictionary(node as JsonObject, (subNode) => ParseExpression(subNode as JsonObject));
            return new Variant(expressions);
        });

        var expressions = ParseDictionary(json["expressions"] as JsonObject, (node) =>
        {
            return ParseExpression(node as JsonObject);
        });

        var rules = ParseDictionary(json["rules"] as JsonObject, node =>
        {
            var array = node as JsonArray;
            if (array is null)
                return Enumerable.Empty<Rule>();

            var rules = new List<Rule>();
            foreach (var item in array)
            {
                var rule = ParseRule(item as JsonObject);
                if (rule is not null)
                    rules.Add(rule);
            }

            return rules;
        });

        return new Flagset(variants, expressions, rules);
    }

    private Rule? ParseRule(JsonObject? json)
    {
        if (json is null)
            return null;

        var when = ParseExpression(json["when"] as JsonObject);
        var then = ParseVariantReference(json["then"] as JsonObject);
        return new Rule(when, then);
    }
    #region Rules

    #endregion

    #region Variants
    private IDictionary<string, T> ParseDictionary<T>(JsonObject? json, Func<JsonNode?, T> convert)
    {
        var dictionary = new Dictionary<string, T>();
        if (json is null)
            return dictionary;

        foreach (var kvp in json)
        {
            var value = convert(kvp.Value);
            dictionary[kvp.Key] = value;
        }

        return dictionary;
    }

    private IVariant ParseVariantReference(JsonObject? json)
    {
        if (json is null)
            return new InlineVariant(Variant.Empty);

        return ParseLiteral<string, IVariant>(json, "variant", x => new VariantReference(x))
            ?? ParseInlineVariat(json)
            ?? new InlineVariant(Variant.Empty);
    }

    private IVariant? ParseInlineVariat(JsonObject json)
    {
        var node = json["inline-variant"] as JsonObject;
        if (node is null)
            return null;

        var expressions = ParseDictionary(node as JsonObject, (subNode) => ParseExpression(subNode as JsonObject));
        return new InlineVariant(new Variant(expressions));
    }
    #endregion

    #region Expressions
    private IExpression ParseExpression(JsonObject? json)
    {
        if (json is null)
            return BooleanExpression.False;

        return ParseLiteral<string, IExpression>(json, "context", c => new ContextExpression(c))
            ?? ParseLiteral<string, IExpression>(json, "expr", c => new ExpressionReference(c))
            ?? ParseArrayExpression(json, "eq", c => new EqExpression(c))
            ?? ParseArrayExpression(json, "and", c => new AndExpression(c))
            ?? ParseArrayExpression(json, "or", c => new OrExpression(c))
            ?? ParseArrayExpression(json, "sum", c => new SumExpression(c))
            ?? ParseArrayExpression(json, "concat", c => new ConcatExpression(c))
            ?? ParseBinaryExpression(json, "gt", (l, r) => new GreaterThanExpression(l, r))
            ?? ParseBinaryExpression(json, "lt", (l, r) => new LessThanExpression(l, r))
            ?? ParseBinaryExpression(json, "mod", (l, r) => new ModExpression(l, r))
            ?? ParseUnaryExpression(json, "not", c => new NotExpression(c))
            ?? ParseUnaryExpression(json, "negate", c => new NegateExpression(c))
            ?? ParseLiteralExpression(json);
    }

    private ILiteralExpression ParseLiteralExpression(JsonObject? json)
    {
        if (json is null)
            return BooleanExpression.False;

        return ParseLiteral<string, ILiteralExpression>(json, "string", c => new StringExpression(c))
            ?? ParseLiteral<int, ILiteralExpression>(json, "integer", c => new IntegerExpression(c))
            ?? ParseLiteral<bool, ILiteralExpression>(json, "boolean", c => new BooleanExpression(c))
            ?? BooleanExpression.False;
    }

    private TExp? ParseLiteral<TLit, TExp>(JsonObject json, string name, Func<TLit, TExp> convert)
    {
        var node = json[name];
        if (node is null)
            return default(TExp);
        var key = node.GetValue<TLit>();
        if (key is null)
            return default(TExp);
        return convert(key);
    }

    private IExpression? ParseArrayExpression(JsonObject json, string name, Func<IEnumerable<IExpression>, IExpression> convert)
    {
        var node = json[name] as JsonArray;
        if (node is null)
            return null;

        var items = ParseExpressionArray(node)
            .ToList();

        return convert(items);
    }

    private IExpression? ParseBinaryExpression(JsonObject json, string name, Func<IExpression, IExpression, IExpression> convert)
    {
        var node = json[name] as JsonObject;
        if (node is null)
            return null;

        var left = ParseExpression(node["left"] as JsonObject);
        var right = ParseExpression(node["right"] as JsonObject);
        return convert(left, right);
    }

    private IEnumerable<IExpression> ParseExpressionArray(JsonArray json)
    {
        foreach (var item in json)
        {
            if (item is null)
                continue;

            var parsed = ParseExpression(item.AsObject());
            yield return parsed;
        }
    }

    private IExpression? ParseUnaryExpression(JsonObject json, string name, Func<IExpression, IExpression> convert)
    {
        var node = json[name] as JsonObject;
        if (node is null)
            return null;

        var expression = ParseExpression(node);
        if (expression is null)
            return null;

        return convert(expression);
    }
    #endregion
}

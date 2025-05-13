# TinyFlags

The tiniest little C# Feature Flag library ever. It really emphasizes the "minimal" in minimally viable.

**Note:** This is still beta software and has not been battle tested in production. Use at your own risk.

## How it works

TinyFlags works by polling for a JSON file at a URL at roughly a known frequency (say every few minutes or so), and then using the data in the JSON file to make decisions based on developer-provided context.

## Example

At its core, TinyFlags is a tiny evaluation engine that is remotely configured via JSON to make decisions, based on context and rules. The rules are defined in JSON. Rules are a set of expressions that are evaluated in order. The first expression that returns `true` defines a variant, which is effectively a dictionary of values that become the answer to the decision.

For example, let's say we want to define 3 possible log levels, and choose one at run time:

* Log level `1` for users on prod who are authenticated.
* Log level `0` for users are authenticated in all other environments.
* Log level `-1` for all other cases.

Defined as JSON, that might look like this:

```json
{
  "variants": {
    "log_level_high": {
      "level": { "integer": 1 }
    },
    "log_level_medium": {
      "level": { "integer": 0 }
    }
  },
  "expressions": {
    "is_on_prod": { "eq": [
      { "context": "environment" },
      { "string": "prod" }
    ] },
    "is_authenticated": { "eq": [
      { "context": "is_authenticated" },
      { "boolean": true }
    ] }
  },
  "rules": {
    "log_level": [
      {
        "when": { "and": [
          { "expr": "is_on_prod" },
          { "expr": "is_authenticated" }
        ] },
        "then": { "variant": "log_level_high" }
      },
      {
        "when": { "expr": "is_authenticated" },
        "then": { "variant": "log_level_medium" }
      },
      {
        "when": { "boolean": true },
        "then": { 
          "inline-variant": {
            "level": { "integer": -1 }
          }
        }
      }
    ]
  }
}
```

In this JSON, the following top level nodes:

* `variants` - Optional - Allows you to define variants that can be referenced in rules. Otherwise, variants can also be defined in-line.
* `expressions` - Optional - Allows you to define a dictionary of expressions that can be referenced in rules. Useful for expressions that get repeated, or naming them for clarity.
* `rules` - Required - Defines a mapping of expressions to variants. The first rule to match wins.

Next you would host this JSON file somewhere the webserver can get to it. It could be as simple as NGinx, Caddy, an S3 bucket, etc. For the below example, let's say it's hosted at `https://www.example.com/example-ruleset.json`.

To make decisions, you need a `FlagsService`. Typically you would set this up within your Dependency Injection service of choice, but here's a manual example:

```csharp
    // Retrieve Flags data from the Web.
    // Note that `IFlagsetSource` is the slow, uncached source
    // of Flagset data.
    var httpSourceSettings = new HttpFlagsetSourceSettings
    {
        Url = "https://www.example.com/example-ruleset.json"
    };
    var jsonParser = new JsonFlagsetParser();
    var httpSource = new HttpFlagsetSource(
        new OptionsWrapper<HttpFlagsetSourceSettings>(httpSourceSettings), 
        jsonParser
    );

    // The `IFlagsetResolver` is the quick, cached, "safe" source of
    // Flagset data. The CachedFlagsetResolver will return empty Flagset
    // data while fetching the first batch of data in the background. Once the 
    // Flagset data expries, it will fetch new data in the background while 
    // returning stale data. The goal here is that `IFlagsetResolver` is always 
    // fast and never blocks, at the sacrifice of potentially being out of date
    // or empty.
    var nullLogger = new NullLogger<CachedFlagsetResolver>();
    var resolverSettings = new OptionsWrapper<CachedFlagsetResolverSettings>(new CachedFlagsetResolverSettings
    {
        // If the JSON fails to load, wait 500ms before trying again.
        RetyDelayMilliseconds = 500,

        // Keep data for ~2 minutes. After that, attempt to get fresh data
        TimeToLive = TimeSpan.FromMinutes(2)
    });
    var resolver = new CachedFlagsetResolver(resolverSettings, httpSource, nullLogger);

    var flagsService = new FlagsService(resolver);
```

Then use a combination of `Context` and `FlagsService` to make decisions based on the rules configured.

```csharp
    // Contexts can be nested, you may have a shared "global" context for a given environment
    // such as below. This can be used for data that is identical across all decisions, or setting
    // defaults.
    var serverContext = Context.Create()
        .Set("environment", environment)
        .Set("is_authenticated", false);

    // Then a nested "child" context for each user. Child context values override parent context
    // values.
    var context = serverContext
        .ChildContext()
        .Set("is_authenticated", authenticated);
        .Set("user_id", 1001)

    // Assuming you'll get your flag service from some kind of dependency injection
    var flags = ServiceLocator.GetInstance<FlagsService>();

    // Create a solver for current context, which includes both the user and the environment.
    var solver = flags.WithContext(context);

    // Calculate the "level" variable of the "log_level" rule.
    // If the rule isn't found, or if no variant is matched, use 2 as a fallback.
    var logLevel = solver.Get("log_level", "level", 2);
```

## Security

Of course, when hosting a JSON file publicly, there is nothing preventing any random person from guessing the URL and seeing the values. I strongly recommend: **do not put any sensitive data in these JSON files**.

That said, if you need to secure the connection, I'd recommend overriding or replacing the `HttpFlagsetSource` class. For example, here's an implementation using a simple `X-API-Key` header:

```csharp
public class SecureFlagsetSourceSettings : HttpFlagsetSourceSettings
{
    public string? ApiKey { get; set; }
}

public class SecureFlagsetSource : HttpFlagsetSource
{
    public SecureFlagsetSource(IOptions<SecureFlagsetSourceSettings> settings, IFlagsetParser parser) 
        : base(settings, parser)
    {
        if (!string.IsNullOrEmpty(settings.Value.ApiKey))
        {
            this.httpClient.DefaultRequestHeaders.Add("X-API-Key", settings.Value.ApiKey);
        }
    }
}
```

If you need something more robust, implement `IFlagsetSource`. There is only method:

```csharp
public interface IFlagsetSource
{
    Task<Flagset> LoadFlagsetAsync();
}
```

## Expressions

TinyFlags has a rudimentary expression engine. It currently supports 3 primitive types:

* Strings
* Integers
* Booleans

Type mismatches between these primitives typically result in `false`. An integer is never greater than a string, for example. Summing integers and strings will ignore the strings. Concatted integers will be treated as empty strings. The expression engine falls back on `false`, `0`, or `empty` instead of throwing exceptions.

The following expressions are supported:

### Literals

* String literal: `{ "string": "value" }` - Returns a string value.
* Integer literal: `{ "integer": 100 }` - Returns an integer value.
* Boolean literal: `{ "boolean": false }` - Return a boolean value.

### Lookup

* Context Lookup: `{ "context": "is_logged_in" }` - Looks up a value from context based on key (`is_logged_in` in this example). Return type depends on the context type.
* Expression Lookup: `{ "expr": "is_prod" }` - Looks up and evaluates an expression from the `expressions` dictionary. If no expression is found, returns `false`.

### Aggregate

* Eq: `{ "eq": [ ...expressions ] }` - Returns `true` if all expressions in the array resolve to the same value and type. Otherwise, `false`.
* And: `{ "and": [ ] }` - Returns `true` if all expressions in the array return `true`. Otherwise, `false`.
* Or: `{ "or": [ ] }` - Returns `true` if any expression in the array return `true`. Otherwise, `false`.
* Concat: `{ "concat": [ ... ] }` - Joins all the string values together into a single string. Any non-string types will be ignored.
* Sum: `{ "sum": [ ... ] }` - Sums all the integer values together. Any non-integer types will be ignored.


### Arithmetic

* Mod: `{ "mod": { "left": ..., "right": ... } }` - Performs modulus on the resolved `left` and `right` values. Only works for integer values, returns `false` otherwise.
* Greater Than: `{ "gt": { "left": ..., "right": ... } }` - Returns `true` if the resolved `left` value is greater than the resolved `right` value. Only works for integer values, returns `false` otherwise.
* Less Than: `{ "lt": { "left": ..., "right": ... } }` - Returns `true` if the resolved `left` value is less than the resolved `right` value. Only works for integer values, returns `false` otherwise.

### Unary

* Negate: `{ "negate": ... }` - Negates the resolved value. Only works for integer values. Non-integer values are considered `0`.
* Not: `{ "not": ... }` - Returns `false` if the resolved value is `true`. Returns `true` for all other values (including non-boolean values).

## Who is this for?

Mostly for me, but it may also be useful for other devs who need a way to remotely manage some very basic decision making expressions, but don't want the hassle of an entire Flags product, or doing deployments to live sites to make small configuration changes.

## What it does NOT do

A whole slew of things are completely, 100% unsupported:

* Experimentation - There is no support for A/B testing, tracking outcomes, stats, analysis, etc.
* Rollout - There is nothing here that would apply a consistent decision to a specific percentage of traffic. You might be able to dummy something up bucketing traffic via context, but out-of-the-box support is poor at best.
* Rules Management UI - It's just a JSON file that you edit with your favorite editor.
* Web API - I suppose you could build an API to return the JSON, but the intention is to host it statically.

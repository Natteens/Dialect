# Custom flow and value nodes

Put runtime types in a player assembly referencing `Dialect`. Put authoring types in an Editor assembly referencing `Dialect.Editor`.

A custom flow node derives `DialectNode`, implements `IDialectNodeCompiler`, defines ports with the protected helpers, compiles explicit targets, and validates through the supplied context. A custom value node derives `DialectValueNode`, implements `IDialectValueNodeCompiler`, and returns a serializable `DialectValueResolver`.

```csharp
[Serializable, Node("My Game", null, "Player Name"), UseWithGraph(typeof(DialectGraph))]
public sealed class PlayerNameNode : DialectValueNode, IDialectValueNodeCompiler
{
    protected override void OnDefinePorts(IPortDefinitionContext ports) =>
        AddValueOutput<DialectText>(ports);

    public Type ValueType => typeof(string);
    public DialectValueResolver CompileValue(DialectValueNodeCompilationContext context) =>
        new PlayerNameResolver();
    public void Validate(DialectValueNodeValidationContext context) { }
}

[Serializable]
public sealed class PlayerNameResolver : DialectValueResolver
{
    public override Type ValueType => typeof(string);
    public override object Resolve(DialectExecutionContext context) =>
        context.TryGetUserData<PlayerState>(out var state) ? state.Name : string.Empty;
}
```

Compilation contexts expose `Graph`, `Node`, `Read<T>`, `ReadText`, `ReadRequiredText`, `CompileValue`, `Target`, and `Error` where applicable. Validation contexts expose equivalent read/connection helpers, `Mode`/`IsStrict`, and GraphLogger-backed Error/Warning methods. Keep incomplete-value requirements behind `IsStrict`; reserve live errors for broken structural contracts. External tests compile and execute both SDK paths without importer changes.

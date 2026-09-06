# Custom nodes

Place authoring nodes in an Editor assembly that references `Dialect.Editor`; place runtime nodes in a runtime assembly that references `Dialect`.

```csharp
[Serializable, Node("My Game", null, "Wait")]
public sealed class WaitNode : DialectNode, IDialectNodeCompiler
{
    protected override void OnDefinePorts(IPortDefinitionContext ports)
    {
        AddFlowInput(ports);
        AddFlowOutput(ports);
    }

    public RuntimeNode Compile(DialectNodeCompilationContext context) =>
        new WaitRuntimeNode(context.Target(FlowOutput));

    public void Validate(DialectNodeValidationContext context) { }
    public bool WaitsForInput => true;
}
```

Runtime nodes are serializable managed references and return a `DialectExecutionResult`. Use `context.Target(portName)` for explicit flow semantics and `context.Read<T>(portName)` for values. Add validation through the supplied context. The importer discovers this interface across assemblies.

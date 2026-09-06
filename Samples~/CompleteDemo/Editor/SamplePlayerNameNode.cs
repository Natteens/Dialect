using System;
using Dialect.Blackboards;
using Dialect.Editor;
using Dialect.Editor.Nodes;
using Dialect.Values;
using Unity.GraphToolkit.Editor;

namespace Dialect.Samples.Complete.Editor
{
    [Serializable, Node("Dialect Samples", null, "Player Name"), UseWithGraph(typeof(DialectGraph))]
    public sealed class SamplePlayerNameNode : DialectValueNode, IDialectValueNodeCompiler
    {
        protected override void OnDefinePorts(IPortDefinitionContext context) => AddValueOutput<DialectText>(context);
        public Type ValueType => typeof(string);
        public DialectValueResolver CompileValue(DialectValueNodeCompilationContext context) => new SamplePlayerNameResolver();
        public void Validate(DialectValueNodeValidationContext context) { }
    }
}

using System;
using Dialect.Core;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable]
    [Node("Dialogue/Flow", null, "End")]
    public sealed class EndNode : DialectNode, IDialectNodeCompiler
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.75f, .25f, .3f);
            Subtitle = "Exit";
            AddFlowInput(context);
        }

        public RuntimeNode Compile(DialectNodeCompilationContext context) => new EndRuntimeNode();
        public void Validate(DialectNodeValidationContext context) { }
        public bool WaitsForInput => true;
    }
}

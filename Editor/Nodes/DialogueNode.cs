using System;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable, Node("Dialogue", null, "Dialogue")]
    public sealed class DialogueNode : DialectNode, IDialectNodeCompiler
    {
        const string SpeakerPort = "speaker";
        const string TextPort = "text";
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.25f, .48f, .78f);
            Subtitle = "Line";
            AddFlowInput(context);
            AddValueInput<DialectText>(context, SpeakerPort, "Speaker");
            AddValueInput<DialectText>(context, TextPort, "Line");
            AddFlowOutput(context);
        }
        public RuntimeNode Compile(DialectNodeCompilationContext context) => new DialogueRuntimeNode(
            context.Read<DialectText>(SpeakerPort), context.Read<DialectText>(TextPort), context.Target(FlowOutput));
        public void Validate(DialectNodeValidationContext context)
        { if (!context.IsConnected(FlowOutput)) context.Error("Dialogue must continue to another node."); }
        public bool WaitsForInput => true;
    }
}

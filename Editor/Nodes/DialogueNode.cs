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
        public const string SpeakerPort = "speaker";
        public const string TextPort = "text";
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
            context.ReadText(SpeakerPort), context.ReadText(TextPort), context.Target(FlowOutput));
        public void Validate(DialectNodeValidationContext context)
        {
            context.ValidateText(SpeakerPort, "Speaker");
            context.ValidateText(TextPort, "Line");
            if (!context.IsConnected(FlowOutput)) context.Error("Dialogue must continue to another node.");
        }
        public bool WaitsForInput => true;
    }
}

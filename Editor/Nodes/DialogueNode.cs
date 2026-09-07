using System;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable, Node("Dialogue", null, "Dialogue", "Packages/com.natteens.dialect/Editor/Styles/DialectNodes.uss")]
    public sealed class DialogueNode : DialectNode, IDialectNodeCompiler
    {
        public const string SpeakerPort = "speaker";
        public const string TextPort = "text";
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.25f, .48f, .78f);
            Subtitle = "Line";
            AddFlowInput(context);
            AddValueInput<DialectText>(context, SpeakerPort, "Speaker", "Optional speaker. Inline or Localized when unconnected; a wire overrides the default.");
            AddValueInput<DialectText>(context, TextPort, "Line", "Required line. Inline or Localized when unconnected; a wire overrides the default.");
            AddFlowOutput(context);
        }
        public RuntimeNode Compile(DialectNodeCompilationContext context) => new DialogueRuntimeNode(
            context.ReadText(SpeakerPort), context.ReadRequiredText(TextPort, "Line"), context.Target(FlowOutput));
        public void Validate(DialectNodeValidationContext context)
        {
            if (!context.IsStrict) return;
            context.ValidateText(TextPort, "Line");
            if (!context.IsConnected(FlowOutput)) context.Error("Dialogue must continue to another node.");
        }
        public bool WaitsForInput => true;
    }
}

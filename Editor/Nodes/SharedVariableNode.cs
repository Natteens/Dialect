using System;
using Dialect.Blackboards;
using Dialect.Values;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable, Node("Dialogue/Values", null, "Shared Variable")]
    public sealed class SharedVariableNode : DialectValueNode, IDialectValueNodeCompiler
    {
        public const string ReferencePort = "reference";

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.42f, .55f, .72f);
            Subtitle = "Shared blackboard";
            Tooltip = "Reads a String or LocalizedString from a shared Dialect Blackboard.";
            AddValueInput<DialectVariableReference>(context, ReferencePort, "Variable");
            AddValueOutput<DialectText>(context);
        }

        public Type ValueType => typeof(DialectText);

        public DialectValueResolver CompileValue(DialectValueNodeCompilationContext context)
        {
            var reference = context.Read<DialectVariableReference>(ReferencePort);
            if (!reference.IsValid || !reference.Blackboard.TryGetDefinition(reference.VariableId, out var definition))
            { context.Error("Select a valid shared variable."); return null; }
            if (!context.Graph.IsBlackboardLinked(reference.Blackboard))
            { context.Error("Link the referenced blackboard to this graph."); return null; }
            if (definition.Type is not (DialectValueType.String or DialectValueType.LocalizedString))
            { context.Error("Dialogue text requires a String or LocalizedString variable."); return null; }
            return new DialectVariableValueResolver(definition.Id, definition.Type);
        }

        public void Validate(DialectValueNodeValidationContext context)
        {
            var reference = context.Read<DialectVariableReference>(ReferencePort);
            if (!reference.IsValid) context.Error("Select a valid shared variable.");
            else if (!context.Graph.IsBlackboardLinked(reference.Blackboard))
                context.Error("Link the referenced blackboard to this graph.");
            else if (reference.Blackboard.TryGetDefinition(reference.VariableId, out var definition) &&
                     definition.Type is not (DialectValueType.String or DialectValueType.LocalizedString))
                context.Error("Shared text variables must be String or LocalizedString.");
        }
    }
}

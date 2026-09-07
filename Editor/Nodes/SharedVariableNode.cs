using System;
using Dialect.Blackboards;
using Dialect.Values;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable, Node("Values", null, "Shared Variable", "Packages/com.natteens.dialect/Editor/Styles/DialectNodes.uss")]
    public sealed class SharedVariableNode : DialectValueNode, IDialectValueNodeCompiler
    {
        public const string ReferencePort = "reference";

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.42f, .55f, .72f);
            Subtitle = "Shared blackboard";
            Tooltip = "Reads a typed session value from a shared Dialect Blackboard.";
            AddValueInput<DialectVariableReference>(context, ReferencePort, "Variable");
            AddValueOutput<DialectText>(context);
        }

        public Type ValueType => TryGetResolvedType(out var type) ? type : typeof(DialectText);

        internal bool TryGetResolvedType(out Type type)
        {
            var port = GetInputPortByName(ReferencePort);
            if (port != null && port.TryGetValue(out DialectVariableReference reference) && reference.IsValid &&
                reference.Blackboard.TryGetDefinition(reference.VariableId, out var definition))
            {
                type = DialectValueUtility.GetSystemType(definition.Type);
                return true;
            }
            type = typeof(DialectText);
            return false;
        }

        public DialectValueResolver CompileValue(DialectValueNodeCompilationContext context)
        {
            var reference = context.Read<DialectVariableReference>(ReferencePort);
            if (!reference.IsValid || !reference.Blackboard.TryGetDefinition(reference.VariableId, out var definition))
            { context.Error("Select a valid shared variable."); return null; }
            if (!context.Graph.IsBlackboardLinked(reference.Blackboard))
            { context.Error("Link the referenced blackboard to this graph."); return null; }
            return new DialectVariableValueResolver(definition.Id, definition.Type);
        }

        public void Validate(DialectValueNodeValidationContext context)
        {
            if (!context.IsStrict) return;
            var reference = context.Read<DialectVariableReference>(ReferencePort);
            if (!reference.IsValid) context.Error("Select a valid shared variable.");
            else if (!context.Graph.IsBlackboardLinked(reference.Blackboard))
                context.Error("Link the referenced blackboard to this graph.");
            else if (!reference.Blackboard.TryGetDefinition(reference.VariableId, out _))
                context.Error("The selected shared variable no longer exists.");
        }
    }
}

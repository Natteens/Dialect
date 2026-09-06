using System;
using System.Collections.Generic;

namespace Dialect.Core
{
    [Serializable]
    public readonly struct DialectLine
    {
        public DialectLine(string speaker, string text, string nodeId)
        {
            Speaker = speaker ?? string.Empty;
            Text = text ?? string.Empty;
            NodeId = nodeId ?? string.Empty;
        }

        public string Speaker { get; }
        public string Text { get; }
        public string NodeId { get; }
    }

    [Serializable]
    public readonly struct DialectChoice
    {
        public DialectChoice(string text, int targetNodeIndex)
        {
            Text = text ?? string.Empty;
            TargetNodeIndex = targetNodeIndex;
        }

        public string Text { get; }
        public int TargetNodeIndex { get; }
    }

    public sealed class DialectChoiceSet
    {
        public DialectChoiceSet(IReadOnlyList<DialectChoice> choices) => Choices = choices;
        public IReadOnlyList<DialectChoice> Choices { get; }
        public int Count => Choices?.Count ?? 0;
    }

    public readonly struct DialectValuePreview
    {
        public DialectValuePreview(string portId, string value)
        { PortId = portId ?? string.Empty; Value = value ?? string.Empty; }
        public string PortId { get; }
        public string Value { get; }
    }
}

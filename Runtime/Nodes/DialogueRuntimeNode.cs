using System;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Executors;
using Dialect.Values;
using UnityEngine;

namespace Dialect.Nodes
{
    [Serializable]
    public sealed class DialogueRuntimeNode : RuntimeNode
    {
        [SerializeField] DialectTextExpression speaker;
        [SerializeField] DialectTextExpression text;
        [SerializeField] int next = -1;

        public DialogueRuntimeNode(DialectText speaker, DialectText text, int next)
            : this(new DialectTextExpression(speaker), new DialectTextExpression(text), next) { }

        public DialogueRuntimeNode(DialectTextExpression speaker, DialectTextExpression text, int next)
        {
            this.speaker = speaker;
            this.text = text;
            this.next = next;
        }

        public override DialectExecutionResult Execute(DialectExecutionContext context)
        {
            Present(context);
            return DialectExecutionResult.WaitForAdvance(next);
        }

        public override bool RefreshPresentation(DialectExecutionContext context)
        {
            Present(context);
            return true;
        }

        void Present(DialectExecutionContext context) =>
            context.Director.PresentLine(new DialectLine(speaker.Resolve(context), text.Resolve(context), AuthoringId));
    }
}

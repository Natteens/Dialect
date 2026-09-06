using System;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Executors;
using UnityEngine;

namespace Dialect.Nodes
{
    [Serializable]
    public sealed class DialogueRuntimeNode : RuntimeNode
    {
        [SerializeField] DialectText speaker;
        [SerializeField] DialectText text;
        [SerializeField] int next = -1;

        public DialogueRuntimeNode(DialectText speaker, DialectText text, int next)
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

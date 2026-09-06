using System;
using System.Collections.Generic;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Executors;
using UnityEngine;

namespace Dialect.Nodes
{
    [Serializable]
    public struct DialectChoiceDefinition
    {
        [SerializeField] DialectText text;
        [SerializeField] int target;

        public DialectChoiceDefinition(DialectText text, int target)
        {
            this.text = text;
            this.target = target;
        }

        public DialectChoice Resolve(DialectExecutionContext context) => new(text.Resolve(context), target);
    }

    [Serializable]
    public sealed class ChoiceRuntimeNode : RuntimeNode
    {
        [SerializeField] List<DialectChoiceDefinition> choices = new();

        public ChoiceRuntimeNode(List<DialectChoiceDefinition> choices) => this.choices = choices;

        public override DialectExecutionResult Execute(DialectExecutionContext context)
        {
            Present(context);
            return DialectExecutionResult.AwaitChoice();
        }

        public override bool RefreshPresentation(DialectExecutionContext context)
        {
            Present(context);
            return true;
        }

        void Present(DialectExecutionContext context)
        {
            var resolved = new DialectChoice[choices.Count];
            for (var i = 0; i < choices.Count; i++) resolved[i] = choices[i].Resolve(context);
            context.Director.PresentChoices(new DialectChoiceSet(resolved));
        }
    }
}

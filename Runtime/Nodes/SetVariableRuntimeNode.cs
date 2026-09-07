using System;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Executors;
using Dialect.Values;
using UnityEngine;

namespace Dialect.Nodes
{
    [Serializable]
    public sealed class SetVariableRuntimeNode : RuntimeNode
    {
        [SerializeField] string variableId;
        [SerializeField] DialectValueType variableType;
        [SerializeField] DialectValueExpression value;
        [SerializeField] int next = -1;

        public SetVariableRuntimeNode(string variableId, DialectValueType variableType, DialectValueExpression value, int next)
        {
            this.variableId = variableId;
            this.variableType = variableType;
            this.value = value;
            this.next = next;
        }

        public override DialectExecutionResult Execute(DialectExecutionContext context)
        {
            var resolved = DialectValueUtility.Create(variableType, value.Resolve(context));
            if (!context.Variables.TrySet(variableId, resolved))
                throw new InvalidOperationException("Set Variable could not update its session variable.");
            return DialectExecutionResult.ContinueTo(next);
        }
    }
}

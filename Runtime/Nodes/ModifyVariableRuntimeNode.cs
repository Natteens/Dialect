using System;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Executors;
using Dialect.Values;
using UnityEngine;

namespace Dialect.Nodes
{
    public enum DialectNumericOperation { Add, Subtract, Multiply }

    [Serializable]
    public sealed class ModifyVariableRuntimeNode : RuntimeNode
    {
        [SerializeField] string variableId;
        [SerializeField] DialectValueType variableType;
        [SerializeField] DialectNumericOperation numericOperation;
        [SerializeField] DialectValueExpression operand;
        [SerializeField] int next = -1;

        public ModifyVariableRuntimeNode(string variableId, DialectValueType variableType,
            DialectNumericOperation numericOperation, DialectValueExpression operand, int next)
        {
            this.variableId = variableId;
            this.variableType = variableType;
            this.numericOperation = numericOperation;
            this.operand = operand;
            this.next = next;
        }

        public override DialectExecutionResult Execute(DialectExecutionContext context)
        {
            var current = VariableRuntimeUtility.Read(context, variableId, variableType).BoxedValue;
            object modified = variableType switch
            {
                DialectValueType.Integer when current is int value && operand.Resolve(context) is int amount =>
                    Apply(value, amount),
                DialectValueType.Float when current is float value && operand.Resolve(context) is float amount =>
                    Apply(value, amount),
                DialectValueType.Boolean when current is bool value => !value,
                DialectValueType.String when current is string value && operand.Resolve(context) is string suffix =>
                    value + suffix,
                _ => throw new InvalidOperationException("Modify Variable does not support the selected variable type or operand.")
            };
            VariableRuntimeUtility.Write(context, variableId, variableType, modified);
            return DialectExecutionResult.ContinueTo(next);
        }

        int Apply(int value, int amount) => numericOperation switch
        {
            DialectNumericOperation.Add => value + amount,
            DialectNumericOperation.Subtract => value - amount,
            DialectNumericOperation.Multiply => value * amount,
            _ => throw new InvalidOperationException("Modify Variable contains an invalid numeric operation.")
        };

        float Apply(float value, float amount) => numericOperation switch
        {
            DialectNumericOperation.Add => value + amount,
            DialectNumericOperation.Subtract => value - amount,
            DialectNumericOperation.Multiply => value * amount,
            _ => throw new InvalidOperationException("Modify Variable contains an invalid numeric operation.")
        };
    }
}

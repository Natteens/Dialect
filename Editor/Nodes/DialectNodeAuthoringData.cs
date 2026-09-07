using System;
using Dialect.Blackboards;
using Dialect.Nodes;
using Dialect.Values;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Dialect.Editor.Nodes
{
    [Serializable]
    public struct DialectPortCount
    {
        [SerializeField] int count;
        public DialectPortCount(int count) => this.count = count;
        public int Count => Math.Clamp(count, 1, 8);
    }

    [Serializable]
    public struct DialectCompareSettings
    {
        [SerializeField] DialectCompareType type;
        [SerializeField] DialectComparisonOperator comparisonOperator;

        public DialectCompareSettings(DialectCompareType type, DialectComparisonOperator comparisonOperator)
        {
            this.type = type;
            this.comparisonOperator = CompareValueResolver.Normalize(type, comparisonOperator);
        }

        public DialectCompareType Type => type;
        public DialectComparisonOperator Operator => CompareValueResolver.Normalize(type, comparisonOperator);
        internal DialectComparisonOperator RawOperator => comparisonOperator;
    }

    public enum DialectVariableScope { Local, Shared }

    [Serializable]
    public struct DialectVariableTarget
    {
        [SerializeField] DialectVariableScope scope;
        [SerializeField] string localVariableId;
        [SerializeField] DialectBlackboard blackboard;
        [SerializeField] string sharedVariableId;

        public DialectVariableScope Scope => scope;
        public string LocalVariableId => localVariableId;
        public DialectBlackboard Blackboard => blackboard;
        public string SharedVariableId => sharedVariableId;

        public static DialectVariableTarget Local(string id) => new()
        { scope = DialectVariableScope.Local, localVariableId = id ?? string.Empty };

        public static DialectVariableTarget Shared(DialectBlackboard board, string id) => new()
        { scope = DialectVariableScope.Shared, blackboard = board, sharedVariableId = id ?? string.Empty };

        public bool TryResolve(DialectGraph graph, out string id, out Type type, out DialectValueType valueType)
        {
            if (graph == null)
            {
                id = string.Empty;
                type = typeof(string);
                valueType = DialectValueType.String;
                return false;
            }
            if (scope == DialectVariableScope.Local)
            {
                foreach (var variable in graph.GetVariables())
                    if (variable != null && variable.ID.ToString() == localVariableId &&
                        DialectValueCompiler.TryCreateValue(variable.DataType, variable, out var localValue))
                    {
                        id = localVariableId;
                        type = variable.DataType;
                        valueType = localValue.Type;
                        return true;
                    }
            }
            else if (blackboard != null && graph.IsBlackboardLinked(blackboard) &&
                     blackboard.TryGetDefinition(sharedVariableId, out var definition))
            {
                id = definition.Id;
                type = DialectValueUtility.GetSystemType(definition.Type);
                valueType = definition.Type;
                return true;
            }

            id = string.Empty;
            type = typeof(string);
            valueType = DialectValueType.String;
            return false;
        }
    }

    [Serializable]
    public struct DialectModifySettings
    {
        [SerializeField] DialectVariableTarget target;
        [SerializeField] DialectNumericOperation numericOperation;

        public DialectModifySettings(DialectVariableTarget target,
            DialectNumericOperation numericOperation = DialectNumericOperation.Add)
        {
            this.target = target;
            this.numericOperation = numericOperation;
        }

        public DialectVariableTarget Target => target;
        public DialectNumericOperation NumericOperation => numericOperation;
    }
}

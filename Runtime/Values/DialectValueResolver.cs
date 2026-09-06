using System;
using Dialect.Blackboards;
using Dialect.Executors;
using UnityEngine;
using UnityEngine.Localization;

namespace Dialect.Values
{
    [Serializable]
    public abstract class DialectValueResolver
    {
        public abstract Type ValueType { get; }
        public abstract object Resolve(DialectExecutionContext context);
    }

    [Serializable]
    public sealed class DialectConstantValueResolver : DialectValueResolver
    {
        [SerializeReference] DialectValue value;

        public DialectConstantValueResolver(DialectValue value) => this.value = value?.Clone();
        public override Type ValueType => DialectValueUtility.GetSystemType(value?.Type ?? DialectValueType.String);
        public override object Resolve(DialectExecutionContext context) => value?.BoxedValue;
    }

    [Serializable]
    public sealed class DialectVariableValueResolver : DialectValueResolver
    {
        [SerializeField] string variableId;
        [SerializeField] DialectValueType valueType;

        public DialectVariableValueResolver(string variableId, DialectValueType valueType)
        { this.variableId = variableId; this.valueType = valueType; }

        public string VariableId => variableId;
        public override Type ValueType => DialectValueUtility.GetSystemType(valueType);
        public override object Resolve(DialectExecutionContext context) =>
            context.Variables.TryGetValue(variableId, out var value) ? value.BoxedValue : null;
    }

    [Serializable]
    public sealed class DialectAuthoredTextResolver : DialectValueResolver
    {
        [SerializeField] DialectText value;
        public DialectAuthoredTextResolver(DialectText value) => this.value = value;
        public override Type ValueType => typeof(DialectText);
        public override object Resolve(DialectExecutionContext context) => value;
    }

    public static class DialectValueUtility
    {
        public static Type GetSystemType(DialectValueType type) => type switch
        {
            DialectValueType.LocalizedString => typeof(LocalizedString),
            DialectValueType.Boolean => typeof(bool),
            DialectValueType.Integer => typeof(int),
            DialectValueType.Float => typeof(float),
            DialectValueType.Object => typeof(UnityEngine.Object),
            _ => typeof(string)
        };

        public static DialectValue Create(DialectValueType type, object value) => type switch
        {
            DialectValueType.LocalizedString => new DialectLocalizedStringValue(value as LocalizedString ?? new LocalizedString()),
            DialectValueType.Boolean => new DialectBoolValue(value is bool boolean && boolean),
            DialectValueType.Integer => new DialectIntValue(value is int integer ? integer : 0),
            DialectValueType.Float => new DialectFloatValue(value is float number ? number : 0f),
            DialectValueType.Object => new DialectObjectValue(value as UnityEngine.Object),
            _ => new DialectStringValue(value as string ?? string.Empty)
        };
    }
}

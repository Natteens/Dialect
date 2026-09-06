using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Dialect.Blackboards
{
    public enum DialectValueType { String, LocalizedString, Boolean, Integer, Float, Object }

    [Serializable]
    public abstract class DialectValue
    {
        public abstract DialectValueType Type { get; }
        public abstract object BoxedValue { get; }
        public abstract DialectValue Clone();
    }

    [Serializable]
    public sealed class DialectStringValue : DialectValue
    {
        [SerializeField] string value;
        public DialectStringValue() { }
        public DialectStringValue(string value) => this.value = value;
        public string Value => value;
        public override DialectValueType Type => DialectValueType.String;
        public override object BoxedValue => value;
        public override DialectValue Clone() => new DialectStringValue(value);
    }

    [Serializable]
    public sealed class DialectLocalizedStringValue : DialectValue
    {
        [SerializeField] LocalizedString value = new();
        public LocalizedString Value => value;
        public override DialectValueType Type => DialectValueType.LocalizedString;
        public override object BoxedValue => value;
        public override DialectValue Clone() => new DialectLocalizedStringValue { value = value };
    }

    [Serializable]
    public sealed class DialectBoolValue : DialectValue
    {
        [SerializeField] bool value;
        public DialectBoolValue() { }
        public DialectBoolValue(bool value) => this.value = value;
        public bool Value => value;
        public override DialectValueType Type => DialectValueType.Boolean;
        public override object BoxedValue => value;
        public override DialectValue Clone() => new DialectBoolValue(value);
    }

    [Serializable]
    public sealed class DialectIntValue : DialectValue
    {
        [SerializeField] int value;
        public DialectIntValue() { }
        public DialectIntValue(int value) => this.value = value;
        public int Value => value;
        public override DialectValueType Type => DialectValueType.Integer;
        public override object BoxedValue => value;
        public override DialectValue Clone() => new DialectIntValue(value);
    }

    [Serializable]
    public sealed class DialectFloatValue : DialectValue
    {
        [SerializeField] float value;
        public DialectFloatValue() { }
        public DialectFloatValue(float value) => this.value = value;
        public float Value => value;
        public override DialectValueType Type => DialectValueType.Float;
        public override object BoxedValue => value;
        public override DialectValue Clone() => new DialectFloatValue(value);
    }

    [Serializable]
    public sealed class DialectObjectValue : DialectValue
    {
        [SerializeField] UnityEngine.Object value;
        public UnityEngine.Object Value => value;
        public override DialectValueType Type => DialectValueType.Object;
        public override object BoxedValue => value;
        public override DialectValue Clone() => new DialectObjectValue { value = value };
    }
}

using System;
using Dialect.Blackboards;
using Dialect.Executors;
using UnityEngine;

namespace Dialect.Values
{
    [Serializable]
    public sealed class SelectValueResolver : DialectValueResolver
    {
        [SerializeField] DialectValueType valueType;
        [SerializeField] DialectValueExpression condition;
        [SerializeField] DialectValueExpression trueValue;
        [SerializeField] DialectValueExpression falseValue;

        public SelectValueResolver(DialectValueType valueType, DialectValueExpression condition,
            DialectValueExpression trueValue, DialectValueExpression falseValue)
        {
            this.valueType = valueType;
            this.condition = condition;
            this.trueValue = trueValue;
            this.falseValue = falseValue;
        }

        public override Type ValueType => DialectValueUtility.GetSystemType(valueType);

        public override object Resolve(DialectExecutionContext context)
        {
            if (condition.Resolve(context) is not bool selectTrue)
                throw new InvalidOperationException("Select Condition must resolve to Boolean.");
            var value = (selectTrue ? trueValue : falseValue).Resolve(context);
            if (value != null && !ValueType.IsInstanceOfType(value))
                throw new InvalidOperationException("Select input does not match its selected type.");
            return value;
        }
    }
}

using System;
using Dialect.Executors;
using UnityEngine;

namespace Dialect.Values
{
    public enum DialectCompareType { String, Boolean, Integer, Float, Object }
    public enum DialectComparisonOperator { Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual }

    [Serializable]
    public sealed class CompareValueResolver : DialectValueResolver
    {
        [SerializeField] DialectCompareType comparisonType;
        [SerializeField] DialectComparisonOperator comparisonOperator;
        [SerializeField] DialectValueExpression left;
        [SerializeField] DialectValueExpression right;

        public CompareValueResolver(DialectCompareType comparisonType, DialectComparisonOperator comparisonOperator,
            DialectValueExpression left, DialectValueExpression right)
        {
            this.comparisonType = comparisonType;
            this.comparisonOperator = comparisonOperator;
            this.left = left;
            this.right = right;
        }

        public override Type ValueType => typeof(bool);

        public override object Resolve(DialectExecutionContext context)
        {
            if (!Enum.IsDefined(typeof(DialectCompareType), comparisonType) ||
                !IsSupported(comparisonType, comparisonOperator))
                throw new InvalidOperationException("Compare contains an unsupported operator for its selected type.");
            var a = left.Resolve(context);
            var b = right.Resolve(context);
            return comparisonType switch
            {
                DialectCompareType.String when IsString(a) && IsString(b) =>
                    CompareEquality(string.Equals(a as string, b as string, StringComparison.Ordinal)),
                DialectCompareType.Boolean when a is bool x && b is bool y => CompareEquality(x == y),
                DialectCompareType.Integer when a is int x && b is int y => Compare(x, y),
                DialectCompareType.Float when a is float x && b is float y => Compare(x, y),
                DialectCompareType.Object when IsObject(a) && IsObject(b) =>
                    CompareEquality((a as UnityEngine.Object) == (b as UnityEngine.Object)),
                _ => throw new InvalidOperationException("Compare inputs do not match its selected type.")
            };
        }

        bool CompareEquality(bool equal) => comparisonOperator == DialectComparisonOperator.Equal ? equal : !equal;

        bool Compare(int a, int b) => comparisonOperator switch
        {
            DialectComparisonOperator.Equal => a == b,
            DialectComparisonOperator.NotEqual => a != b,
            DialectComparisonOperator.Less => a < b,
            DialectComparisonOperator.LessOrEqual => a <= b,
            DialectComparisonOperator.Greater => a > b,
            DialectComparisonOperator.GreaterOrEqual => a >= b,
            _ => false
        };

        bool Compare(float a, float b) => comparisonOperator switch
        {
            DialectComparisonOperator.Equal => a == b,
            DialectComparisonOperator.NotEqual => a != b,
            DialectComparisonOperator.Less => a < b,
            DialectComparisonOperator.LessOrEqual => a <= b,
            DialectComparisonOperator.Greater => a > b,
            DialectComparisonOperator.GreaterOrEqual => a >= b,
            _ => false
        };

        static bool IsString(object value) => value == null || value is string;
        static bool IsObject(object value) => value == null || value is UnityEngine.Object;

        public static bool IsSupported(DialectCompareType type, DialectComparisonOperator comparisonOperator) =>
            type is DialectCompareType.Integer or DialectCompareType.Float ||
            comparisonOperator is DialectComparisonOperator.Equal or DialectComparisonOperator.NotEqual;

        public static DialectComparisonOperator Normalize(DialectCompareType type,
            DialectComparisonOperator comparisonOperator) =>
            IsSupported(type, comparisonOperator) ? comparisonOperator : DialectComparisonOperator.Equal;
    }
}

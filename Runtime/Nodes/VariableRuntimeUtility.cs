using System;
using Dialect.Blackboards;
using Dialect.Executors;
using Dialect.Values;

namespace Dialect.Nodes
{
    static class VariableRuntimeUtility
    {
        public static DialectValue Read(DialectExecutionContext context, string variableId, DialectValueType type)
        {
            if (!context.Variables.TryGetValue(variableId, out var value) || value.Type != type)
                throw new InvalidOperationException("The selected session variable is missing or has changed type.");
            return value;
        }

        public static void Write(DialectExecutionContext context, string variableId, DialectValueType type, object value)
        {
            var expected = DialectValueUtility.GetSystemType(type);
            if ((value == null && expected.IsValueType) || (value != null && !expected.IsInstanceOfType(value)))
                throw new InvalidOperationException("The value does not match the selected session variable type.");
            var resolved = DialectValueUtility.Create(type, value);
            if (!context.Variables.TrySet(variableId, resolved))
                throw new InvalidOperationException("The selected session variable could not be updated.");
        }
    }
}

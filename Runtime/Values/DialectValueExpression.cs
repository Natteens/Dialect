using System;
using Dialect.Blackboards;
using Dialect.Executors;
using UnityEngine;

namespace Dialect.Values
{
    [Serializable]
    public struct DialectValueExpression
    {
        [SerializeReference] DialectValue fallback;
        [SerializeReference] DialectValueResolver resolver;
        [SerializeField] string sourcePortId;

        public DialectValueExpression(DialectValue fallback)
        {
            this.fallback = fallback?.Clone();
            resolver = null;
            sourcePortId = string.Empty;
        }

        public DialectValueExpression(DialectValueResolver resolver, string sourcePortId)
        {
            fallback = null;
            this.resolver = resolver;
            this.sourcePortId = sourcePortId ?? string.Empty;
        }

        public Type ValueType => resolver?.ValueType ?? DialectValueUtility.GetSystemType(fallback?.Type ?? DialectValueType.String);

        public object Resolve(DialectExecutionContext context)
        {
            var value = resolver != null ? resolver.Resolve(context) : fallback?.BoxedValue;
            context.ReportResolvedValue(sourcePortId, FormatPreview(value));
            return value;
        }

        static string FormatPreview(object value) => value switch
        {
            null => "null",
            bool boolean => boolean ? "true" : "false",
            UnityEngine.Object asset => asset != null ? asset.name : "None",
            _ => value.ToString()
        };
    }
}

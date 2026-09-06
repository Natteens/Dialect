using System;
using Dialect.Blackboards;
using Dialect.Executors;
using UnityEngine;
using UnityEngine.Localization;

namespace Dialect.Values
{
    [Serializable]
    public struct DialectTextExpression
    {
        [SerializeField] DialectText fallback;
        [SerializeReference] DialectValueResolver resolver;
        [SerializeField] string sourcePortId;

        public DialectTextExpression(DialectText fallback)
        { this.fallback = fallback; resolver = null; sourcePortId = string.Empty; }

        public DialectTextExpression(DialectValueResolver resolver, string sourcePortId)
        { fallback = default; this.resolver = resolver; this.sourcePortId = sourcePortId ?? string.Empty; }

        public bool IsDynamic => resolver != null;
        public Type ValueType => resolver?.ValueType ?? typeof(DialectText);

        public string Resolve(DialectExecutionContext context)
        {
            if (resolver == null) return fallback.Resolve(context);
            var value = resolver.Resolve(context);
            var text = value switch
            {
                string literal => literal,
                LocalizedString localized => DialectText.ResolveLocalized(localized),
                DialectText authored => authored.Resolve(context),
                _ => string.Empty
            };
            context.ReportResolvedValue(sourcePortId, text);
            return text;
        }
    }
}

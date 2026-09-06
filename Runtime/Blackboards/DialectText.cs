using System;
using Dialect.Executors;
using UnityEngine;
using UnityEngine.Localization;

namespace Dialect.Blackboards
{
    public enum DialectTextSource { Inline, Localized, Blackboard }
    [Serializable]
    public struct DialectText
    {
        [SerializeField] DialectTextSource source;
        [SerializeField] string inlineText;
        [SerializeField] LocalizedString localizedText;
        [SerializeField] DialectVariableReference variable;
        public DialectTextSource Source => source;
        public string InlineValue => inlineText;
        public LocalizedString LocalizedValue => localizedText;
        public DialectVariableReference VariableReference => variable;
        public static DialectText Inline(string value) => new() { source = DialectTextSource.Inline, inlineText = value };
        public static DialectText Localized(LocalizedString value) => new() { source = DialectTextSource.Localized, localizedText = value };
        public static DialectText Blackboard(DialectVariableReference value) => new() { source = DialectTextSource.Blackboard, variable = value };
        public bool IsEmpty => source switch
        {
            DialectTextSource.Localized => localizedText == null || localizedText.IsEmpty,
            DialectTextSource.Blackboard => !variable.IsValid,
            _ => string.IsNullOrEmpty(inlineText)
        };
        public string Resolve(DialectExecutionContext context)
        {
            switch (source)
            {
                case DialectTextSource.Localized:
                    return ResolveLocalized(localizedText);
                case DialectTextSource.Blackboard:
                    if (context.Variables.TryGet<string>(variable.VariableId, out var text)) return text;
                    if (context.Variables.TryGet<LocalizedString>(variable.VariableId, out var localized) && localized != null && !localized.IsEmpty)
                        return ResolveLocalized(localized);
                    return string.Empty;
                default: return inlineText ?? string.Empty;
            }
        }

        internal static string ResolveLocalized(LocalizedString value) =>
            value != null && !value.IsEmpty ? value.GetLocalizedString() : string.Empty;
    }
}

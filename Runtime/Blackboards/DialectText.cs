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
        public static DialectText Inline(string value) => new() { source = DialectTextSource.Inline, inlineText = value };
        public static DialectText Localized(LocalizedString value) => new() { source = DialectTextSource.Localized, localizedText = value };
        public static DialectText Blackboard(DialectVariableReference value) => new() { source = DialectTextSource.Blackboard, variable = value };
        public string Resolve(DialectExecutionContext context)
        {
            switch (source)
            {
                case DialectTextSource.Localized:
                    return localizedText != null && !localizedText.IsEmpty ? localizedText.GetLocalizedString() : string.Empty;
                case DialectTextSource.Blackboard:
                    if (context.Variables.TryGet<string>(variable.VariableId, out var text)) return text;
                    if (context.Variables.TryGet<LocalizedString>(variable.VariableId, out var localized) && localized != null && !localized.IsEmpty)
                        return localized.GetLocalizedString();
                    return string.Empty;
                default: return inlineText ?? string.Empty;
            }
        }
    }
}

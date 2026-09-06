using System;
using System.Collections.Generic;
using Dialect.Blackboards;
using Dialect.Editor.Nodes;
using Dialect.Values;
using Unity.GraphToolkit.Editor;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEditor.Localization;

namespace Dialect.Editor
{
    static class DialectValueCompiler
    {
        public static DialectTextExpression CompileText(DialectGraph graph, IPort input, List<string> diagnostics)
        {
            if (input == null)
            {
                diagnostics.Add("A required text port is missing.");
                return default;
            }
            if (!input.IsConnected)
            {
                input.TryGetValue(out DialectText text);
                return new DialectTextExpression(text);
            }

            var output = input.FirstConnectedPort;
            if (output == null)
            {
                diagnostics.Add($"Text input '{input.DisplayName}' has an invalid connection.");
                return default;
            }

            var resolver = CompileConnectedValue(graph, output, diagnostics);
            if (resolver == null) return default;
            if (!IsTextType(resolver.ValueType))
            {
                diagnostics.Add($"Value connected to '{input.DisplayName}' resolves as {resolver.ValueType.Name}, expected String, LocalizedString, or DialectText.");
                return default;
            }
            return new DialectTextExpression(resolver, output.ID.ToString());
        }

        public static void ValidateText(IPort input, string label, Action<string> error)
        {
            if (input == null) { error($"{label} input is missing."); return; }
            if (!input.IsConnected)
            {
                if (!input.TryGetValue(out DialectText text) || text.IsEmpty) error($"{label} is empty.");
                else ValidateLocalization(text, label, error);
                return;
            }
            var output = input.FirstConnectedPort;
            if (output == null || !IsTextType(output.DataType)) error($"{label} has an incompatible value connection.");
        }

        public static bool IsTextType(Type type) => type == typeof(string) || type == typeof(LocalizedString) || type == typeof(DialectText);

        static DialectValueResolver CompileConnectedValue(DialectGraph graph, IPort output, List<string> diagnostics)
        {
            switch (output.GetNode())
            {
                case IVariableNode variableNode:
                    return CompileVariable(variableNode.Variable, diagnostics);
                case IConstantNode constantNode:
                    return CompileConstant(constantNode, diagnostics);
                case IDialectValueNodeCompiler compiler when output.GetNode() is Node authoringNode:
                    try { return compiler.CompileValue(new DialectValueNodeCompilationContext(graph, authoringNode, diagnostics)); }
                    catch (Exception exception)
                    { diagnostics.Add($"{output.GetNode().Title}: {exception.Message}"); return null; }
                case IDialectValueNodeCompiler:
                    diagnostics.Add("Connected value node is not a supported Graph Toolkit authoring node.");
                    return null;
                default:
                    diagnostics.Add($"{output.GetNode().Title} does not implement IDialectValueNodeCompiler.");
                    return null;
            }
        }

        static void ValidateLocalization(DialectText text, string label, Action<string> error)
        {
            if (text.Source != DialectTextSource.Localized || text.LocalizedValue == null || text.LocalizedValue.IsEmpty) return;
            var localized = text.LocalizedValue;
            var collection = LocalizationEditorSettings.GetStringTableCollection(localized.TableReference);
            if (collection == null)
            {
                error($"{label} references a missing localization table.");
                return;
            }
            var entry = localized.TableEntryReference;
            var exists = entry.ReferenceType switch
            {
                TableEntryReference.Type.Name => collection.SharedData.GetEntry(entry.Key) != null,
                TableEntryReference.Type.Id => collection.SharedData.GetEntry(entry.KeyId) != null,
                _ => false
            };
            if (!exists) error($"{label} references a missing localization entry.");
        }

        static DialectValueResolver CompileVariable(IVariable variable, ICollection<string> diagnostics)
        {
            if (variable == null) { diagnostics.Add("Connected graph variable is missing."); return null; }
            if (!TryCreateValue(variable.DataType, variable, out var value))
            { diagnostics.Add($"Local variable '{variable.Name}' uses unsupported type {variable.DataType.Name}."); return null; }
            return new DialectVariableValueResolver(variable.ID.ToString(), value.Type);
        }

        static DialectValueResolver CompileConstant(IConstantNode constant, ICollection<string> diagnostics)
        {
            if (constant.TryGetValue(out string text)) return new DialectConstantValueResolver(new DialectStringValue(text));
            if (constant.TryGetValue(out LocalizedString localized)) return new DialectConstantValueResolver(new DialectLocalizedStringValue(localized));
            if (constant.TryGetValue(out DialectText authored)) return new DialectAuthoredTextResolver(authored);
            diagnostics.Add($"Constant uses unsupported type {constant.DataType.Name} for dialogue text.");
            return null;
        }

        public static bool TryCreateValue(Type type, IVariable variable, out DialectValue value)
        {
            if (type == typeof(string) && variable.TryGetDefaultValue(out string text)) { value = new DialectStringValue(text); return true; }
            if (type == typeof(LocalizedString) && variable.TryGetDefaultValue(out LocalizedString localized)) { value = new DialectLocalizedStringValue(localized); return true; }
            if (type == typeof(bool) && variable.TryGetDefaultValue(out bool boolean)) { value = new DialectBoolValue(boolean); return true; }
            if (type == typeof(int) && variable.TryGetDefaultValue(out int integer)) { value = new DialectIntValue(integer); return true; }
            if (type == typeof(float) && variable.TryGetDefaultValue(out float number)) { value = new DialectFloatValue(number); return true; }
            if (typeof(UnityEngine.Object).IsAssignableFrom(type) && variable.TryGetDefaultValue(out UnityEngine.Object asset))
            { value = new DialectObjectValue(asset); return true; }
            value = null;
            return false;
        }
    }
}

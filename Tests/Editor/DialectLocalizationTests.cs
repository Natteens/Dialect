using System.Collections.Generic;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Nodes;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Dialect.Tests.Editor
{
    public sealed class DialectLocalizationTests
    {
        const string Folder = "Assets/__DialectLocalizationTests";
        Locale locale;
        readonly List<string> createdAddressableGroups = new();

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(Folder);
            createdAddressableGroups.Clear();
            AssetDatabase.CreateFolder("Assets", "__DialectLocalizationTests");
            locale = Locale.CreateLocale("en-DL");
            AssetDatabase.CreateAsset(locale, Folder + "/English.asset");
        }

        [TearDown]
        public void TearDown()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
                for (var i = settings.groups.Count - 1; i >= 0; i--)
                {
                    var group = settings.groups[i];
                    if (group != null && createdAddressableGroups.Contains(group.Guid)) settings.RemoveGroup(group);
                }
            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.Refresh();
        }

        [Test]
        public void ValidLocalizedStringAndBlackboardValueResolve()
        {
            var collection = LocalizationEditorSettings.CreateStringTableCollection(
                "Dialect Test Strings", Folder, new List<Locale> { locale });
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
                foreach (var group in settings.groups)
                    if (group != null)
                        foreach (var entry in group.entries)
                            if (entry != null && AssetDatabase.GUIDToAssetPath(entry.guid).StartsWith(Folder))
                            {
                                createdAddressableGroups.Add(group.Guid);
                                break;
                            }
            var table = (StringTable)collection.GetTable(locale.Identifier);
            table.AddEntry("line", "Localized line");
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
            AssetDatabase.SaveAssets();

            var localized = new LocalizedString(collection.SharedData.TableCollectionNameGuid, "line")
            { LocaleOverride = locale };
            var board = ScriptableObject.CreateInstance<DialectBlackboard>();
            var definition = board.AddVariable("Line", new DialectLocalizedStringValue(localized));
            var graph = ScriptableObject.CreateInstance<DialectRuntimeGraph>();
            graph.Configure("localized", 0, new List<RuntimeNode>
            {
                new DialogueRuntimeNode(default,
                    DialectText.Blackboard(new DialectVariableReference(board, definition.Id)), 1),
                new EndRuntimeNode()
            }, new List<DialectBlackboard> { board }, new List<string>());
            var owner = new GameObject("Dialect Localization Test");
            try
            {
                DialectLine line = default;
                var director = owner.AddComponent<DialectDirector>();
                director.LinePresented += value => line = value;
                director.Play(graph);
                Assert.That(line.Text, Is.EqualTo("Localized line"));
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(graph);
                Object.DestroyImmediate(board);
            }
        }

        [Test]
        public void MissingLocalizationReferencesDoNotAdvanceOrFaultSession()
        {
            var missingTable = new LocalizedString("Dialect Missing Table", "missing")
            { LocaleOverride = locale };
            var graph = ScriptableObject.CreateInstance<DialectRuntimeGraph>();
            graph.Configure("missing-localization", 0, new List<RuntimeNode>
            {
                new DialogueRuntimeNode(default, DialectText.Localized(missingTable), 1),
                new EndRuntimeNode()
            }, null, new List<string>());
            var owner = new GameObject("Dialect Missing Localization Test");
            try
            {
                var director = owner.AddComponent<DialectDirector>();
                Assert.DoesNotThrow(() => director.Play(graph));
                Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.WaitingForAdvance));
                Assert.That(director.Session.CurrentNodeIndex, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(graph);
            }
        }
    }
}

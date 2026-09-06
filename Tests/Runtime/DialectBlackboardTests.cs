using System.Collections.Generic;
using Dialect.Blackboards;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization;

namespace Dialect.Tests
{
    public sealed class DialectBlackboardTests
    {
        DialectBlackboard blackboard;

        [SetUp]
        public void SetUp() => blackboard = ScriptableObject.CreateInstance<DialectBlackboard>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(blackboard);

        [Test]
        public void RenamePreservesStableId()
        {
            var variable = blackboard.AddVariable("PlayerName", new DialectStringValue("Mara"));
            var id = variable.Id;
            variable.Name = "ProtagonistName";
            Assert.That(variable.Id, Is.EqualTo(id));
            Assert.That(blackboard.TryGetDefinition(id, out var found), Is.True);
            Assert.That(found, Is.SameAs(variable));
        }

        [Test]
        public void SessionStoreDoesNotMutateAssetDefault()
        {
            var variable = blackboard.AddVariable("Trust", new DialectIntValue(2));
            var store = new DialectVariableStore(new[] { blackboard });
            Assert.That(store.TrySet(variable.Id, new DialectIntValue(8)), Is.True);
            Assert.That(store.TryGet<int>(variable.Id, out var runtimeValue), Is.True);
            Assert.That(runtimeValue, Is.EqualTo(8));
            Assert.That(((DialectIntValue)variable.DefaultValue).Value, Is.EqualTo(2));
        }

        [Test]
        public void TypeMismatchAndUnknownVariableAreRejected()
        {
            var variable = blackboard.AddVariable("Enabled", new DialectBoolValue(true));
            var store = new DialectVariableStore(new[] { blackboard });
            Assert.That(store.TrySet(variable.Id, new DialectIntValue(1)), Is.False);
            Assert.That(store.TrySet("missing", new DialectBoolValue(false)), Is.False);
        }

        [Test]
        public void SnapshotCanRestoreSessionValues()
        {
            var variable = blackboard.AddVariable("Score", new DialectIntValue(4));
            var store = new DialectVariableStore(new[] { blackboard });
            var snapshot = new Dictionary<string, DialectValue>(store.CreateSnapshot());
            store.TrySet(variable.Id, new DialectIntValue(10));
            store.RestoreSnapshot(snapshot);
            Assert.That(store.TryGet<int>(variable.Id, out var value), Is.True);
            Assert.That(value, Is.EqualTo(4));
        }

        [Test]
        public void DuplicateCreatesIndependentValueAndNewStableId()
        {
            var source = blackboard.AddVariable("Name", new DialectStringValue("Mara"));
            var copy = blackboard.DuplicateVariable(source.Id);
            Assert.That(copy, Is.Not.Null);
            Assert.That(copy.Id, Is.Not.EqualTo(source.Id));
            Assert.That(copy.Name, Is.EqualTo("Name Copy"));
            Assert.That(copy.DefaultValue, Is.Not.SameAs(source.DefaultValue));
        }

        [Test]
        public void ReorderPreservesIds()
        {
            var first = blackboard.AddVariable("First", new DialectIntValue(1));
            var second = blackboard.AddVariable("Second", new DialectIntValue(2));
            var firstId = first.Id;
            var secondId = second.Id;
            Assert.That(blackboard.MoveVariable(second.Id, 0), Is.True);
            Assert.That(blackboard.Variables[0].Id, Is.EqualTo(secondId));
            Assert.That(blackboard.Variables[1].Id, Is.EqualTo(firstId));
        }

        [Test]
        public void StoreLoadsAllBuiltInTypes()
        {
            var asset = ScriptableObject.CreateInstance<DialectBlackboard>();
            var objectValue = ScriptableObject.CreateInstance<DialectBlackboard>();
            try
            {
                var text = asset.AddVariable("Text", new DialectStringValue("value"));
                var localized = asset.AddVariable("Localized", new DialectLocalizedStringValue(new LocalizedString()));
                var boolean = asset.AddVariable("Bool", new DialectBoolValue(true));
                var integer = asset.AddVariable("Int", new DialectIntValue(4));
                var number = asset.AddVariable("Float", new DialectFloatValue(2.5f));
                var reference = asset.AddVariable("Object", new DialectObjectValue(objectValue));
                var store = new DialectVariableStore(new[] { asset });
                Assert.That(store.TryGet(text.Id, out string textValue) && textValue == "value", Is.True);
                Assert.That(store.TryGet(localized.Id, out LocalizedString localizedValue), Is.True);
                Assert.That(localizedValue, Is.Not.Null);
                Assert.That(store.TryGet(boolean.Id, out bool boolValue) && boolValue, Is.True);
                Assert.That(store.TryGet(integer.Id, out int intValue) && intValue == 4, Is.True);
                Assert.That(store.TryGet(number.Id, out float floatValue) && floatValue == 2.5f, Is.True);
                Assert.That(store.TryGet(reference.Id, out UnityEngine.Object storedObject) && storedObject == objectValue, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(objectValue);
            }
        }

        [Test]
        public void LocalAndSharedDefaultsComposeAndSessionOverrideWins()
        {
            var local = new DialectVariableDefinition("local-id", "Local", new DialectStringValue("local"));
            var shared = blackboard.AddVariable("Shared", new DialectIntValue(3));
            var store = new DialectVariableStore(new[] { local }, new[] { blackboard });
            Assert.That(store.Count, Is.EqualTo(2));
            Assert.That(store.TryGet(local.Id, out string localValue) && localValue == "local", Is.True);
            Assert.That(store.TrySet(shared.Id, new DialectIntValue(9)), Is.True);
            Assert.That(store.TryGet(shared.Id, out int sharedValue) && sharedValue == 9, Is.True);
            Assert.That(((DialectIntValue)shared.DefaultValue).Value, Is.EqualTo(3));
        }

        [Test]
        public void DuplicateIdsAcrossScopesAreRejected()
        {
            var local = new DialectVariableDefinition("same-id", "Local", new DialectStringValue("local"));
            var shared = new DialectVariableDefinition("same-id", "Shared", new DialectStringValue("shared"));
            var error = Assert.Throws<System.InvalidOperationException>(() =>
                new DialectVariableStore(new[] { local, shared }, null));
            Assert.That(error.Message, Does.Contain("same-id"));
        }

        [Test]
        public void EmptyIdsAreRejectedAtRuntimeBoundary()
        {
            var invalid = new DialectVariableDefinition(string.Empty, "Invalid", new DialectStringValue("value"));
            Assert.Throws<System.InvalidOperationException>(() => new DialectVariableStore(new[] { invalid }, null));
        }

        [Test]
        public void DuplicateSharedBoardReferenceIsRejectedAtRuntimeBoundary()
        {
            blackboard.AddVariable("Name", new DialectStringValue("Mara"));
            Assert.Throws<System.InvalidOperationException>(() =>
                new DialectVariableStore(null, new[] { blackboard, blackboard }));
        }
    }
}

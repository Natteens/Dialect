using System.Collections.Generic;
using Dialect.Blackboards;
using NUnit.Framework;
using UnityEngine;

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
    }
}

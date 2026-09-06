using System;
using Dialect.Executors;
using UnityEngine;

namespace Dialect.Core
{
    [Serializable]
    public abstract class RuntimeNode
    {
        [SerializeField] string authoringId;

        public string AuthoringId => authoringId;

        public abstract DialectExecutionResult Execute(DialectExecutionContext context);

        public virtual bool RefreshPresentation(DialectExecutionContext context) => false;

        public void SetAuthoringId(string value) => authoringId = value;
    }
}

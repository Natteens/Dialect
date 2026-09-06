using System.Collections.Generic;
using Dialect.Core;
using Unity.GraphToolkit.Editor.GraphVisualization;
using UnityEditor;
using UnityEngine;

namespace Dialect.Editor.Visualization
{
    [InitializeOnLoad]
    static class DialectRuntimeVisualization
    {
        static readonly HashSet<DialectDirector> Directors = new();
        static readonly Dictionary<DialectSession, DialectTransition> PendingTransitions = new();
        static readonly HashSet<DialectRuntimeGraph> ActiveGraphs = new();

        static DialectRuntimeVisualization()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            DialectDirector.EditorDirectorEnabled += OnDirectorEnabled;
            DialectDirector.EditorDirectorDisabled += OnDirectorDisabled;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                foreach (var director in Object.FindObjectsByType<DialectDirector>(FindObjectsInactive.Include)) Attach(director);
            else if (state == PlayModeStateChange.ExitingPlayMode) ClearAll();
        }

        static void OnDirectorEnabled(DialectDirector director)
        {
            if (Application.isPlaying) Attach(director);
        }

        static void OnDirectorDisabled(DialectDirector director)
        {
            if (director == null) return;
            if (director.Session?.Graph != null) Clear(director.Session.Graph);
            Detach(director);
        }

        static void Attach(DialectDirector director)
        {
            if (director == null || !Directors.Add(director)) return;
            director.SessionStarted += OnSessionStarted;
            director.NodeEntered += OnNodeEntered;
            director.Transitioned += OnTransitioned;
            director.ValueResolved += OnValueResolved;
            director.SessionEnded += OnSessionEnded;
            if (director.Session?.Graph != null) ActiveGraphs.Add(director.Session.Graph);
        }

        static void Detach(DialectDirector director)
        {
            if (director == null || !Directors.Remove(director)) return;
            director.SessionStarted -= OnSessionStarted;
            director.NodeEntered -= OnNodeEntered;
            director.Transitioned -= OnTransitioned;
            director.ValueResolved -= OnValueResolved;
            director.SessionEnded -= OnSessionEnded;
        }

        static void OnSessionStarted(DialectSession session)
        {
            if (session?.Graph != null) { ActiveGraphs.Add(session.Graph); Clear(session.Graph); }
        }

        static void OnTransitioned(DialectSession session, DialectTransition transition)
        {
            if (session != null) PendingTransitions[session] = transition;
        }

        static void OnNodeEntered(DialectSession session, RuntimeNode node)
        {
            if (!DialectProjectSettings.instance.VisualizationEnabled || !TryGetContext(session?.Graph, out var context)) return;
            ActiveGraphs.Add(session.Graph);
            context.NodeCustomizationEnabled = true;
            context.WireCustomizationEnabled = true;
            context.PortPreviewEnabled = true;
            context.ClearAllVisualization();
            if (TryParse(node?.AuthoringId, out var nodeId))
            {
                var reference = context.GetNodeReference(nodeId);
                reference.FillAmount = 1f;
                context.Motion.Play(reference, .28f);
            }
            if (PendingTransitions.Remove(session, out var transition) &&
                TryParse(transition.OutputPortId, out var output) && TryParse(transition.InputPortId, out var input))
            {
                var wire = context.GetWireReference(output, input);
                wire.Opacity = 1f;
                wire.WidthOverride = 4f;
                context.Motion.Play(wire, .28f);
            }
        }

        static void OnValueResolved(DialectSession session, DialectValuePreview preview)
        {
            if (!DialectProjectSettings.instance.VisualizationEnabled || string.IsNullOrEmpty(preview.PortId) ||
                !TryGetContext(session?.Graph, out var context) || !TryParse(preview.PortId, out var portId)) return;
            context.PortPreviewEnabled = true;
            var value = preview.Value.Length <= 80 ? preview.Value : preview.Value.Substring(0, 77) + "...";
            context.GetPortReference(portId).SetPreview(value);
        }

        static void OnSessionEnded(DialectSession session, DialectTerminationReason reason)
        {
            PendingTransitions.Remove(session);
            Clear(session?.Graph);
        }

        static bool TryGetContext(DialectRuntimeGraph graph, out Context context)
        {
            context = null;
            if (graph == null || !TryParse(graph.GraphId, out var graphId)) return false;
            context = Registry.GetActiveContext(graphId);
            return context != null && context.IsValid && context.IsGraphLoaded;
        }

        static bool TryParse(string value, out Hash128 result)
        {
            try { result = Hash128.Parse(value); return true; }
            catch { result = default; return false; }
        }

        static void Clear(DialectRuntimeGraph graph)
        {
            if (TryGetContext(graph, out var context)) context.ClearAllVisualization();
        }

        static void ClearAll()
        {
            foreach (var graph in ActiveGraphs) Clear(graph);
            var directors = new List<DialectDirector>(Directors);
            foreach (var director in directors) Detach(director);
            PendingTransitions.Clear();
            ActiveGraphs.Clear();
        }
    }
}

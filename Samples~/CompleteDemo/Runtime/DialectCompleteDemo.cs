using System.Collections.Generic;
using Dialect.Blackboards;
using Dialect.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dialect.Samples.Complete
{
    public sealed class DialectCompleteDemo : MonoBehaviour
    {
        [SerializeField] DialectDirector director;
        [SerializeField] DialectRuntimeGraph greeting;
        [SerializeField] DialectRuntimeGraph goodbye;
        [SerializeField] UIDocument document;
        [SerializeField] SampleState state;

        Label speaker;
        Label line;
        VisualElement choices;

        void OnEnable()
        {
            speaker = document.rootVisualElement.Q<Label>("speaker");
            line = document.rootVisualElement.Q<Label>("line");
            choices = document.rootVisualElement.Q("choices");
            director.LinePresented += ShowLine;
            director.ChoicesPresented += ShowChoices;
            director.SessionEnded += Hide;
        }

        void OnDisable()
        {
            director.LinePresented -= ShowLine;
            director.ChoicesPresented -= ShowChoices;
            director.SessionEnded -= Hide;
        }

        public void PlayGreeting(string playerName)
        {
            state.PlayerName = playerName;
            var overrides = new Dictionary<string, DialectValue>();
            if (greeting.TryGetVariableDefinition("PlayerName", out var definition))
                overrides.Add(definition.Id, new DialectStringValue(playerName));
            director.Play(greeting, state, overrides);
        }

        public void PlayGoodbye() => director.Play(goodbye, state);
        public void Advance() => director.Advance();

        void ShowLine(DialectLine value)
        {
            speaker.text = value.Speaker;
            line.text = value.Text;
            choices.Clear();
            document.rootVisualElement.style.display = DisplayStyle.Flex;
        }

        void ShowChoices(DialectChoiceSet value)
        {
            choices.Clear();
            for (var i = 0; i < value.Count; i++)
            {
                var index = i;
                var button = new Button(() => director.Choose(index)) { text = value.Choices[i].Text };
                choices.Add(button);
            }
            document.rootVisualElement.style.display = DisplayStyle.Flex;
        }

        void Hide(DialectSession session, DialectTerminationReason reason) =>
            document.rootVisualElement.style.display = DisplayStyle.None;
    }
}

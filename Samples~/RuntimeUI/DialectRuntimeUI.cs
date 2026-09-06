using Dialect.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dialect.Samples
{
    public sealed class DialectRuntimeUI : MonoBehaviour
    {
        [SerializeField] DialectDirector director;
        [SerializeField] UIDocument document;
        [SerializeField] string speakerElement = "speaker";
        [SerializeField] string lineElement = "line";

        Label speaker;
        Label line;

        void OnEnable()
        {
            speaker = document.rootVisualElement.Q<Label>(speakerElement);
            line = document.rootVisualElement.Q<Label>(lineElement);
            director.LinePresented += ShowLine;
            director.SessionEnded += Hide;
        }

        void OnDisable()
        {
            director.LinePresented -= ShowLine;
            director.SessionEnded -= Hide;
        }

        public void Advance() => director.Advance();
        public void Choose(int index) => director.Choose(index);

        void ShowLine(DialectLine value)
        {
            speaker.text = value.Speaker;
            line.text = value.Text;
            document.rootVisualElement.style.display = DisplayStyle.Flex;
        }

        void Hide(DialectSession session, DialectTerminationReason reason) =>
            document.rootVisualElement.style.display = DisplayStyle.None;
    }
}

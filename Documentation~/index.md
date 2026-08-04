# Dialect documentation

Dialect is a graph-authored dialogue framework for Unity. Graph assets are created in the Editor and imported into a compact runtime representation executed by `DialectDirector`.

## Requirements

- Unity 6000.0 or newer.
- Unity Localization 1.5.9 or newer.

Dialect uses Unity's graph authoring APIs supplied by the Editor. It does not require a separate Graph Toolkit entry in `package.json`.

## Creating a dialogue

1. Create a Dialect graph asset from the Unity asset menu.
2. Open the graph in the Editor.
3. Add a start node and connect dialogue, choice, condition, action and end nodes.
4. Save the graph so the importer updates its runtime representation.
5. Assign the imported runtime graph to a `DialectDirector`.

## Core nodes

- **Start** defines the graph entry point.
- **Dialogue** presents a speaker and line of text.
- **Choice** presents multiple routes to the player.
- **Condition** selects a branch from game state.
- **Action** invokes project-specific dialogue behavior.
- **End** completes the conversation.
- **Localized** stores a reusable `LocalizedString` reference.

## Runtime flow

`DialectDirector` executes the current runtime node and exposes events that a game UI can observe. The UI remains project-owned; Dialect reports dialogue text, choices and completion without imposing a visual layout.

Typical integration:

```csharp
using Dialect;
using UnityEngine;

public sealed class DialoguePresenter : MonoBehaviour
{
    [SerializeField] private DialectDirector director;

    private void OnEnable()
    {
        director.OnDialogueShown += ShowDialogue;
        director.OnChoiceShown += ShowChoices;
        director.OnDialogueEnded += HideDialogue;
    }

    private void OnDisable()
    {
        director.OnDialogueShown -= ShowDialogue;
        director.OnChoiceShown -= ShowChoices;
        director.OnDialogueEnded -= HideDialogue;
    }

    private void ShowDialogue(string speaker, string text) { }
    private void ShowChoices(string[] choices) { }
    private void HideDialogue() { }
}
```

Start and advance the conversation through the director methods exposed by the installed version.

## Localization

Dialogue and choice content can reference Unity `LocalizedString` values. When the selected locale changes, the current content can be processed again so the presentation refreshes without restarting the graph.

Use plain strings for content that never changes by locale and localized references for player-facing text.

## Conditions and actions

Conditions and actions connect the dialogue graph to game logic. Keep those implementations small and delegate to game-owned systems. Dialogue assets should describe flow rather than become a second gameplay architecture.

## Authoring guidance

- Keep graphs focused on one conversation or scene.
- Prefer explicit branches over deeply nested condition chains.
- Keep user-facing text in localization tables.
- Test malformed or incomplete graphs before shipping content.
- Treat serialized graph compatibility as part of the package API.
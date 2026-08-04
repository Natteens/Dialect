<div align="center">

# Dialect

**Write the conversation as a graph. Keep the presentation in your game.**

A Unity dialogue package that combines visual authoring, runtime events and Localization without
locking the project into a predefined dialogue UI.

[![Release](https://img.shields.io/github/v/release/Natteens/dialect?sort=semver&label=release&style=flat-square)](https://github.com/Natteens/dialect/releases)
[![Unity](https://img.shields.io/badge/Unity-6000.0%2B-000000?style=flat-square&logo=unity)](https://unity.com)
[![Localization](https://img.shields.io/badge/Localization-1.5.9%2B-555555?style=flat-square)](https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/index.html)
[![License](https://img.shields.io/github/license/Natteens/dialect?style=flat-square)](./LICENSE.md)

[Why Dialect?](#authoring-and-presentation-stay-separate) · [Installation](#installation) · [Workflow](#basic-workflow) · [Documentation](#documentation)

</div>

---

## Authoring and Presentation Stay Separate

A dialogue graph should describe the conversation: what is said, which choices are available, what
conditions must pass and where the flow continues. It should not decide how a particular game draws
a portrait, animates a textbox or handles input.

Dialect keeps that boundary explicit. Conversations are authored visually and imported into a
runtime representation. `DialectDirector` executes the flow and reports what happened through
events, leaving the project free to present dialogue in its own style.

<table>
<tr>
<td width="50%"><strong>Visual conversation flow</strong><br><sub>Dialogue, choices, conditions, actions and endings remain readable as connected authoring nodes.</sub></td>
<td width="50%"><strong>Project-owned UI</strong><br><sub>Runtime events provide the content; the game decides how that content looks and behaves.</sub></td>
</tr>
<tr>
<td width="50%"><strong>Localization-ready</strong><br><sub>Dialogue content integrates with the Unity Localization package instead of inventing a parallel text system.</sub></td>
<td width="50%"><strong>Extensible behavior</strong><br><sub>Conditions and actions connect conversation flow to project-specific state without hard-coding it into the editor.</sub></td>
</tr>
</table>

## Installation

Requires Unity **6000.0** or newer. The Localization dependency is declared by the package. Graph
authoring is provided by the Unity Editor and does not require a separate Graph Toolkit entry in
`package.json`.

In the Package Manager, choose **Add package from git URL** and paste:

```text
https://github.com/Natteens/dialect.git
```

Or declare it in `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.natteens.dialect": "https://github.com/Natteens/dialect.git"
  }
}
```

Pin the dependency to a release tag for reproducible installs.

## Basic Workflow

1. Create a Dialect graph asset.
2. Build the conversation from dialogue, choice and control-flow nodes.
3. Save the graph so its runtime data is imported.
4. Assign the result to a `DialectDirector`.
5. Connect the director events to the project's dialogue presentation.

The graph owns conversation structure. The scene owns UI, animation, audio and gameplay reactions.

## Documentation

Graph authoring, director integration, custom actions, conditions and Localization are covered in
[Documentation](./Documentation~/index.md). Those guides hold the detailed workflow so this page can
stay focused on the package's role and boundaries.

See the [changelog](./CHANGELOG.md) for release history and compatibility notes.

## License

MIT. See [LICENSE.md](./LICENSE.md).

<div align="center">

# Dialect

Graph-authored dialogue for Unity with localization support.

[![Release](https://img.shields.io/github/v/release/Natteens/dialect?style=flat-square)](https://github.com/Natteens/dialect/releases)
[![Unity](https://img.shields.io/badge/Unity-6000.0%2B-000000?style=flat-square&logo=unity)](https://unity.com)
[![Localization](https://img.shields.io/badge/Localization-1.5.9%2B-555555?style=flat-square)](https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/index.html)
[![License](https://img.shields.io/github/license/Natteens/dialect?style=flat-square)](LICENSE.md)

</div>

Dialect lets dialogue designers build conversations as visual graphs while game code owns presentation and gameplay behavior. Graphs are imported into a runtime representation executed by `DialectDirector`.

## Features

- Dialogue, choice, condition, action and end nodes.
- Unity Localization integration.
- Runtime events for project-owned UI.
- Extensible conditions and actions.

## Installation

Add the package through `Window > Package Manager > Add package from git URL`:

```text
https://github.com/Natteens/dialect.git
```

Or add it to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.natteens.dialect": "https://github.com/Natteens/dialect.git"
  }
}
```

Unity resolves the Localization dependency declared by the package. The graph authoring APIs are supplied by the Unity Editor and are not listed as a separate package dependency.

## Quick start

1. Create a Dialect graph asset.
2. Add and connect the conversation nodes.
3. Save the graph to update its runtime representation.
4. Assign it to a `DialectDirector`.
5. Subscribe your UI to the director events.

## Documentation

Graph authoring, runtime integration and localization guidance are available in [Documentation](Documentation~/index.md).

## License

MIT. See [LICENSE.md](LICENSE.md).
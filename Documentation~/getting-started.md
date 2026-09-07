# Getting started

Dialect requires Unity 6000.6 or newer. Install the package from its Git URL, then create a graph from **Assets > Create > Dialect > Dialogue Graph**. A new graph opens with a valid Start-to-End flow so it can be tested immediately.

Add a **Dialogue** node between Start and End. In the node itself, choose **Inline**, type a speaker such as `Player`, and write the line `Where am I?`. Speaker is optional, so leave it empty for narration. No constant node, variable, resolver, or compiler knowledge is needed.

Press **Validate**, assign the imported `.dlg` to a `DialectDirector`, connect your UI to the Director events, and call `Play`. Use `Advance` for a visible Dialogue line and `Choose` for a Choice.

Once the basic flow works, switch a text field from **Inline** to **Localized** to choose a String Table entry. Use the native Graph Toolkit blackboard for values that live only in this `.dlg`, and **Shared Boards** for reusable `DialectBlackboard` assets.

The focused built-in set includes Dialogue and Choice; Start, End, Branch, Random Branch, Wait, Wait Until, and Wait For Resume; Action, Condition, Set Variable, and Modify Variable; and the Shared Variable, Compare, and Select value nodes. Condition, Action, and the custom-node SDK remain the integration points for project-specific behavior.

Dialect does not distribute presentation UI. Subscribe your own UI to Director events and render those payloads with UI Toolkit, uGUI, or another presentation layer.

Continue with [Authoring](authoring.md), [Variables](variables.md), and [Runtime](runtime.md).

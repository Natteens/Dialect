# Localization

Dialogue speakers, lines, and choices use `DialectText`. Select Inline for literal text, Localized for a Unity `LocalizedString`, or Blackboard for a shared string value.

Dialect subscribes to `LocalizationSettings.SelectedLocaleChanged` while a director is enabled. If a line or choice set is visible, the current runtime node resolves its content again and publishes a replacement payload. The session stays on the same node and does not advance.

Missing or empty localization references resolve to an empty string. Validate tables and entries with Unity Localization tools before shipping.

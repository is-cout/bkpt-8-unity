# Skills

All skills live flat under `.claude/skills/<name>/SKILL.md` (the extension only discovers
top-level skill folders, not nested category subfolders).

## Unity CLI & Editor automation

- **[unity-cli](unity-cli/SKILL.md)** — Use the `unity` terminal command: install/upgrade editors, licenses, auth, create/open/build/test projects, and drive a live-connected Editor instead of hand-editing scene/asset YAML.
- **[unity-custom-cli-commands](unity-custom-cli-commands/SKILL.md)** — Author project-specific `[CliCommand]` tools when the connected Editor's built-in command set doesn't cover a task (editing internal-only authoring data, scripting a repeated Editor operation). Covers the ops-layer pattern and the dry-run/confirm/backup safety convention.

## Unity subsystems

- **[localization](localization/SKILL.md)** — Add languages, translate UI text, CJK font setup, Unity Localization tables.
- **[optimize-audio](optimize-audio/SKILL.md)** — Cut Unity audio memory / CPU: Load Type, sample rate, codec, force-to-mono, AudioMixer cost.
- **[optimize-text-mesh-pro](optimize-text-mesh-pro/SKILL.md)** — Fix TMP problems: atlas bloat, fallback fonts, SDF quality, AutoSize discipline.
- **[physics-3d-collision](physics-3d-collision/SKILL.md)** — Diagnose 3D PhysX collision/trigger issues: events not firing, tunnelling, raycast misses, ragdolls, `AddForce` settling.
- **[ui](ui/SKILL.md)** — Router for Unity UI work when uGUI / UI Toolkit / IMGUI isn't specified.
- **[ui-ugui](ui-ugui/SKILL.md)** — Canvas / uGUI / RectTransform hierarchies.
- **[ui-imgui](ui-imgui/SKILL.md)** — `OnGUI` / `OnInspectorGUI` immediate-mode editor UI.

## Engineering (portable)

- **[diagnose](diagnose/SKILL.md)** — Disciplined diagnosis loop for hard bugs and performance regressions: reproduce → minimise → hypothesise → instrument → fix → regression-test.
- **[tdd](tdd/SKILL.md)** — Test-driven development with a red-green-refactor loop.
- **[grill-with-docs](grill-with-docs/SKILL.md)** — Grilling session that challenges a plan against the existing domain model and sharpens terminology.
- **[improve-codebase-architecture](improve-codebase-architecture/SKILL.md)** — Find deepening opportunities in a codebase.
- **[grill-me](grill-me/SKILL.md)** — Get relentlessly interviewed about a plan or design until every branch of the decision tree is resolved.
- **[write-a-skill](write-a-skill/SKILL.md)** — Create new skills with proper structure, progressive disclosure, and bundled resources.

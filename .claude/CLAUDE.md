# Claude Instructions — movement-study

Unity 6 sandbox for studying **character-movement techniques**. Not a shipping game — a
prototyping repo where different movement systems are ported in, tuned, and compared side by side:

- **Catlike Coding – Movement** series (custom-gravity `Rigidbody` sphere, orbit camera, climbing,
  swimming, moving platforms).
- **Kinematic Character Controller (KCC)** package + its walkthrough scenes.
- **Tarodev** 2D controller and a 3D port of it.
- **Deadlock** experiment — custom motor, stamina, zipline (`Assets/Scripts/Deadlock/`).
- **Dynamic Parkour System** asset (`Assets/parkour/`).
- **3D platformer template** (`Assets/3d platform tempalte/`).

Each experiment lives in its own folder under `Assets/Scripts/<Name>/` with a matching test scene
under `Assets/Scenes/`. Treat every experiment as self-contained: don't couple one movement system
to another.

Unity version: **6000.5.6f1**. Render pipeline: **URP 17.5**. Input: **new Input System**.

---

## Language Policy

Instruction files, skill files, and code comments are written in **English**. The user often writes
prompts in Portuguese — reply in the language they used, but keep committed text (code, comments,
docs) in English.

---

## Engineering Rules

These rules apply to every task unless explicitly overridden.

### Think Before Coding
State assumptions explicitly. If uncertain, ask — don't guess. Present multiple interpretations when ambiguity exists. Push back when a simpler approach exists. Stop when confused and name what's unclear.

### This Is a Study Repo — Ship It, Don't Deliberate
These builds are throwaway experiments, not production. **Do whatever works and keep moving.** Don't
stop to ask which of two approaches is nicer, don't wait for approval on a judgement call, don't
hedge on architectural purity. Pick the option that gets the movement feeling right and carry on.
Only stop for things that are genuinely destructive or that you cannot undo.

### Simplicity First
Minimum code that solves the problem. Nothing speculative. No features beyond what was asked. No abstractions for single-use code. Test: would a senior engineer say this is overcomplicated? If yes, simplify.

### Keep Experiments Isolated
Each movement system is its own study. Don't refactor two of them into a shared base "to reduce
duplication" — the duplication is the point, it keeps each experiment readable on its own and free
to diverge. Shared helpers are fine only when they're genuinely generic (math, debug drawing).

### Surgical Changes
Touch only what you must. Clean up only your own mess. Don't "improve" adjacent code, comments, or formatting. Don't refactor what isn't broken. Match existing style.

### Read Before You Write
Before adding code, read exports, immediate callers, and shared utilities. "Looks orthogonal" is dangerous. If unsure why code is structured a certain way, ask.

### Zoom Out When Unfamiliar
When unfamiliar with an area of code, or when the user asks how something fits into the bigger picture: go up a layer of abstraction and map all relevant modules and callers before answering.

### Goal-Driven Execution
Define success criteria before starting. Loop until verified. Don't follow steps blindly — define what done looks like and iterate toward it.

### Surface Conflicts, Don't Average Them
If two patterns contradict, pick one (more recent or more tested). Explain why. Flag the other for cleanup. Don't blend conflicting patterns.

### Tests Verify Intent
Tests must encode WHY behavior matters, not just WHAT it does. A test that can't fail when logic changes is wrong.

### Checkpoint After Significant Steps
Summarize what was done, what's verified, and what's left. Don't continue from a state you can't describe clearly.

### Match Codebase Conventions
Conformance over personal taste inside the codebase. If you think a convention is genuinely harmful, surface it — don't fork silently. Third-party folders (`KinematicCharacterController/`, `parkour/`, `3d platform tempalte/`) keep their own style — match the file you're editing.

### Fail Loud
"Completed" is wrong if anything was skipped silently. "Tests pass" is wrong if any were skipped. Default to surfacing uncertainty, not hiding it.

---

## Skills — Auto-Trigger Rules

Skills live in `.claude/skills/`. Users can invoke them explicitly via `/skill-name`, but you MUST also invoke them automatically (via the `Skill` tool) when the trigger conditions below are met — **before generating any other response**.

| Skill | Auto-trigger when... |
|---|---|
| `/unity-cli` | Any task needs the `unity` terminal command — installing/upgrading editors, licenses, auth, creating/opening/building/testing a project, browsing releases, or driving a live-connected Editor (create/edit GameObjects, scenes, assets, run C# via `eval`, recompiling and discovering custom `[CliCommand]` tools) instead of hand-editing scene/asset YAML. Run `unity status` before touching any `.unity`/`.prefab`/`.asset` file to check for a connected Editor first. |
| `/unity-custom-cli-commands` | A task needs a capability the connected Editor's built-in `unity command` catalog doesn't have — especially editing internal-only authoring data with no public C# API — or the user asks to script a repeated Editor operation as a reusable command. Covers where project `[CliCommand]` tools live, the shared-operations-layer pattern, the dry-run/confirm/backup safety convention, and how to reflect safely into a package's internal API. |
| `/diagnose` | User reports a bug, says something is broken/crashing/throwing, or describes a performance regression |
| `/tdd` | User asks to implement a new feature or fix a bug and hasn't provided a test yet |
| `/grill-with-docs` | User presents a design plan or asks "how should I implement X" for a non-trivial system |
| `/grill-me` | User explicitly says "grill me" or wants to stress-test a plan |
| `/improve-codebase-architecture` | User asks to refactor, improve architecture, or reduce coupling |
| `/write-a-skill` | User wants to create a new skill |
| `/localization` | User asks to add languages, translate UI text, support CJK (Chinese/Japanese/Korean) fonts, or otherwise mentions i18n/l10n/multilingual support |
| `/optimize-audio` | User wants to reduce Unity audio memory or CPU cost, fix Load Type / sample-rate / codec import settings, force 3D audio to mono, or cut AudioMixer CPU cost |
| `/optimize-text-mesh-pro` | User mentions TextMeshPro/TMP problems — font atlas bloat, CJK/fallback font setup, SDF quality, AutoSize discipline, or text rendering/memory issues |
| `/physics-3d-collision` | User reports 3D PhysX collision/trigger issues — `OnCollisionEnter`/`OnTriggerEnter` not firing, objects passing through each other, `Physics.Raycast` misses, ragdoll problems, or `AddForce` stopping after settling |
| `/ui` | User asks about Unity UI (menus, HUDs, panels, buttons, layout, styling) without specifying uGUI/UI Toolkit/IMGUI — this skill routes to the right one |
| `/ui-ugui` | Task references Canvas, uGUI, RectTransform, ScrollRect, or a `.prefab` that is UI — editing or generating Canvas-based hierarchies |
| `/ui-imgui` | Task touches existing `OnGUI`/`OnInspectorGUI` editor code, or the user explicitly asks for IMGUI/immediate-mode GUI |

**Important**: Check if a matching skill is already running before invoking it — never invoke a skill that's already active in the current conversation.

### Unity MCP — one-time setup (optional)

`unity command <name> --format json` through Bash always works, but every call pays subprocess
startup cost and has to be parsed out of shell output. If this coding tool supports MCP servers,
register the Unity CLI's built-in MCP server **once** so a connected Editor's commands show up as
native tools instead:

```bash
unity mcp configure claude-code            # global, or:
unity mcp configure claude-code --local    # project-local config instead
```

This only changes *how* the same commands are reached (native MCP tool call vs. `Bash` + `unity
command`) — it does not add capability by itself, and it still requires a connected Editor
(`unity status` / `unity command` / `unity list` remain the Bash fallback when no MCP client is
available). Re-run `unity mcp configure claude-code --yes` if the CLI is reinstalled or the config
drifts, and `unity skill refresh` after `unity upgrade` so the vendored `/unity-cli` skill docs
stay current.

The project-level `com.unity.pipeline` package (what lets the CLI drive a live Editor) is installed
in `Packages/manifest.json`. If `unity status` reports no Pipeline instances, re-add it with
`unity pipeline install`.

---
name: unity-custom-cli-commands
description: Author project-specific [CliCommand] tools to restore or extend what the old (deprecated) Unity MCP server could do, when the official unity CLI's built-in command set doesn't cover a task — especially editing internal-only authoring data (Behavior Graphs, other custom-editor assets) that has no public C# API. Use when a task needs a capability the connected Editor doesn't already expose via `unity command`, or when asked to "turn what the old MCP did into a skill/command."
---

# Authoring custom Unity CLI commands

The (now deprecated) standalone Unity MCP server ran its own bespoke code inside the Editor
process, so it could reach into internal/undocumented Editor state that the official `unity` CLI's
built-in [Pipeline](../unity-cli/SKILL.md) command set (`create_gameobject`, `save_scene`, ...)
doesn't cover. **`unity mcp` does not restore that on its own** — it only changes *transport*
(exposes whatever commands a connected Editor's Pipeline server has as MCP tools instead of plain
CLI subcommands). The commands available are the same either way. To get equivalent capability
back, **register the missing commands as project-side `[CliCommand]` tools** — no CLI update or
separate MCP-adapter layer needed, they show up under both `unity command`/`unity list` and
`unity mcp` automatically once compiled.

This mirrors the approach in ["I migrated 31 Unity MCP tools to the new Unity CLI"](https://impossiblerobert.medium.com/i-migrated-31-unity-mcp-tools-to-the-new-unity-cli-it-was-easier-than-i-expected-7df8d398ee4e):
a thin `[CliCommand]` adapter over a transport-agnostic "operations" layer, so the actual logic is
testable and reusable independent of how it's invoked.

## Where this lives

Put each tool group in its own Editor-only assembly, e.g.
`Assets/Editor/<Feature>Tools/` (or `Packages/<your-package>/Editor/<Feature> Tools Editor/` if the
project has a local package), with an asmdef that references `Unity.Pipeline` by name and the same
way other package assemblies are referenced in this project's existing `.asmdef`s:

```json
{
    "name": "<Project>.<Feature>Tools.Editor",
    "rootNamespace": "<Project>.Editor.<Feature>Tools",
    "references": ["Unity.Pipeline"],
    "includePlatforms": ["Editor"]
}
```

A "Behavior Graph Tools Editor" assembly built exactly on the pattern below is the worked example
referenced from [unity-behavior-trees/CLI-EDITING.md](../unity-behavior-trees/CLI-EDITING.md).

## The pattern: two layers, not three

The article's migration used three layers (shared ops → CLI adapter → legacy MCP adapter) because
it had to keep an existing MCP client working side-by-side with the new CLI. This project doesn't
have that constraint — `unity mcp` already forwards the CLI's own command catalog — so use **two**
layers:

1. **Operations layer** (plain C# static class, e.g. `FooAutomation.cs`) — the actual logic.
   Returns a small result type (success flag, human message, warnings list, optional data) rather
   than throwing on expected failure paths, so the adapter can format a clean response either way.
2. **`[CliCommand]` adapter** (e.g. `FooCliCommands.cs`) — thin wrapper: parse `[CliArg]`s, call
   the operations layer, format the result as a `string` return (the CLI wraps whatever you return
   into its `data` envelope field under `--format json`; returning a formatted string is the
   simplest reliable shape — see `MiniJson`/`Format` in the Behavior Graph tools for a
   no-external-dependency way to render nested result data as readable text).

```csharp
using Unity.Pipeline.Commands;   // [CliCommand] / [CliArg] — assembly: Unity.Pipeline

public static class FooCliCommands
{
    [CliCommand("foo_do_thing", "One-line description an agent will read to decide whether to call this.",
                Tags = new[] { "foo" })]
    public static string DoThing(
        [CliArg("target", "What to act on")] string target,
        [CliArg("dry_run", "Preview only, default true")] bool dryRun = true,
        [CliArg("confirm", "Must be true together with dry_run=false to actually write")] bool confirm = false)
        => Format(FooAutomation.DoThing(target, dryRun, confirm));
}
```

## Mandatory safety pattern for anything that mutates project data

Every command that writes to a scene, asset, or other project file must follow this — it's what
makes a custom command **safer than a blind file edit**, not just a different way to do one:

1. **`dry_run` defaults to `true`.** The command must be safe to call with no arguments beyond the
   dry-run preview — never require an explicit opt-out to preview.
2. **`confirm=true` required in addition to `dry_run=false`** to actually write. Two separate flags,
   both required, so a copy-pasted command from a dry-run example can't accidentally apply.
3. **Back up the target file before writing**, outside `Assets/`/`Packages/` so Unity never tries to
   import the backup itself (e.g. `<project root>/ClaudeToolBackups/<tool>/`).
4. **Write through the owning system's real API** (`GraphAsset.SaveAsset()`, `AssetDatabase`,
   `EditorSceneManager`, ...) — never hand-write the serialized format, even when reflecting into
   internal types to get there. Reflection into a real API is a supported (if fragile) automation
   technique; hand-editing YAML/serialized binary is not.
5. **Verify after writing**: reimport/reload and confirm the asset still loads, reporting a
   before/after count or diff so the caller doesn't need a manual round-trip to sanity-check.
6. **Still recommend a human glance at the Console** before closing the Editor, at least until the
   command has a track record — say so in the command's result message.
7. **Delete the backups this session created once the change is verified working.** The backup
   directory (e.g. `ClaudeToolBackups/<tool>/`) is a safety net for the edit *sequence* you're
   currently running, not permanent storage — it's `.gitignore`d for exactly this reason, so nothing
   catches it if cleanup is skipped. A multi-step edit (several `bt_*` calls, or several `eval`
   writes) leaves one backup per mutating call; once the final result reimports clean and the
   Console is clear, remove that session's backups (`rm` the specific files, or the whole tool's
   backup subfolder if nothing else is relying on it) as the last step of the task — don't leave
   them for the user to notice and clean up later. If something DID go wrong and a backup is the
   only way back, say so explicitly and leave that one in place; only delete backups you've
   confirmed are no longer needed.

## Reflection into internal Editor/package APIs

When the capability you need lives on an `internal` class (common in Unity's own packages —
Behavior, Timeline, etc. all keep their authoring/graph internals `internal`), a `[CliCommand]`
running inside the Editor process can still reach it via reflection: access modifiers only gate
*compile-time* C# code, not `System.Reflection`. Rules of thumb, established while building the
Behavior Graph tools:

- **Read the package source first** (`Library/PackageCache/<package>@<hash>/`) — confirm the exact
  type/method/field names and whether a member is `public` (reachable with `BindingFlags.Public`)
  or fully `internal`/`private` (needs `BindingFlags.NonPublic` too). Don't guess signatures.
- **Prefer calling the same method the UI calls**, not reimplementing its effect by poking fields
  directly — the UI's command handlers are usually thin wrappers around one or two model methods
  (e.g. `Asset.CreateNode(...)`, `Asset.ConnectEdge(...)`) that already do the real
  serialization/validation work. Find the handler class the UI dispatches to and read what it
  actually calls before writing the reflection code.
- **Fail loud, never silently no-op.** A generic reflection helper (see `BehaviorReflection.cs`)
  that throws a clear "member X not found on type Y, package layout may have changed" beats a
  `TryGetValue`-style helper that swallows the miss — an agent (or a person) needs to know
  immediately when the target package's internals have shifted, not discover it as a silent no-op
  days later.
- **Record the package version this was verified against** (a constant + a comment) and re-verify
  after any upgrade of that package.
- **This is genuinely coupled to internal API and can break on a minor package update** — it is a
  reasonable trade for capability the package doesn't expose any other way, not a first choice when
  a public API exists.
- **Prove each risky reflection call live with `unity command eval` before writing it into a
  permanent `[CliCommand]`**, whenever a connected Editor is available. `eval` runs a snippet against
  the warm, already-loaded Editor in ~200–600 ms with the real exception on failure — far cheaper
  than a recompile-and-retest cycle, and the difference between shipping a tool that works the first
  time and one that needs a live debugging pass after the fact. See
  [unity-behavior-trees/CLI-EDITING.md](../unity-behavior-trees/CLI-EDITING.md#prototyping-against-a-live-editor-before-writing-a-permanent-command)
  for what this cost when it was skipped (three bugs found only during the first real run, on the
  Behavior Graph tools). Only write speculative, unverified reflection code when no Editor is
  reachable at all — and say plainly that it's unverified when you do.

## After adding or changing a command

```bash
unity command recompile
unity command recompile_status   # poll until "completed"
unity list --tag <your-tag>      # confirm it registered with the expected signature
```

Then smoke-test read-only commands first, mutating commands dry-run first, and — for anything that
touches an asset that matters — a throwaway duplicate before the real one.

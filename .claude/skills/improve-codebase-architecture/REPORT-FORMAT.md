# Architecture Review Report Format

Output the review as Markdown directly in the conversation. No external files, no CDN dependencies — Claude Code renders it inline.

## Structure

```md
# Architecture Review — {Project Name}

## Candidates

### {N}. {Short title — names the deepening}

**Strength**: Strong | Worth exploring | Speculative
**Files**: `path/to/File.cs`, `path/to/OtherFile.cs`

**Problem**: One sentence. What hurts.
**Solution**: One sentence. What changes.

**Before**
\```
[Module A] → [Module B] → [Module C]
                              ↓ leaks into
                          [SaveService]
\```

**After**
\```
[Module A] → [Deep Module]
                (Module B + C internalized)
                (SaveService behind interface)
\```

**Wins**
- Locality: save logic concentrated in one place
- Leverage: one interface, N call sites
- Play Mode tests hit one seam instead of three

---

## Top Recommendation

**{Candidate title}** — {one sentence on why this one first}.
```

## Rules

- Diagrams are ASCII box-and-arrow. No Mermaid, no external tools.
- Before/after side by side when width allows; stacked when not.
- Prose is sparse. Bullet points over paragraphs.
- Use [LANGUAGE.md](LANGUAGE.md) vocabulary for architecture terms and `CONTEXT.md` vocabulary for domain terms.
- Wins bullets name the gain in glossary terms: *"locality: bugs concentrate in one module"*, *"leverage: one interface, N call sites"*. Not *"cleaner code"* or *"easier to maintain"*.
- After presenting all candidates, ask: "Which of these would you like to explore?"

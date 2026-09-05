# CONTEXT.md Format

## Structure

```md
# {Project Name}

{One or two sentence description of what this game is and why it exists.}

## Language

**Stage**:
{A one or two sentence description of the term}
_Avoid_: Level, round, map

**Resource**:
A collectible item the player accumulates and spends to progress.
_Avoid_: Currency, item, loot

**Animal**:
A farmable entity the player owns, feeds, and harvests for resources.
_Avoid_: Pet, creature, unit
```

## Rules

- **Be opinionated.** When multiple words exist for the same concept, pick the best one and list the others under `_Avoid_`.
- **Keep definitions tight.** One or two sentences max. Define what it IS, not what it does.
- **Only include terms specific to this project's context.** General programming concepts (MonoBehaviour, ScriptableObject, coroutine) don't belong even if the project uses them extensively. Before adding a term, ask: is this a concept unique to this game's design, or a general Unity/programming concept? Only the former belongs.
- **Group terms under subheadings** when natural clusters emerge (e.g. Gameplay, Economy, Progression). If all terms belong to a single cohesive area, a flat list is fine.

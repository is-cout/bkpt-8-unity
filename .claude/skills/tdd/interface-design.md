# Interface Design for Testability

Good C# interfaces make testing natural:

**1. Accept dependencies, don't create them**

```csharp
// Testable — inject the dependency
public sealed class StageCompleteHandler
{
    private readonly IRewardService rewards;

    public StageCompleteHandler(IRewardService rewards)
    {
        this.rewards = rewards;
    }
}

// Hard to test — creates its own dependency
public sealed class StageCompleteHandler
{
    public void Handle()
    {
        var rewards = ServiceLocator.Get<IRewardService>(); // not injectable
    }
}
```

**2. Return results, don't produce hidden side effects**

```csharp
// Testable — result is observable
public RewardData CalculateReward(StageResult result) { ... }

// Hard to test — side effect is invisible from the outside
public void ApplyReward(StageResult result)
{
    _inventory.AddItem("gold", result.Score / 100); // where did the gold go?
}
```

**3. Small surface area**

- Fewer methods = fewer tests needed
- Fewer params = simpler test setup
- If the constructor needs 5+ dependencies, the class is doing too much

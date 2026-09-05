# When to Mock

Mock at **system boundaries** only:

- External SDKs you don't control (Unity IAP, Firebase, AppsFlyer)
- Service-layer boundaries when testing logic that depends on them (a save service, an inventory service)
- Time/randomness (`UnityEngine.Time`, `Random`)

Don't mock:

- Your own classes/modules
- Internal collaborators
- Anything you control

## Designing for Mockability

At system boundaries, extract a C# interface and inject it:

**1. Use dependency injection via constructor or ServiceLocator**

```csharp
// Easy to test
public sealed class RewardHandler
{
    private readonly IInventoryService inventory;

    public RewardHandler(IInventoryService inventory)
    {
        this.inventory = inventory;
    }

    public void GiveReward(RewardData reward)
    {
        inventory.AddItem(reward.ItemId, reward.Amount);
    }
}

// Hard to test — tight coupling to concrete service
public sealed class RewardHandler
{
    public void GiveReward(RewardData reward)
    {
        InventoryService.Instance.AddItem(reward.ItemId, reward.Amount);
    }
}
```

**2. Prefer specific interfaces over generic ones**

```csharp
// GOOD: Each method is independently mockable, returns a specific shape
public interface IInventoryService
{
    void AddItem(string itemId, int amount);
    int GetItemCount(string itemId);
}

// BAD: Generic Execute forces conditional logic inside the mock
public interface IInventoryService
{
    object Execute(string command, params object[] args);
}
```

## In-memory fakes over Moq

For Unity, prefer hand-written in-memory fakes over mocking frameworks — they're simpler in Play Mode tests and don't require additional packages:

```csharp
public sealed class FakeInventoryService : IInventoryService
{
    private readonly Dictionary<string, int> items = new();

    public void AddItem(string itemId, int amount)
        => items[itemId] = GetItemCount(itemId) + amount;

    public int GetItemCount(string itemId)
        => items.TryGetValue(itemId, out var count) ? count : 0;
}
```

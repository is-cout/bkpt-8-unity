# Good and Bad Tests

Use **Unity Test Framework** (NUnit) for both EditMode and PlayMode tests.

## Good Tests

**Integration-style**: test through real interfaces, not mocks of internal parts.

```csharp
// GOOD: Tests observable behavior through the public interface
[Test]
public void Player_ReceivesReward_AfterStageComplete()
{
    var inventory = new FakeInventoryService();
    var rewardHandler = new RewardHandler(inventory);

    rewardHandler.GiveReward(new RewardData("gold_coin", 10));

    Assert.AreEqual(10, inventory.GetItemCount("gold_coin"));
}
```

Characteristics:

- Tests behavior callers care about
- Uses public API only
- Survives internal refactors
- Describes WHAT the system does, not HOW
- One logical assertion per test

## Bad Tests

**Implementation-detail tests**: coupled to internal structure.

```csharp
// BAD: Tests implementation — breaks on refactor even if behavior is unchanged
[Test]
public void RewardHandler_CallsInventoryAddItem()
{
    var mockInventory = Substitute.For<IInventoryService>(); // NSubstitute
    var handler = new RewardHandler(mockInventory);

    handler.GiveReward(new RewardData("gold_coin", 10));

    mockInventory.Received(1).AddItem("gold_coin", 10); // verifying HOW, not WHAT
}
```

Red flags:

- Asserting on call counts or invocation order
- Mocking internal collaborators (classes you own)
- Test name describes HOW not WHAT
- Test breaks when you rename an internal method without changing behavior

## EditMode vs PlayMode

- **EditMode**: pure C# logic, no MonoBehaviour lifecycle needed. Fastest. Prefer these.
- **PlayMode**: requires Unity lifecycle (Awake/Start/Update). Use when the bug lives in lifecycle interactions, coroutines, or physics.

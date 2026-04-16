# Feature Spec: Economy — Egg Production & Baker Contract (L4-001 MVP)

## Summary

Each morning the player's single chicken automatically produces 3 eggs that
land in the inventory alongside the usual mail delivery. On any given day there
is an equal, fully random chance the baker walks onto the farm and offers a
contract: deliver a random number of eggs (3–12) before a randomly chosen
deadline (9 am–midnight game time) in exchange for Chicken Coins. Fulfilling
the contract removes the eggs from the inventory and credits the wallet.
Ignoring or missing the deadline causes the contract to expire with no reward.
Coins accumulate in a wallet but have nothing to spend on yet — the spend side
is a future sprint.

## User Story

As a farmer, I want my chicken to produce eggs each morning and be able to
fulfill the baker's delivery requests so that I earn Chicken Coins and feel
like my farm is part of a living economy.

## Acceptance Criteria

### Egg Production
- [ ] Exactly 3 eggs are added to the inventory every morning when the new day begins
- [ ] Eggs are stored under item id `"egg"` in the existing `InventorySystem`
- [ ] `"egg"` exists in `ItemDatabase.CreateStarterDatabase()` with category `Produce`, max stack 24, sell value 5
- [ ] If inventory is full, eggs that cannot fit are discarded (no crash, no silent hang)

### Baker Visit
- [ ] Each new day, the baker visit is decided by a single fair coin flip (50 % chance)
- [ ] The coin flip uses an injected `System.Random` so tests can control it deterministically
- [ ] On a visit day, `MerchantService` generates exactly one `MerchantContract` for the baker
- [ ] On a non-visit day, no contract is created and the baker does not appear on the farm

### Contract Generation
- [ ] Required egg quantity is a random integer in the inclusive range [3, 12]
- [ ] Reward in Chicken Coins = `RequiredQuantity * 5` (5 coins per egg, always)
- [ ] Contract expiry is a random game-time hour in [9, 24) — 9 am up to (not including) midnight
- [ ] `ContractStatus` starts as `Pending`

### Fulfillment
- [ ] `MerchantService.FulfillContract()` succeeds only when `InventorySystem` contains ≥ `RequiredQuantity` eggs and `ContractStatus == Pending`
- [ ] On success: eggs are removed from inventory, coins are added to `WalletService`, status → `Fulfilled`
- [ ] On failure (not enough eggs, or wrong status): no inventory change, no coin change, returns descriptive failure result

### Expiry
- [ ] `MerchantService.ExpireOverdueContracts(float currentGameHour)` marks any `Pending` contract whose `ExpiresAtHour ≤ currentGameHour` as `Expired`
- [ ] Expired contracts cannot be fulfilled

### Wallet
- [ ] `WalletService` tracks a non-negative integer `Balance`
- [ ] `AddCoins(int amount)` throws `ArgumentOutOfRangeException` for amount ≤ 0
- [ ] `SpendCoins(int amount)` returns `false` and makes no change when balance is insufficient
- [ ] `OnBalanceChanged` event fires after every successful add or spend

## VR Interaction Model

*MonoBehaviour wiring is a follow-on task. This spec covers Core/ logic only.*

- Baker NPC walks to farm entrance on visit days (future: `BakerVisitController`)
- Player approaches baker → dialogue shows contract offer (future: `BakerNPCInteraction`)
- Fulfillment triggered by UI confirmation or physical hand-off gesture (future sprint)

## Edge Cases

- Inventory full when eggs are produced → add as many as fit, silently drop the rest
- Baker visits but player has 0 eggs and contract requires 3 → contract stays Pending until expiry
- `FulfillContract` called twice on the same contract → second call fails (status already `Fulfilled`)
- `ExpireOverdueContracts` called with no active contracts → no-op, no exception
- `RequiredQuantity` of 12 means the player needs a 4-day stockpile — this is intentional

## Performance Impact

- All new systems are pure C# with no Unity dependencies — zero frame-time impact
- No Quest budget implications at this stage

## Dependencies

### Existing systems this builds on
- `InventorySystem` / `IInventorySystem` (`FarmSimVR.Core.Inventory`)
- `ItemDatabase` — adding `"egg"` item + new `Produce` category
- `GameEventBus` — `MorningPhaseStarted` event for triggering egg production (event struct to be defined)

### New systems this introduces
- `EggService` (`Core/Economy/`)
- `WalletService` (`Core/Economy/`)
- `MerchantContract` + `ContractStatus` (`Core/Economy/`)
- `MerchantService` (`Core/Economy/`)

## File Map

```
Assets/_Project/Scripts/Core/
  Inventory/
    ItemCategory.cs          ← add Produce value
    ItemDatabase.cs          ← add "egg" to CreateStarterDatabase()
  Economy/
    EggService.cs            ← new
    WalletService.cs         ← new
    ContractStatus.cs        ← new enum
    MerchantContract.cs      ← new
    FulfillResult.cs         ← new (mirrors AddResult / RemoveResult pattern)
    MerchantService.cs       ← new

Tests/EditMode/
  Economy/
    EggServiceTests.cs       ← new
    WalletServiceTests.cs    ← new
    MerchantServiceTests.cs  ← new
```

## Out of Scope

- Baker selling anything to the player
- Coin spending of any kind
- Multiple chickens (hardcoded to 1 chicken = 3 eggs/morning)
- Baker NPC MonoBehaviour, visit animation, dialogue UI
- `BakerVisitController` scene wiring
- Any second merchant
- Save/load of wallet balance or contract state

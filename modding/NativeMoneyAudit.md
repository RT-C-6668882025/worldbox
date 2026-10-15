# Native money integration audit — 2026-10-15

Scope: the managed assemblies embedded in the installed-compatible Alpha 3.1 APK, compared with the pinned pristine source APK used by `build.py`. Findings below come from actual IL bodies and call sites, not only class names. No Android runtime is available here.

## Native flows confirmed

- Work, delivery, construction, roads, mining, farming, ruins and housing can award `Actor.addLoot`. This is pending income, distinct from `ActorData.money`.
- `BehActorGiveTax.payTaxToLeader`: split pending income using the kingdom's local tax rate; actor receives the remainder as money, leader receives tax as pending income; clear the actor's pending income.
- `payTributeToKing`: give tribute to the king as pending income, put half the remaining amount (integer truncation) into the city gold stockpile, credit the rest to the leader personally; clear pending income.
- Kings, and residents without a leader, settle their pending income using `takeAllOwnLoot`.
- Kingdom tax rates are recalculated from native base settings and kingdom traits. The extension does not replace these rates or tax distribution.
- `addMoney` is a generic credit operation used by settlement and gifts, not a salary event. Stolen/killed actors' money can also become loot and later be settled.
- `getMoneyForGift` computes an amount but does not debit the donor in the inspected native flow. The adapter now explicitly transfers that money.
- Actual food, crafting, equipment repair and fireworks call `spendMoney`; plots charge at normal creation but can intentionally skip payment when forced.
- Native reading rewards are applied by `BehFinishReading.execute`, not `Book.readIt` (which only records a timestamp). Completed reading can affect actor attributes, traits, experience and culture/language/religion membership; ruler reading can transmit group traits.

## Changes

1. Remove the global salary interception from `Actor.addMoney`. Keep settlement, taxes and existing money transfers on their native paths.
2. Fund only newly earned production income at ten verified producer call sites. Debit payroll escrow, business funds and then public funds before crediting native pending income. The public share uses gold only; GOD must be exchanged manually by the user first. Tax/loot transfer call sites are not re-funded.
3. Remove the global spending interception. Route verified commercial purchases to business, and normal plot fees to a separate political wallet. Charge no more than the actor actually holds.
4. Debit the gift donor once, with recipient capacity checked before debit. Do not charge city funds for gifts.
5. Remove synthetic annual resident consumption, model sales, 55% model wages and the model's fixed 15% commercial tax. Retain native taxes and actual purchase events.
6. Allocate education/military budgets separately; unused budgets are not allocated again. Pay education rewards on completed native reading and military pay to actual warriors (full annual census in the lifecycle extension). Actual production/practice supports pre-literate knowledge growth; idle sampling does not grant research.
7. Preserve legacy payroll/business balances and initialize additional wallet fields when older JSON is loaded. Keep the existing save key.
8. Replace the old 99,999 money/loot clamp with saturation at the native integer representation limit.

## Limits

- Education, military, political and payroll wallets are adapter accounting, not pre-existing native government departments.
- The current lifecycle adapter replaces sampling with a full annual census in bounded batches and tracks per-actor payroll years. Monetary statistics are the most recently completed census, not a live global total.
- Native gold-resource production and explicit player powers remain native. This is not a claim that every possible gold creation path in the game has been removed.
- See STATUS.md and RequirementsCoverage.md: the lifecycle extension removes phantom annual output, adds material-funded industrial chains and six funded building appearances. Enterprise organization remains a city-sector model.
- Tests cover the accounting adapter and compiled IL routing. Installation, Unity startup, real save loading and UI rendering still need device validation for this build.

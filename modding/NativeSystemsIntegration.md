# Native systems integration

Built from the pinned pristine ARM64 APK, retaining native UI, save structures, rules and artwork. The installer compatibility settings and test signing certificate are unchanged.

| System | Native entry points | Adapter behaviour |
| --- | --- | --- |
| Jobs | Ten native production income events; Actor.getConstructionSpeed | Fund real earnings, record occupation, apply production skill to funded pay and construction speed; no second salary on settlement. |
| Processing | CityBehProduceResources.tryToProduce | Keep native ingredient checks/deductions; require liquidity for the processing fee, credit the commercial account after accepted output, restore ingredients if output/fee settlement fails. |
| Logistics and trade | CityBehSupplyKingdomCities.shareResource; BehBoatMakeTrade.execute | Move only accepted goods, refund rejected goods and reverse unpaid deliveries; domestic redistribution pays freight, foreign trade pays for goods and freight. The dock no longer creates five inventory coins without a payer. |
| Succession | SuccessionTool.getKingFromRoyalClan/getKingFromLeaders; Kingdom.setKing | Preserve the native eligible royal-clan or leader candidate sets and native preferred-candidate advantage. Governance skill and native stewardship can change ranking. Record actual succession and add bounded governance/relationship loyalty effects. |
| Alliances | Alliance.update/leave/dissolve | Annual contributions are capped at 1% of member capital wealth and population needs; fund poorer/at-war capitals from existing contributions. Exit refunds are proportional to the remaining pool. Refunds blocked by treasury capacity remain owned world-save credits and are retried annually. |
| Equipment | Native crafting/repair plus Item.getDamaged/reforge | Preserve native material/coin costs and era checks, route actual repair payments once, record completed repair/reforge, and bound skill-based wear reduction. Divine editor operations remain native. |
| Ownership | Item.setUnitHasIt/setInCityStorage | Save a bounded eight-event owner/storage history plus transfer counters in item custom data; retain original creator, origin and kill history. Loot is not converted into an involuntary purchase. |
| Family and relationships | Native parents, family, lover, best-friend links; existing death inheritance | Retain wealth inheritance; allow a small one-time production apprenticeship seed from parents. Related leaders affect trade price and loyalty without minting a rebate. |
| Genetics and population | Native construction speed, intelligence, adult counts; Subspecies.genesChangedEvent | Preserve genetic construction contributions, apply intelligence to real learning, exclude children from industrial workforce, record genome edits and show native generation/metabolism/food need. Does not replace native breeding or genome simulation. |
| World laws | Existing diplomacy and hunger settings | Block cross-kingdom trade and alliance contributions/aid while diplomacy is off; food exports preserve a population reserve when hunger is on. Native eligibility, wars and other world-law checks remain in their original systems. |

## Accounting and execution boundaries

- Existing public, commercial, payroll, education, military and political balances retain the previous save key. GOD can fund integer native public costs by conversion at the current rate.
- Native domestic transfer eligibility and dock destination selection are retained. Foreign trade is disallowed between enemies; the adapter does not create new roads, ships or diplomacy targets.
- Dock settlement is limited to one successful destination per boat per world year; a failed or blocked delivery does not mark the destination as traded.
- Alliance accounting runs once per alliance/year. A dissolved alliance's genuinely unclaimed residual is held in world custom data and can fund a future live alliance; a named exiting member's blocked refund stays separate.
- Extra read-only rows use native city, kingdom, alliance, item and subspecies windows. They are not separate overlays or replacement editors.
- Military payments and resident statistics now process the full annual census in resumable batches of at most 64 residents. Prices, contributions and economic development are explicit adapter policies, not claims that the original game already implements a complete market economy.
- Industrial chains, funded native-building upgrades and six pixel appearances are provided by NativeLifecycle and IndustrialAppearance; see STATUS.md for the current model boundaries.

## Validation

Real game assemblies are used for compilation. Structural IL checks verify each native entry point, native tax preservation, removal of unfunded dock gold, and stack consistency. Transaction-policy tests exercise rejected capacity, partial delivery, insufficient payment and rollback. Accounting adapter tests cover existing saves, GOD liquidity, recipient capacity and exclusion of children from workforce. These are not Android/Unity runtime tests; the new build still requires device installation, startup, save and UI checks.

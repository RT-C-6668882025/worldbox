> 历史 API 核查记录。当前规则已改为全装备/个人特质解锁、发展只支付金币；早期材料时代门槛和 GOD 自动支付描述已被 STATUS.md 的最新规则取代。

# Native progression integration (verified original managed DLL)

`NativeProgression.cs` keeps individual XP in native `BaseSystemData` custom integer fields and reads Actor's internal data using cached reflection. Roles: production 0, commerce 1, research 2, governance 3, combat 4 (steam). Threshold is 25 × level², bounded by the integer XP representation; five completed work points give one native XP. It does not award idle XP. The runtime adapter additionally integrates commerce/education/governance and native kill events.

## Precise IL hooks

1. `bool ItemCrafting.craftItem(Actor,string,EquipmentType,int,City)`: `Actor` is arg0. The string arg1 is **creator name**, not weapon ID. Insert actor+bool `NativeProgression.AfterCraft` on each return without consuming original bool result. Only true earns production XP. The original method selects equipment, creates it, spends native actor money and `City.takeResource(string,int)` materials, then returns true.
2. `EquipmentAsset ItemCrafting.getItemAssetToCraft(Actor,List<EquipmentAsset>,City,int,bool)`: loop reads each candidate, checks equipment value, then calls `hasEnoughResourcesToCraft(Actor,EquipmentAsset,City)`. After the original availability result is true, AND with `AllowEquipment(actor,candidate,city)` or prepend gating within `hasEnoughResourcesToCraft`. Candidate material is public `ItemAsset.material`.
3. `craftItem` argument3 crafting skill may be transformed with `CraftSkill(arg0,arg3)` at entry. Original `tryToCraftRandomWeapon` already adds culture `weaponsmith_mastery`; preserve this.
4. `Actor.giveInventoryResourcesToCity()` calls `City.addResourcesToRandomStockpile(string,int)` per resource and discards returned accepted amount. Call `AfterDelivery(actor,accepted)` on that result. Unconditional end hook is incorrect: method empties inventory even failed delivery.
5. Native reading/combat XP already exists; add role work only after actual successful read/hit if those events are integrated later.

## Actual assets and era indexing

`ItemLibrary.initWeaponsSwords/initWeaponsBows` IL confirms `sword_wood`, `sword_stone`, `sword_copper`, `sword_bronze`, `sword_silver`, `sword_iron`, `sword_steel`, `sword_mythril`, `sword_adamantine` and equivalent bow materials. Resource costs differ: copper/bronze/iron/steel consume `common_metals`, **not iron resource**. Material-based gating avoids that ambiguity. Helper assumes parent `GetEra` returns primitive 0, stone 1, agrarian 2, metal 3, steam 4, industrial 5 and later values. Wood/stone allowed at 1; metals at 3; refined/fantasy metals at 4 (steam). Unknown original material IDs stay native. No free equipment generation. No verified steam/electric visual assets.

## Existing family/culture to reuse

Native `Family.newFamily(Actor,Actor,WorldTile)`, `FamilyManager.newFamily(Actor,WorldTile,Actor)`, `Actor.getParents()`, `Actor.getChildren(bool)`; actor parent IDs, family, ancestor_family and generation persist ancestry. Family UI is already native (`FamilyWindow`, `FamilyMembersContainer`, `FamilyOriginElement`, `UnitFamilyOriginElement`). `ActorData.money` is native personal wealth. Family treasury/inheritance is additional logic, not already provided by Family.

Culture uses traits/books (`Culture.books`, `CultureData.saved_traits`), not a direct research-era API. Native IDs include `roads`, `architecture`, `weaponsmith_mastery`, `fast_learners`, `reading_lovers`, `attentive_readers`, `training_potential`. Reuse native trait interfaces rather than inventing obsolete culture level fields.

## Limits and politics

Prefer named simulation limits: `Actor.getMaxPossibleLevel()`, `Family.isFull()`, `Actor.hasReachedOffspringLimit()`, `City.getLimitOfBuildingsType(BuildOrder)`, population world-law gates. Preserve tile/pathfinding/array bounds and storage consistency. Do not remove all conditions indiscriminately.

`War.getDuration/getYearStarted/getDeadAttackers/getDeadDefenders` plus side money/population/warriors counts are native escalation inputs. `City.getLoyalty(bool)` is cached; penalties at every lookup would compound incorrectly. `Actor.changeHappiness(string,int)` supports keyed native reasons.

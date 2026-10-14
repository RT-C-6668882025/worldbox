# WorldBox rebellion prototype — 2026-10-14

Source APK SHA-256: c39817efa21e539fe61f318dc1ad4050b424c6120ab8ed384d94148f869c4bdc.
Unity Mono, ARM64 only. Assemblies are in assets/Mono/Managed.zip.
Assembly-CSharp.dll has 2,908 types and 21,572 methods. No original game binaries or decompiled game source are committed.

## Implemented prototype
- Rebel manpower slots: floor(adult population * 0.92), within the requested 90–95% range before integer rounding. Original age/culture eligibility and recruitment pacing remain in effect; this is a target/slot limit, not instant forced recruitment.
- Rebellion defeat: attacker-side total population <= 5 triggers original War.lostWar(main attacker). This currently counts the whole rebel side, not the initial city's population.
- Capture of an enemy capital by an active rebel kingdom: the captured city becomes the rebel kingdom's capital after the original ownership transfer.
- Ordinary wars and kingdoms retain original rules.

Patched methods: City.updateCityStatus, CityBehCheckCitizenTasks.getPossibleWarriors, War.update, City.finishCapture.

## Validation
- Helper compiles against actual original managed assemblies (Unity Mono/netstandard).
- Rewritten method bodies parsed independently using dnfile/dncil; each expected helper call found.
- 9 isolated helper tests passed; these use stand-in game objects, not a Unity runtime.
- APK v1/v2/v3 signature verification passed with apksig 8.2.2.
- Stored native libraries aligned to 16 KiB.
- No Android emulator or physical-device gameplay test was performed. Boot/loading and game effects remain unverified.
- Test signing differs from the original signing key, so ordinary overwrite installation is not supported. Back up saves before any uninstall.

## Not implemented
Economic cycle, currency/exchange controls, graded war intensity, economy UI, and performance optimization. ARM64 presence does not establish performance improvement.

## Tool evidence
REA 6.4.0 inspect-managed-artifact confirms unity-mono/CIL/anycpu. Full member inspection exceeded REA's 64 MiB representation budget; dnfile/dncil provided focused IL inspection. reverse-skill APK/Mono routing supplied the workflow.

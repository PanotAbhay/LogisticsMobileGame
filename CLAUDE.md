# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

"Empire" is an early-stage Unity **2022.3.62f3** 2D project (`com.unity.feature.2d`, uGUI + TextMeshPro, legacy Input Manager) that **targets Android phones**. The Android Build Support module, with its SDK, NDK, and OpenJDK, is installed for this editor. It's a business sim: a student sells candy at school and balances price, demand, stock, and the risk of getting caught.

- **Game code:** `Assets/Scripts/` (four MonoBehaviours), compiled into the `Empire` assembly (`Assets/Scripts/Empire.asmdef`, which references `Unity.TextMeshPro` and `UnityEngine.UI`). If a script needs another package assembly, add it to that asmdef's `references`.
- **Tests:** `Assets/Tests/EditMode/`, in the `Empire.Tests.EditMode` assembly.
- **Git:** the default branch is `main`, and the remote is `origin` = `https://github.com/PanotAbhay/LogisticsMobileGame`. The repo name predates this game idea. `.gitignore` is the standard Unity one.

## Commands

**Fast compile check (no editor or license needed):**
```sh
dotnet build Empire.csproj -nologo -v:q -p:OutputPath=<a dir outside the project>/
```
- Unity generates one csproj per assembly (`Empire.csproj`, `Empire.Tests.EditMode.csproj`), and each lists its `.cs` files explicitly.
- The csproj files are regenerated when the editor imports changes (on open or focus) or via Preferences > External Tools > Regenerate project files. Until then, a new script or asmdef isn't reflected in them.
- Don't edit the csproj or sln files by hand.

**Unity batch mode** is the authoritative compile, import, and test. The user's Unity license is active (Unity Hub signed in).
```sh
"C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" -batchmode -nographics -projectPath <project> -logFile <log> <args>
```
- **When the editor is open:** batch mode fails on `C:/Unity/Empire` while the project is open in the editor, which is the usual case. Commit first, then `git clone C:/Unity/Empire <scratch dir>` and point `-projectPath` at the clone. A fresh clone takes a few minutes to import the first time. This also proves the committed repo is complete.
- **Tests:** pass `-runTests -testPlatform EditMode -testResults <file.xml>`, without `-quit`. To run a single test, add `-testFilter "EconomyManagerTests.Restock_FillsBackpackToCapacity"`. Results are NUnit XML; check the `result`, `passed`, and `failed` attributes on `<test-run>`.
- **Other runs:** for plain compile and import, use `-quit` instead.
- **In the editor:** use Window > General > Test Runner > EditMode.

## Architecture

- **The playable scene is `Assets/Scenes/MainGame.unity`.** Build Settings list only `SampleScene.unity`, which is an empty scene with just a camera. `Assets/Prefabs`, `Resources`, `ScriptableObjects`, and `Scenes 1` are empty placeholders.
- **Wiring:** each manager is a component on its own GameObject in MainGame. They reference each other through public Inspector fields assigned in the scene. There are no singletons, C# events, or ScriptableObjects. `UIManager` falls back to `FindObjectOfType` when its references are unset.
- **Tick loop:** `GameManager.Update` accumulates `Time.deltaTime`. Every `weekLength` seconds (5) it runs `RunWeek()`, which:
  1. calls `EconomyManager.RunSchoolDay()`,
  2. increments `currentWeek`,
  3. auto-restocks to full if stock hits 0,
  4. calls `UIManager.RefreshUI()`.

  The naming is inconsistent: one "week" tick simulates one "school day". `EventManager.ProcessMonthlyEvents()` is a stub that nothing calls yet.
- **`EconomyManager` holds all game state and rules** as public serialized fields, grouped by `[Header]`: cash/pricing, backpack stock, market demand, social stats, risk, and loyal customers. `RunSchoolDay()` runs these steps in order:
  1. Compute demand from a price-elasticity curve, clamped to backpack capacity.
  2. Serve loyal customers first; they buy multiple units and pay a shortage premium when they empty the stock.
  3. Serve regular students; each may become loyal or raise `riskLevel` via a snitch roll, which popularity and gossip reduce.
  4. Settle cash.
  5. Check confiscation: at the risk threshold, stock is wiped, a fine is charged, and risk resets.

  It also records the `last*` stats for the UI.
- **UI is pull-based.** `UIManager.RefreshUI()` rebuilds the dashboard string from `EconomyManager`/`GameManager` fields. Any new code path that changes economy state outside `RunWeek()` must call `RefreshUI()` itself, or the display goes stale. The price slider (range 0–5, set in the scene) writes `pricePerCandy` directly.

## Gotchas

- **Scene values override C# field initializers.** Changing a default in code does not change the existing component in MainGame. For example, `priceElasticity` is 1.2 in the scene but 1.3 in code. Fields added after the scene was last saved (popularity, gossip, snitch, and loyal-customer fields) use code defaults until the scene is re-saved, and a removed field (`riskIncreasePerUnit`) is still serialized in the scene. When renaming a serialized field, add `[FormerlySerializedAs("old")]` so scene data isn't lost.
- **`UIManager.cs` is Latin-1/Windows-1252 encoded.** The `£` currency symbol is the single byte `0xA3` and appears as `�` in tools. A naive edit can permanently replace it with U+FFFD. If you edit this file, rewrite it as UTF-8 with a literal `£`. The other scripts use CRLF, and `EconomyManager.cs` is UTF-8 with a BOM.
- **Scenes reference scripts by the GUID in their `.cs.meta` file.** Never delete or regenerate `.meta` files, and move or rename scripts together with their `.meta`. A MonoBehaviour's class name must match its file name. Every new asset and folder needs its `.meta` committed alongside it. Unity only writes metas when it imports, so if the editor is closed or unfocused, write one yourself: use a fresh GUID (`[guid]::NewGuid().ToString('N')`) and copy the format of an existing meta of the same type (folder, `.cs`, `.asmdef`).
- **EditMode tests build components with `AddComponent`, and `Start()` doesn't run.** State comes from the C# field initializers, not from the MainGame scene values, which differ (see above). Seed `Random.InitState` in tests, because `RunSchoolDay()` uses `Random.value`.
- `Library/`, `obj/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, and `*.sln` are generated. Don't edit them.

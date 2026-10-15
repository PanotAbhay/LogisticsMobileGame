# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

"Empire" is an early-stage Unity **2022.3.62f3** 2D project (`com.unity.feature.2d`, uGUI + TextMeshPro, legacy Input Manager) that **targets Android phones**. The Android Build Support module, with its SDK, NDK, and OpenJDK, is installed for this editor. It's a business sim: a student sells candy at school and balances price, demand, stock, and the risk of getting caught.

- **Game code:** `Assets/Scripts/` (three MonoBehaviours), compiled into the `Empire` assembly (`Assets/Scripts/Empire.asmdef`, which references `Unity.TextMeshPro` and `UnityEngine.UI`). If a script needs another package assembly, add it to that asmdef's `references`.
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

**UnityMCP** (`com.coplaydev.unity-mcp`, registered in `.mcp.json` at `http://127.0.0.1:8080/mcp`) drives the open editor: console, play mode, scene objects. Its server only runs once it's started in Unity via Window > MCP for Unity > Toggle MCP Window. Prefer it over hand-editing scene YAML while the editor is open.

## Architecture

- **`Assets/Scenes/MainGame.unity` is the only scene** and the only entry in Build Settings.
- **Wiring:** each manager is a component on its own GameObject in MainGame. They reference each other through public Inspector fields assigned in the scene. There are no singletons, C# events, or ScriptableObjects. `UIManager` falls back to `FindObjectOfType` when its references are unset.
- **Tick loop:** `GameManager.Update` accumulates `Time.deltaTime`. Every `dayLength` seconds (5) it runs `RunDay()`, which:
  1. calls `EconomyManager.RunSchoolDay()`,
  2. increments `currentDay`,
  3. auto-restocks to full if stock hits 0,
  4. calls `UIManager.RefreshUI()`.

  One tick is one school day; the dashboard shows it as "Day: N".
- **`EconomyManager` holds all game state and rules** as public serialized fields, grouped by `[Header]`: cash/pricing, backpack stock, market demand, social stats, risk, and loyal customers. `RunSchoolDay()` runs these steps in order:
  1. Compute demand from a price-elasticity curve, clamped to backpack capacity.
  2. Serve loyal customers first; they buy multiple units and pay a shortage premium when they empty the stock.
  3. Serve regular students; each may become loyal or raise `riskLevel` via a snitch roll, which popularity and gossip reduce.
  4. Settle cash.
  5. Check confiscation: at the risk threshold, stock is wiped, a fine is charged, and risk resets.

  It also records the `last*` stats for the UI.
- **UI is pull-based.** `UIManager.RefreshUI()` rebuilds the dashboard string from `EconomyManager`/`GameManager` fields. Any new code path that changes economy state outside `RunDay()` must call `RefreshUI()` itself, or the display goes stale. The price slider (range 0–5, set in the scene) writes `pricePerCandy` directly.

## Gotchas

- **Scene values override C# field initializers.** Changing a default in code does not change the existing component in MainGame, so change both, or set the value on the component (via UnityMCP or the Inspector) and save the scene. As of the last scene save, the scene's values match the code defaults. A new or removed field only appears in or disappears from the scene file once the scene is re-saved. When renaming a serialized field, either re-save the scene after the rename (the old value is lost if it differs from the default) or add `[FormerlySerializedAs("old")]`.
- **Encoding:** scripts are UTF-8 with CRLF line endings. `EconomyManager.cs` and `UIManager.cs` have a BOM because they contain non-ASCII characters (emoji, `£`). Keep the `£` as a literal UTF-8 character.
- **Scenes reference scripts by the GUID in their `.cs.meta` file.** Never delete or regenerate `.meta` files, and move or rename scripts together with their `.meta`. A MonoBehaviour's class name must match its file name. Every new asset and folder needs its `.meta` committed alongside it. Unity only writes metas when it imports, so if the editor is closed or unfocused, write one yourself: use a fresh GUID (`[guid]::NewGuid().ToString('N')`) and copy the format of an existing meta of the same type (folder, `.cs`, `.asmdef`).
- **EditMode tests build components with `AddComponent`, and `Start()` doesn't run.** State comes from the C# field initializers, not from the MainGame scene values (see above). Seed `Random.InitState` in tests, because `RunSchoolDay()` uses `Random.value`.
- `Library/`, `obj/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, and `*.sln` are generated. Don't edit them.

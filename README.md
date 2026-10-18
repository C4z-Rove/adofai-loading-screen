# Adofai Loading Screen

A [UnityModManager](https://www.nexusmods.com/site/mods/21) mod for **A Dance of Fire and Ice**
that replaces the black scene transition with an animated loading screen, adds per-chart rating
badges, and fixes a startup language bug.

> 冰与火之舞的 UMM 模组：把进出关卡的黑屏过渡换成带曲图的加载动画，
> 并给谱面加评级徽章、修掉「设置英文却显示中文」的问题。

---

## Features

### 1. Loading screen (entering a level)
* Full-screen cover art with a slow Ken Burns zoom
* Phigros-style progress bar at the top: a red segment sweeps left→right, then a blue one,
  alternating forever (configurable speed / width; can fall back to a plain bar)
* Song title / artist / charter on the right
* Chart difficulty icon in the bottom-right corner
* Minimum display time is configurable; if the level itself loads slower it never extends the wait

### 2. Exit animation
Leaving a level plays the same style of overlay (cover + info, no bar, ~0.5 s) instead of the
vanilla black wipe. Toggle: `ExitAnimation`.

### 3. In-game corner labels
* bottom-left: song title
* bottom-right: `TUF / Lv.GG` from [AutoRating](https://github.com/) (optional) or the chart's rating

Both are anchored to the screen corners and scale with screen height, so they stay put at any
resolution. Hidden while paused / in the editor / while the level is still loading.

### 4. Rating prompt & badges
* On level exit a rating window pops up: five tabs (Planetary / Galactic / Universal / UJ&Q / Special),
  current rating preview, AutoRating suggestion pre-selected
* Already rated charts can skip the prompt (`SkipRatedPrompt`)
* Custom-level-select badge next to the chart info, plus a **judgment icon** (wide / normal / strict)
  to the right of the `Best XACC` line
* Badges fade in only after their position has been stable for ~0.6 s, so they never visibly jump

### 5. In-game hotkey
Default `F7` (rebindable in the UMM panel): while paused, before the song starts, or on the
result screen, open the rating window and change the chart's rating/judgment on the spot.

### 6. Language fix
Some setups end up with the game UI in Chinese while the settings say English: at startup
`RDString.Setup()` reads `Persistence.language` before PlayerPrefs has the real value, so the
runtime language gets stuck on the system default. This mod re-applies the saved language and
keeps watching it, so changing the language in the settings takes effect immediately.
Toggle: `FixLanguage`.

### 7. Localization
All UI strings follow the game language (Chinese → Chinese, otherwise English), and can be forced
in the UMM panel (`Language = auto|zh|en`).

---

## Installation

1. Install [UnityModManager](https://www.nexusmods.com/site/mods/21) for A Dance of Fire and Ice.
2. Download the latest release zip and drop the `PhigrosLoading` folder into
   `<game>/Mods/`.
3. Start the game, enable the mod in the UMM list.

Rating icons are read from, in order:

```
1. MarksPath in config.json (if set)
2. <mod>/Marks/
3. <game>/Mods/LevelLibrary/assets/marks/   (if LevelLibrary is installed)
4. <mod>/Difficulties/
```

You can simply copy `LevelLibrary/assets/marks/*.png` into `<mod>/Marks/`.
`wide.png` / `normal.png` / `strict.png` are used for the judgment badge.
The judgment of a clear is recorded automatically (read from `GCS.difficulty` when the level is
finished); it can also be set manually in the rating window.

---

## Building

Requirements: the game (for its managed DLLs), UMM installed, and a Roslyn C# compiler.

```bat
set ADOFAI_DIR=D:\Steam\steamapps\common\A Dance of Fire and Ice
build.bat
```

`build.bat` looks for `csc.exe` in this order:

1. `%ROSC%` (environment variable)
2. `tools\roslyn\tasks\net472\csc.exe`
3. Visual Studio / Build Tools (`vswhere` → `MSBuild\Current\Bin\Roslyn\csc.exe`)

Output: `build\PhigrosLoading.dll`.

---

## Configuration

Everything is editable from the UMM panel (changes apply live). The same values live in
`config.json` next to the DLL; see `config.example.json` for the full list and defaults.

| key | meaning |
| --- | --- |
| `Enabled` | master switch for the loading screen |
| `RatingPrompt` / `SkipRatedPrompt` | show the rating window on exit / skip it when already rated |
| `Hotkey` | in-game rating hotkey (KeyCode name) |
| `FixLanguage` | re-apply the saved language at startup |
| `ExitAnimation` | play the overlay when leaving a level |
| `InGameDifficulty` | corner labels while playing |
| `AutoRatingSuggest` / `AutoRatingDisplay` | use AutoRating in the prompt / in the corner |
| `ClsBadge` | level-select rating + judgment badges |
| `ClsBadgeOffsetX/Y`, `ClsJudgeScale`, `ClsJudgeOffsetX/Y` | badge placement |
| `MinLoadingSeconds` / `MaxLoadingSeconds` | loading overlay duration bounds |
| `BarSweep`, `BarSweepSeconds`, `BarSweepWidth`, `BarThickness`, `PercentFont` | top bar |
| `Dim`, `KenBurns`, `ShowPercent` | cover styling |
| `InfoRight`, `InfoY`, `LoadTitleFont`, `LoadArtistFont`, `LoadChartFont` | right info block |
| `DiffHeight`, `DiffRight`, `DiffBottom` | difficulty icon |
| `TitleX/Y/Font`, `RatingX/Y/Font` | in-game corner labels |
| `Language` | `auto` / `zh` / `en` |

---

## Compatibility / notes

* Tested with ADOFAI v3.3.x (Unity 6, Mono), UMM 0.32.5.
* [LevelLibrary](https://github.com/) is optional: ratings and judgments are read from its
  `MarkStore`, falling back to `marks.txt`, then `records.txt`. Without it the mod keeps its own
  `ratings.txt` / `judges.txt`.
* [AutoRating](https://github.com/) is optional; when present its results are read (never recomputed).
* Scene loading in ADOFAI is synchronous, so a pause in the animation during loading is expected.

---

## License

MIT — see [LICENSE](LICENSE).

Not affiliated with 7th Beat Games. Icons and cover art belong to their respective authors.

# Changelog

## 2.8.2
- GG intro: the two digits now settle on the same baseline (the tens digit was sitting too high); only
  their start times are staggered (tens at 0.42 s, ones at 0.60 s).
- White flash now fires right after the digits land.

## 2.8.1
- GG intro: `<Lv.>` starts closed and opens while the digits rise; digits are now 4x the base size
  (1000 px) and the brackets 0.25x (62 px) by default. Long values shrink automatically to fit 62% of
  the screen width.

## 2.8.0
- GG intro sizing: the number is now 2x bigger (500 px) and `<Lv.>` half size (125 px) by default, with both
  adjustable (`BossGgNumberSize` / `BossGgLabelSize`, sliders in the UMM panel under GG mode).
  Label and digits are roughly baseline-aligned.

## 2.7.1
- GG intro uses ASCII brackets: one row `<Lv.XX>` — `<Lv.` and `>` appear as one group, the digits rise on
  their own and the brackets spread apart on impact.

## 2.7.0
- **TUF / GG switch** for the boss intro (`BossRatingSource`). GG renders a single row `《Lv.XX》`:`n  the number rises from below, the book-title marks spread open on impact (white flash + shake + two`n  glitch ghosts), single digits are zero-padded (`Lv.07`). GG falls back to TUF when AutoRating has`n  no cached value.

## 2.6.0
- **Exit fade**: when the boss intro is used, leaving a level is now a plain fade (0.18 s black in,
  0.44 s out) instead of the cover transition. New `ExitStyle` = auto / fade / overlay, also
  selectable in the UMM panel.

## 2.5.2
- Glitch no longer loops: each tier fires a fixed number of bursts (0 for P1-9, 1-2 for most, 3 for
  U15-16 / U20) at the digit-landing moment and shortly after. `BossGlitch = 0` disables them.

## 2.5.1
- Boss animation is now tunable from the UMM panel: speed, circle size, digit size and glitch
  strength (`BossSpeed` / `BossScale` / `BossDigitScale` / `BossGlitch`). The minimum display
  time follows the speed so the show is never cut off.

## 2.5.0
- **10 boss variants**, picked automatically from the chart rating:
  P1-9 (simple), P10-20 (classic), G1-9 (spin + side-in), G10-20 (bounce drop + flash),
  U1-9 (dash), U10-14 (corner arcs), U15-16 (heavy glitch), U17-19 (digits first),
  U20 (ultimate: flicker + shockwave + flash, 5.2 s), UJ (rainbow ring + spiral).
- `PreviewMark` config to preview a specific rating.

## 2.4.1
- **boss animation**: black fade-in, difficulty circle zooms into the centre (letters only — digits are
  stripped into `Marks\_split\<mark>_L.png` + `_d0/_d1` by `tools/split_marks.py`), then the digits
  fly up into the circle (tens first, ease-out), with glitch/RGB-split bursts; total ~4.6 s.
- `Application.runInBackground = true` so animations keep running while the window is unfocused.

## 2.4.0
- **Loading animations**: 7 built-in styles — `sweep`, `pulse`, `wave`, `flow`, `spinner`,`ndots` and `custom` — switchable in the UMM panel.
- **Custom animations**: drop PNG sequences or a sprite sheet into `Animations\<name>\` (optional`n`anim.json` with `fps` / `columns` / `frames`). Can be shown in the center or in the top bar.
- Judgment is manual by default (`RecordJudge` off).

## 2.3.2
- Fix: the judge buttons in the rating window were declared but never actually built, so the
  selector was invisible and judgments could not be set manually. The row (Wide / Normal /
  Strict / None) is now rendered below the icon grid.

## 2.3.1
- The judgment used for a clear is now **recorded automatically** when a level is finished
  (`States.Won`), read from `GCS.difficulty` (Lenient / Normal / Strict). The game does not store
  per-chart judgments, so this is the only exact source — no pixel sampling needed.
- `RecordJudge` config switch.

## 2.3.0
- Rating window: added a **judgment selector** (wide / normal / strict / none). The choice is saved to
  `judges.txt` and synchronised to LevelLibrary when it is loaded.
- Judgment resolution is now tolerant (`宽判/宽/wide/0`, `标判/标/normal/1`, `严判/严/strict/2`) and falls
  back to LevelLibrary's `records.txt` (judgment slot of the best clear).
- `judge` debug logging for unmapped values.

## 2.2.1
- Level-select badges: fade in only after the position is stable for ~0.6 s, fade out when switching
  charts, so they no longer teleport while the panel is still laying out.
- Judge icon placement/size are configurable (`ClsJudgeScale`, `ClsJudgeOffsetX/Y`).

## 2.2.0
- Renamed to **Adofai Loading Screen** (mod Id and DLL name stay `PhigrosLoading` for compatibility).
- Added per-chart judgment icon next to `Best XACC` in the custom level select.
- Mark icon folder now defaults to `<mod>/Marks/` (falls back to LevelLibrary's `assets/marks`).

## 2.1.0
- UMM toggle now works live: disabling the mod hides every element immediately and stops all hooks.
- English localization for the whole UI (`Language = auto|zh|en`).
- Fix for "settings say English but the game is Chinese" (`FixLanguage`).

## 1.9.0
- In-game rating hotkey (default `F7`), skip the rating prompt for already rated charts,
  Phigros-style exit animation.

## 1.8.0
- Phigros-style loading bar: red/blue segments sweeping left to right.

## 1.7.0
- Rating badges in the custom level select, AutoRating integration.

## 1.0.0
- Initial release: Phigros-style loading screen, in-game corner labels, rating prompt.

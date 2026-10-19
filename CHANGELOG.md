# Changelog

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

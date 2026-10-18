# Changelog

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

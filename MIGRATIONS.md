# Backup migrations

Rise state is versioned at the backup-file level. A backup file is a
JSON envelope — a format version, an export date, and the saved
state — so an old file can be brought forward instead of rejected.
The version lives in the file, not in the storage keys: the `.v1`
suffixes on the localStorage keys (`rise.quicklinks.v1`,
`rise.layouts.v1`, …) are key names, not format versions.

## Rules

- **Forward-only.** A backup migrates to its own version (identity)
  or to a newer version, never back. A file newer than the app
  supports is refused, not guessed at.
- **Append-only migrations.** A migration, once shipped, is never
  edited. A new shape change adds a new step from the previous
  version to the next one, plus a section below.
- **Validate, then save.** Import parses and validates the whole
  file — and migrates it to the current version — before saving
  anything, so a bad file never half-restores state.
- **Migrate normalizes.** Migrating a file to a target version
  rewrites it in that version's schema; fields the target version
  doesn't know are dropped, never carried along.
- **Migrate never touches state.** The migrate button only converts
  a file and downloads the result. Only import writes state, and
  import reloads the page afterwards so every widget re-reads it.
- **Repairs mirror the app.** An orphaned custom theme choice
  (`custom:<id>` with no matching theme in the file) repairs to
  `system` on import, the same repair the theme toggle applies on
  load. Unknown built-in theme values are an import error instead:
  an orphaned custom theme is a normal case (the theme was deleted
  after export), an unknown built-in is corruption.

## v1 (baseline, current)

Envelope:

```json
{
  "FormatVersion": 1,
  "ExportedAt": "2026-11-02T18:00:00.0000000+00:00",
  "State": { "Links": [], "Theme": "system", "Themes": [], "Layouts": {}, "WidgetConfigs": {} }
}
```

Sections (property names are PascalCase, matching the adapters'
stored JSON):

- `Links`: array of `{ "Id": "<guid>", "Name": "…", "Url": "…" }`.
  Import requires a non-empty name and a valid http(s) URL per link.
- `Theme`: `"light"`, `"dark"`, `"system"`, or `"custom:<id>"`.
- `Themes`: array of custom themes, each `{ "Id", "Name", "Light",
  "Dark" }` with palettes of the nine calm variables
  (`Background`, `Text`, `Muted`, `Faint`, `Surface`, `Border`,
  `BorderSoft`, `TileBackground`, `Error`). Blank ids are dropped
  and blank names become `"Untitled"` on import, mirroring the
  custom-theme service's load sanitizing.
- `Layouts`: `{ "ActiveId", "Layouts": [{ "Id", "Name", "Widgets":
  [{ "Id", "Visible" }] }] }`. Widget zones are not stored —
  placeholders (`Main`) are written and the layout service
  reconciles zones, unknown ids, and blank names (`"Untitled"`)
  against the registered definitions on load.
- `WidgetConfigs`: object keyed by widget id, values are per-widget
  config JSON strings. Values pass through import untouched: the
  envelope migration never rewrites them, because widget configs
  are versioned per widget, not by the envelope version. Each
  config carries its own `Version` field (missing means v1), its
  owning slice migrates it on load, and the migrated JSON is saved
  back; a version newer than the app knows passes through
  untouched, and a widget that can't parse its own config falls
  back to defaults when it loads, as it always has.

No migrations exist yet: v1 is the first versioned format, so
migrating a v1 file to v1 is an identity, and the migrate button's
version list holds just v1 until a shape changes.

v1 extension — groups (compatible, no version bump): each layout
may carry `GroupChildren`, an array of widget ids in group order
(one layout-level list, not a per-widget field). A file that
predates groups simply has no list, which reads as empty on import;
blank ids are dropped; the layout service reconciles the rest
against the registered definitions on load (known Main widgets
that aren't groups themselves). Adding an optional field does not
bump the version: old files still parse, and the v1 identity
migration keeps the field when present.

v1 extension — todos and notes (compatible, no version bump): the
state gains `Todos` (array of `{ "Id", "Text", "Done" }`) and `Notes`
(array of `{ "Id", "Text" }`). A file that predates them simply has
no sections, which read as empty on import; blank text is an import
error, never silently dropped, mirroring links. Same rule as groups:
adding optional sections does not bump the version.

## Adding a version (v2 and beyond)

1. Bump `BackupFormat.CurrentVersion` and document the new shape
   in a section below, including what changed and why.
2. Add the migration step (previous version → new version) to the
   `Migrations` registry in `BackupFormat`. It takes the old state
   JSON and returns the new state JSON, nothing else.
3. Update the current-version DTOs in `BackupFormat` to the new
   shape, so export writes it and import reads it.
4. Add tests: the migration step itself (old shape in, new shape
   out), import of an old file (migrated then applied), and
   rejection of a newer-than-current file.

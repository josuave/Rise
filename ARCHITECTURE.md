# Rise architecture

Rise is a modular monolith: one Blazor WebAssembly app, published as one site
to GitHub Pages, built from a hexagonal clean core plus vertical slices.

## The core: hexagonal clean architecture

```
                ┌─────────────────────────────────────────┐
                │  Imperative shell (effects only)         │
                │  Blazor components, timers, JS interop,  │
                │  localStorage, browser tabs              │
                └───────────────┬─────────────────────────┘
                                │ calls ports, renders results
                ┌───────────────▼─────────────────────────┐
                │  Application (ports + orchestration)     │
                │  ILinkStore, ISearchLauncher, IClock     │
                │  StartPageService, SearchService         │
                └───────────────┬─────────────────────────┘
                                │ calls pure functions
                ┌───────────────▼─────────────────────────┐
                │  Functional core (pure, no I/O)          │
                │  Link, LinkUrl, StartPage, SearchQuery,  │
                │  Greeting, Result                        │
                └─────────────────────────────────────────┘
                                ▲ implemented by
                ┌───────────────┴─────────────────────────┐
                │  Infrastructure (adapters)               │
                │  LocalStorageLinkStore,                  │
                │  BrowserSearchLauncher, SystemClock      │
                └─────────────────────────────────────────┘
```

Dependency rule: core never imports a browser, storage, or clock API.
Application depends only on the domain. Infrastructure depends on
Application (to implement its ports). The shell (Rise.Wasm) is the only
place that wires services to adapters, via `AddRiseCore()` in Program.cs.

### Functional core, imperative shell

The split is deliberately Gary Bernhardt's: the core is pure functions
over immutable data, the shell is thin and does all the effects.

- Pure core: `StartPage.AddLink` / `RemoveLink` take a page and return a
  new page (or a `Result` failure) — they never save anything.
  `LinkUrl.TryCreate` validates and normalizes without touching a browser.
  `SearchQuery.Build` turns text into a URI. `Greeting.For` turns a
  `DateTime` into words. These are trivially testable with no mocks,
  because they have no dependencies at all.
- Imperative shell: application services load a page through a port,
  hand it to the core, and persist whatever the core returns.
  Components render, collect input, and call services. Adapters do the
  actual JS interop / localStorage / tab opening / clock reading.
  The only place `DateTime.Now` is read is `SystemClock`.

If a bug is a wrong rule (duplicate link allowed, bad URL accepted),
it is in the core. If a bug is a failed save or a blocked popup, it is
in the shell. That separation is the whole point.

### What lives where

| Project | Role | May depend on |
|---|---|---|
| `src/Rise.Domain` | Functional core: entities, value objects, aggregates, pure functions | Nothing |
| `src/Rise.Application` | Ports (interfaces) + shell orchestration services | Domain |
| `src/Rise.Infrastructure` | Adapters implementing the ports + DI wiring (`AddRiseCore`) | Application |
| `src/Rise.Wasm` | Shell / composition root: pages, layout, theme, Program.cs | Core + slices |
| `src/Slices/*` | Vertical slices (below) | Application only |

## Slices: `src/Slices/`

A slice is one user-visible feature, end to end, as a Razor class
library (RCL) at a minimum:

```
src/Slices/
  Rise.Slices.Links/    QuickLinks.razor (+ code-behind shell)
  Rise.Slices.Search/   Search.razor (+ code-behind shell)
  Rise.Slices.Settings/ SettingsGear.razor (+ code-behind shell)
  Rise.Slices.Layout/   LayoutEditor.razor (+ code-behind shell)
```

Rules for a slice:

1. **An RCL is the floor.** A slice ships as its own RCL project so it
   can be referenced, tested, reused, or lazy-loaded independently.
   Its components are its public surface.
2. **A slice depends only on the core's ports/services**, never on
   Infrastructure and never on another slice's internals. Slices
   communicate, if they ever must, through core ports or domain
   events — not by importing each other.
3. **All-in-one or split out is the implementer's choice.** A small
   slice may instead live as a folder inside a combined `Rise.Slices`
   RCL (e.g. `Slices/Clock/`), because the dependency rule is the
   same either way: folder or project, it still talks only to core
   ports. Split a slice into its own RCL when it grows its own
   dependencies, its own routes, or becomes a lazy-loading candidate.
4. **A slice may itself be clean/hexagonal, naturally.** Nothing in
   the outer architecture prevents a slice from organizing its
   internals as `Domain/` / `Ports/` / `Adapters/` subfolders inside
   its RCL — its public surface stays the same (components + optional
   DI registration), and its internals stay private to the slice.
   Use that when a slice gets complex enough to earn it (its own
   rules, its own storage, its own tests); don't pay for it on a
   slice that is one component and one service call.
5. **The shell owns the theme.** The calm CSS variables and the
   classes shared by more than one component live in the Wasm app's
   global `app.css`; each component's own styles live in a colocated
   `.razor.css` file next to it (Blazor scopes them to that component
   and bundles RCL styles into the app's stylesheet automatically).
   Rules that style markup rendered by another component stay global:
   the settings dialog's buttons belong to sections from other slices,
   and scoped CSS cannot reach into a child component's markup. The
   theme is never duplicated per slice: scoped files reference the
   shared variables.

Current slices: Links (tiles, persisted via `ILinkStore`), Search
(query box, launched via `ISearchLauncher`; the engine is a Domain
registry, `SearchEngines` — Google, DuckDuckGo, Bing, Brave — plus
user-added customs stored in the Search widget's config, picked in
a Settings section that shows each engine's own favicon with a
letter tile when it cannot load, so the box and the command palette
share one resolution through the `ISearchEngineProvider` port; the
box itself shows the configured engine's favicon where the Search
button used to be — the box is a form, so Enter and the keyboard's
search key submit it (a form is what makes the search key work on
Android, where soft keyboards often send no usable keydown at all)
— and clicking the
favicon opens a quick engine picker that sets the same configured
engine), Settings (gear button
and a Settings dialog: a built-in Theme section — Light / Dark /
System, persisted via the `IThemeStore` port — plus sections
contributed by other slices), and Layout (the layout editor).

## Widgets and layout: the page is a list, not a template

The start page is no longer a hardcoded template. It is a `PageLayout`
(Domain): an ordered list of `WidgetPlacement`s — an opaque string
id, a zone, and a visible flag. The core knows no concrete widgets:
which ids exist, what they are called, and their default order and
visibility all come from the registered widget definitions.

A widget definition is the `IWidgetDefinition` contract (Application
port): Id, Title, Zone, DefaultVisible, Order, ComponentType. Each
slice owns its definitions and ships a DI extension for them
(`AddSearchWidget`, `AddLinksWidgets`, `AddSettingsWidgets`), wired
once in the shell's Program.cs — the same shape as the Settings
contract, but as a core port, because the layout service genuinely
needs the metadata (unlike a dialog section, which is purely UI).
ComponentType is System.Type only, so Application stays free of any
Blazor reference; the shell renders definitions via DynamicComponent.

Widgets live in two zones: Theme and Settings render in the
top-right cluster, everything else in the main column, each in
layout order. Moves only swap within a zone, so a widget can never
wander into the other zone, and hiding a widget never loses its
position. The layout service reconciles a stored layout against the
definitions on load: unknown ids are dropped, zones always come from
the definition, visibility is kept, and widgets the stored layout
predates slot in by definition Order, ahead of every widget ordered
after them — so a new widget lands where its Order puts it instead
of last, and the stored relative order of existing widgets is never
disturbed. The default layout is just the definitions in Order —
it reproduces the old page (Date, Clock, Greeting, Search, Links,
Theme, Settings visible).

The header is three widgets, not one: the Header slice renders
date, clock, and greeting as separate widgets, each owning its own
refresh timer, so the shell re-renders only when the layout changes
instead of on every clock tick. Widgets may also carry settings,
and those live in the Settings dialog as sections (Clock, Greeting,
Group, and Notes join Links and Backup there; Theme is built in).
The layout editor only arranges — it no longer renders settings.
Config is stored per widget id (key `rise.widgets.v1`), not per
layout, so switching layouts keeps a widget's settings. Configs are self-managing objects:
each widget's config derives from the SelfMigratingWidgetConfig
base in the Settings abstractions (widget id, current version, a
Migrate step), owns its schema as typed properties over its JSON,
and the widget-config service runs the migration on load and saves
the migrated JSON back; a missing version field means v1, and a
version newer than the app knows passes through untouched. The
service takes the Migrate method as a delegate, so the core only
ever sees JSON strings and never references a config type. The
config objects follow the options pattern (per-widget named
options, JSON binding, change notifications through the service's
ConfigChanged event) rather than implementing IOptionsMonitor,
whose CurrentValue is synchronous while browser storage reads are
async.
State is also portable: the Backup slice exports all five stores
(links, theme choice, custom themes, layouts, widget configs) as
one versioned JSON file, imports a file (validating it fully and
migrating it to the current version first, then reloading the
page), and migrates an older file to a chosen version for download
without touching state. Versions and migration rules live in
`MIGRATIONS.md`; the Backup controls render both as a Settings
section and as a page widget.
The "Edit layout" button and the editor stay shell-rendered, not
widgets: an editor you could hide would strand the user with no
way back.

Modules: a slice is a module, and the shell holds the catalog
(`IModuleCatalog`, one entry per slice: module id, title, widget
ids, assembly name, lazy flag, can-disable flag). Module choices
live under `rise.modules.v1` and hold only explicit choices — no
entry means enabled, so existing users see zero change. Hide and
disable stay distinct: hide is layout-level and live; disable is
module-level, removes the module's widgets from the page and the
editor and its section from the dialog, and restarts the page (an
assembly cannot be unloaded mid-session, so the reload is what
makes it truly gone). Enable is live, no reload. Settings is
locked on: it owns the gear and the dialog the Modules section
itself lives in. Module choices ride the backup envelope as a
compatible v1 extension (see `MIGRATIONS.md`).

Lazy modules: Notes is the first. A lazy module's assembly is
excluded from the boot download with `BlazorWebAssemblyLazyLoad`
and fetched on first show through `LazyAssemblyLoader`, via a
shell container component. The container needs metadata that
exists without the assembly, so the shell registers it directly:
a lazy widget definition (id, title, zone, order, visibility,
assembly file, component type name) and, for its dialog section,
a lazy section (`ILazySettingsSection`) rendered through a lazy
section container that loads the assembly and then renders the
real section component. Two consequences of loading after
startup: a lazy slice cannot use a typed config class (DI
registrations run at startup, so lazy slices read their settings
through the generic widget config service), and a second load of
an already-loaded assembly does not return it again (both
containers fall back to the already-loaded assemblies). A third
gotcha is the service worker: the published worker precaches
every `.wasm` in the assets manifest, which would download at
install time exactly what lazy loading keeps out of boot — so
each lazy module gets an exclusion pattern in
`service-worker.published.js`. Known leaks, not bugs: a lazy
slice's scoped CSS still loads at boot, and a disabled eager
module still downloads at boot (skipping that needs every module
lazy). Grouped lazy widgets render through the same container
branch, but grouping a lazy widget has not been browser-verified
yet.

Keyboard-first: the shell owns a command palette and a hotkey
system, both shell-rendered, not widgets. The palette
(`CommandPalette`, opened with Ctrl/Cmd+K) filters one list over
links, widget visibility, layouts, built-in themes, and the
edit-layout toggle, plus a web-search fallback for the typed
query; it runs through the same services the widgets use, so the
page updates through the usual Changed events. Hotkeys are named
actions (`HotkeyActions`: open palette, toggle edit layout,
toggle light/dark theme) with bindings stored as overrides under
`rise.hotkeys.v1` — no entry means the default. A document
keydown listener in `index.html` forwards keystrokes to the shell
(`riseHotkeys`, via a DotNetObjectReference); it skips keystrokes
typed into editable fields, except modifier shortcuts and Escape,
and ignores repeats, so holding a key never fires twice. The
Keyboard section in Settings (Order 160, shell-owned like the
module switchboard) lists each action with Rebind — captured in a
readonly input, since a button would re-fire its click on Space —
and Reset to defaults; a conflicting rebind fails naming the
action that already owns the binding. Hotkey bindings ride the
backup envelope as a compatible v1 extension (see
`MIGRATIONS.md`).

Preview features: the shell owns a small preview-feature manager
for opting into upcoming features before they become the default.
Features are registered in one place (`PreviewFeatures` in Domain)
and stored as explicit opt-ins under `rise.preview.v1` — no entry
means off. The Preview features section in Settings (Order 215,
shell-owned like the module switchboard) lists each feature with a
checkbox; toggling is live through the service's Changed event.
The first feature is the Calm Board layout: with it on, the page
renders the board treatment — Main widgets sit in slot wrappers
that are `display: contents` until the flag opts in, then become a
two-column grid of warm-paper cards with the Clock slot spanning
full width as the hero. v0 is the treatment only: widget contents
keep their own styles, and which widgets show stays with the
layout, not the flag. A second flag, `calm-board-landscape`,
applies the same treatment only when the viewport is landscape (a
rotated phone, a wide browser window); in portrait the calm stack
stays. Its rules live inside `@media (orientation: landscape)`
and duplicate the always-board block, since plain CSS cannot share
declarations across a media query. If both flags are on, the
always-board wins. Under phone widths (560px and below) the board
collapses to one column — two columns force the grid wider than a
portrait phone, so the page spills sideways and the clock clips —
and slots carry `min-width: 0` so a track can shrink below its
content minimum. Preview choices ride the backup envelope as
a compatible v1 extension (see `MIGRATIONS.md`). Deliberately not
Microsoft.FeatureManagement: that library's flags are
configuration-driven and deploy-time, while Rise's are toggled by
the user at runtime and persisted per browser; the hand-rolled
store is one port, one adapter, and one service in the shape of
the module and hotkey stores.

A third flag, `calm-board-full`, is the full Calm Board (mock-up D):
the same board treatment, plus a calm top — Date, Clock, Greeting,
and Search render as full-width unboxed rows above the board — an
Edit board bar above the board (active layout name, layout switch,
Edit board toggle, Apply D arrangement), and an add-widget slot last
in the grid that shows a hidden widget. Precedence: when the full
flag is on the page carries only `calm-board-full`, so it wins over
`calm-board` and `calm-board-landscape`; it neither requires nor
forces them. The D arrangement (Date, Clock, Greeting, Weather,
Search, Links, Todo, Notes visible in definition Order; Backup and
the group hidden; any grouping cleared) is explicit-apply only: the
flag never rewrites a stored layout — it applies when the user asks
for it from the bar, through `LayoutService.ApplyCalmBoardArrangementAsync`,
on the same path as Reset. Visibility and relative order always come
from the stored layout; the top/board split only partitions by the
top-id set (`CalmBoardArrangement.TopIds`). The 560px one-column
phone rule carries forward unchanged. Under the full board, Notes
defaults to post-it view when no view choice is stored (a stored
choice always wins) and Todo shows a "Today · X of Y done" line;
both read the flag through `PreviewFeatureService`. No card grips:
there is no drag yet, and a grip that does nothing would be
dishonest UI. The Weather widget renders once, in the board as a
card with current conditions plus its 3-day forecast strip; the
mock's separate weather header line is a second presentation of
one widget and is not built.

A fourth flag, `calm-settings`, redraws the Settings dialog itself
in the Calm Board style: the same sections as warm-paper cards in
a grid, grouped under hairline headings — Appearance (Theme, Home
screen icon, Background), Content (Links, Search, Clock, Greeting,
Group, Notes, 2048, Weather), System (Keyboard, Backup, Modules,
Preview features) — in Order order within each group, so the board
keeps the dialog's established ordering and only adds the grouping.
The dialog subscribes to the preview service's Changed event and
re-reads the flag on every open, so toggling the flag inside the
dialog re-renders it live, and a toggle made while it was closed is
honored on the next open. The Theme section is hard-coded in the
dialog, so it is the first card of Appearance; its picker markup is
duplicated between the flat dialog and the board card, and the two
copies must stay in sync. A section whose title is in no group
renders at the end under no heading, so a new section never
vanishes. All D styles are scoped under a `calm-dialog-board`
class on the dialog, so the flat dialog is untouched when the flag
is off. The board grid is two columns in landscape and one column
in portrait and under 560px, mirroring the page board's collapse;
the dialog widens to 860px under the flag so two columns breathe.

Backgrounds: one chosen background for the page — a built-in calm
gradient preset (None, Dawn, Mist, Dusk, Forest, registered in
Domain) or a custom image address pasted in Settings. The choice
lives under `rise.background.v1` through the same port/adapter/
service shape as the preview features, and the Background section
in Settings (Order 170, shell-owned like the preview manager)
applies it live through the service's Changed event, no restart.
The shell renders the background as a fixed layer behind the page
content (above the body's calm background, below everything else),
with a scrim of the theme's calm background over it so text stays
readable in both themes — one choice covers light and dark in v1.
The page background lives on `<body>` only, never on the shell
(`.calm-shell`) or `<html>`: a body's background becomes the canvas
background, so the fixed layer paints above it. An opaque background
on the shell covered the layer entirely in v1 — the layer was in the
DOM with the right computed style, but no pixel ever showed it
(fixed in the follow-up that moved the background to the body;
verify visual features by sampling screenshot pixels, not just
computed styles).
A custom address must be an absolute http(s) URL; the page
references it like any image, so it needs the network to load and
is not cached offline. An uploaded image file is hashed (SHA-256)
and its bytes stored in IndexedDB keyed by that hash, through the
`riseBackgroundImage` JS helper; the choice carries only the
descriptor (hash, original file name, size, type). At boot the
service turns the stored bytes back into a blob URL — one live
URL at a time, revoked when the choice moves on. When the bytes
are gone — a backup restored on a fresh device, or the browser
evicted the store — the service flags the upload missing and the
Background section prompts for a re-upload naming that file;
re-uploading the same file restores it (hash-checked), a
different file is flagged and only replaces it on an explicit
"use this image instead". Replacing an upload deletes the old
bytes; uploads cap at 10 MB, the same cap as backup imports.
Background choices ride the backup envelope as a compatible v1
extension (see `MIGRATIONS.md`); an unknown preset in an
imported file repairs to none, a custom choice without a valid
address is an import error, and an upload descriptor without
valid details is an import error — the bytes never ride the
backup, so a restored upload always arrives as the re-upload
prompt.

Adding a widget is a new definition plus one line in Program.cs:
no change to Domain, the layout service, the editor, or the shell.

Architecture tests (`tests/Rise.Architecture.Tests`) enforce the
slice rule instead of assuming it: a slice may reference only the
core (Application, and Domain where it uses domain types
directly) and the shared settings contract — never
Infrastructure, never another slice, never the shell. Domain
references no other Rise assembly, Application sits on Domain
only, Infrastructure adapts the core only, and the settings
contract is dependency-free. The tests read each assembly's
manifest references, so they catch a new reference the moment it
is added; a completeness test fails if a slice-shaped DLL appears
in the output that the tests do not know. The shell (Rise.Wasm)
is tested in the reverse direction only: as the composition root
it may reference everything, but nothing may reference it. Two
further guards follow Milan Jovanovic's architecture-test
pattern: the core references no ASP.NET Core assembly, and every
type in a slice assembly lives in that slice's own namespace.
The namespace guard ignores Coverlet's injected tracker types:
coverage collection adds one per instrumented assembly, and
without the exclusion the guard fails only when coverage runs.
The suite runs as its own step in `pr.yml`, alongside core and
slice tests.
The shell (`Index`) renders widgets from the layout instead of naming
components directly, and re-renders whenever the layout service's
`Changed` event fires. The link service has the same event, so tiles
stay fresh when the Settings section saves. Links are edited only in
the Settings dialog: the Links section adds, removes, and resets. The layout editor itself is a
widget of last resort in the shell: move up/down, hide/show, reset to
defaults, all buttons (drag-and-drop is a later step, not v1), with
titles taken from the definitions. Like the Settings contract, this
is an extension point for first-party slices, not a plugin loader:
DynamicComponent only renders components from assemblies the app
already loads.

Named layouts: the user can save more than one arrangement. The
stored shape is a `LayoutCollection` (Domain): an active id plus a
list of `NamedLayout`s (id, name, `PageLayout`). The default layout
always exists, can be renamed but never deleted, and is the fallback
when the active layout is deleted. Names are unique (a clash gets
" 2", " 3", ...), saving the current arrangement activates the copy,
and every layout is reconciled against the definitions on load the
same way a single layout was — per layout, so an old layout gains
new widgets with defaults just like the default one does. The
single-layout operations (move, hide, reset, show/hide widget) always
act on the active layout, so the shell and the other widgets did not
change: they render the active layout and re-render on `Changed`.

Groups: the page has one group (v1), a Main container that hosts
other widgets inside it. Grouping is deliberately not a property on
placements — every widget would carry a field that only matters to
grouped ones. Instead the layout carries the group's member list
(`PageLayout.GroupChildren`: widget ids in group order), and grouped
widgets stay in the flat list too, so ungrouping one loses nothing:
it renders at its flat position again. The group is a widget
definition like any other (Id `Group`, Main, hidden by default,
Order 45, configurable name stored per widget id like every other
config), marked with `IsGroup` on the definition contract — the one
layout-level concept the contract names, so the shell knows to
render its members inside the shell instead of on their own, and
the layout service knows to filter membership to known Main widgets
that aren't groups themselves. Moves respect containers: a grouped
widget moves within the group, a top-level widget moves past
top-level neighbours only. The layout editor shows group members
indented beneath the group row, with a per-row action to move a Main
widget in or out (moving one in also shows the group), and deleting
a group asks whether its member widgets go with it; without that,
they return to the top level and the group itself is hidden. Going
with it means hidden, not removed: reconcile re-adds a widget
missing from a layout, so a removed member would come straight
back — a hidden one stays hidden until the editor shows it again.
No nesting and Cluster members in v1; both are open follow-ups, not
v1 scope.
Todo and Notes are two separate slices, one PR: each owns its
definition (Id `Todo`/`Notes`, Main, hidden by default, Orders 36/37)
and its widget, following the same shape as links — a Domain list
(`TodoList`/`NoteList`) with pure add/toggle/delete (and
clear-completed for todos, edit for notes), an Application service
over a store port, and a LocalStorage adapter under `rise.todos.v1`
/ `rise.notes.v1`. Both widgets are minimal v1: todos are add,
toggle, delete, clear completed (no due dates, reminders, or
priorities); notes are a list of plain-text notes (add, edit,
delete), deliberately no markdown editor — that would be the heavy
library that reopens the parked lazy-loading decision. Notes carries
one self-managing config, `NotesViewConfig` (v1, under `rise.widgets.v1`
like the header configs): a toggle on the widget switches between the
list view and an optional post-it view — pastel cards (one of five, by
note id), a handwritten font stack, a slight tilt — and the choice
persists; the same choice is a Notes section in the Settings dialog,
the theme pattern of dialog setting plus quick toggle.
Both lists join the backup envelope
unconditionally, as a compatible v1 extension: a file that predates
them reads their sections as empty, and blank text is an import
error, mirroring links. No encryption in v1: the lists are plain
storage like links, and password protection stays an open
follow-up, not v1 scope.

Weather is a Main widget (Order 8, between Greeting and Search),
hidden by default: current conditions plus optional daily rows
from Open-Meteo, no key. The core carries the forecast model
(`WeatherForecast`, `DailyForecast`, `WeatherCode` descriptions)
and two outbound ports — `IWeatherClient` and `IPlaceGeocoder` —
with a thin `WeatherService` over them; the Infrastructure
adapters call Open-Meteo's forecast and geocoding APIs. The
place is typed, never geolocated: the widget resolves a place
without coordinates on first load, and the Settings section
(Order 150) resolves on save, showing an error when nothing
matches. The place, forecast toggle, and day count live in a
self-managing `WeatherConfig` under `rise.widgets.v1`, so they
ride the backup envelope with the other widget configs. The
widget refreshes on load and every 30 minutes.
The layout editor adds a layouts section above the widget rows:
switch, save the current arrangement as new, rename the active
layout, delete (never the default).

Persistence is the same localStorage pattern as links: `LayoutService`
(Application) over the `ILayoutCollectionStore` port,
`LocalStorageLayoutCollectionStore` (Infrastructure) under
`rise.layouts.v1`, storing only layout ids, names, widget ids, and
visibility (zones come from the definitions), corrupt or missing data
falling back to the default collection. Per-page layouts (a layout
per route) are a later step: Rise has one page today, so named
layouts on that page come first. The theme editor is Phase 2, built
on the same shape: a custom theme is a name over a light palette
and a dark palette (the nine calm CSS variables; the built-in
defaults mirror app.css), created as a copy of the built-ins and
tweaked with color pickers in the Settings dialog. Definitions live
under `rise.themes.v1` via `ICustomThemeStore`; the active choice
stays in `rise.theme.v1` as "custom:&lt;id&gt;", so the picker,
the quick toggle, and the before-first-paint script all keep
working: built-in choices use the data-theme attribute, custom
choices apply one palette half as inline variables (dark half when
the device prefers dark, re-applied on OS-preference change), and
switching to a built-in clears the inline variables. Deleting the
active custom theme repairs the choice to "system"; an orphaned
custom choice repairs the same way on startup. Generating a theme
from a seed color or image is a later step, not this one. The
Omarchy themes are a third kind of theme, supplied by an optional
module rather than made by the user: all 23 built-in themes from
omacom/omarchy, each one palette with its declared mode — dark
themes stay dark and light themes stay light whatever the device
prefers, so a pack theme has no second half; the quick toggle
shows the pack's own declared mode and stays in sync with picks
made in Settings through the theme store's Changed event
(flipping away from a pack theme still lands on an explicit
built-in, like a custom theme). The palettes
are copied mechanically from each theme's colors.toml into the
OmarchyThemes slice (Background is background, Text is
bright_foreground, Muted is muted, Faint is dark_foreground,
Surface is lighter_background, Border is muted, BorderSoft is
lighter_background, TileBackground is selection, Error is red,
Accent is accent for the picker's swatch). The module is a
catalog entry with no widgets; the theme picker lists the pack
only while the module is switched on, and switching it off
repairs an active pack choice to "system" on startup. The choice
is stored as "pack:omarchy:&lt;slug&gt;" in `rise.theme.v1`, so it
rides backup as the theme string with no format change; the
palettes themselves never ride the backup — the module re-supplies
them. The before-first-paint script applies a pack choice from a
palette cache the app writes to `rise.omarchy-themes.v1` on boot,
because the script runs before Blazor starts. The
shell's index.html provides the `window.riseTheme` script block the
theme adapter calls (localStorage, the page's data-theme attribute,
custom inline variables, matchMedia), the same way it provides
`window.riseStorage` for links,
plus a small head script that applies an explicit stored theme before
first paint. The same head order gives the loading screen its theme
for free: `#app` holds a minimal centered "Rise" wordmark and sliding
bar (styled from app.css's calm variables), shown until Blazor
starts and clears it — so the loading screen already matches
light/dark/system/custom with no extra code. The diagnostics/maintenance dialog that lived here while
the dead-clicks bug was being chased was removed once that bug was
fixed (PR #9); its `IDiagnosticsProbe` port and adapter went with it,
and can be restored from git history if a future bug needs them again.

## Settings contract: how a slice injects into Settings

Settings is its own slice and its own library — `Rise.Slices.Settings`,
a Razor class library in `src/Slices/` — not part of the core. The core
is Domain / Application / Infrastructure; Settings depends on the core's
services, never the other way around. And its contract lives in its own
abstractions library, separate from the core: Settings is not the core,
so its contract does not belong in a core abstractions library either.

So `ISettingsSection` (`Title`, `Order`, `Description`,
`ComponentType`) lives in `src/Slices/Rise.Slices.Settings.Abstractions`,
borrowing the Microsoft.Extensions.*.Abstractions pattern: interfaces
only, no implementations, no dependencies. A future core abstractions
library, if one ever earns its existence, would hold only contracts
shared across slices — not settings-specific ones — and would stay
separate from this one:

1. **Contribute.** A slice implements `ISettingsSection` and ships its
   own Razor component for the section. The component is the section's
   behavior and UI: it injects core services and does the work, exactly
   like any slice component. Example: `LinksSettingsSection`
   (`Order = 100`, component `LinksSettings`, which lists, adds,
   removes, and resets links) in `Rise.Slices.Links`.
2. **Register.** The slice exposes a small DI extension (e.g.
   `AddLinksSettings()` registering
   `ISettingsSection -> LinksSettingsSection`) and the shell calls it
   in `Program.cs`. Registration is the shell's job, not the Settings
   slice's, so slices stay independent of each other.
3. **Render.** The Settings dialog injects
   `IEnumerable<ISettingsSection>`, orders by `Order`, and renders each
   section with Blazor's `DynamicComponent` using its `ComponentType`.
   It knows nothing about the contributing slice's internals, and a
   section's component is only ever rendered from an assembly the app
   already loads — this is an extension point for first-party slices,
   not a plugin loader for arbitrary assemblies.

To add a section to a new slice: implement the interface, add the
component, expose the `Add…Settings()` extension, and call it from
the shell. No change to the Settings slice is needed. The home
page in Rise.Wasm composes them; it contains no feature logic of
its own beyond the clock/greeting shell.

## Future options this structure keeps open

- **More lazy modules:** Notes proved the pattern (see the Modules
  section above). Extending it to another slice is the same shape:
  shell metadata definition plus container, generic config, lazy
  section, service-worker exclusion. Worth it only for a heavy
  slice or many optional ones — not for Links/Search today.
- **Tests:** pure core tests need no browser; service tests need only
  a fake port; component tests need only a fake service. That last
  one is now real: `tests/Rise.Slices.Tests` (bUnit + xUnit) renders
  slice components, clicks their buttons, and asserts the result —
  including the Edit-links regression test that would have caught
  the PR #9 dead-clicks bug. Slice tests use real application
  services with fake ports (in-memory link store, recording search
  launcher, recording theme store, in-memory layout store), never a
  browser, JS, or Infrastructure. The widget phase added bUnit tests
  for the editor widget, the split-out link editor, and the split-out
  theme toggle; the registry rework added core layout tests (zones,
  in-zone moves, hide-keeps-position) and layout service tests
  (defaults and reconciliation against definitions); named layouts
  added collection tests (name uniqueness, delete fallback, rename)
  and service tests (save/switch/rename/delete, active-only edits)
  plus editor tests for the save-as/switch/delete/rename UI; the
  header widget added a component test (fixed clock) and a reconcile
  test proving new widgets slot in by Order. Both test projects carry `coverlet.collector`;
  CI collects Cobertura coverage via `coverage.runsettings`
  (test assemblies excluded); nothing uploads it anywhere.
- **New slice:** copy a slice project, depend on Application, add its
  component to the shell page, wire any new port in Infrastructure.
  No core or existing slice changes required.

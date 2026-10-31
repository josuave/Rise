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
5. **The shell owns the theme.** Calm styling lives in the Wasm
   app's CSS, so slices render with shared class names and the look
   stays coherent. A slice may add component-scoped CSS for its own
   layout, but the theme is not duplicated per slice.

Current slices: Links (editable tiles, persisted via `ILinkStore`),
Search (query box, launched via `ISearchLauncher`), and Settings
(gear button + diagnostics/maintenance dialog, via the
`IDiagnosticsProbe` port: raw saved-links JSON, user agent, service-
worker state, clipboard copy of a diagnostics summary, clear saved
links, unregister service workers, reset links to defaults via
`StartPageService.ResetToDefaultsAsync`). The shell's index.html
provides the `window.riseDiagnostics` script block the probe adapter
calls, the same way it provides `window.riseStorage` for links.

## Settings contract: how a slice injects into Settings

The Settings dialog is itself a slice, but no slice may depend on
another slice — and adding a slice contract should not require editing
the root application library either. So the contract lives in its own
abstractions project, `src/Rise.Slices.Abstractions`, borrowing the
Microsoft.Extensions.*.Abstractions pattern: interfaces only, no
implementations, no dependencies. `ISettingsSection` (`Title`, `Order`,
`Description`, `ComponentType`) lives there, and that is where future
cross-slice contracts go too:

1. **Contribute.** A slice implements `ISettingsSection` and ships its
   own Razor component for the section. The component is the section's
   behavior and UI: it injects core services and does the work, exactly
   like any slice component. Example: `LinksSettingsSection`
   (`Order = 100`, component `LinksSettings`, which lists, removes,
   and resets links) in `Rise.Slices.Links`.
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

- **Lazy loading:** slices already publish as separate assemblies
  (`Rise.Slices.*.wasm` under `_framework`). Marking one with
  `BlazorWebAssemblyLazyLoad` and loading it on its route via
  `LazyAssemblyLoader` is a small change, worth it only for a heavy
  slice — not for Links/Search today.
- **Tests:** pure core tests need no browser; service tests need only
  a fake port; component tests need only a fake service.
- **New slice:** copy a slice project, depend on Application, add its
  component to the shell page, wire any new port in Infrastructure.
  No core or existing slice changes required.

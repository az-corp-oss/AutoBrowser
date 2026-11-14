# Product Requirements Document: AutoBrowser

Status: **Active development** | Owner: TBD | Last updated: 2026-11-14

## 1. Executive Summary

### Problem Statement

Users who work across multiple browsers (e.g., Chrome for work, Firefox for dev, Edge for personal) must manually copy-paste URLs between browsers, and Windows offers no way to route links by destination automatically.

### Proposed Solution

AutoBrowser is a portable Windows desktop application that intercepts URLs (via the `autobrowser://` protocol, default-browser registration, and Windows Search interception) and routes them to user-configured browsers based on ordered regex/substring rules, with a fallback browser for unmatched links. It lives in the system tray, self-updates atomically, and requires no installation or admin rights.

### Success Criteria

| # | KPI | Target | Measure |
|---|---|---|---|
| SC-1 | Rule match accuracy | ≥99% of configured patterns resolve to the intended browser | Rule-evaluation unit tests + manual Test URL validation |
| SC-2 | Startup-to-ready time | <2s on Windows 10/11 cold start | Timed launch benchmark |
| SC-3 | URL routing latency | <500ms from URL receipt to browser launch | End-to-end routing benchmark |
| SC-4 | Update success rate | >95% without manual intervention | AutoUpdater success/failure log aggregation |
| SC-5 | Memory footprint | <50MB typical steady state | Process working-set measurement |

---

## 2. User Experience & Functionality

### User Personas

| Persona | Description | Key Needs |
|---|---|---|
| **Power User** | Uses multiple browsers for distinct tasks; wants set-and-forget routing | Ordered rules, per-rule toggle, Test URL |
| **Developer** | Needs specific browsers/builds for testing | Custom browser paths, regex patterns, quick rule iteration |
| **Work/Personal Splitter** | Separates work and personal browsing | Default-browser mode, silent routing, tray presence |
| **Windows Search User** *(v1.1)* | Opens web results from Windows Search and expects them in the right browser | Windows Search interception, `microsoft-edge:` handling |

### User Stories & Acceptance Criteria

#### Core URL Routing

**US-001 — Define pattern-to-browser rules**
> As a user, I want to define rules that match URL patterns and open them in specific browsers.

- [ ] AC: User can create a rule with Name, Pattern, and BrowserPath via the Rule Editor.
- [ ] AC: Rules persist to `Data/rules.json` and survive restart.
- [ ] AC: Invalid regex patterns are rejected at save time with an error message.

**US-002 — Priority-ordered evaluation**
> As a user, I want rules evaluated in a defined order so I can control which match wins.

- [ ] AC: Rules are evaluated in `Sequence` ascending order (lower = first).
- [ ] AC: First matching enabled rule wins; evaluation stops.
- [ ] AC: Reordering rules (Move Up/Down) changes evaluation order and persists immediately.

**US-003 — Rule groups**
> As a user, I want to group rules into named categories for better organization.

- [ ] AC: Rules can be assigned to a named group.
- [ ] AC: Groups are collapsible/expandable in the Home tree view.
- [ ] AC: Group membership does not change evaluation order unless group sequence does.

**US-004 — Test URL before going live**
> As a user, I want to test a URL against my rules before they take effect.

- [ ] AC: Test URL dialog shows which rule matched (or fallback/no-match), and the target browser.
- [ ] AC: Testing does not actually launch a browser.

**US-005 — Enable/disable rules**
> As a user, I want to enable or disable rules without deleting them.

- [ ] AC: Disabled rules are skipped during evaluation.
- [ ] AC: Rule state is visually indicated (enabled/disabled) in the tree.
- [ ] AC: Toggle persists immediately (auto-save).

**US-018 — Drop rule action** *(current build)*
> As a user, I want a rule to drop (block) a URL so certain links never open.

- [ ] AC: A rule can be marked as Drop; matched URLs are not launched in any browser.
- [ ] AC: Routing result reports `Dropped` with the matching rule name.

#### Browser Management

**US-006 — Auto-detect installed browsers**
> As a user, I want AutoBrowser to detect installed browsers automatically.

- [ ] AC: Installed browsers are discovered at startup via the browser provider.
- [ ] AC: Detected browsers appear with display name and executable path in browser selection.
- [ ] AC: Unknown/newly installed browsers are picked up on next refresh without restart of the app where feasible.

**US-007 — Fallback browser**
> As a user, I want a fallback browser for URLs that match no rule.

- [ ] AC: Unmatched URLs launch the configured fallback browser.
- [ ] AC: If fallback path is missing/invalid, routing reports `NoMatch` and does not crash.

**US-008 — Custom browser paths**
> As a user, I want to add custom browser paths if detection misses one.

- [ ] AC: User can browse to / type an arbitrary executable path.
- [ ] AC: Invalid paths are surfaced at rule save or at launch time with fallback behavior.

#### Windows Integration

**US-009 — Default browser handler**
> As a user, I want AutoBrowser to register as the default browser handler.

- [ ] AC: Registers `AutoBrowserLink` ProgId (HKCU, no admin) and can be selected in Windows Settings > Default Apps.
- [ ] AC: HTTP(S) links are routed through AutoBrowser, which then dispatches per rules.

**US-010 — Custom protocol**
> As a user, I want AutoBrowser to register the `autobrowser://` protocol.

- [ ] AC: `autobrowser://` URIs open in AutoBrowser and are routed by rules.
- [ ] AC: Protocol registration lives in HKCU only; no elevation required.

**US-011 — Re-register prompt**
> As a user, I want a re-register prompt if Windows registry entries are missing.

- [ ] AC: If the app detects it moved (path change) or registrations are stale, it prompts to re-register.
- [ ] AC: User can manually re-tick registration checkboxes in Settings to force re-registration.

**US-019 — Windows Search interception** *(v1.1, current build)*
> As a user, I want web results opened from Windows Search to route through my rules.

- [ ] AC: Enabling "Intercept Windows Search" registers `AutoBrowser.microsoft-edge` ProgId (HKCU) and starts the reactive watcher.
- [ ] AC: `microsoft-edge:?url=...` URIs are parsed (including `&timestamp=` and similar params) and routed.
- [ ] AC: Disabling unregisters the ProgId and stops the watcher; state persists in settings.

#### System Tray & UI

**US-012 — System tray presence**
> As a user, I want AutoBrowser to live in the system tray and not clutter my taskbar.

- [ ] AC: App runs in tray; main window is hidden on startup unless opened.
- [ ] AC: Tray context menu offers Show/Hide, Check for Updates, Exit.

**US-013 — Minimize/close to tray**
> As a user, I want minimize-to-tray and close-to-tray as independent options.

- [ ] AC: Each behavior is independently configurable in Settings.
- [ ] AC: With close-to-tray off, closing exits the app.

**US-014 — Theme switching**
> As a user, I want to switch between light and dark themes.

- [ ] AC: Theme options: Light / Dark / System (default).
- [ ] AC: Choice persists to `Data/settings.json`.

#### Updates

**US-015 — Startup update check**
> As a user, I want AutoBrowser to check for updates automatically on startup.

- [ ] AC: Checks GitHub Releases API (throttled to once per hour).
- [ ] AC: If a newer version exists, a dialog offers to update; user can defer.

**US-016 — One-click, incremental updates**
> As a user, I want one-click updates that don't re-download the whole app unnecessarily.

- [ ] AC: Update downloads the release ZIP, extracts to a temp workspace, and hands off to AutoUpdater.exe.

**US-017 — Atomic updates**
> As a user, I want the update to be atomic — if it fails, the old version is restored.

- [ ] AC: Current version is backed up before swap; SHA256 verification gates the swap.
- [ ] AC: On failure, backup is restored and the app relaunches on the old version.
- [ ] AC: All update operations are logged.

### Non-Goals

The following are **explicitly out of scope** for the current PRD:

- No telemetry, analytics, or data collection of any kind.
- No cloud sync of rules or settings.
- No plugin system or third-party rule matchers.
- No browser profile / container support (Chrome profiles, Firefox containers) in v1.
- No mobile, macOS, or Linux support.
- No installer or MSI; distribution remains portable ZIP (framework-dependent and self-contained).
- No scheduled/time-of-day routing in v1.
- No multi-user or per-account rule sets.

---

## 3. AI System Requirements

**Not applicable.** AutoBrowser's routing engine is deterministic rule evaluation (regex/substring + ordered matching). There is no ML, LLM, or heuristic inference in scope, and none is planned for the next two releases.

### Where "intelligence" is simulated today

- **Windows Search interception** (v1.1): purely rule-driven — a registered ProgId + reactive watcher + URL normalization in `UrlInterceptorService.StripProtocolPrefix` (handles `microsoft-edge:` variants). No learned behavior.
- If a future version adds natural-language rule authoring or smart categorization, this section will be re-opened with its own evaluation strategy (e.g., NL→regex conversion accuracy on a 50-pattern benchmark).

---

## 4. Technical Specifications

### Architecture Overview

- **Form**: Single-process WPF desktop app (MVVM, CommunityToolkit.Mvvm) with an optional companion `AutoUpdater.exe` (AOT console) for atomic file swaps.
- **Pattern**: Service-based modular layout (`Core` interfaces/services, `Features` (Home, RulesEditor, Settings, About, Shell), `Infrastructure` (Persistence, Windows)).
- **DI**: Microsoft.Extensions.DependencyInjection registers services at startup.
- **Data flow**:
  1. URL arrives via `autobrowser://` protocol, default-browser association, or Windows Search interception (all land in `AutoBrowser.exe` with the URL as an argument).
  2. Single-instance mutex: a second instance forwards the URL to the first via named pipe.
  3. `UrlInterceptorService.TryRoute(url, fallbackPath)` normalizes the URL (strips protocol prefixes, decodes `microsoft-edge:` params, defaults to `https://`).
  4. Enabled rules ordered by `Sequence` are matched first-match-wins; matched rules either launch the configured browser (via `IBrowserLauncher` strategy — generic + specialized launchers) or Drop the URL.
  5. No match → fallback browser if a valid path is set, else `NoMatch`.
  6. **Infinite-loop protection**: unmatched URLs are launched by direct EXE path, never via shell association back into AutoBrowser.

### Integration Points

| Integration | Detail |
|---|---|
| **Registry (HKCU)** | `autobrowser` protocol; `AutoBrowserLink` default-browser ProgId; `AutoBrowser.microsoft-edge` Windows Search ProgId. All user-level, no admin. |
| **GitHub Releases API** | Update checks (version comparison Major.Minor.Patch), release ZIP download. Throttled 1/hour. |
| **Named pipe IPC** | Single-instance URL forwarding. |
| **Windows Search** | Reactive watcher (`IWindowsSearchReactiveService`) + ProgId registration to catch `microsoft-edge:` links. |
| **Filesystem** | `Data/rules.json`, `Data/settings.json` next to the EXE; portable. |
| **Auth** | None — no accounts, no credentials. |

### Security & Privacy

- **No data collection or telemetry.** Only outbound network traffic is GitHub update checks/downloads.
- All data is local (`Data/` folder); portable across machines.
- Registry writes limited to HKCU (per-user, no elevation).
- Update integrity: SHA256 verification before file swap; backup/restore on failure.
- Logging (Serilog, console + file) never writes URLs' query-string secrets by default; URL values are logged at Info — **TBD**: consider redacting query params from logs.

---

## 5. Risks & Roadmap

### Phased Rollout

| Phase | Scope | Exit Criteria |
|---|---|---|
| **MVP (current)** | Core routing (Sequence ordering, regex/substring, first-match-wins, fallback), Drop action, rule groups, auto-save, single instance, tray, protocol + default-browser registration, re-register prompt, atomic updates | All P0/P1 stories pass AC; routing latency <500ms; match accuracy ≥99% |
| **v1.1 (in progress)** | Windows Search interception, hardening of URL normalization edge cases, error-path tests, performance benchmarks | Windows Search stories (US-019) pass AC; reliability metrics recorded |
| **v1.2** | Rule import/export, URL pattern templates, browser profiles, time-of-day routing (stretch), telemetry **only if opted-in** (revisit SC-1 measurement) | Feature ACs pass; no regression on routing KPIs |
| **v2.0** | Rule sharing, cloud sync (opt-in), plugin matchers, SQLite-backed storage if rule count grows beyond ~1000 | Scaled rule benchmarks pass (10k rules) |

### Technical Risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| **Edge cases in URL normalization** (exotic `microsoft-edge:` param orders, encoded URLs) misroute links | Medium | High (silent wrong-browser) | Centralize in `StripProtocolPrefix`, unit-test known Windows Search formats, add integration tests |
| **Default-browser registration is reversible by Windows/other apps** | High | Medium | Re-register prompt (US-011); periodic registration health check |
| **Update swap failure** leaves broken install | Low | High | Backup + SHA256 + restore path already implemented; test rollback explicitly |
| **Registry writes blocked** by policy | Low | Medium | Log + continue without registration; manual re-tick in Settings |
| **Rule count growth** (O(n) evaluation) | Low | Medium | Regex compilation + caching in v1.2; SQLite alternative |
| **Self-contained build size** grows beyond portability targets | Low | Low | Single-file publish; monitor installed size (<10MB target) |

---

## 6. Appendices

### Appendix A: Data Formats

**Rule (rules.json)** — fields: Name, UrlPattern, BrowserPath, BrowserName/BrowserDisplayName, IsEnabled, IsForward (Forward vs Drop), Priority, Sequence.

```json
{
  "Name": "Google Apps",
  "UrlPattern": "^https?://(mail|drive|docs|calendar|meet)\\.google\\.com",
  "BrowserPath": "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
  "BrowserDisplayName": "Chrome",
  "IsEnabled": true,
  "IsForward": true,
  "Priority": 10,
  "Sequence": 1
}
```

**Settings (settings.json)** — includes ThemeMode, MinimizeToTray, CloseToTray, LastUpdateCheckTime, and (v1.1) InterceptWindowsSearch.

```json
{
  "ThemeMode": "Dark",
  "MinimizeToTray": true,
  "CloseToTray": true,
  "LastUpdateCheckTime": "2026-07-25T10:00:00Z",
  "InterceptWindowsSearch": false
}
```

### Appendix B: Registry Entries (all HKCU)

**Protocol Handler**
```
Software\Classes\autobrowser
  (Default) = "AutoBrowser:URL"
  shell\open\command
    (Default) = "\"C:\Path\To\AutoBrowser.exe\" \"%1\""
```

**Default Browser**
```
Software\Classes\AutoBrowserLink
  (Default) = "HTTP:AutoBrowser"
  shell\open\command
    (Default) = "\"C:\Path\To\AutoBrowser.exe\" \"%1\""
```

**Windows Search Interception** *(v1.1)*
```
Software\Classes\AutoBrowser.microsoft-edge
  (Default) = "URL:AutoBrowser Microsoft Edge Redirect"
  "URL Protocol" = ""
  shell\open\command
    (Default) = "\"C:\Path\To\AutoBrowser.exe\" \"%1\""
```

### Appendix C: Compatibility & Constraints

- Windows 10 (1809+) / Windows 11; .NET 10 runtime (framework-dependent) or self-contained.
- Portable, no install, no admin; all state in `Data/`.
- Tech stack: WPF + WPF UI (Lepo.co), CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection, Serilog, System.CommandLine (AutoUpdater), xUnit + FlaUI.

### Open Questions

1. Should URL query strings be redacted from Info-level logs (privacy hardening)?
2. Should SC-1 match accuracy be measured by opt-in telemetry or purely by tests + Test URL usage?
3. Does Windows Search interception belong behind a "beta" flag until v1.2?

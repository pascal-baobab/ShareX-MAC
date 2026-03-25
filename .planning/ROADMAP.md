# Roadmap: ShareXMac

## Overview

ShareXMac is built bottom-up from the native macOS layer outward. Phase 1 establishes the Avalonia app shell with confirmed ScreenCaptureKit integration, permissions onboarding, and Developer ID signing — every subsequent phase depends on this being correct. Phase 2 delivers complete screenshot capture across all modes plus local output and history. Phase 3 wires the workflow engine and full hotkey configuration, making the after-capture pipeline configurable. Phase 4 builds the annotation editor, completing the "capture, annotate, share" core loop. Phase 5 adds all upload destinations and cloud sharing. Phase 6 delivers screen recording, GIF, and advanced capture utilities. The product is usable after Phase 3; each subsequent phase adds significant capability.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Foundation and App Shell** - Avalonia app runs on macOS with TrayIcon, permissions onboarding, ScreenCaptureKit integration, and notarized signing pipeline
- [ ] **Phase 2: Core Capture** - Region, window, and full-screen screenshots work end-to-end with clipboard copy, local save, history, and preview thumbnail
- [ ] **Phase 3: Workflow Engine and Hotkeys** - After-capture workflow is configurable per action, hotkeys are user-configurable, and the full settings window is complete
- [ ] **Phase 4: Annotation Editor** - Full annotation canvas with arrows, text, shapes, highlight, blur, crop, and image beautifier; pin/float screenshot window
- [ ] **Phase 5: Upload Destinations and Sharing** - Imgur, S3, Cloudflare R2, Google Cloud Storage, custom HTTP uploader, URL shortening, and QR code generation
- [ ] **Phase 6: Recording, GIF, and Advanced Capture** - Screen recording to MP4, GIF capture, scrolling capture, OCR, color picker, and pixel ruler

## Phase Details

### Phase 1: Foundation and App Shell
**Goal**: A notarized Avalonia macOS app runs from the menu bar, requests permissions correctly, registers a global hotkey, and can take one confirmed screenshot via ScreenCaptureKit
**Depends on**: Nothing (first phase)
**Requirements**: FOUND-01, FOUND-02, FOUND-03, FOUND-04, FOUND-05
**Success Criteria** (what must be TRUE):
  1. App launches and appears as a TrayIcon in the macOS menu bar with a dropdown menu for capture actions
  2. Full settings/history window opens from the menu bar icon
  3. On first launch, user is walked through granting Screen Recording permission with clear guidance; denied permission is recoverable, not fatal
  4. On first launch requiring accessibility, user is walked through granting Input Monitoring permission with clear guidance
  5. Pressing the configured global hotkey while the app is in the background fires the registered callback (verified by log or placeholder action)
**Plans**: 3 plans

Plans:
- [x] 01-01-PLAN.md — Solution scaffold: three-project structure, NuGet packages, entitlements.plist, Info.plist
- [x] 01-02-PLAN.md — macOS interop layer: TccPermissionManager (TCC P/Invoke) + ScreenCaptureKitBridge (one-screenshot proof)
- [ ] 01-03-PLAN.md — App shell: TrayIcon + NativeMenu, PermissionWizardWindow, MainWindow skeleton, HotkeyService

### Phase 2: Core Capture
**Goal**: Users can capture any region, window, or full screen and the result lands in clipboard, local file, history, and a preview thumbnail — the complete screenshot → output loop
**Depends on**: Phase 1
**Requirements**: CAPT-01, CAPT-02, CAPT-03, CAPT-04, CAPT-05, CAPT-06, OUT-01, OUT-02, OUT-03, OUT-04, OUT-05
**Success Criteria** (what must be TRUE):
  1. User can drag-select a screen region and capture it; the region selector shows a crosshair cursor with real-time dimension overlay
  2. User can capture a specific application window (with and without drop shadow) using the system window picker
  3. User can capture the full screen (active monitor or all monitors)
  4. User can freeze the screen to capture transient UI (tooltips, menus, hover states)
  5. A preview thumbnail appears after every capture with quick action buttons (copy, save, annotate)
  6. Captures are automatically copied to clipboard and saved to the configured local directory with a configurable filename template
  7. Capture history shows thumbnails of past captures with re-share and re-edit options; history is searchable and filterable
**Plans**: TBD
**UI hint**: yes

### Phase 3: Workflow Engine and Hotkeys
**Goal**: Every capture action has a configurable after-capture workflow (save, copy, upload, edit — any combination), hotkeys are user-configurable in settings, and the full settings UI is complete
**Depends on**: Phase 2
**Requirements**: FOUND-06, FOUND-07
**Success Criteria** (what must be TRUE):
  1. User can configure a per-action after-capture workflow (e.g., "region capture: save + copy + open editor") from the settings window
  2. User can remap any capture action to a custom global hotkey from the settings window; the new hotkey fires the correct action in the background
  3. Changing the workflow configuration takes effect on the next capture without restarting the app
**Plans**: TBD
**UI hint**: yes

### Phase 4: Annotation Editor and Image Effects
**Goal**: Users can open a captured image in an annotation canvas, draw arrows, add text, redact regions, crop, and apply image beautifier effects, then save or share the result
**Depends on**: Phase 2
**Requirements**: EDIT-01, EDIT-02, EDIT-03, EDIT-04, EDIT-05, EDIT-06, EDIT-07, EDIT-08, EDIT-09, UTIL-04
**Success Criteria** (what must be TRUE):
  1. User can draw arrows, add text labels, draw shapes (rectangle, ellipse, line), highlight regions, and blur/pixelate areas on a captured image
  2. User can add numbered step/counter annotations to a captured image
  3. User can crop a captured image and apply padding, background, shadow, and rounded corners (image beautifier)
  4. All annotation tools have configurable color, thickness, and font size
  5. User can pin a screenshot as an always-on-top floating reference window that stays visible while working in other apps
**Plans**: TBD
**UI hint**: yes

### Phase 5: Upload Destinations and Sharing
**Goal**: Users can upload captures to Imgur, S3, Cloudflare R2, Google Cloud Storage, or any custom HTTP endpoint; the upload URL is auto-copied to clipboard; URL shortening and QR code generation are available
**Depends on**: Phase 2
**Requirements**: UPLD-01, UPLD-02, UPLD-03, UPLD-04, UPLD-05, UPLD-06, UPLD-07, UPLD-08, UTIL-05
**Success Criteria** (what must be TRUE):
  1. User can upload a capture to Imgur anonymously and get a shareable link automatically copied to clipboard
  2. User can authenticate with Imgur via OAuth2 and upload to their account
  3. User can upload captures to Amazon S3, Cloudflare R2, and Google Cloud Storage using configured credentials stored in macOS Keychain
  4. User can configure a custom HTTP upload endpoint using the ShareX JSON format and upload to it
  5. User can shorten an upload URL via a configured URL shortener service (e.g., bit.ly)
  6. User can generate a QR code from the clipboard URL or last upload link
**Plans**: TBD

### Phase 6: Recording, GIF, and Advanced Capture
**Goal**: Users can record screen regions or full screen to MP4 or animated GIF, scroll-capture long pages, extract text via OCR, pick screen colors, and measure pixel distances
**Depends on**: Phase 2
**Requirements**: REC-01, REC-02, REC-03, REC-04, REC-05, REC-06, UTIL-01, UTIL-02, UTIL-03
**Success Criteria** (what must be TRUE):
  1. User can record a selected screen region or full screen to MP4 video; an elapsed-time indicator and stop button are visible during recording
  2. User can capture a selected region or full screen as an animated GIF
  3. User can scroll-capture a long web page or document by triggering auto-scroll and receiving a stitched full-length screenshot
  4. User can drag-select a screen region and extract all text from it via OCR, with the result copied to clipboard
  5. User can sample any pixel on screen to get its hex, RGB, and HSL color values
  6. User can measure pixel distances on screen with a ruler overlay
**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6

Note: Phases 3, 4, 5, and 6 all depend on Phase 2 but have no dependency on each other — they can be sequenced in any order after Phase 2 is complete. The order above reflects recommended priority.

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation and App Shell | 1/3 | In Progress|  |
| 2. Core Capture | 0/? | Not started | - |
| 3. Workflow Engine and Hotkeys | 0/? | Not started | - |
| 4. Annotation Editor and Image Effects | 0/? | Not started | - |
| 5. Upload Destinations and Sharing | 0/? | Not started | - |
| 6. Recording, GIF, and Advanced Capture | 0/? | Not started | - |

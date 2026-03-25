# Requirements: ShareXMac

**Defined:** 2026-03-25
**Core Value:** Reliable screen capture and instant sharing on macOS — capture anything, share it anywhere, in one keystroke.

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Foundation

- [ ] **FOUND-01**: App launches as menu bar icon with dropdown menu for all capture actions
- [ ] **FOUND-02**: App has a full settings/history window accessible from menu bar
- [ ] **FOUND-03**: macOS Screen Recording permission is requested on first launch with clear guidance
- [ ] **FOUND-04**: macOS Accessibility permission is requested when needed with clear guidance
- [ ] **FOUND-05**: Global hotkeys trigger capture actions when app is in background
- [ ] **FOUND-06**: Hotkeys are user-configurable in settings
- [ ] **FOUND-07**: After-capture workflow is configurable per action (save, copy, upload, edit — any combination)

### Capture

- [ ] **CAPT-01**: User can capture a selected region of the screen
- [ ] **CAPT-02**: User can capture a specific application window (with/without shadow)
- [ ] **CAPT-03**: User can capture the full screen (active monitor or all monitors)
- [ ] **CAPT-04**: User can freeze the screen to capture transient UI (tooltips, menus, dropdowns)
- [ ] **CAPT-05**: Region selector shows crosshair cursor with dimension overlay
- [ ] **CAPT-06**: Capture preview thumbnail appears after capture for quick actions

### Recording

- [ ] **REC-01**: User can record a selected screen region to MP4 video
- [ ] **REC-02**: User can record the full screen to MP4 video
- [ ] **REC-03**: User can capture a selected region as animated GIF
- [ ] **REC-04**: User can capture the full screen as animated GIF
- [ ] **REC-05**: Recording shows elapsed time and stop button
- [ ] **REC-06**: User can scroll-capture a long page (auto-scroll and stitch)

### Annotation & Editing

- [ ] **EDIT-01**: User can draw arrows on captured images
- [ ] **EDIT-02**: User can add text labels to captured images
- [ ] **EDIT-03**: User can draw shapes (rectangle, ellipse, line) on captured images
- [ ] **EDIT-04**: User can highlight regions on captured images
- [ ] **EDIT-05**: User can blur/pixelate regions to redact sensitive info
- [ ] **EDIT-06**: User can add numbered step/counter annotations
- [ ] **EDIT-07**: User can add padding, background, shadow, rounded corners (image beautifier)
- [ ] **EDIT-08**: User can crop captured images
- [ ] **EDIT-09**: Annotation tools have configurable color, thickness, font size

### Upload & Sharing

- [ ] **UPLD-01**: User can upload captures to Imgur (anonymous)
- [ ] **UPLD-02**: User can upload captures to Imgur (authenticated with OAuth)
- [ ] **UPLD-03**: User can upload captures to Amazon S3
- [ ] **UPLD-04**: User can upload captures to Cloudflare R2
- [ ] **UPLD-05**: User can upload captures to Google Cloud Storage
- [ ] **UPLD-06**: User can configure custom HTTP upload endpoints (ShareX JSON format)
- [ ] **UPLD-07**: Upload URL is automatically copied to clipboard on success
- [ ] **UPLD-08**: User can shorten upload URLs via configurable URL shortener (bit.ly etc.)

### Output & Storage

- [ ] **OUT-01**: Captures are automatically copied to clipboard
- [ ] **OUT-02**: Captures are saved to a user-configurable local directory
- [ ] **OUT-03**: Filename template is configurable (date, time, type patterns)
- [ ] **OUT-04**: Capture history shows thumbnails with re-share and re-edit options
- [ ] **OUT-05**: History is searchable and filterable

### Utilities

- [ ] **UTIL-01**: User can extract text from a screen region via OCR
- [ ] **UTIL-02**: User can sample colors from screen with hex/RGB/HSL output
- [ ] **UTIL-03**: User can measure pixel distances on screen with ruler overlay
- [ ] **UTIL-04**: User can pin/float a screenshot as always-on-top reference window
- [ ] **UTIL-05**: User can generate QR code from clipboard URL or last upload link

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Advanced Recording

- **AREC-01**: Webcam overlay during screen recording
- **AREC-02**: System audio capture during recording
- **AREC-03**: Video trimming (start/end point) before save

### Advanced Sharing

- **ASHR-01**: User can share directly to Slack/Discord via webhooks
- **ASHR-02**: User can auto-watermark captures before sharing

### Desktop Enhancements

- **DESK-01**: Desktop icon hiding for clean screenshots
- **DESK-02**: Auto-screenshot scheduler (time interval capture)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Video editor (timeline, transitions, effects) | Scope creep — not a video editing tool |
| Social media direct posting | API fragility, OAuth maintenance per platform |
| Browser extension | Separate product surface, native capture is sufficient |
| iOS/mobile companion | Desktop-first, revisit after v1 |
| Real-time collaboration on screenshots | Not a collaboration tool |
| Built-in cloud storage service | Requires backend infrastructure, billing, GDPR |
| Full document OCR indexing | OCR-to-clipboard is the use case, not searchable history |
| Template system for documentation | Enterprise niche, high complexity |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| (To be filled by roadmapper) | | |

**Coverage:**
- v1 requirements: 38 total
- Mapped to phases: 0
- Unmapped: 38

---
*Requirements defined: 2026-03-25*
*Last updated: 2026-03-25 after initial definition*

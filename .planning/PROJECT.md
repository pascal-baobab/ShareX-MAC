# ShareXMac

## What This Is

A native macOS port of ShareX built with C# and Avalonia UI, adapting the original ShareX codebase to run on macOS. It provides screen capture (screenshots, recordings, GIFs), image annotation/editing, and flexible sharing (clipboard, local save, upload to Imgur/cloud storage/custom destinations) — all accessible from the menu bar or a full app window.

## Core Value

Reliable screen capture and instant sharing on macOS — capture anything, share it anywhere, in one keystroke.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] Screen capture (full screen, region, window)
- [ ] Screen recording (region or full screen, output to video)
- [ ] GIF capture (region or full screen)
- [ ] Image annotation and editing (arrows, text, highlights, blur)
- [ ] Clipboard integration (copy capture to clipboard)
- [ ] Local file save with configurable output directory
- [ ] Upload to Imgur (anonymous and authenticated)
- [ ] Upload to cloud storage (S3, Google Cloud, Cloudflare R2)
- [ ] Custom uploader support (user-configurable endpoints)
- [ ] Global hotkey system for all capture actions
- [ ] Menu bar presence with quick-access controls
- [ ] Full app window for settings, history, and configuration
- [ ] Capture history with thumbnails and re-share capability
- [ ] After-capture workflow (copy, save, upload, edit — configurable)
- [ ] OCR from screen captures

### Out of Scope

- iOS/mobile companion app — desktop-first, mobile later
- Video editing beyond trimming — keep it simple, not a video editor
- Social media direct posting — upload + link is sufficient
- Browser extension — native app only for v1

## Context

- Adapting from the original ShareX C# codebase (github.com/pascal-baobab/ShareX-MAC)
- ShareX has mature libraries: ScreenCaptureLib, ImageEditor, UploadersLib, HistoryLib, MediaLib
- Avalonia UI chosen for cross-platform C# UI on macOS — better Mac support than .NET MAUI
- macOS has specific APIs for screen capture (ScreenCaptureKit), accessibility permissions, and menu bar apps
- The original ShareX uses Windows-specific APIs (GDI+, DirectX) that need macOS equivalents
- Key challenge: replacing Windows capture/recording APIs with macOS ScreenCaptureKit and AVFoundation

## Constraints

- **Platform**: macOS only (Avalonia UI targeting Mac)
- **Language**: C# / .NET with Avalonia UI framework
- **Permissions**: macOS requires explicit user permission for screen recording and accessibility
- **Capture API**: Must use ScreenCaptureKit (macOS 12.3+) or CGWindowList for screenshots
- **Recording**: AVFoundation or ScreenCaptureKit for video/GIF capture
- **Code reuse**: Maximize reuse from original ShareX libraries, replace Windows-specific parts

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Avalonia UI over SwiftUI | Stay closer to original C# codebase, reuse ShareX libraries | — Pending |
| Menu bar + full window | Best of both worlds — quick access and full configuration | — Pending |
| ScreenCaptureKit for capture | Modern macOS API, better performance than legacy CGWindowList | — Pending |
| All ShareX uploaders in scope | User wants feature parity with original upload destinations | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd:transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd:complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-03-25 after initialization*

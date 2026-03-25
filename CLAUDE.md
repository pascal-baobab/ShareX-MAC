<!-- GSD:project-start source:PROJECT.md -->
## Project

**ShareXMac**

A native macOS port of ShareX built with C# and Avalonia UI, adapting the original ShareX codebase to run on macOS. It provides screen capture (screenshots, recordings, GIFs), image annotation/editing, and flexible sharing (clipboard, local save, upload to Imgur/cloud storage/custom destinations) — all accessible from the menu bar or a full app window.

**Core Value:** Reliable screen capture and instant sharing on macOS — capture anything, share it anywhere, in one keystroke.

### Constraints

- **Platform**: macOS only (Avalonia UI targeting Mac)
- **Language**: C# / .NET with Avalonia UI framework
- **Permissions**: macOS requires explicit user permission for screen recording and accessibility
- **Capture API**: Must use ScreenCaptureKit (macOS 12.3+) or CGWindowList for screenshots
- **Recording**: AVFoundation or ScreenCaptureKit for video/GIF capture
- **Code reuse**: Maximize reuse from original ShareX libraries, replace Windows-specific parts
<!-- GSD:project-end -->

<!-- GSD:stack-start source:research/STACK.md -->
## Technology Stack

## Recommended Stack
### Core Framework
| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| .NET | 9.0 | Runtime | Current non-LTS; .NET 8 is LTS alternative. Both supported by Avalonia 11. Use .NET 9 for best macOS ARM performance; switch to .NET 10 (LTS) when it releases. |
| Avalonia UI | 11.3.12 | Cross-platform XAML UI | Only mature C# UI framework with real macOS support. Uses Skia rendering on Metal. Better Mac support than .NET MAUI. Actively maintained, 12k+ GitHub stars. |
| Avalonia.Desktop | 11.3.12 | Desktop platform targets | Bundles macOS + Windows + Linux platform backends. Required for single-project cross-platform builds. |
| Avalonia.Native | 11.3.12 | macOS Objective-C bridge | The platform-specific backend. Exposes MicroCom interop for calling Cocoa APIs from C#. Required for NSStatusItem, NSWindow handle access, and native menu integration. |
| C# | 13 | Language | Version shipped with .NET 9. No change needed. |
### macOS Native APIs (accessed via P/Invoke or net9.0-macos TFM)
| API | Access Method | Purpose | Notes |
|-----|--------------|---------|-------|
| ScreenCaptureKit (`SCStream`, `SCShareableContent`, `SCContentFilter`) | `net9.0-macos` TFM bindings via `dotnet/macios` | Screenshots and screen recording (macOS 12.3+) | Modern Apple API, replaces deprecated `CGWindowListCreateImage`. Use for all capture modes. Requires Screen Recording permission. |
| AVFoundation (`AVAssetWriter`, `AVAssetWriterInput`) | `net9.0-macos` TFM bindings | Writing video files from captured frames | SCStream provides raw CMSampleBuffers; AVFoundation writes them to disk as MP4/MOV. |
| Vision Framework | `net9.0-macos` TFM bindings | OCR (text recognition from screenshots) | Apple's native on-device OCR. Free, fast, no API key needed. Preferred over Tesseract for macOS-specific builds. |
| CGWindowList (`CGWindowListCopyWindowInfo`) | P/Invoke via `CoreGraphics` | Window enumeration (list open windows) | Needed even with ScreenCaptureKit to enumerate windows for selection UI. |
| NSStatusItem / NSStatusBar | Avalonia.Native / P/Invoke | Menu bar icon | Avalonia TrayIcon wraps NSStatusItem. For advanced customization (template images, variable-width), direct P/Invoke is required. |
| CGEventTap | P/Invoke (via SharpHook) | Global hotkeys, input monitoring | Requires Input Monitoring permission (not Accessibility). Available to sandboxed apps. |
| NSPasteboard | P/Invoke | Clipboard integration | Copy screenshots and text to clipboard. |
### Image Processing
| Library | Version | Purpose | Why |
|---------|---------|---------|-----|
| SixLabors.ImageSharp | 3.1.12 | Image decode/encode, GIF creation, format conversion | Pure managed .NET. Cross-platform. No GDI+ or System.Drawing dependency (which is Windows-specific). Required replacement for ShareX's GDI+ image handling. Built-in animated GIF support with frame metadata. |
| SixLabors.ImageSharp.Drawing | 2.1.7 | Drawing primitives (lines, rects, text) on images | Companion to ImageSharp for raster annotation operations. Use for blur, highlight, and shape overlays in the annotation editor. |
| SkiaSharp | 3.x (bundled with Avalonia) | Canvas rendering for annotation UI | Avalonia uses SkiaSharp internally. Use `ICustomDrawOperation` to render annotation canvas directly in the Avalonia render pass. Eliminates a second image library for UI drawing. |
- `System.Drawing` / `System.Drawing.Common`: Windows-only on .NET 6+. Throws `PlatformNotSupportedException` on macOS unless a compatibility shim is used. ShareX uses this extensively — all usages must be replaced.
- GDI+: Windows-only. Same as above.
- `System.Windows.Media.GifBitmapEncoder`: WPF-only. Replace with ImageSharp's animated GIF encoder.
### Video and GIF
| Library | Version | Purpose | Why |
|---------|---------|---------|-----|
| FFMpegCore | 5.4.0 | Video encoding (MP4 output from frame sequences) | Clean .NET wrapper over FFmpeg binary. Cross-platform. Handles frame-to-MP4 pipeline. Requires bundling FFmpeg binary (macOS arm64 + x64). |
| SixLabors.ImageSharp (GIF) | 3.1.12 | Animated GIF encoding | ImageSharp's `GifEncoder` handles animated GIF creation directly from frame sequences. Avoids FFmpeg for GIF output (simpler, no binary dependency for GIF). |
### Global Hotkeys
| Library | Version | Purpose | Why |
|---------|---------|---------|-----|
| SharpHook | 7.1.1 | Global keyboard/mouse hooks | Cross-platform wrapper over `libuiohook`. Uses CGEventTap on macOS with Input Monitoring permission (not Accessibility, which is harder to grant). Supports async event handlers. Actively maintained (updated Dec 2025). |
| SharpHook.Reactive | 7.1.1 | Reactive extensions for SharpHook | Use if the rest of the app uses ReactiveUI/Rx; provides `IObservable<HookEvent>` interface for hotkey streams. |
- Avalonia's built-in `KeyBindings`: Application-scoped only. Does not fire when the app window is hidden or another app is focused. Useless for a menu bar app where the main window may never be open.
- Raw CGEventTap P/Invoke without SharpHook: Feasible but verbose. SharpHook provides a cleaner API and handles edge cases (thread safety, event suppression).
### OCR
| Library | Version | Purpose | Why |
|---------|---------|---------|-----|
| Apple Vision Framework (via net9.0-macos bindings) | macOS 11+ | OCR text recognition from captures | Native, on-device, no dependencies, no cost, high accuracy. Prefer this over any Tesseract wrapper for macOS-exclusive builds. |
| TesseractOCR (fallback) | 5.5.2 | Fallback OCR if Vision interop proves difficult | Free, open-source, works in sandboxed apps. More complex setup (native libs must be bundled). Use only if Vision framework binding complexity blocks progress. |
### Data Persistence (History)
| Library | Version | Purpose | Why |
|---------|---------|---------|-----|
| Microsoft.Data.Sqlite | 9.x | Capture history database | Ships with .NET 9. SQLite is the standard embedded DB for desktop apps. Proven with Avalonia. |
| SQLite-net-pcl | 1.9.x | ORM for SQLite | Lightweight ActiveRecord-style ORM. No EF Core overhead needed for a simple history/thumbnails schema. |
### MVVM / Architecture
| Library | Version | Purpose | Why |
|---------|---------|---------|-----|
| Avalonia.ReactiveUI | 11.3.12 | MVVM framework | Avalonia ships its own ReactiveUI fork. Preferred over CommunityToolkit.Mvvm because Avalonia templates default to it and it provides `RoutedViewHost` for navigation between panels. |
| ReactiveUI | (transitive) | Rx-based MVVM primitives | Provides `ReactiveCommand`, `ObservableAsPropertyHelper`, and `WhenAnyValue`. Pairs well with SharpHook.Reactive for hotkey handling. |
- CommunityToolkit.Mvvm: Good alternative but Avalonia's own templates and documentation lean on ReactiveUI. Mixing both adds confusion. Pick one.
- Prism: Overkill for a single-process desktop app without plugin architecture requirements.
### Upload / Networking
| Library | Version | Purpose | Why |
|---------|---------|---------|-----|
| System.Net.Http.HttpClient | Built-in (.NET 9) | HTTP uploads (Imgur, custom endpoints) | No external library needed for straightforward REST/multipart uploads. Use `IHttpClientFactory` for lifetime management. |
| AWSSDK.S3 | 4.0.18.4 | S3 and Cloudflare R2 uploads | AWS SDK v4 is current (Feb 2026). R2 uses the S3-compatible API — same SDK, different endpoint URL. |
| Google.Apis.Storage.v1 | Latest stable | Google Cloud Storage uploads | Official Google client library. Handles OAuth flow. |
| Imgur.API | 5.0.0 | Imgur-specific upload API | Thin wrapper over Imgur v3 API. Handles both anonymous and authenticated (OAuth2) uploads. |
### Distribution and Packaging
| Tool | Purpose | Why |
|------|---------|-----|
| `dotnet publish` with `osx-arm64` + `osx-x64` RIDs | Build native binaries | Produce self-contained ARM64 and x64 binaries. |
| `lipo` (macOS tool) | Create universal binary | Merges arm64 + x64 into a single universal binary. |
| Apple Developer ID | Code signing | Required for notarization. Without it, Gatekeeper blocks the app. |
| `xcrun notarytool` | Notarization | Apple's automated scan. Required for non-App Store distribution since macOS 10.15. |
| Parcel (Avalonia tool) | App bundle packaging | Automates `Info.plist` generation, code signing, notarization, and DMG creation. Recommended by Avalonia docs over manual scripts. |
## Project Structure (Recommended)
## Alternatives Considered
| Category | Recommended | Alternative | Why Not |
|----------|-------------|-------------|---------|
| UI Framework | Avalonia 11 | .NET MAUI | MAUI's macOS support (Mac Catalyst) is immature and wraps UIKit rather than AppKit. Worse native feel, less community usage for desktop apps. |
| UI Framework | Avalonia 11 | SwiftUI + C# via Swift interop | Abandons C# codebase reuse. Swift interop in .NET is experimental (designs/proposed/swift-interop.md not yet shipped). |
| Hotkeys | SharpHook | Direct CGEventTap P/Invoke | SharpHook provides the same mechanism with a clean API. No reason to re-implement. |
| Image processing | ImageSharp | SkiaSharp direct | SkiaSharp lacks an animated GIF encoder and high-level image I/O API. Use SkiaSharp for rendering, ImageSharp for encode/decode/GIF. |
| OCR | Vision (native) | IronOCR | IronOCR is commercial ($), adds a dependency, and wraps Tesseract anyway. Apple Vision is free, native, and on-device. |
| Database | SQLite | LiteDB | LiteDB is document-oriented; a relational schema (captures, uploads, tags) fits SQLite better. LiteDB's Studio is Avalonia-based but that's not a reason to use the DB. |
| Video encoding | FFMpegCore | AVFoundation direct | AVFoundation frame writing from C# is possible but requires deep net9.0-macos binding work. FFMpegCore provides a simpler pipeline with a well-known output format. AVFoundation is still used for capturing (via SCStream); FFMpeg is only for the encode step. |
| Cloud upload | AWSSDK.S3 v4 | Minio .NET SDK | AWSSDK is the authoritative library for S3-compatible APIs. Cloudflare R2 documents using S3-compatible clients; the AWS SDK works with R2 by setting a custom endpoint URL. |
## Installation
# Core framework
# Core packages
# Image processing
# Video
# Hotkeys
# History
# Uploaders
# macOS platform project (net9.0-macos target)
# ScreenCaptureKit, AVFoundation, Vision Framework bindings come
# automatically with the net9.0-macos TFM via dotnet workload install macos
## macOS Permission Requirements (Info.plist)
## Sources
- [Avalonia NuGet Gallery (v11.3.12)](https://www.nuget.org/packages/avalonia)
- [Avalonia Supported Platforms Docs](https://docs.avaloniaui.net/docs/supported-platforms)
- [Avalonia macOS Deployment Guide](https://docs.avaloniaui.net/docs/deployment/macOS)
- [Avalonia TrayIcon Docs](https://docs.avaloniaui.net/docs/reference/controls/tray-icon)
- [dotnet/macios — .NET for macOS SDK bindings](https://github.com/dotnet/macios)
- [ScreenCaptureKit Xcode 16.2 bindings (dotnet/macios)](https://github.com/dotnet/macios/wiki/ScreenCaptureKit-macOS-xcode16.2-b2)
- [Apple ScreenCaptureKit Developer Docs](https://developer.apple.com/documentation/ScreenCaptureKit/capturing-screen-content-in-macos)
- [SharpHook NuGet (v7.1.1)](https://www.nuget.org/packages/SharpHook/)
- [SharpHook GitHub](https://github.com/TolikPylypchuk/SharpHook)
- [SixLabors.ImageSharp NuGet (v3.1.12)](https://www.nuget.org/packages/sixlabors.imagesharp/)
- [SixLabors ImageSharp Animated GIF Docs](https://docs.sixlabors.com/articles/imagesharp/animatedgif.html)
- [FFMpegCore NuGet (v5.4.0)](https://www.nuget.org/packages/FFMpegCore)
- [AWSSDK.S3 NuGet (v4.0.18.4)](https://www.nuget.org/packages/AWSSDK.S3)
- [Imgur.API NuGet (v5.0.0)](https://www.nuget.org/packages/Imgur.API)
- [Avalonia.ReactiveUI NuGet (v11.3.12)](https://www.nuget.org/packages/Avalonia.ReactiveUI/)
- [Avalonia Native Platform Interop Docs](https://docs.avaloniaui.net/docs/app-development/native-interop)
- [macOS Notarization — Avalonia Guide](https://avaloniaui.net/blog/the-definitive-guide-to-building-and-deploying-avalonia-applications-for-macos)
- [CGEventTap and Input Monitoring — Apple Developer Forums](https://developer.apple.com/forums/thread/744440)
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
## Conventions

Conventions not yet established. Will populate as patterns emerge during development.
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

Architecture not yet mapped. Follow existing patterns found in the codebase.
<!-- GSD:architecture-end -->

<!-- GSD:workflow-start source:GSD defaults -->
## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd:quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd:debug` for investigation and bug fixing
- `/gsd:execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->



<!-- GSD:profile-start -->
## Developer Profile

> Profile not yet configured. Run `/gsd:profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->

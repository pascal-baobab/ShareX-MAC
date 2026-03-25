# Feature Landscape

**Domain:** macOS screen capture, annotation, and sharing app (ShareX port)
**Researched:** 2026-03-25
**Competitive references:** ShareX (Windows), CleanShot X, Snagit, Shottr, Kap, macOS built-in Screenshot.app

---

## Table Stakes

Features users expect from any serious macOS screen capture app. Missing these = users dismiss the product
immediately or reach for CleanShot X instead.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Region/area capture | Core of every screenshot tool; macOS native has it | Low | Selection overlay with crosshair cursor |
| Window capture | Single-app capture is daily workflow | Low | macOS has window detection APIs; shadow handling needed |
| Full-screen capture | Fastest single-keystroke capture | Low | All monitors, or active monitor |
| Global hotkeys | Must work when app is in background or hidden | Medium | macOS requires Accessibility permission; conflict detection |
| Menu bar presence | macOS convention for utility apps; quick access without full window | Low | Status bar item; standard NSStatusItem / Avalonia equivalent |
| Clipboard copy after capture | Every screenshot user expects Cmd+C behavior | Low | Auto-copy or after-capture toggle |
| Local file save with path config | Users want captures organized, not dumped on Desktop | Low | Configurable directory, filename template |
| Basic annotation (arrows, text, shapes, highlight) | Without annotations, you just use Command+Shift+4 | Medium | Rectangle, ellipse, arrow, text label, highlight — minimum viable editor |
| Blur / redact sensitive info | Expected by anyone sharing screenshots of work tools | Medium | Gaussian blur over selected region; pixelate as alternative |
| Capture history / recents | "I just took that screenshot — where did it go?" | Medium | Thumbnail list, quick re-access; even 20-item history is enough |
| After-capture workflow configuration | Power users configure: save AND copy AND upload | Medium | Sequential task list per hotkey; ShareX's model is the gold standard here |
| Screenshot preview / quick look | See the capture before committing | Low | Floating preview thumbnail, dismissable |
| macOS permissions flow | App is useless without Screen Recording permission; smooth onboarding matters | Low | System Settings deep link, clear messaging |

---

## Differentiators

Features that set the product apart. Not universally expected, but create strong loyalty and word-of-mouth.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Scrolling capture | macOS has NO native scrolling screenshot — huge gap | High | Auto-scroll + stitch; both vertical and horizontal; needs accessibility permission to drive scroll events |
| Screen recording to video | Kap and CleanShot offer this; ShareX Windows has it; users expect it from a "ShareX port" | High | AVFoundation / ScreenCaptureKit; region or full screen; MP4 output |
| GIF capture | Highly requested for short demos, bug reports; GIFs are shareable anywhere | High | Frame capture loop → GIF encoding; needs FFmpeg or native GIF encoder; quality vs file size tuning required |
| OCR (text from screen) | Eliminates manual retyping; CleanShot X and Shottr both have it; macOS Vision framework makes it feasible | Medium | macOS Vision framework (VNRecognizeTextRequest); no third-party dependency needed on Apple Silicon |
| Upload to Imgur (anonymous + authenticated) | ShareX Windows power feature; instant shareable link; no cloud account needed for quick sharing | Medium | Imgur API v3; anonymous uploads work without OAuth; authenticated uploads need OAuth2 flow |
| Upload to S3 / R2 / GCS (cloud storage) | Developer and power user segment; self-hosted links; ShareX Windows has this; differentiates from CleanShot X | High | Per-provider credential config; presigned URL generation; copy link to clipboard on success |
| Custom uploader (configurable HTTP endpoint) | ShareX's killer differentiator; supports any upload service with JSON config | High | JSON schema for endpoint, headers, response parsing; ShareX's existing UploadersLib JSON format reusable |
| Pin / float screenshot (always-on-top window) | Useful for reference while typing; Shottr and Xnip have it; CleanShot X has it | Medium | Borderless floating NSWindow / Avalonia overlay window; drag to reposition |
| Freeze screen capture (capture transient UI) | Capture hover states, dropdown menus, tooltips that disappear on click | Medium | Render screen to bitmap, freeze display, then allow region selection over frozen image |
| Desktop icon hiding for clean screenshots | CleanShot X differentiator; prevents cluttered backgrounds in shared screenshots | Low | Temporarily hide ~/Desktop items via AppleScript or file rename trick |
| Image beautifier (background/padding/shadow) | Trendy for sharing on Twitter/LinkedIn; makes plain screenshots look polished | Medium | Add configurable padding, gradient/solid background, rounded corners, drop shadow |
| URL shortening after upload | Post-upload step: shorten the link via bit.ly, etc. | Low | Optional API call after upload; toggleable per destination |
| QR code from URL | Useful for sharing links to mobile; ShareX has it as a utility | Low | Generate QR code PNG from clipboard URL or last upload link |
| Color picker (screen sampling) | Designers and developers; Shottr differentiates on this; no good native alternative | Low | Magnified lens + hex/RGB/HSL output; system color picker is adequate but clunky |
| Ruler / measurement overlay | Pixel-precise measurement for designers; Shottr's calling card | Low | Overlay with px/pt distance indicator; show dimensions of selected area |
| Step/counter annotations | Numbered callouts for tutorials and documentation; Snagit strength | Medium | Auto-incrementing bubble numbers; popular for walkthroughs |

---

## Anti-Features

Features to deliberately NOT build, at least for v1. Building these wastes time and adds maintenance burden
without proportional user value.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Video editor (trim + cut + effects) | This is Final Cut / iMovie territory; Snagit tries to do this and it shows; keeping it simple is a feature | Support trim-only (start/end point); no transitions, filters, or timeline editing |
| Social media direct posting (Twitter, Slack, etc.) | API fragility, OAuth maintenance per platform, constant breakage | Upload to cloud → copy link → user pastes. That is the workflow. |
| Browser extension | Separate product surface; maintenance overhead; out of scope in PROJECT.md | Native app captures browser windows fine; scrolling capture covers the web page use case |
| iOS / mobile companion | Desktop-first; adds App Store distribution complexity | Focus on macOS; revisit after v1 ships |
| Real-time collaboration on screenshots (Figma-style) | Not a screen capture feature; this is a document collaboration product | Share link → recipient opens in browser; that is collaboration enough |
| Built-in cloud storage service (like CleanShot Cloud) | Requires backend infrastructure, billing, storage costs, GDPR compliance | Integrate with existing services (Imgur, S3, R2); do not build storage |
| Screen recording editor with webcam overlay | Complex; requires multi-track composition; scope creep | Webcam passthrough in recording is fine if trivial; no editing of combined tracks |
| Template system for documentation (Snagit-style) | Enterprise documentation feature; very niche; high complexity | Annotation tools + save to file is enough |
| Auto-screenshot scheduler (time interval capture) | Surveillance-adjacent; users almost never need this; ShareX has it but it is rarely used | Available as advanced setting if ShareX code reuse makes it trivial; do not design UI for it |
| Optical character recognition with full document indexing | OCR-to-clipboard is the use case; indexing all screenshots is a separate product | Copy text from screen region; do not build searchable OCR history |

---

## Feature Dependencies

These dependencies matter for phase planning — building in the wrong order blocks progress.

```
Global hotkey system
  └─> All capture modes (region, window, fullscreen, scrolling, recording)
  └─> After-capture workflow

macOS Screen Recording permission
  └─> All capture (ScreenCaptureKit requires it)
  └─> Screen recording / GIF capture

Capture (screenshot exists in memory)
  └─> Clipboard copy
  └─> Local file save
  └─> Upload to Imgur / S3 / custom
  └─> Annotation editor
  └─> Capture history

After-capture workflow engine
  └─> Configurable task chaining (save + copy + upload)
  └─> Per-hotkey workflow overrides

Annotation editor
  └─> All annotation tools (arrows, text, blur, shapes, step numbers)
  └─> Image beautifier (needs editing canvas)
  └─> Pin / float screenshot (needs rendered image output)

Upload destinations (Imgur, S3, R2, custom)
  └─> URL shortening (needs a URL to shorten)
  └─> QR code from URL (needs a URL)
  └─> Capture history with cloud link (needs upload result)

Screen recording (AVFoundation/ScreenCaptureKit)
  └─> GIF capture (video frames → GIF encoding)
  └─> Video output (MP4)

FFmpeg (or native encoder)
  └─> GIF capture output
  └─> Video format conversion
```

---

## MVP Recommendation

The minimum set that makes the product genuinely useful and competitive on day one.

**Prioritize for MVP:**
1. Region, window, and full-screen capture (table stakes — product is nothing without these)
2. Global hotkeys with macOS permission onboarding (required for any background capture)
3. Clipboard copy + local file save (users need a result from every capture)
4. Basic annotation: arrows, text, rectangle, highlight, blur (minimum to justify switching from Command+Shift+4)
5. Capture history with thumbnails (reduces "where did my screenshot go?" frustration)
6. Upload to Imgur (anonymous) — instant shareable link with zero config, highest-value upload feature
7. After-capture workflow engine — save + copy + upload chaining (this is the ShareX differentiator)
8. Menu bar presence (macOS utility convention; required for quick access)

**Defer to post-MVP:**
- Screen recording (video): High complexity, requires AVFoundation integration; ship separately
- GIF capture: Depends on recording pipeline; defer until recording is stable
- Scrolling capture: High complexity (auto-scroll + stitch); valuable but not blocking
- S3 / R2 / custom uploader: Valuable for power users, but Imgur covers the quick-share use case at launch
- OCR: Medium complexity; macOS Vision makes it feasible but not MVP-critical
- Image beautifier / background: Nice to have; not what makes users switch
- Freeze screen: Complex; useful but not table stakes for v1

**Rationale for ordering:**
The core value ("capture anything, share it anywhere, in one keystroke") requires capture + hotkeys + after-capture workflow. Everything else layers on top. Imgur upload ships the "share it anywhere" half of the value prop with minimal infrastructure. Recording and GIF require separate, complex pipelines and should not block the screenshot foundation.

---

## Sources

- CleanShot X features page: https://cleanshot.com/features
- CleanShot X changelog (2025 updates): https://cleanshot.com/changelog
- Shottr feature overview: https://shottr.cc/
- Shottr vs CleanShot X comparison: https://setapp.com/app-reviews/cleanshot-x-vs-shottr
- ShareX official site + feature list: https://getsharex.com/
- ShareX Windows features via web fetch (direct scrape above)
- Screenshot app comparison (12 best for Mac): https://blog.grabshot.io/best-screenshot-apps-for-mac/
- CleanShot X vs Shottr vs Snagit showdown: https://blog.apps.deals/2025-01-23-screenshot-tools-comparison
- Best screenshot apps for Mac 2026: https://www.screensnap.pro/blog/best-screenshot-apps-for-mac
- macOS built-in screenshot limitations: https://zight.com/blog/mac-screenshot-tool/
- Kap open-source screen recorder: https://github.com/wulkano/Kap
- Kap features review: https://www.screensnap.pro/blog/kap-screen-recorder-mac-review
- ShareX macOS port demand (GitHub issues): https://github.com/ShareX/ShareX/issues/498

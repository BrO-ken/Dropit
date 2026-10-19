# Dropit

🇬🇧 **English** · 🇸🇦 [العربية](README.ar.md)

**Click-free file conversion for Windows (and Linux).** Grab a file, tap a key while dragging, and drop it on a format in a radial wheel. Everything runs locally; your files never leave your PC.

Inspired by the wonderful [Tangerine](https://tangerineformac.com/) for macOS. Dropit is an independent Windows take on the idea and is not affiliated with Tangerine.

<p align="center">
  <img src="docs/screenshots/en/wheel-video-convert.png" width="32%" alt="Convert wheel for a video" />
  <img src="docs/screenshots/en/wheel-image-tools.png" width="32%" alt="Tools wheel for an image" />
  <img src="docs/screenshots/en/wheel-actions.png" width="32%" alt="Actions wheel" />
</p>
<p align="center">
  <img src="docs/screenshots/en/wheel-light.png" width="32%" alt="Light mode" />
  <img src="docs/screenshots/en/settings.png" width="32%" alt="Settings" />
  <img src="docs/screenshots/en/jobs.png" width="32%" alt="Progress" />
</p>

## How it works

1. Grab a file: in File Explorer, on the desktop, an image or link in your browser, an e-mail attachment, a file inside a ZIP folder…
2. While dragging, **tap** a key:

   | Key | Wheel | What it offers |
   |---|---|---|
   | **Shift** | Convert | JPG, PNG, WebP, MP4, MP3, DOCX, PDF, XLSX… depending on the file type |
   | **Ctrl** | Tools | compress, shrink, rotate, trim, merge, split, archive… |
   | **Alt** | Actions | copy to…, move to…, copy path, terminal, floating shelf, share |

3. Drop the file on a slice. The result is saved next to the original (the original is never changed). Drop it in the middle of the wheel to cancel.

The wheel stays open until you drop, so you can release the key and tap another one to switch wheels. Keys can be reassigned in Settings.

**Repeat the last action:** tap the same key again while dragging (or press **R** on an open wheel) and the wheel turns into a single slice that repeats what you did last, with the same settings (e.g. the same trim times).

**Your own wheel:** in **Settings → Customize wheels** you can star favourite slices (they always come first, in the same place) and hide the ones you never use.

**No mouse:** select files in File Explorer, or copy a file or a screenshot (**Win + Shift + S**), and press **Ctrl + Alt + Space**. Arrows pick a slice, Enter runs it, Tab switches wheels, Esc closes, 1–9 run a slice directly.
You can also open the wheel with **right-click → Dropit…** (on Windows 11 under “Show more options”).

Progress shows up in the corner of the screen, where you can open the result, show it in its folder or cancel.

## What it converts

| Type | From | To |
|---|---|---|
| Images | JPG, PNG, WebP, HEIC, TIFF, AVIF, BMP, GIF, ICO, SVG | JPG, PNG, WebP, AVIF, TIFF, ICO, PDF (GIF → MP4/WebM) |
| Audio | MP3, M4A, AAC, WAV, FLAC, OGG, Opus, AIFF, WMA | MP3, M4A, WAV, FLAC, OGG, Opus, AIFF |
| Video | MP4, MOV, MKV, WebM, AVI, WMV, FLV, MPG, 3GP, TS | MP4, MOV, MKV, WebM, GIF, MP3 |
| PDF | PDF | DOCX, JPG, PNG (all pages, 300 DPI), PPTX, TXT, MD, HTML, EPUB |
| Documents | DOCX, DOC, ODT, RTF, MD, TXT, HTML, EPUB | DOCX, PDF, ODT, MD, HTML, TXT, EPUB, RTF |
| Slides | PPTX, PPT, ODP | PDF, PNG, JPG (all slides), PPTX, ODP |
| Spreadsheets | XLSX, XLSM, XLS, ODS, CSV, TSV | XLSX, CSV, TSV, ODS, PDF |
| Subtitles | SRT, VTT | SRT, VTT, TXT |
| Archives | ZIP, TAR, TAR.GZ, GZ, BZ2, XZ, RAR, 7z | extract, ZIP, TAR, TAR.GZ |

**Tools (Ctrl):**
- images: compress, shrink to 50 %, max 1920 px, rotate, mirror, crop to square, grayscale, strip metadata (GPS, EXIF), merge into PDF,
- video: compress (H.264), 720p, mute, trim by time, rotate, 2× speed, thumbnail frame,
- audio: compress, normalize loudness, trim, join,
- PDF: merge, split into pages, rotate, optimize,
- images and scanned PDFs: **extract text (OCR)** with the recognizer built into Windows; the text is saved as .txt and copied to the clipboard,
- anything, folders included: archive to ZIP / TAR.GZ.

Things that aren't files yet (images dragged from a browser, Outlook attachments, screenshots on the clipboard) are saved to **Downloads\Dropit** first, and the results go next to them.

With several files selected, each one is converted separately; tools like “merge” produce a single result.

### Helper programs

Dropit handles images, PDFs, spreadsheets, subtitles and archives on its own. Some formats need extra programs:

- **FFmpeg** (audio and video, 34 MB) and **Pandoc** (documents, 35 MB) are downloaded the first time you need them: the wheel shows their slices dimmed with “get FFmpeg”, and dropping a file there asks once, downloads the tool (checked against its published SHA-256) and runs the conversion. If you already have them installed (e.g. `winget install Gyan.FFmpeg`), Dropit uses those,
- **LibreOffice** (DOC, PPTX, ODS, documents and spreadsheets to PDF) has to be installed separately: `winget install TheDocumentFoundation.LibreOffice`.

**Settings → Helper programs** shows what is missing and can download or install it for you. Keeping the tools out of the installer keeps it under 50 MB.

## Install

Download `Dropit-Setup-x.y.z.exe` from [Releases](../../releases), or from the artifacts of the latest [GitHub Actions](../../actions) run. The installer (under 50 MB) doesn't need admin rights. A portable `.zip` is available too.

The app speaks English and Arabic, and the language can be switched in Settings. Arabic is laid out right to left.

### Home window

Double-clicking `Dropit.exe` opens a window rather than a tray balloon: the three wheels with their keys, a drop target, a settings button and a close button. Drop files on it to open the wheel for their type, or press a row to open the wheel with whatever is on the clipboard. The window drags by its title strip and resizes from any edge.

### Themes

Five art directions plus *follow Windows*, switchable live in Settings → Appearance:

- **Dark glass** — anthracite, layered translucent surfaces, thin luminous rims, a restrained amber glow.
- **Deep night** — near-black navy under a slow atmospheric wash, cool cyan hairlines.
- **Mint** — deep navy under a mint accent, the palette of the macOS app this was modelled on.
- **Light** — the same layering on a light surface, for bright rooms.
- **Warm** — warm paper neutrals with a vivid orange accent, the palette of Tangerine on macOS.

Every surface carries a token for its elevation — window backdrop, panel, card, control — plus a hairline and a one-pixel top highlight. That is what makes the panels read as stacked glass rather than flat grey boxes.

**Softness** is one slider that rounds every corner by the same amount, so the whole app can read as soft or as sharp without anything else changing. **Less movement** leaves the animations out; the app behaves the same, it just stops moving.

#### Your own colours

Put a `theme.json` in `%APPDATA%\Dropit` (next to `settings.json`) and the app paints itself with your colours:

```json
{
  "Warm": {
    "accent": "#7A2BD1",
    "accent2": "#4C108C",
    "page": "#F2EFF7",
    "card": "#FBFAFD"
  }
}
```

- The top level holds the theme names, each with the colour slots you want to change. `DarkGlass`, `DeepNight`, `Mint`, `Light` and `Warm` are the names.
- The slots are `backdrop`, `ambient`, `panel`, `panelEdge`, `highlight`, `card`, `cardEdge`, `slice`, `sliceEdge`, `hub`, `track`, `page`, `hover`, `text`, `muted`, `success`, `error`, `accent`, `accent2` and `textOnAccent`.
- Colours are `#RRGGBB` or `#AARRGGBB`. Anything you leave out keeps the colour the app ships with, so one line changes one thing.
- A slot name that does not exist, a value that is not a colour, and a theme name the app does not know are ignored and reported in Settings → Appearance.
- The file is never rewritten by the app. Delete it and the built-in colours come back.

On first launch Dropit adds itself to startup and to the File Explorer context menu (both can be turned off in Settings). It then lives in the notification area next to the clock.

**Updates** install themselves: Dropit asks GitHub for the latest release now and then (nothing about your files is sent), offers the update in a notification and the tray menu, verifies the installer's SHA-256 and installs it silently. Clicking the notification opens a window that says what is being installed and shows the download as it goes. You can turn the check off in Settings. The portable version opens the download page instead.

Requirements: Windows 10 (1809+) or Windows 11, x64.

## Linux

**Arch Linux:** every release has a `PKGBUILD` attached (the `dropit-bin` package). Download it into an empty folder and run `makepkg -si`. The package is coming to the AUR (`yay -S dropit-bin`) as soon as AUR account registration reopens.

**Other distributions:** download `Dropit-x.y.z-linux-x64.tar.xz` (about 32 MB) from [Releases](../../releases), unpack it and run `dropit/app/dropit` (no .NET needed).

Linux doesn't let apps watch the keyboard and mouse of other programs (Wayland forbids it on purpose), so the "drag and tap a key" gesture is Windows-only. On Linux you use the same wheel like this:

- **Drop zone:** run `dropit` (or *Dropit* from the app menu) and drag files onto the wheel. It stays on screen; move it by its middle.
- **Right-click in the file manager:** `dropit integrate` adds *Dropit…* to Nautilus (*Scripts* submenu), Dolphin and Nemo (the AUR package installs Dolphin and Nemo for everyone).
- **Command line** and keyboard shortcuts of your desktop:

  ```sh
  dropit photo.heic                       # the wheel for this file
  dropit list photo.heic                  # what can be done with it
  dropit run image.to.webp *.heic         # do it, no window
  dropit run video.trim clip.mov --param start=0:10 --param end=0:25
  ```

Optional packages: `ffmpeg` (audio, video), `pandoc-cli` (documents), `libreoffice-fresh` (Office files), `tesseract` + `tesseract-data-eng` (extract text). Settings live in `~/.config/dropit/settings.json`.

## Build from source

```powershell
# .NET 10 SDK
./scripts/fetch-tools.ps1                      # downloads ffmpeg.exe and pandoc.exe into .\tools (for the tests)
$env:PATH = "$PWD\tools;$env:PATH"; dotnet test tests/Dropit.Tests
dotnet publish src/Dropit.App -c Release -r win-x64 --self-contained -o publish   # trimmed, ~165 MB unpacked
publish\Dropit.exe
```

`Dropit.exe --self-test <folder>` builds every window offscreen and saves PNG previews in both languages, including right-to-left layouts (used by CI).

### Releasing

Push a commit to `main` whose message starts with `Release v1.2.3` (or run the **Build** workflow manually with a version). CI builds the installer and zip and publishes them as a GitHub release. Add a repository secret `RELEASE_TOKEN` (a fine-grained personal access token with *Contents: read and write* on this repository) to publish releases under your own account instead of github-actions.

The same run builds the Linux tarball, installs the AUR package on Arch Linux to test it and, when the `AUR_SSH_PRIVATE_KEY` secret holds the key of the AUR account that maintains `dropit-bin`, pushes the updated PKGBUILD to the AUR.

### How an installed copy updates itself

Once a release exists, an installed copy finds it on its own:

1. Twenty seconds after start, then every six hours, the app asks `api.github.com/repos/BrO-ken/Dropit/releases/latest` for the newest release. Nothing about your files is sent.
2. If the tag is newer than the running version, a balloon appears and the tray menu gains an *Update to version x.y.z* entry.
3. Clicking it downloads `Dropit-Setup-x.y.z.exe` to a uniquely named file in `%TEMP%`, checks its SHA-256 against the `digest` GitHub published for that asset, and refuses the update if the two differ or if no digest was published at all.
4. The installer replaces the running copy and relaunches the app with `--updated`, which reports *Dropit was updated*.

`Settings > Updates` shows the same state and has a **Check now** button that works whether or not the automatic check is enabled.

A copy unzipped from the zip has no uninstaller next to it, so it cannot replace itself: it opens the release page instead. Only the installer can update in place.

Two things silently break this, and both have to be right:

- **The repository has to be reachable without a token.** The app queries the API anonymously, and GitHub answers `404 Not Found` for a private repository. Every check then fails quietly and no update is ever offered. A personal access token must not be embedded in the app to work around this: it can be extracted from the binary.
- **The PKGBUILD has to download from this repository.** It once pointed at the upstream project, where the tag did not exist, and `makepkg` failed on a package that had passed every CI job. The packaging step now fails the build if the generated PKGBUILD points anywhere else.

## Project layout

```
src/Dropit.Core     conversions and tools (cross-platform library, also tested on Linux)
  Backends/             ImageMagick, FFmpeg, Pandoc, LibreOffice, PDF, spreadsheets, subtitles, archives
  Operations/Catalog.cs every slice on the Convert and Tools wheels
  Loc.cs                English / Arabic strings
src/Dropit.Linux    the Linux app (Avalonia): drop zone, wheel, CLI, file manager integration
src/Dropit.App      the Windows app (WPF)
  Input/GlobalInput.cs  global mouse and keyboard hooks (the “drag + tap” gesture)
  Wheel/                the radial menu and when it opens
  Actions/              the Actions wheel, floating shelf, sharing
  Jobs/                 background jobs and progress cards
  Shell/                tray icon, hotkey, context menu, single instance
tests/Dropit.Tests  unit tests plus integration tests that run every operation on sample files
installer/              Inno Setup
packaging/              Linux tarball and the AUR PKGBUILD template
```

## License

[MIT](LICENSE). Dropit uses other open-source components under their own licenses; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Differences from Tangerine

- Shift / Ctrl / Alt instead of Shift / Command / Control.
- Apple formats (Pages, Keynote, RTFD) and writing HEIC are not supported; HEIC can be opened.
- PDF → DOCX/EPUB/HTML converts the text, not the exact page layout (use PDF → PPTX or images for that).
- Tangerine's “AirDrop” action is **Share** on Windows (Nearby Sharing, Teams, Mail…).

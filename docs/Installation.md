# Installation Guide

## Requirements

### To run

- Windows 10 or newer
- .NET Framework 4.7.2 runtime
- No server, database or internet connection is required
- **Microsoft Print to PDF** — only for the PDF report. This printer ships with Windows and is normally present. PDF reports are produced by drawing to it through `System.Drawing.Printing`, which is what keeps the project free of external libraries. If it has been removed, the application reports a clear error for that one action; everything else, including the `.txt` report, keeps working.

### To build

- Visual Studio 2019 / 2022 (or MSBuild 15.0+)
- .NET Framework 4.7.2 Developer Pack

## Building from Source

1. Clone the repository.

   ```bash
   git clone https://gitlab.com/Melikkochan/RORAMuzikMerkezi.git
   ```

2. Open `RORAMuzikMerkezi.sln` with Visual Studio.

3. Build the solution (`Ctrl+Shift+B`) and run it (`F5`).

The project has **no NuGet dependencies**. It references only the standard .NET Framework assemblies (`System`, `System.Drawing`, `System.Windows.Forms`, `System.Xml`, ...), so no package restore step is needed.

### Note on the signing key

The project file enables ClickOnce manifest signing and references `RORAMuzikMerkezi_TemporaryKey.pfx`. This key is intentionally **not** committed to the repository and is excluded by `.gitignore`. A normal build works without it; the key is only required when publishing with ClickOnce.

## Running the Built Application

No installer is required. Copy `RORAMuzikMerkezi.exe` together with all three image assets into the same folder and run it:

| File | Used for |
|---|---|
| `favicon.ico` | Window and taskbar icon |
| `rora1.png` | Splash screen logo |
| `rora.jpeg` | Logo in the PDF report (falls back to `rora1.png` if missing) |

The build copies all three into the output folder automatically. Assets are resolved relative to the executable, not the working directory, so a shortcut with a different "Start in" folder still finds them. A missing asset never crashes the application; that element is simply left out.

The data file is created automatically on first launch, and backups are written next to it:

```
%AppData%\RORAMuzikMerkezi\veriler.xml
%AppData%\RORAMuzikMerkezi\yedekler\
```

## Repository Workflow

### Branches

- **`main`** — the only long-lived branch. It is protected: push and merge are restricted to Maintainers and force push is disabled.
- **Work branches** — created per issue, typically named after the issue (for example `12-bozuk-veri-dosyasi`) or with a short prefix such as `fix/`, `feat/`, `docs/`, `chore/`, `ci/`.

There is no `develop` branch.

### Making a change

1. Open an issue describing the work.
2. Create a branch from `main` (GitLab can create one directly from the issue).
3. Commit your changes to that branch and push it.
4. Open a merge request targeting `main`, using the merge request template.
5. After the merge request is accepted, the source branch is deleted automatically.

### Backing up your data before testing

Running the application uses the real data file at `%AppData%\RORAMuzikMerkezi\veriler.xml`. Take a backup before running experimental builds — either copy the file aside, or use **💾 Veri → 📤 Yedek Al...** in the application. The automatic startup backup in `yedekler\` also covers you, but it only keeps the last 7 days.

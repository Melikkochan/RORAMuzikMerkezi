# Installation Guide

## Requirements

### To run

- Windows 10 or newer
- .NET Framework 4.7.2 runtime
- No server, database or internet connection is required

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

No installer is required. Copy `RORAMuzikMerkezi.exe` together with `favicon.ico` and `rora1.png` into the same folder and run it. The data file is created automatically on first launch at:

```
%AppData%\RORAMuzikMerkezi\veriler.xml
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

Running the application uses the real data file at `%AppData%\RORAMuzikMerkezi\veriler.xml`. Copy it aside before running experimental builds.

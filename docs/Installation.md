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
| `favicon.ico` | Window and taskbar icon — the RORA medallion, generated from `rora.jpeg` |
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

## Continuous Integration

`.gitlab-ci.yml` builds the solution on every push and merge request and publishes the resulting `RORAMuzikMerkezi.exe` as an artifact. It has one job, `derleme`, and it verifies exactly one thing: that the repository still compiles from a clean checkout. There is no test suite, so a green pipeline says nothing about behaviour.

### Why a local Windows runner

The job needs real MSBuild and the .NET Framework, so it is written for a Windows runner you host yourself:

- The Linux shared runners could only build through Mono, which is an approximation for a Windows Forms project — `System.Deployment`, ClickOnce and designer behaviour can differ. A green Mono build would not prove the project builds on a real machine.
- A self-hosted Windows runner has no monthly minute limit and builds exactly the way you build locally.

The trade-off is that the machine hosting the runner must be switched on for a pipeline to run. On a single-developer project that is usually your own computer.

### Shared runners must be turned off

This one is not obvious and it blocks everything until it is done.

On GitLab.com's free plan, an account that has not completed identity verification cannot run CI jobs. The refusal happens when the pipeline is **created**, before any runner is chosen, so having your own runner does not help by itself:

```
POST /projects/:id/pipeline
{"message":{"base":["Identity verification is required in order to run CI jobs"]}}
```

The requirement is tied to the project being able to use GitLab's shared compute. Turn shared runners off for the project and it no longer applies:

**Settings → CI/CD → Runners → Shared runners → disable**, or:

```
PUT /projects/:id  {"shared_runners_enabled": false}
```

After that, pipelines are created normally and your own runner picks them up. Leave this setting off; turning it back on brings the verification requirement back.

### Registering the runner

The runner for this project is already registered and lives in `C:\Users\MELİK\gitlab-runner`. These are the steps that produced it, in case it has to be redone on another machine.

1. Download the runner binary from GitLab's official downloads into a folder that will stay put — not a temporary directory.
2. In GitLab, open **Settings → CI/CD → Runners** and create a project runner with the tags `windows` and `msbuild`. The job in `.gitlab-ci.yml` selects the runner by those tags, so they must match.
3. Register with the **shell** executor, so the job runs PowerShell directly on the machine:

   ```
   gitlab-runner register --non-interactive --url https://gitlab.com/ --token <glrt-token> --executor shell --shell powershell
   ```

4. Run it:

   ```
   gitlab-runner run --working-directory C:\Users\MELİK\gitlab-runner
   ```

### Keeping the runner running

A shortcut in the Startup folder starts the runner minimised at every logon:

```
%AppData%\Microsoft\Windows\Start Menu\Programs\Startup\GitLab Runner (RORA).lnk
```

Deleting that shortcut is all it takes to stop the runner from starting. This route needs no administrator rights, and the runner only runs while you are logged in — which is what a single-developer project needs.

If you would rather have it run as a proper Windows service, independent of who is logged in, do this once from an **administrator** shell instead:

```
gitlab-runner install --working-directory C:\Users\MELİK\gitlab-runner
gitlab-runner start
```

and remove the Startup shortcut so the runner is not started twice.

Visual Studio (or the Build Tools) with the **MSBuild** component and the **.NET Framework 4.7.2 Developer Pack** must be installed on that machine. The job locates MSBuild through `vswhere` rather than a hard-coded path, so it keeps working when Visual Studio is updated or installed elsewhere.

### Merge requests require a green pipeline

**Settings → Merge requests → Pipelines must succeed** is on. A merge request cannot be merged until its pipeline passes, which is the point of having the pipeline at all.

The catch: the runner lives on one machine. If that machine is off, or you are not logged in, pipelines sit in **pending** and merge requests cannot be merged. If that happens, either start the runner or turn the setting off:

```
PUT /projects/:id  {"only_allow_merge_if_pipeline_succeeds": false}
```

The manual equivalent of the pipeline is worth knowing regardless: clone the repository into an empty folder and build it there. That is the one failure this pipeline actually guards against — a repository that builds on your machine but not from a clean clone.

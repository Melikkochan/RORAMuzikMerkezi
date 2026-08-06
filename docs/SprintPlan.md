# Sprint Plan

Work is tracked as issues in GitLab, grouped into milestones. This file mirrors their scope and status.

## Sprint 1 — 3–4 August 2026

Core application. This work predates issue tracking, so the milestone carries no issues.

- Student registration and the tabbed interface by instrument
- Weekly lesson tracking
- Local XML data storage
- Monthly payment recording

**Status:** Completed

---

## Sprint 2 — 5–6 August 2026

Feature depth and data safety. All issues below closed on 5 August 2026.

- Edit a registered student (#15)
- Track the payment amount and report income (#16)
- Backup and restore, plus an automatic startup backup (#17)
- Phone format validation and duplicate registration warning (#18)
- Single-instance enforcement, so two windows cannot overwrite each other (#19)
- Splash screen progress division fix (#20)
- Build warning cleanup, CS0168 (#22)
- Per-instrument lesson fee definitions, prefilled on payment (#23)
- Global search across all instruments (#24)
- Monthly summary as PDF (#25), with logo and layout work (#26, #27)
- Atomic write of the data file (#28)
- Report for a chosen month and year (#29)
- Confirmation before deleting a student (#2)
- Student search improvements (#1)

**Status:** Completed

---

## Sprint 3 — 5–14 August 2026

Documentation, repository hygiene, CI, and interface work.

### Done

- Full project documentation produced and reviewed
- Repository hygiene: `.gitignore` added, `RORAMuzikMerkezi.csproj.user` untracked
- Missing project files restored, so a fresh clone builds again (`Properties/`, `App.config`, image assets, `.sln`)
- Labels and issue board rebuilt around a workflow (`to do` → `in progress` → `in review` → `blocked`)
- Documented findings converted into tracked issues
- Fixed: image assets were resolved relative to the working directory (#11)
- Fixed: a corrupt data file was silently replaced with an empty one (#12)
- README cleaned up and screenshots moved into the repository (#13)
- Documentation corrected to match the actual repository setup (#21)
- Payment state toggled by clicking the table cell (#30)
- High-DPI support, so text is not blurred on scaled displays (#31)
- Refreshed visual language of the interface (#32)
- Documentation brought back in line with the code after 29 commits (#35)
- Milestone records reconciled with the work actually delivered (#37)

### Open

- Yearly and date-range reports (#33)
- Per-student payment and lesson history screen (#34)
- README screenshots to be retaken now that the interface has been refreshed, along with the README lines for #30 and #31 (#36)

### Deferred

- **CI build pipeline (#14).** `.gitlab-ci.yml` is written but has never run, so we do not know that it passes. Deferred deliberately rather than blocked: its only job is to answer "does this commit compile", the project has no test suite, and compile errors already surface immediately in Visual Studio. The one risk it genuinely covers — a repository that builds locally but not from a clean clone, which happened once in this project — is covered almost as well by cloning to a temporary folder and building before a release.

  If it is taken up, the local Windows runner option is preferred over GitLab's shared runners: no account verification, no monthly minute limit, and it builds with real MSBuild and .NET Framework instead of Mono, whose result is only an approximation for a Windows Forms project. The current `.gitlab-ci.yml` assumes a shared Linux runner and would need rewriting for that.

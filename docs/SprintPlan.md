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
- Yearly and date-range reports, month by month and by instrument (#33)
- Per-student lesson and payment history screen (#34)
- Fixed: bottom panel buttons were sized in fixed pixels and clipped their labels; widths are now measured from the text at runtime (#38)
- README screenshots retaken against the refreshed interface, and the missing README lines for #30 and #31 added (#36)
- CI build pipeline running on a self-hosted Windows runner (#14)
- Application icon replaced with the RORA medallion; it used to show "MK" (#39)
- Summary tab content centred horizontally instead of hugging the left edge, and the screenshots refreshed with the new icon (#40)

### Open

_Nothing open in this sprint._

### Note on the CI pipeline

It ran for the first time in this sprint, and the reason it had never run before turned out not to be the missing runner. On GitLab.com's free plan an unverified account cannot run CI jobs, and the refusal happens when the pipeline is **created**, before any runner is chosen — so a self-hosted runner does not help by itself. Turning shared runners off for the project lifts the requirement. Details in [Installation.md](Installation.md), *Continuous Integration*.

What the pipeline actually verifies is narrow: that the repository compiles from a clean checkout. There is no test suite, so a green pipeline says nothing about behaviour.

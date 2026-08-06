# Sprint Plan

Work is tracked as issues in GitLab. This file summarises what has shipped and what is open.

> **Note on milestones.** The `Sprint 1` and `Sprint 2` milestones were created while the project was being set up and contain only throwaway test issues (#4–#9). The feature work listed below under Sprint 1 and Sprint 2 was delivered as issues #15–#29, which carry no milestone. Milestone bookkeeping therefore does not match the sprint boundaries described here; the issue numbers are the reliable record.

## Sprint 1 — 3–4 August 2026

Core application.

- Student registration and the tabbed interface by instrument
- Weekly lesson tracking
- Local XML data storage
- Monthly payment recording

**Status:** Completed

---

## Sprint 2 — 5 August 2026

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

### In review

- Payment state toggled by clicking the table cell (#30, `!23`)
- High-DPI support, so text is not blurred on scaled displays (#31, `!24`)
- Refreshed visual language of the interface (#32, `!25`)

### In progress

- Documentation brought back in line with the code after 29 commits (#35)

### Open

- Yearly and date-range reports (#33)
- Per-student payment and lesson history screen (#34)
- README screenshots to be retaken once `!25` merges (#36)

### Blocked

- CI pipeline is written and validated but cannot run yet: GitLab.com requires account verification before free-tier projects may use shared runners (#14)

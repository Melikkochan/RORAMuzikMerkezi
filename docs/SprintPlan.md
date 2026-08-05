# Sprint Plan

Milestones are tracked in GitLab. This file mirrors their scope and status.

## Sprint 1 — 3–7 August 2026

- Student registration
- XML data storage
- Lesson tracking

**Status:** Completed (milestone closed)

---

## Sprint 2 — 10–14 August 2026

- Payment management
- Monthly report
- UI improvements

**Status:** Completed (milestone closed)

---

## Sprint 3 — 3–14 August 2026

- Documentation
- GitLab migration
- CI/CD preparation

**Status:** In progress

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

### Remaining

- CI pipeline is written and validated but cannot run yet: GitLab.com requires account verification before free-tier projects may use shared runners (#14)

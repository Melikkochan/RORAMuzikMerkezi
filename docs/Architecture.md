# System Architecture

## Architecture Type

Single-process desktop application. No server, no database, no network calls.

## Technology Stack

- **Presentation** — Windows Forms
- **Business logic** — student, lesson, payment and fee rules
- **Data** — XML serialization to a file under `%AppData%`

## Components

### Presentation

| File | Role |
|---|---|
| `Program.cs` | Entry point; declares DPI awareness, enforces the single-instance rule via a named mutex, and loads the data before the main window is built |
| `SplashForm.cs` | Startup screen |
| `MainForm.cs` | Main window: menu bar, global search, summary panel, instrument tabs |
| `CalgiSekmesi.cs` | One instrument tab: student list, weekly lesson checkboxes, payment state |
| `OgrenciKayitForm.cs` | Student registration and editing dialog |
| `OgrenciGecmisFormu.cs` | One student's month-by-month lesson and payment history (read only) |
| `UcretTanimFormu.cs` | Per-instrument lesson fee definitions |
| `TutarGirisFormu.cs` | Payment amount entry |
| `RaporAySecimFormu.cs` | Month and year picker for the monthly report |
| `RaporDonemSecimFormu.cs` | Year or free month-range picker for the period report |

### Shared presentation helpers

| File | Role |
|---|---|
| `Renkler.cs` | Colour palette |
| `Olcek.cs` | Sizing and spacing values |
| `Metin.cs` | Shared text and formatting helpers |
| `Varliklar.cs` | Loads icons and images relative to the executable, returning `null` instead of throwing when a file is missing |

### Business logic and data

| File | Role |
|---|---|
| `Models.cs` | Student, payment and related records |
| `Donem.cs` | A closed month range; a yearly report is just January–December of that year |
| `VeriYoneticisi.cs` | Loading, atomic saving, backup and restore, summary and income calculations, text reports |
| `PdfRapor.cs` | Shared PDF chrome: printer setup, logo, page header and footer, pagination |
| `PdfRaporYazici.cs` | Monthly report layout |
| `DonemPdfRaporu.cs` | Period report layout: income month by month and by instrument |

## Data Flow

```
User
 ↓
Windows Forms (MainForm / CalgiSekmesi / dialogs)
 ↓
VeriYoneticisi  ──────────────→  PdfRaporYazici
 ↓                                    ↓
veriler.xml                    Microsoft Print to PDF
 ↓
yedekler\
```

All state lives in memory in `VeriYoneticisi.Veriler` while the application runs, and is serialised back to disk on change.

## Write Safety

Two mechanisms protect the data file:

- **Atomic write** — the new content is written to a temporary file, then swapped in with `File.Replace`, which also keeps the previous version alongside it. A crash mid-write leaves the old file intact rather than a half-written one.
- **Single instance** — a named mutex prevents a second window from opening. Two instances would each hold the full dataset in memory, and whichever saved last would silently discard the other's changes.

## PDF Generation

The PDF report has no third-party dependency. `PdfRaporYazici` draws the report with `System.Drawing.Printing` and targets the `Microsoft Print to PDF` printer that ships with Windows. The trade-off is deliberate: the project stays dependency-free and deployment stays a folder copy, at the cost of not being able to produce PDFs on a machine where that printer has been removed. That case is reported to the caller as an explicit error.

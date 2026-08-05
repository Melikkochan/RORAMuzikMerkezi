# 🎵 RORA Sanat Merkezi — Öğrenci Takip Sistemi

C# ve Windows Forms ile geliştirilmiş masaüstü uygulaması.

## 📌 Proje Hakkında

RORA Sanat Merkezi için geliştirilen bu uygulama; öğrenci kayıt, haftalık ders takibi ve aylık ödeme yönetimini dijital ortama taşımaktadır. Sunucu, veritabanı veya internet bağlantısı gerektirmez; tüm veri kullanıcının kendi bilgisayarında XML dosyası olarak saklanır.

## ✨ Özellikler

- Öğrenci kayıt ve silme
- Çalgı türüne göre sekmeli arayüz
- 4 haftalık ders takibi (işaret kutusu ile)
- Aylık ödeme yönetimi (ödeme alma ve geri alma)
- Ayın 15'inden sonra çalışan ödeme hatırlatıcısı
- Aylık özet raporu (masaüstüne `.txt` olarak kaydeder)
- Veriler XML biçiminde yerel olarak saklanır

## 🖼️ Ekran Görüntüleri

**Genel Özet** — toplam öğrenci, bu ay ödeme yapan, bu hafta ders alan sayıları ve borçlu listesi

![Genel Özet ekranı](docs/img/genel-ozet.png)

**Çalgı sekmesi** — ay/yıl filtresi, haftalık ders kutucukları ve ödeme durumu

![Çalgı sekmesi](docs/img/calgi-sekmesi.png)

**Ödeme alma** — seçili öğrenci için seçili aya ödeme işleme

![Ödeme alma](docs/img/odeme-alma.png)

## 🛠️ Kullanılan Teknolojiler

- C#
- .NET Framework 4.7.2
- Windows Forms
- XML Serialization

## 🚀 Kurulum

Ayrıntılı adımlar için [docs/Installation.md](docs/Installation.md) dosyasına bakınız.

```bash
git clone https://gitlab.com/Melikkochan/RORAMuzikMerkezi.git
```

Projeyi Visual Studio ile açıp derlemeniz yeterlidir; harici bağımlılık yoktur.

## 💾 Veri Konumu

Tüm veri tek bir dosyada tutulur:

```
%AppData%\RORAMuzikMerkezi\veriler.xml
```

Yedekleme, bu dosyanın kopyalanmasından ibarettir.

## 📚 Dokümantasyon

| Belge | İçerik |
|---|---|
| [ProjectOverview.md](docs/📄%20ProjectOverview.md) | Projenin amacı ve kapsamı |
| [Architecture.md](docs/Architecture.md) | Katmanlar ve veri akışı |
| [Requirements.md](docs/Requirements.md) | İşlevsel ve işlevsel olmayan gereksinimler |
| [Installation.md](docs/Installation.md) | Kurulum ve derleme adımları |
| [SprintPlan.md](docs/SprintPlan.md) | Sprint planı ve durumu |
| [FutureImprovements.md](docs/FutureImprovements.md) | Planlanan geliştirmeler |

## 👤 Geliştirici

Melik KOÇHAN

---

# 🎵 RORA Art Center — Student Tracking System

A desktop application developed with C# and Windows Forms.

## 📌 About

This application was developed for RORA Art Center to digitally manage student registration, weekly lesson tracking, and monthly payment processes. It requires no server, database, or internet connection; all data is stored locally as an XML file.

## ✨ Features

- Student registration and deletion
- Tabbed interface organized by instrument type
- Weekly lesson tracking with checkboxes (4 weeks per month)
- Monthly payment management (record and revert payments)
- Payment reminder shown after the 15th of the month
- Monthly summary report (saved as `.txt` to the desktop)
- Data stored locally in XML format

## 🛠️ Technologies Used

- C#
- .NET Framework 4.7.2
- Windows Forms
- XML Serialization

## 🚀 Getting Started

See [docs/Installation.md](docs/Installation.md) for detailed steps.

```bash
git clone https://gitlab.com/Melikkochan/RORAMuzikMerkezi.git
```

Open the project in Visual Studio and build it; there are no external dependencies.

## 💾 Data Location

All data is kept in a single file:

```
%AppData%\RORAMuzikMerkezi\veriler.xml
```

Backing up means copying this file.

## 👤 Developer

Melik KOÇHAN

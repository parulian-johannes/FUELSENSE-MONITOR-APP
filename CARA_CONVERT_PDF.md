# 📄 CARA CONVERT TUTORIAL KE PDF

File tutorial telah dibuat dalam format **Markdown (.md)** di:
```
TUTORIAL_ENGINE_MONITOR.md
```

Berikut beberapa cara untuk mengkonversi ke PDF:

---

## OPSI 1: Menggunakan Pandoc (Recommended)

### Install Pandoc

1. Download Pandoc dari: https://pandoc.org/installing.html
2. Install dengan default settings
3. Restart terminal

### Convert ke PDF

```powershell
# Basic conversion
pandoc TUTORIAL_ENGINE_MONITOR.md -o TUTORIAL_ENGINE_MONITOR.pdf

# Dengan custom styling
pandoc TUTORIAL_ENGINE_MONITOR.md -o TUTORIAL_ENGINE_MONITOR.pdf --pdf-engine=xelatex -V geometry:margin=1in
```

---

## OPSI 2: Menggunakan Visual Studio Code

### Install Extension

1. Buka VS Code
2. Install extension: **"Markdown PDF"** by yzane
3. Restart VS Code

### Convert

1. Buka file `TUTORIAL_ENGINE_MONITOR.md` di VS Code
2. Tekan `Ctrl+Shift+P`
3. Ketik "Markdown PDF: Export (pdf)"
4. File PDF akan otomatis ter-generate di folder yang sama

---

## OPSI 3: Online Converter

### Menggunakan Website

1. Buka: https://www.markdowntopdf.com/
2. Upload file `TUTORIAL_ENGINE_MONITOR.md`
3. Klik "Convert"
4. Download hasil PDF

**Alternatif Website:**
- https://md2pdf.netlify.app/
- https://www.browserling.com/tools/markdown-to-pdf

---

## OPSI 4: Microsoft Word

### Convert Manual

1. Buka file `TUTORIAL_ENGINE_MONITOR.md` dengan Notepad
2. Copy semua isi
3. Paste ke Microsoft Word
4. Format manual (bold, table, etc)
5. File → Save As → PDF

---

## OPSI 5: Chrome/Edge Browser

### Print to PDF

1. Klik kanan `TUTORIAL_ENGINE_MONITOR.md`
2. Open with → Chrome/Edge
3. Tekan `Ctrl+P` (Print)
4. Destination: "Save as PDF"
5. Adjust settings (margins, scale)
6. Save PDF

---

## HASIL AKHIR

Setelah konversi, Anda akan mendapatkan:

**File:** `TUTORIAL_ENGINE_MONITOR.pdf`

**Isi:**
- 12 Chapter lengkap
- 50+ halaman
- Table of Contents
- Tabel dan diagram
- Step-by-step instructions
- Troubleshooting guide
- FAQ section

---

## REKOMENDASI

**Terbaik:** Pandoc (hasil paling profesional)  
**Tercepat:** VS Code extension  
**Termudah:** Online converter

---

## CUSTOM STYLING (Pandoc)

Untuk hasil PDF yang lebih bagus, buat file `template.yaml`:

```yaml
---
title: "Tutorial ENGINE MONITOR"
author: "Developer Team"
date: "November 2025"
geometry: margin=2cm
toc: true
toc-depth: 2
numbersections: true
header-includes:
  - \usepackage{fancyhdr}
  - \pagestyle{fancy}
  - \fancyhead[L]{ENGINE MONITOR}
  - \fancyhead[R]{\thepage}
---
```

Lalu convert dengan:
```powershell
pandoc TUTORIAL_ENGINE_MONITOR.md -o TUTORIAL_ENGINE_MONITOR.pdf --pdf-engine=xelatex --metadata-file=template.yaml
```

---

**Happy Converting! 📄→📕**

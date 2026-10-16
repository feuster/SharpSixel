# CLAUDE History

Ausgelagerte, nicht mehr aktuelle Informationen aus `CLAUDE.md`.

## 2026-10-17 – Initialer Aufbau
- Entscheidungen: .NET 10 (Library zusätzlich netstandard2.0), xUnit, SkiaSharp 4.153.1 (MIT) für PNG/JPEG, BMP nativ im Kern, NuGets nur lokal.
- Pakete: (1) Decoder + Tests, (2) Encoder/Quantizer/BMP + Tests, (3) Skia + CLI + Tests, (4) Pack/Doku.
- Fund: `SKBitmap.Decode(byte[])` wirft in SkiaSharp 4 `ArgumentNullException` bei ungültigen Daten; stattdessen `SKCodec.Create` prüfen.

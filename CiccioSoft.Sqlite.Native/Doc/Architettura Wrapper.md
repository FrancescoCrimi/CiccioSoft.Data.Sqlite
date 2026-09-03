# 📋 Linee Guida Architetturali: Wrapper SQLite Multithread & Cross-Platform (.NET / C)

Questo documento riassume le decisioni di design prese per garantire le massime prestazioni, l'azzeramento delle allocazioni di memoria non necessarie e la totale portabilità del database tra diversi Sistemi Operativi (Windows, Linux, macOS).

---

## 1. Codifica Nativa del Database (Storage)
* **Decisione:** Il file del database SQLite sarà memorizzato su disco **esclusivamente in formato UTF-8**.
* **Motivazione:** UTF-8 è lo standard universale del software moderno (Rust, Python, Go, sistemi Linux/macOS). Riduce lo spazio su disco per i caratteri occidentali rispetto all'UTF-16 e garantisce che il file `.db` sia nativamente portabile su qualsiasi piattaforma senza conversioni di file.
* **Implementazione:** All'apertura/creazione del database si utilizzerà esclusivamente l'API `sqlite3_open()` e verrà forzato il comando `PRAGMA encoding = "UTF-8";` (se il DB è vuoto). **Le API UTF-16 di SQLite (`sqlite3_*16`) non verranno utilizzate.**

---

## 2. Design dell'Interfaccia del Wrapper (Pattern di Overload)
* **Decisione:** Per ogni funzione di interazione con il database che accetta testo, verranno forniti **due overload speculari**: uno basato su `ReadOnlySpan<byte>` (UTF-8 nativo) e uno basato su `String` (UTF-16 di comodità).

### Schema logico del codice:

1. **Il metodo CORE (`ReadOnlySpan<byte>`):**
   * Accetta testo in formato UTF-8 nativo.
   * Rappresenta la via ad alte prestazioni (zero allocazioni nella Heap, zero copie).
   * Ideale per dati che arrivano già in UTF-8 (es. parsing JSON, file di log, configurazioni, stringhe letterali con suffisso `u8`).
   * Passa direttamente il puntatore di byte al layer C tramite P/Invoke.

2. **L'metodo di COMODITÀ (`String`):**
   * Accetta le stringhe standard di .NET (UTF-16).
   * Calcola la dimensione necessaria e converte il testo in un buffer UTF-8 allocato temporaneamente nello **Stack** (`stackalloc byte[]`) per stringhe di piccole/medie dimensioni, azzerando l'impatto sul Garbage Collector.
   * Invoca internamente il metodo CORE passando il buffer ottenuto.

---



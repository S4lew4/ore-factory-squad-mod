# Ore Factory Squad – Co-op Mod

Ein privater Mod für **Ore Factory Squad** (Steam) zum Spielen mit Freunden.
Basiert auf **BepInEx (IL2CPP)** + Harmony.

## Tasten
| Taste | Funktion |
|---|---|
| **F8** | „Atombombe" auf der Mine (nur der Host) |
| **H** | Handy: Laptop-Oberfläche öffnen/schließen (Verträge, Bank, Markt …) |
| **J** | Slot-Maschine **drehen** (nächste/gewählte) |
| **K** | Einsatz wechseln (100 / 500 / 1000) |
| **L** | Slot-Maschine auswählen (jeder Spieler eine andere) |
| **O** | Autospin an/aus |

## Weitere Effekte
- **LKW-Kapazität ×5** (mehr Ladung pro LKW)
- **Erz-Ertrag ×3** (auch für die Bombe)
- **Casino:** Ausgabe-/Gewinnlimit praktisch unbegrenzt, Gewinn-Auszahlung erhöht, Winrate leicht erhöht
- Slot-Anzeige mit **Einsatz / Gesetzt / Gewonnen / Saldo**

Alles einstellbar in `BepInEx/config/ofs.nuke.cfg` (wird beim ersten Start erzeugt).

## Installation (für Mitspieler)
1. Unter **[Releases](../../releases)** die aktuelle `OreFactorySquad-Mod.zip` herunterladen.
2. ZIP **entpacken**.
3. Spiel **schließen**.
4. **`INSTALLIEREN.bat`** doppelklicken – findet den Spielordner automatisch und kopiert alles rein.
5. Spiel starten. Der **erste Start dauert länger** (BepInEx richtet sich ein) – nicht abbrechen.

## Multiplayer-Hinweis
- **F8 (Bombe)** kann nur der **Host** auslösen; **LKW ×5**, **Erz ×3** und **Casino** wirken host-seitig für alle.
- **H (Handy)**, **J/K/L/O (Slots)** braucht **jeder Spieler selbst** installiert.

## Mod deaktivieren
Im Spielordner `winhttp.dll` in `winhttp.dll.off` umbenennen. Zum Reaktivieren zurückbenennen.

---
*Enthält keine Spieldateien – nur BepInEx, den .NET-Loader und den Mod.*

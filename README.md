# Ore Factory Squad – Co-op Mod

Ein privater Mod für **Ore Factory Squad** (Steam) zum Spielen mit Freunden.
Basiert auf **BepInEx (IL2CPP)** + Harmony.

## Funktionen
| Taste / Feature | Wirkung |
|---|---|
| **F8** | „Atombombe" auf der Mine (nur der Host) |
| **H** | Handy: Laptop-Oberfläche öffnen (Verträge, Bank, Markt …) |
| **J** | Slots / Casino öffnen |
| LKW-Kapazität ×5 | mehr Ladung pro LKW |
| Erz-Ertrag ×3 | auch für die Bombe |
| Förderbänder | beliebig hoch stapelbar |

Alle Werte/Tasten sind einstellbar in `BepInEx/config/ofs.nuke.cfg` (wird beim ersten Start erzeugt).

## Installation (für Mitspieler)
1. Unter **[Releases](../../releases)** die aktuelle `OreFactorySquad-Mod.zip` herunterladen.
2. ZIP **entpacken**.
3. Spiel **schließen**.
4. **`INSTALLIEREN.bat`** doppelklicken – findet den Spielordner automatisch und kopiert alles rein.
5. Spiel starten. Der **erste Start dauert länger** (BepInEx richtet sich ein) – nicht abbrechen.

## Multiplayer-Hinweis
- **F8 (Bombe)** kann nur der **Host** auslösen; **LKW ×5** und **Erz ×3** wirken automatisch für alle.
- **H (Handy)**, **J (Slots)** und **hohe Bänder** braucht **jeder Spieler selbst** installiert.

## Mod deaktivieren
Im Spielordner `winhttp.dll` in `winhttp.dll.off` umbenennen. Zum Reaktivieren zurückbenennen.

---
*Enthält keine Spieldateien – nur BepInEx, den .NET-Loader und den Mod.*

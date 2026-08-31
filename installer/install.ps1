# Ore Factory Squad - Mod Installer
# Findet den Spielordner automatisch und kopiert die Mod-Dateien hinein.
$ErrorActionPreference = 'Stop'
$src = $PSScriptRoot

function Write-Info($m){ Write-Host $m -ForegroundColor Cyan }
function Write-Ok($m){ Write-Host $m -ForegroundColor Green }
function Write-Err($m){ Write-Host $m -ForegroundColor Red }

Write-Host ""
Write-Info "=== Ore Factory Squad - Mod Installer ==="
Write-Host ""

# --- Spielordner finden ---
function Find-Game {
    $libs = New-Object System.Collections.Generic.List[string]

    # Steam-Installpfad aus Registry
    $steam = $null
    foreach ($k in @('HKCU:\Software\Valve\Steam','HKLM:\SOFTWARE\WOW6432Node\Valve\Steam','HKLM:\SOFTWARE\Valve\Steam')) {
        try {
            $p = (Get-ItemProperty $k -ErrorAction SilentlyContinue)
            if ($p.SteamPath) { $steam = $p.SteamPath }
            elseif ($p.InstallPath) { $steam = $p.InstallPath }
            if ($steam) { break }
        } catch {}
    }
    if ($steam) {
        $steam = $steam -replace '/','\'
        $libs.Add($steam)
        $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
        if (Test-Path $vdf) {
            foreach ($line in Get-Content $vdf) {
                $m = [regex]::Match($line, '"path"\s+"([^"]+)"')
                if ($m.Success) { $libs.Add(($m.Groups[1].Value -replace '\\\\','\')) }
            }
        }
    }
    # Alle Laufwerke nach ueblichen Steam-Bibliotheken absuchen
    foreach ($d in [System.IO.DriveInfo]::GetDrives()) {
        if ($d.IsReady) {
            $libs.Add((Join-Path $d.RootDirectory.FullName 'SteamLibrary'))
            $libs.Add((Join-Path $d.RootDirectory.FullName 'Steam'))
            $libs.Add((Join-Path $d.RootDirectory.FullName 'Program Files (x86)\Steam'))
            $libs.Add((Join-Path $d.RootDirectory.FullName 'Games\SteamLibrary'))
        }
    }

    foreach ($l in ($libs | Select-Object -Unique)) {
        $p = Join-Path $l 'steamapps\common\Ore Factory Squad'
        if (Test-Path (Join-Path $p 'Ore Factory Squad.exe')) { return $p }
    }
    return $null
}

$game = Find-Game
if (-not $game) {
    Write-Err "Spielordner nicht automatisch gefunden."
    Write-Host "Bitte den Ordner angeben, in dem 'Ore Factory Squad.exe' liegt."
    Write-Host "(In Steam: Rechtsklick auf das Spiel -> Verwalten -> Lokale Dateien durchsuchen)"
    $game = (Read-Host "Pfad").Trim('"')
}

if (-not (Test-Path (Join-Path $game 'Ore Factory Squad.exe'))) {
    Write-Err "In '$game' wurde keine 'Ore Factory Squad.exe' gefunden. Abbruch."
    return
}
Write-Ok "Spiel gefunden: $game"

# --- Pruefen, ob das Spiel laeuft ---
$proc = Get-Process -Name 'Ore Factory Squad' -ErrorAction SilentlyContinue
if ($proc) {
    Write-Err "Das Spiel laeuft gerade. Bitte SCHLIESSEN und den Installer erneut starten."
    return
}

# --- Kopieren ---
$items = @('BepInEx','dotnet','winhttp.dll','doorstop_config.ini','.doorstop_version')
Write-Host ""
Write-Info "Installiere Mod-Dateien..."
foreach ($i in $items) {
    $s = Join-Path $src $i
    if (Test-Path $s) {
        Copy-Item $s $game -Recurse -Force
        Write-Host ("  + " + $i)
    }
}

Write-Host ""
Write-Ok "FERTIG! Der Mod ist installiert."
Write-Host ""
Write-Info "Wichtig beim ERSTEN Start:"
Write-Host "  - Der erste Spielstart dauert laenger (BepInEx richtet sich ein). Bitte NICHT abbrechen."
Write-Host ""
Write-Info "Tasten im Spiel:"
Write-Host "  F8 = Bombe (nur der Host)      H = Laptop/Handy      J = Slots/Casino"
Write-Host ""
Write-Info "Einstellungen (Tasten/Werte) findest du nach dem 1. Start unter:"
Write-Host "  <Spielordner>\BepInEx\config\ofs.nuke.cfg"
Write-Host ""

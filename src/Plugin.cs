using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

namespace OfsNuke
{
    [BepInPlugin("ofs.nuke", "OFS Nuke (F8)", "0.1.0")]
    public class Plugin : BasePlugin
    {
        internal static ManualLogSource Log;

        // Tunable config (BepInEx/config/ofs.nuke.cfg)
        internal static ConfigEntry<float> BreakRadius;
        internal static ConfigEntry<float> BoxHalfX;
        internal static ConfigEntry<float> BoxHalfY;
        internal static ConfigEntry<float> BoxHalfZ;
        internal static ConfigEntry<float> VerticalOffset;
        internal static ConfigEntry<float> DigOpacity;
        internal static ConfigEntry<bool>  DigTerrain;
        internal static ConfigEntry<bool>  BreakOre;

        // Truck + miners
        internal static ConfigEntry<bool>  TruckBoostEnabled;
        internal static ConfigEntry<float> TruckCapacityMultiplier;
        internal static ConfigEntry<bool>  MinerBoostEnabled;
        internal static ConfigEntry<int>   ExtraMinerCapacity;

        // Belts
        internal static ConfigEntry<bool>  BeltUnlimitedHeight;
        internal static ConfigEntry<bool>  BeltDebug;

        // Ore yield
        internal static ConfigEntry<bool>  OreYieldEnabled;
        internal static ConfigEntry<float> OreYieldMultiplier;

        // Phone (portable laptop + slots)
        internal static ConfigEntry<bool>   PhoneEnabled;
        internal static ConfigEntry<string> PhoneLaptopKey;
        internal static ConfigEntry<string> PhoneSlotsKey;   // Slot drehen
        internal static ConfigEntry<string> PhoneBetKey;     // Einsatz wechseln
        internal static ConfigEntry<string> PhoneSlotSelectKey; // Maschine auswaehlen
        internal static ConfigEntry<string> PhoneAutoSpinKey;   // Autospin an/aus

        // Casino
        internal static ConfigEntry<bool>  CasinoNoLimit;
        internal static ConfigEntry<bool>  SlotWinBoost;
        internal static ConfigEntry<float> SlotWinChance;
        internal static ConfigEntry<float> SlotWinMultiplier;
        internal static ConfigEntry<int>   SlotBet1;
        internal static ConfigEntry<int>   SlotBet2;
        internal static ConfigEntry<int>   SlotBet3;
        internal static ConfigEntry<float> SlotPayoutMultiplier;

        private Harmony _harmony;

        public override void Load()
        {
            Log = base.Log;

            BreakOre     = Config.Bind("Nuke", "BreakOre", true,  "Alle Erz-Nodes im Bereich zerbrechen und gutschreiben.");
            DigTerrain   = Config.Bind("Nuke", "DigTerrain", true, "Das Terrain (Erde) im Bereich wegsprengen.");
            BreakRadius  = Config.Bind("Nuke", "BreakRadius", 80f, "Radius (m), in dem Erz-Nodes zerbrochen werden.");
            BoxHalfX     = Config.Bind("Nuke", "BoxHalfX", 60f,    "Halbe Breite der weggegrabenen Box (X).");
            BoxHalfY     = Config.Bind("Nuke", "BoxHalfY", 30f,    "Halbe Hoehe der weggegrabenen Box (Y).");
            BoxHalfZ     = Config.Bind("Nuke", "BoxHalfZ", 60f,    "Halbe Tiefe der weggegrabenen Box (Z).");
            VerticalOffset = Config.Bind("Nuke", "VerticalOffset", -20f, "Vertikaler Versatz des Box-Mittelpunkts relativ zum Spieler (negativ = nach unten).");
            DigOpacity   = Config.Bind("Nuke", "DigOpacity", 1f,   "Grab-Staerke 0..1 (1 = komplett weg).");

            TruckBoostEnabled       = Config.Bind("Truck", "Enabled", true, "LKW-Kapazitaet erhoehen.");
            TruckCapacityMultiplier = Config.Bind("Truck", "CapacityMultiplier", 5f, "Faktor, mit dem die LKW-Gesamtkapazitaet multipliziert wird.");
            MinerBoostEnabled  = Config.Bind("Miners", "Enabled", true, "Mehr Bergarbeiter erlauben.");
            ExtraMinerCapacity = Config.Bind("Miners", "ExtraCapacity", 4, "Zusaetzliche Bergarbeiter-Plaetze ueber das normale Maximum hinaus (z.B. 4 -> 4+4).");

            BeltUnlimitedHeight = Config.Bind("Belts", "UnlimitedHeight", true, "Foerderbaender beliebig hoch stapelbar (hebt die 'nur eins nach oben'-Grenze auf).");
            BeltDebug           = Config.Bind("Belts", "Debug", true, "Diagnose-Logging fuer das Band-Bauen (in LogOutput.log).");

            OreYieldEnabled    = Config.Bind("Ore", "Enabled", true, "Erz-Ertrag multiplizieren (betrifft auch die F8-Bombe).");
            OreYieldMultiplier = Config.Bind("Ore", "YieldMultiplier", 3f, "Faktor fuer die Erz-Gutschrift (z.B. 3 = dreifaches Erz).");

            PhoneEnabled   = Config.Bind("Phone", "Enabled", true, "Handy: Laptop-UI + Slots per Hotkey (jeder Spieler braucht den Mod).");
            PhoneLaptopKey = Config.Bind("Phone", "LaptopKey", "H", "Taste zum Oeffnen/Schliessen der Laptop-UI (Key-Name, z.B. H, N, Numpad0).");
            PhoneSlotsKey  = Config.Bind("Phone", "SlotsKey", "J", "Taste zum DREHEN der naechstgelegenen Slot-Maschine.");
            PhoneBetKey    = Config.Bind("Phone", "BetKey", "K", "Taste zum Wechseln des Einsatzes (5 -> 10 -> 30).");
            PhoneSlotSelectKey = Config.Bind("Phone", "SlotSelectKey", "L", "Taste zum Auswaehlen der Slot-Maschine (durchschalten) - so kann jeder Spieler eine andere nehmen.");
            PhoneAutoSpinKey   = Config.Bind("Phone", "AutoSpinKey", "O", "Taste fuer Autospin an/aus (dreht die gewaehlte Maschine automatisch).");

            CasinoNoLimit     = Config.Bind("Slots", "RemoveDailyLimit", true, "Taegliche Ausgabe-/Gewinn-Limits des Casinos aufheben.");
            SlotWinBoost      = Config.Bind("Slots", "WinBoost", true, "Winrate minimal erhoehen (wandelt selten eine Niete in einen kleinen Gewinn).");
            SlotWinChance     = Config.Bind("Slots", "ExtraWinChance", 0.1f, "Wahrscheinlichkeit (0..1), eine Niete in einen Gewinn zu wandeln.");
            SlotWinMultiplier = Config.Bind("Slots", "ExtraWinMultiplier", 2f, "Auszahlung (x Einsatz) fuer so einen Zusatzgewinn.");
            SlotBet1          = Config.Bind("Slots", "Bet1", 100,  "Einsatz-Stufe 1 (Taste K schaltet durch).");
            SlotBet2          = Config.Bind("Slots", "Bet2", 500,  "Einsatz-Stufe 2.");
            SlotBet3          = Config.Bind("Slots", "Bet3", 1000, "Einsatz-Stufe 3.");
            SlotPayoutMultiplier = Config.Bind("Slots", "PayoutMultiplier", 3f, "Echte Gewinne zahlen x dieser Faktor mehr aus (Gewinnanteil hoeher).");

            _harmony = new Harmony("ofs.nuke");
            try { _harmony.PatchAll(typeof(Plugin).Assembly); Log.LogInfo("Harmony-Patches aktiv (LKW/Bergarbeiter)."); }
            catch (Exception e) { Log.LogError("Harmony-PatchAll-Fehler: " + e); }

            ClassInjector.RegisterTypeInIl2Cpp<NukeRunner>();

            var go = new GameObject("OfsNukeRunner");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<NukeRunner>();

            Log.LogInfo("OFS Nuke geladen. Taste F8 zuendet die Bombe (nur als Host).");
        }
    }

    public class NukeRunner : MonoBehaviour
    {
        public NukeRunner(IntPtr ptr) : base(ptr) { }

        private void Update()
        {
            try
            {
                var kb = Keyboard.current;
                if (kb == null) return;
                if (kb.f8Key.wasPressedThisFrame)
                    Detonate();

                if (Plugin.PhoneEnabled.Value)
                {
                    if (KeyPressed(kb, Plugin.PhoneLaptopKey.Value, Key.H)) ToggleLaptop();
                    if (KeyPressed(kb, Plugin.PhoneSlotsKey.Value, Key.J)) SpinSlots();
                    if (KeyPressed(kb, Plugin.PhoneBetKey.Value, Key.K)) CycleBet();
                    if (KeyPressed(kb, Plugin.PhoneSlotSelectKey.Value, Key.L)) SelectNextSlot();
                    if (KeyPressed(kb, Plugin.PhoneAutoSpinKey.Value, Key.O)) ToggleAutoSpin();
                    AutoSpinTick();
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Update-Fehler: " + e);
            }
        }

        private bool _laptopCursorActive;
        private float _nextLimitTime;
        private bool _limitLogged;

        // Maus sichtbar halten solange der Laptop offen ist; beim Schliessen
        // wieder sperren/ausblenden (sonst bleibt der Cursor bis ESC haengen).
        private void LateUpdate()
        {
            // Casino-Limit aufheben (unabhaengig vom Handy)
            if (Plugin.CasinoNoLimit.Value && Time.realtimeSinceStartup >= _nextLimitTime)
            {
                _nextLimitTime = Time.realtimeSinceStartup + 1f;
                try
                {
                    var cm = CasinoManager.Instance;
                    if (cm != null)
                    {
                        if (!_limitLogged)
                        {
                            _limitLogged = true;
                            Plugin.Log.LogInfo($"[DIAG-Casino] vorher: SpendLimit={cm.dailySpendLimit} WinLimit={cm.dailyWinLimit} spent={cm._dailySpent} won={cm._dailyWon}");
                        }
                        cm.dailySpendLimit = int.MaxValue;
                        cm.dailyWinLimit = int.MaxValue;
                        cm._dailySpent = 0;   // Zaehler niedrig halten -> Limit wird nie erreicht
                        cm._dailyWon = 0;
                    }
                }
                catch (Exception e) { Plugin.Log.LogError("Casino-Limit-Fehler: " + e); }
            }

            if (!Plugin.PhoneEnabled.Value) return;
            try
            {
                var gm = GameManager.Instance;
                var ui = gm != null ? gm.UImanager : null;
                var cui = ui != null ? ui.computerUI : null;
                bool open = cui != null && cui.computerPanel != null && cui.computerPanel.activeSelf;
                if (open)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    _laptopCursorActive = true;
                }
                else if (_laptopCursorActive)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    _laptopCursorActive = false;
                }
            }
            catch { }
        }

        private static bool KeyPressed(Keyboard kb, string keyName, Key fallback)
        {
            Key key = fallback;
            if (!string.IsNullOrEmpty(keyName) && !Enum.TryParse<Key>(keyName, true, out key)) key = fallback;
            try { return kb[key].wasPressedThisFrame; } catch { return false; }
        }

        private bool _slotsOpen;

        private void ToggleLaptop()
        {
            try
            {
                var gm = GameManager.Instance;
                var ui = gm != null ? gm.UImanager : null;
                if (ui == null) { Plugin.Log.LogWarning("Handy: UIManager noch nicht bereit (in Welt sein)."); return; }
                var cui = ui.computerUI;
                bool open = cui != null && cui.computerPanel != null && cui.computerPanel.activeSelf;
                if (open) ui.CloseComputerUI(); else ui.OpenComputerUI();
                Plugin.Log.LogInfo("Handy: Laptop-UI " + (open ? "geschlossen" : "geoeffnet"));
            }
            catch (Exception e) { Plugin.Log.LogError("Handy Laptop-Fehler: " + e); }
        }

        private int _betIndex;          // 0=5, 1=10, 2=30
        private int _curBet = 5;
        private uint _targetNetId;       // gewaehlte Maschine (0 = automatisch naechste)
        private int _selIndex, _selCount;
        private float _slotHudUntil;     // bis wann die Slot-Anzeige sichtbar ist
        private string _slotStatus = "";
        private bool _autoSpin;
        private float _autoNextTime;
        private int _statBet, _statWin;  // Gesamt gesetzt / gewonnen (der gewaehlten Maschine)

        private void RefreshStats(SlotMachine m)
        {
            if (m == null) return;
            try { _statBet = m.NetworktotalBet; _statWin = m.NetworktotalWin; _curBet = m.NetworkcurrentBet; } catch { }
        }

        private void ToggleAutoSpin()
        {
            _autoSpin = !_autoSpin;
            _slotStatus = _autoSpin ? "AUTOSPIN AN" : "AUTOSPIN AUS";
            ShowSlotHud();
            Plugin.Log.LogInfo("Slots: Autospin " + (_autoSpin ? "AN" : "AUS"));
        }

        private void AutoSpinTick()
        {
            if (!_autoSpin) return;
            if (Time.realtimeSinceStartup < _autoNextTime) return;
            _autoNextTime = Time.realtimeSinceStartup + 0.4f; // nicht zu schnell nachpruefen
            try
            {
                var list = AllSlots(); _selCount = list.Count;
                var m = GetTargetSlot(list);
                if (m == null) { _autoSpin = false; _slotStatus = "keine Maschine - Autospin aus"; ShowSlotHud(); return; }
                RefreshStats(m);
                if (!m.NetworkisBusy) { m.Spin(); }
                _slotStatus = "AUTOSPIN AN";
                ShowSlotHud();
            }
            catch (Exception e) { Plugin.Log.LogError("Autospin-Fehler: " + e); _autoSpin = false; }
        }

        private List<SlotMachine> AllSlots()
        {
            var arr = UnityEngine.Object.FindObjectsOfType<SlotMachine>();
            var list = new List<SlotMachine>();
            if (arr != null) for (int i = 0; i < arr.Length; i++) if (arr[i] != null) list.Add(arr[i]);
            list.Sort((a, b) => a.netId.CompareTo(b.netId));
            return list;
        }

        private Vector3 PlayerPos()
        {
            var lp = NetworkClient.localPlayer;
            if (lp != null) return lp.transform.position;
            if (Camera.main != null) return Camera.main.transform.position;
            return Vector3.zero;
        }

        private SlotMachine GetTargetSlot(List<SlotMachine> list)
        {
            if (list.Count == 0) return null;
            if (_targetNetId != 0)
                for (int i = 0; i < list.Count; i++) if (list[i].netId == _targetNetId) { _selIndex = i; return list[i]; }
            Vector3 p = PlayerPos();
            SlotMachine best = null; float bestD = float.MaxValue; int bi = 0;
            for (int i = 0; i < list.Count; i++)
            {
                float d = (list[i].transform.position - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = list[i]; bi = i; }
            }
            _selIndex = bi;
            return best;
        }

        private void ShowSlotHud() { _slotHudUntil = Time.realtimeSinceStartup + 8f; }

        private void SelectNextSlot()
        {
            try
            {
                var list = AllSlots();
                _selCount = list.Count;
                if (list.Count == 0) { _slotStatus = "keine Maschine gefunden"; ShowSlotHud(); return; }
                int idx = -1;
                for (int i = 0; i < list.Count; i++) if (list[i].netId == _targetNetId) { idx = i; break; }
                idx = (idx + 1) % list.Count;
                _targetNetId = list[idx].netId;
                _selIndex = idx;
                _slotStatus = "";
                RefreshStats(list[idx]);
                ShowSlotHud();
                Plugin.Log.LogInfo($"Slots: Maschine {idx + 1}/{list.Count} gewaehlt.");
            }
            catch (Exception e) { Plugin.Log.LogError("Slots Select-Fehler: " + e); }
        }

        private void CycleBet()
        {
            try
            {
                var list = AllSlots(); _selCount = list.Count;
                var m = GetTargetSlot(list);
                if (m == null) { _slotStatus = "keine Maschine gefunden"; ShowSlotHud(); return; }
                _betIndex = (_betIndex + 1) % 3;
                int bet = _betIndex == 0 ? Plugin.SlotBet1.Value : _betIndex == 1 ? Plugin.SlotBet2.Value : Plugin.SlotBet3.Value;
                m.SetBet(bet);
                RefreshStats(m);
                _slotStatus = "";
                ShowSlotHud();
                Plugin.Log.LogInfo($"Slots: Einsatz = {bet}");
            }
            catch (Exception e) { Plugin.Log.LogError("Slots Bet-Fehler: " + e); }
        }

        private void SpinSlots()
        {
            try
            {
                var list = AllSlots(); _selCount = list.Count;
                var m = GetTargetSlot(list);
                if (m == null) { _slotStatus = "keine Maschine gefunden"; ShowSlotHud(); return; }
                m.Spin();
                RefreshStats(m);
                _slotStatus = "gedreht!";
                ShowSlotHud();
                Plugin.Log.LogInfo("Slots: gedreht.");
            }
            catch (Exception e) { Plugin.Log.LogError("Slots Spin-Fehler: " + e); }
        }

        private void OnGUI()
        {
            if (!Plugin.PhoneEnabled.Value) return;
            if (!_autoSpin && Time.realtimeSinceStartup > _slotHudUntil) return;
            string machine = _selCount > 0 ? $"Maschine {_selIndex + 1}/{_selCount}" : "keine Maschine";
            string auto = _autoSpin ? "  [AUTOSPIN AN]" : "";
            int net = _statWin - _statBet;
            string txt =
                $"SLOTS  -  {machine}{auto}\n" +
                $"Einsatz: {_curBet}   {_slotStatus}\n" +
                $"Gesetzt: {_statBet}   Gewonnen: {_statWin}   Saldo: {net}\n" +
                $"[K] Einsatz  [J] Drehen  [L] Maschine  [O] Autospin";
            GUI.Box(new Rect(20f, 20f, 430f, 104f), txt);
        }

        private void Detonate()
        {
            // Nur der Host (Server) darf die serverseitige Logik ausfuehren.
            if (!NetworkServer.active)
            {
                Plugin.Log.LogWarning("Bombe: Nur der Host kann sie zuenden (du bist nicht der Host dieser Welt).");
                return;
            }

            // Mittelpunkt = Position des lokalen Spielers.
            var localId = NetworkClient.localPlayer;
            if (localId == null)
            {
                Plugin.Log.LogWarning("Bombe: lokaler Spieler noch nicht bereit.");
                return;
            }
            Vector3 playerPos = localId.transform.position;
            Vector3 center = new Vector3(playerPos.x, playerPos.y + Plugin.VerticalOffset.Value, playerPos.z);

            var attacker = localId.connectionToClient; // Host-Spielerverbindung -> Belohnung geht an dich

            // 1) Erz zerbrechen + gutschreiben (numerisch an Host -> kein Rucksack-Ueberlauf)
            if (Plugin.BreakOre.Value)
            {
                var nbs = NodeBreakSystem.Instance;
                if (nbs != null)
                {
                    try
                    {
                        nbs.ServerForceBreakInSphere(center, Plugin.BreakRadius.Value, attacker, true);
                        Plugin.Log.LogInfo($"Erz zerbrochen im Radius {Plugin.BreakRadius.Value} um {center}.");
                    }
                    catch (Exception e) { Plugin.Log.LogError("ServerForceBreakInSphere-Fehler: " + e); }
                }
                else Plugin.Log.LogWarning("NodeBreakSystem.Instance == null (bist du in einer Digsite/Welt?).");
            }

            // 2) Terrain wegsprengen (Erde verschwindet)
            if (Plugin.DigTerrain.Value)
            {
                var digger = DiggerController.Instance;
                if (digger != null)
                {
                    try
                    {
                        Vector3 half = new Vector3(Plugin.BoxHalfX.Value, Plugin.BoxHalfY.Value, Plugin.BoxHalfZ.Value);
                        digger.ServerDigBoxAtPosition(center, half, Plugin.DigOpacity.Value);
                        Plugin.Log.LogInfo($"Terrain-Box weggegraben: center={center} half={half}.");
                    }
                    catch (Exception e) { Plugin.Log.LogError("ServerDigBoxAtPosition-Fehler: " + e); }
                }
                else Plugin.Log.LogWarning("DiggerController.Instance == null.");
            }

            Plugin.Log.LogInfo("Bombe gezuendet.");
        }
    }

    // LKW-Kapazitaet: das ECHTE Kapazitaetsfeld (SyncVar) erhoehen, damit die
    // Ladelogik (CanAddSack/CanAddItems/HasSpaceFor) tatsaechlich mehr zulaesst.
    internal static class TruckBoost
    {
        private static bool _busy;

        public static void Apply(T_Truck t, string via)
        {
            if (t == null || !Plugin.TruckBoostEnabled.Value || _busy) return;
            try
            {
                int baseCap = t.Network_totalCapacity;       // aktueller (Basis-)Wert im Feld
                long boosted = (long)Math.Round(baseCap * (double)Plugin.TruckCapacityMultiplier.Value);
                if (boosted > int.MaxValue) boosted = int.MaxValue;
                Plugin.Log.LogInfo($"[DIAG-Truck] {via}: base={baseCap} -> boosted={boosted} (server={NetworkServer.active})");
                if (baseCap <= 0 || boosted <= baseCap) return;
                _busy = true;
                t.Network_totalCapacity = (int)boosted;      // setzt _totalCapacity + synct zu Clients
                Plugin.Log.LogInfo($"[DIAG-Truck] nach set: Network_totalCapacity={t.Network_totalCapacity}");
            }
            catch (Exception e) { Plugin.Log.LogError("TruckBoost-Fehler: " + e); }
            finally { _busy = false; }
        }
    }

    [HarmonyPatch(typeof(T_Truck), "ApplyTotalCapacityFromIndex")]
    internal static class Patch_TruckApplyIndex
    {
        private static void Postfix(T_Truck __instance) => TruckBoost.Apply(__instance, "ApplyIndex");
    }

    [HarmonyPatch(typeof(T_Truck), nameof(T_Truck.SetTotalCapacity))]
    internal static class Patch_TruckSetCap
    {
        private static void Postfix(T_Truck __instance) => TruckBoost.Apply(__instance, "SetTotalCapacity");
    }

    // DIAG: feuert die Ladeprueffung ueberhaupt, und was ist die Kapazitaet dort?
    [HarmonyPatch(typeof(T_Truck), "CanAddSack")]
    internal static class Patch_TruckCanAddSack
    {
        private static void Postfix(T_Truck __instance, int sackItemCount, ref bool __result)
        {
            if (!Plugin.TruckBoostEnabled.Value) return;
            try
            {
                int cap = __instance.TotalCapacity;
                int cur = __instance.CurrentItemCount;
                int boosted = (int)Math.Round(cap * (double)Plugin.TruckCapacityMultiplier.Value);
                Diag.N("truckCanAdd", 8, $"[DIAG-Truck] CanAddSack: cur={cur} cap={cap} sack={sackItemCount} result={__result}");
                if (!__result && cur + sackItemCount <= boosted) __result = true; // gegen die BOOSTED Kapazitaet zulassen
            }
            catch (Exception e) { Plugin.Log.LogError("CanAddSack-Patch-Fehler: " + e); }
        }
    }

    // Bergarbeiter-Kapazitaet erhoehen.
    [HarmonyPatch(typeof(EmployeeManager), nameof(EmployeeManager.MinerCapacity), MethodType.Getter)]
    internal static class Patch_MinerCapacity
    {
        private static void Postfix(ref int __result)
        {
            if (!Plugin.MinerBoostEnabled.Value) return;
            __result += Plugin.ExtraMinerCapacity.Value;
        }
    }

    // Foerderband-Hoehe: die Tier-Deckelung im OutputTier-Getter aufheben.
    // Original: OutputTier = min(ownerBuilding.elevationTier + buildingItemSO.elevationDelta, MaxElevationTier=1).
    // Wir geben den ECHTEN (ungedeckelten) Tier zurueck, damit Baender beliebig hoch stapelbar sind.
    [HarmonyPatch(typeof(T_Socket), "get_OutputTier")]
    internal static class Patch_OutputTier
    {
        private static void Postfix(T_Socket __instance, ref int __result)
        {
            if (!Plugin.BeltUnlimitedHeight.Value) return;
            try
            {
                BuildingObject bo = __instance._ownerBuilding;
                if (bo == null) bo = __instance.OwnerBuilding;
                int tier = bo != null ? bo.ElevationTier : -999;
                int delta = (bo != null && bo.buildingItemSO != null) ? bo.buildingItemSO.elevationDelta : 1;
                Diag.N("outputtier", 30, $"[DIAG-Belt] get_OutputTier: result={__result} owner={(bo!=null)} elevTier={tier} delta={delta}");
                if (bo == null) return;
                int uncapped = tier + delta;
                if (uncapped > __result) __result = uncapped; // nur anheben, nie senken
            }
            catch (Exception e) { Plugin.Log.LogError("OutputTier-Patch-Fehler: " + e); }
        }
    }

    // Zusaetzlich die Tier-Zulassungspruefung entschaerfen (falls sie separat greift).
    [HarmonyPatch(typeof(T_Socket), "TierAllows")]
    internal static class Patch_BeltTier
    {
        private static void Postfix(ref bool __result)
        {
            if (!Plugin.BeltUnlimitedHeight.Value) return;
            __result = true;
        }
    }

    // Erz-Ertrag: rewardMultiplier beim Zerbrechen jedes Stuecks skalieren.
    // Server_ForceBreakPiece multipliziert die Erz-Menge intern mit rewardMultiplier
    // -> deckt normalen Abbau UND die F8-Bombe ab.
    // Erz-Ertrag: die Erz-Menge pro Stueck an der QUELLE multiplizieren.
    // pieceCollectAmounts (SyncList) wird beim Node-Spawn befuellt. Parameter-Patches
    // wirken in IL2CPP nicht, aber eine Collection-Aenderung schon.
    internal static class OreBoost
    {
        private static readonly HashSet<int> _done = new();
        public static void Boost(T_Item node)
        {
            if (node == null || !Plugin.OreYieldEnabled.Value) return;
            if (!NetworkServer.active) return; // SyncList nur auf dem Server aendern
            try
            {
                int id = node.GetInstanceID();
                if (!_done.Add(id)) return;
                int mult = Math.Max(1, (int)Math.Round((double)Plugin.OreYieldMultiplier.Value));
                if (mult <= 1) return;
                var amounts = node.pieceCollectAmounts;
                if (amounts == null) return;
                int n = amounts.Count;
                for (int i = 0; i < n; i++) amounts[i] = amounts[i] * mult;
                Plugin.Log.LogInfo($"[Ore] Node x{mult}: {n} Stuecke geboostet.");
            }
            catch (Exception e) { Plugin.Log.LogError("OreBoost-Fehler: " + e); }
        }
    }

    [HarmonyPatch(typeof(T_Item), "InitializeNodePieces")]
    internal static class Patch_NodeInit
    { private static void Postfix(T_Item __instance) => OreBoost.Boost(__instance); }

    [HarmonyPatch(typeof(T_Item), "InitializeMysteryNodePieces")]
    internal static class Patch_MysteryInit
    { private static void Postfix(T_Item __instance) => OreBoost.Boost(__instance); }

    // Winrate minimal erhoehen: eine Niete (Evaluate<=0) selten in einen Gewinn wandeln.
    [HarmonyPatch(typeof(SlotMachine), "Evaluate")]
    internal static class Patch_SlotEvaluate
    {
        private static void Postfix(ref float __result)
        {
            if (!Plugin.SlotWinBoost.Value) return;
            if (__result > 0f)
                __result *= Plugin.SlotPayoutMultiplier.Value;               // echte Gewinne zahlen mehr
            else if (UnityEngine.Random.value < Plugin.SlotWinChance.Value)
                __result = Plugin.SlotWinMultiplier.Value;                    // seltene Niete -> kleiner Gewinn
        }
    }

    // Diagnose-Zaehler: loggt pro Schluessel die ersten <max> Aufrufe.
    internal static class Diag
    {
        private static readonly Dictionary<string, int> _n = new();
        public static void N(string key, int max, string msg)
        {
            int c = _n.TryGetValue(key, out var v) ? v : 0;
            if (c >= max) return;
            _n[key] = c + 1;
            Plugin.Log.LogInfo(msg);
        }
    }
}

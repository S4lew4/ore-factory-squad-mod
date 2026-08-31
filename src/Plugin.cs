using System;
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
        internal static ConfigEntry<string> PhoneSlotsKey;

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
            PhoneSlotsKey  = Config.Bind("Phone", "SlotsKey", "J", "Taste zum Oeffnen/Schliessen der Slots/Casino-UI.");

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
                    if (KeyPressed(kb, Plugin.PhoneSlotsKey.Value, Key.J)) ToggleSlots();
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Update-Fehler: " + e);
            }
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

        private void ToggleSlots()
        {
            try
            {
                var cm = CasinoManager.Instance;
                if (cm == null) { Plugin.Log.LogWarning("Handy: Casino noch nicht bereit (in Welt sein)."); return; }
                if (_slotsOpen) cm.CloseUI(); else cm.OpenUI();
                _slotsOpen = !_slotsOpen;
                Plugin.Log.LogInfo("Handy: Slots-UI " + (_slotsOpen ? "geoeffnet" : "geschlossen"));
            }
            catch (Exception e) { Plugin.Log.LogError("Handy Slots-Fehler: " + e); }
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

        public static void Apply(T_Truck t)
        {
            if (t == null || !Plugin.TruckBoostEnabled.Value || _busy) return;
            try
            {
                int baseCap = t.Network_totalCapacity;       // aktueller (Basis-)Wert im Feld
                if (baseCap <= 0) return;
                long boosted = (long)Math.Round(baseCap * (double)Plugin.TruckCapacityMultiplier.Value);
                if (boosted > int.MaxValue) boosted = int.MaxValue;
                if (boosted <= baseCap) return;
                _busy = true;
                t.Network_totalCapacity = (int)boosted;      // setzt _totalCapacity + synct zu Clients
            }
            catch (Exception e) { Plugin.Log.LogError("TruckBoost-Fehler: " + e); }
            finally { _busy = false; }
        }
    }

    [HarmonyPatch(typeof(T_Truck), "ApplyTotalCapacityFromIndex")]
    internal static class Patch_TruckApplyIndex
    {
        private static void Postfix(T_Truck __instance) => TruckBoost.Apply(__instance);
    }

    [HarmonyPatch(typeof(T_Truck), nameof(T_Truck.SetTotalCapacity))]
    internal static class Patch_TruckSetCap
    {
        private static void Postfix(T_Truck __instance) => TruckBoost.Apply(__instance);
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
    [HarmonyPatch(typeof(T_Socket), nameof(T_Socket.OutputTier), MethodType.Getter)]
    internal static class Patch_OutputTier
    {
        private static void Postfix(T_Socket __instance, ref int __result)
        {
            if (!Plugin.BeltUnlimitedHeight.Value) return;
            try
            {
                BuildingObject bo = __instance.OwnerBuilding;
                if (bo == null) return;
                T_BuildingItemSO so = bo.buildingItemSO;
                int delta = (so != null) ? so.elevationDelta : 1;
                int uncapped = bo.ElevationTier + delta;
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

    // Erz-Ertrag erhoehen: Gutschrift-Menge multiplizieren (deckt auch die Bombe ab).
    [HarmonyPatch(typeof(T_Item), "Server_GiveOreToHost")]
    internal static class Patch_OreToHost
    {
        private static void Prefix(ref int amount)
        {
            if (!Plugin.OreYieldEnabled.Value) return;
            amount = Mult(amount);
        }
        internal static int Mult(int amount)
        {
            long v = (long)Math.Round(amount * (double)Plugin.OreYieldMultiplier.Value);
            if (v > int.MaxValue) v = int.MaxValue;
            if (v < 0) v = 0;
            return (int)v;
        }
    }

    [HarmonyPatch(typeof(T_Item), "TargetRpc_GiveOre")]
    internal static class Patch_OreToClient
    {
        private static void Prefix(ref int amount)
        {
            if (!Plugin.OreYieldEnabled.Value) return;
            amount = Patch_OreToHost.Mult(amount);
        }
    }

}

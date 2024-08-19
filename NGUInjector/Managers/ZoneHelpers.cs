using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using static NGUInjector.Main;

namespace NGUInjector.Managers
{
    static class ZoneHelpers
    {
        public static Dictionary<int, string> ZoneList = new Dictionary<int, string>
        {
            {-1, "Safe Zone: Awakening Site"},
            {0, "Tutorial Zone"},
            {1, "Sewers"},
            {2, "Forest"},
            {3, "Cave of Many Things"},
            {4, "The Sky"},
            {5, "High Security Base"},
            {6, "Gordon Ramsay Bolton"},
            {7, "Clock Dimension"},
            {8, "Grand Corrupted Tree"},
            {9, "The 2D Universe"},
            {10, "Ancient Battlefield"},
            {11, "Jake From Accounting"},
            {12, "A Very Strange Place"},
            {13, "Mega Lands"},
            {14, "UUG THE UNMENTIONABLE"},
            {15, "The Beardverse"},
            {16, "WALDERP"},
            {17, "Badly Drawn World"},
            {18, "Boring-Ass Earth"},
            {19, "THE BEAST"},
            {20, "Chocolate World"},
            {21, "The Evilverse"},
            {22, "Pretty Pink Princess Land"},
            {23, "GREASY NERD"},
            {24, "Meta Land"},
            {25, "Interdimensional Party"},
            {26, "THE GODMOTHER"},
            {27, "Typo Zonw"},
            {28, "The Fad-Lands"},
            {29, "JRPGVille"},
            {30, "THE EXILE"},
            {31, "The Rad-lands"},
            {32, "Back To School"},
            {33, "The West World"},
            {34, "IT HUNGERS"},
            {35, "The Breadverse"},
            {36, "That 70's Zone"},
            {37, "The Halloweenies"},
            {38, "ROCK LOBSTER"},
            {39, "Construction Zone"},
            {40, "DUCK DUCK ZONE"},
            {41, "The Nether Regions"},
            {42, "AMALGAMATE"},
            {43, "7 Aethereal Seas"},
            {44, "TIPPI THE TUTORIAL MOUSE"},
            {45, "THE TRAITOR"}
        };

        internal static readonly int[] TitanZones = { 6, 8, 11, 14, 16, 19, 23, 26, 30, 34, 38, 42, 44, 45 };

        private static Dictionary<int, TitanSnapshot> _titanDetails = new Dictionary<int, TitanSnapshot>();

        private static TitanSnapshotSummary _titanSnapshotSummary = new TitanSnapshotSummary();

        public static int TitanCount()
        {
            return TitanZones.Length;
        }

        internal static bool ZoneIsTitan(int zone)
        {
            return TitanZones.Contains(zone);
        }

        internal static bool ZoneIsWalderp(int zone)
        {
            return zone == 16;
        }

        internal static bool ZoneIsNerd(int zone)
        {
            return zone == 23;
        }

        internal static bool ZoneIsGodmother(int zone)
        {
            return zone == 26;
        }

        internal static bool ZoneIsExile(int zone)
        {
            return zone == 30;
        }

        internal static bool TitanSpawningSoon(int titanIndex)
        {
            return Main.Character.buttons.adventure.IsInteractable() && IsTitanSpawningSoon(titanIndex);
        }

        internal static int? GetHighestSpawningTitanZone()
        {
            var spawningTitans = _titanSnapshotSummary.TitansSpawningSoon.OrderByDescending(x => x.TitanIndex);

            return spawningTitans.Any() ? (int?)TitanZones[spawningTitans.First().TitanIndex] : null;
        }

        internal static bool AnyTitansSpawningSoon()
        {
            return _titanSnapshotSummary.AnySpawningSoon;
        }

        internal static bool ShouldRunGoldLoadout()
        {
            return _titanSnapshotSummary.RunGoldLoadout;
        }

        internal static bool ShouldRunTitanLoadout()
        {
            return _titanSnapshotSummary.RunTitanLoadout;
        }

        internal static void RefreshTitanSnapshots()
        {
            if (!Main.Character.buttons.adventure.IsInteractable())
            {
                _titanSnapshotSummary = new TitanSnapshotSummary();
                _titanDetails.Clear();
                return;
            }

            int maxZone = GetMaxReachableZone(true);
            for (int titanIndex = 0; titanIndex < TitanZones.Length; titanIndex++)
            {
                if (TitanZones[titanIndex] <= maxZone)
                {
                    TitanSnapshot currentSnapshot = GetTitanSnapshot(titanIndex);
                    if (_titanDetails.ContainsKey(titanIndex))
                    {
                        TitanSnapshot oldSnapshot = _titanDetails[titanIndex];
                        oldSnapshot.ShouldUseGoldLoadout = currentSnapshot.ShouldUseGoldLoadout;
                        oldSnapshot.ShouldUseTitanLoadout = currentSnapshot.ShouldUseTitanLoadout;

                        //The titan is active, if it has been active for over 10 minutes, stats are probably not be high enough to kill
                        //Disable the titan to prevent sitting with suboptimal gear forever
                        if (oldSnapshot.SpawnSoonTimestamp.HasValue && currentSnapshot.SpawnSoonTimestamp.HasValue && (currentSnapshot.ShouldUseGoldLoadout || currentSnapshot.ShouldUseTitanLoadout))
                        {
                            //Waiting for kill...
                            if ((currentSnapshot.SpawnSoonTimestamp.Value - oldSnapshot.SpawnSoonTimestamp.Value).TotalMinutes >= 10)
                            {
                                Log($"Titan {titanIndex} still available after 300 seconds");
                                if (currentSnapshot.ShouldUseGoldLoadout)
                                {
                                    Log($"Disabling Titan {titanIndex} as a valid gold swap target");

                                    var tempTargets = Settings.TitanGoldTargets.ToArray();
                                    tempTargets[titanIndex] = false;
                                    Settings.TitanGoldTargets = tempTargets;

                                    var tempDone = Settings.TitanMoneyDone.ToArray();
                                    tempDone[titanIndex] = false;
                                    Settings.TitanMoneyDone = tempDone;

                                    currentSnapshot.ShouldUseGoldLoadout = false;
                                    _titanDetails[titanIndex] = currentSnapshot;
                                }
                                else
                                {
                                    Log($"Disabling Titan {titanIndex} as a valid swap target");

                                    var tempTargets = Settings.TitanSwapTargets.ToArray();
                                    tempTargets[titanIndex] = false;
                                    Settings.TitanSwapTargets = tempTargets;

                                    currentSnapshot.ShouldUseTitanLoadout = false;
                                    _titanDetails[titanIndex] = currentSnapshot;
                                }
                            }
                        }
                        //If the timestamp is now null, the titan has been killed, flag the kill as done if the titan was set to use a gold loadout
                        else if (oldSnapshot.SpawnSoonTimestamp.HasValue && !currentSnapshot.SpawnSoonTimestamp.HasValue && currentSnapshot.ShouldUseGoldLoadout)
                        {
                            //Marking titan gold swap as complete
                            var tempDone = Settings.TitanMoneyDone.ToArray();
                            tempDone[titanIndex] = true;
                            Settings.TitanMoneyDone = tempDone;


                            _titanDetails[titanIndex] = currentSnapshot;
                        }
                        else
                        {
                            _titanDetails[titanIndex] = currentSnapshot;
                        }
                    }
                    else
                    {
                        _titanDetails[titanIndex] = currentSnapshot;
                    }
                }
            }

            _titanSnapshotSummary.TitansSpawningSoon = _titanDetails.Select(x => x.Value).Where(x => x.SpawnSoonTimestamp.HasValue && (x.ShouldUseGoldLoadout || x.ShouldUseTitanLoadout));
        }

        private static TitanSnapshot GetTitanSnapshot(int titanIndex)
        {
            DateTime? spawnSoonTimestamp = IsTitanSpawningSoon(titanIndex) ? (DateTime?)DateTime.Now : null;
            bool shouldUseTitanLoadout = Main.Settings.SwapTitanLoadouts && Main.Settings.TitanSwapTargets[titanIndex];
            bool shouldUseGoldLoadout = Main.Settings.ManageGoldLoadouts && Main.Settings.TitanGoldTargets[titanIndex] && !Main.Settings.TitanMoneyDone[titanIndex];

            TitanSnapshot titanSnapshot = new TitanSnapshot(titanIndex, spawnSoonTimestamp, shouldUseTitanLoadout, shouldUseGoldLoadout);

            return titanSnapshot;
        }

        private static bool IsTitanSpawningSoon(int bossId)
        {
            //fake IsTitanSpawningSoon for testing
            //return true;

            var controller = Main.Character.adventureController;
            var adventure = Main.Character.adventure;

            var spawnMethod = controller.GetType().GetMethod($"boss{bossId + 1}SpawnTime",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var spawnTimeObj = spawnMethod?.Invoke(controller, null);
            if (spawnTimeObj == null)
                return false;
            var spawnTime = (float)spawnTimeObj;

            var spawnField = adventure.GetType().GetField($"boss{bossId + 1}Spawn",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var spawnObj = spawnField?.GetValue(adventure);

            if (spawnObj == null)
                return false;

            var spawn = (PlayerTime)spawnObj;

            //The way this works is that spawn.totalseconds will count up until it reaches spawnTime and then stays there
            //This triggers the boss to be available, and after killed spawn.totalseconds will reset back down to 0
            return Math.Abs(spawnTime - spawn.totalseconds) < 20;
        }

        internal static int GetMaxReachableZone(bool includingTitans)
        {
            for (var i = Main.Character.adventureController.zoneDropdown.options.Count - 2; i >= 0; i--)
            {
                if (!ZoneIsTitan(i))
                    return i;
                if (includingTitans)
                    return i;
            }
            return 0;
        }

        internal static void OptimizeITOPOD()
        {
            if (!Main.Settings.OptimizeITOPODFloor) return;
            if (Main.Character.arbitrary.boughtLazyITOPOD && Main.Character.arbitrary.lazyITOPODOn) return;
            if (Main.Character.adventure.zone < 1000) return;
            var controller = Main.Character.adventureController;
            var level = controller.itopodLevel;
            var optimal = CalculateBestItopodLevel();
            if (level == optimal) return; // we are on optimal floor
            var highestOpen = Main.Character.adventure.highestItopodLevel;
            var climbing = (level < optimal && level >= highestOpen - 1);
            controller.itopodStartInput.text = optimal.ToString();
            if (climbing)
                optimal++;
            controller.itopodEndInput.text = optimal.ToString();
            controller.verifyItopodInputs();
            if (!climbing)
                controller.zoneSelector.changeZone(1000);
        }

        internal static int CalculateBestItopodLevel()
        {
            var c = Main.Character;
            var num1 = c.totalAdvAttack() / 765f * (Main.Settings.ITOPODCombatMode == 1 || c.training.attackTraining[1] == 0 ? c.idleAttackPower() : c.regAttackPower());
            if (c.totalAdvAttack() < 700.0)
                return 0;
            var num2 = Convert.ToInt32(Math.Floor(Math.Log(num1, 1.05)));
            if (num2 < 1)
                return 1;
            var maxLevel = c.adventureController.maxItopodLevel();
            if (num2 > maxLevel)
                num2 = maxLevel;
            return num2;
        }
    }

    public class TitanSnapshotSummary
    {
        internal IEnumerable<TitanSnapshot> TitansSpawningSoon { get; set; } = new List<TitanSnapshot>();
        internal bool AnySpawningSoon { get { return TitansSpawningSoon.Any(); } }
        internal bool RunTitanLoadout { get { return TitansSpawningSoon.Any(x => x.ShouldUseTitanLoadout); } }
        internal bool RunGoldLoadout { get { return TitansSpawningSoon.Any(x => x.ShouldUseGoldLoadout); } }
    }

    internal class TitanSnapshot
    {
        internal int TitanIndex { get; set; }
        internal DateTime? SpawnSoonTimestamp { get; set; }
        internal bool ShouldUseTitanLoadout { get; set; }
        internal bool ShouldUseGoldLoadout { get; set; }

        public TitanSnapshot(int titanIndex, DateTime? spawnSoonTimestamp, bool shouldUseTitanLoadout, bool shouldUseGoldLoadout)
        {
            TitanIndex = titanIndex;
            SpawnSoonTimestamp = spawnSoonTimestamp;
            ShouldUseTitanLoadout = shouldUseTitanLoadout;
            ShouldUseGoldLoadout = shouldUseGoldLoadout;
        }
    }
}

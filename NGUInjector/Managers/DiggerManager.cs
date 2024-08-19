﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace NGUInjector.Managers
{
    internal static class DiggerManager
    {
        private static int[] _savedDiggers;
        private static int[] _tempDiggers;
        private static int _cheapestDigger;

        internal static LockType CurrentLock { get; set; }
        private static readonly int[] TitanDiggers = { 0, 3, 8, 11 };
        private static readonly int[] YggDiggers = { 8, 11 };

        internal static bool CanSwap()
        {
            return CurrentLock == LockType.None;
        }

        internal static void TryTitanSwap()
        {
            if (!CanAcquireOrHasLock(LockType.Titan))
                return;

            if (CurrentLock == LockType.Titan)
            {
                if (ZoneHelpers.AnyTitansSpawningSoon())
                    return;

                RestoreDiggers();
                ReleaseLock();
                return;
            }

            if (ZoneHelpers.AnyTitansSpawningSoon())
            {
                CurrentLock = LockType.Titan;
                SaveDiggers();
                EquipDiggers(TitanDiggers);
            }
        }

        internal static bool TryYggSwap()
        {
            if (!CanAcquireOrHasLock(LockType.Yggdrasil))
                return false;

            if (CurrentLock == LockType.Yggdrasil)
                return true;

            CurrentLock = LockType.Yggdrasil;
            SaveDiggers();
            EquipDiggers(YggDiggers);
            return true;
        }

        internal static void ReleaseLock()
        {
            CurrentLock = LockType.None;
        }

        internal static void SaveDiggers()
        {
            var temp = new List<int>();
            for (var i = 0; i < Main.Character.diggers.diggers.Count; i++)
            {
                if (Main.Character.diggers.diggers[i].active)
                {
                    temp.Add(i);
                }
            }

            _savedDiggers = temp.ToArray();
        }

        internal static void SaveTempDiggers()
        {
            var temp = new List<int>();
            for (var i = 0; i < Main.Character.diggers.diggers.Count; i++)
            {
                if (Main.Character.diggers.diggers[i].active)
                {
                    temp.Add(i);
                }
            }

            _tempDiggers = temp.ToArray();
        }

        internal static void RestoreTempDiggers()
        {
            EquipDiggers(_tempDiggers);
        }

        internal static void EquipDiggers(int[] diggers)
        {
            Main.Log($"Equipping Diggers: {string.Join(",", diggers.Select(x => x.ToString()))}");
            TryEquipDiggers(diggers);
        }

        internal static bool TryEquipDiggers(int[] diggers)
        {
            Main.Character.allDiggers.clearAllActiveDiggers();
            var sorted = diggers.OrderByDescending(x => x).Where(x => x <= 11 && x >= 0).ToArray();

            bool allEquipped = true;

            for (var i = 0; i < sorted.Length; i++)
            {
                if (Main.Character.diggers.diggers[sorted[i]].maxLevel <= 0)
                {
                    continue;
                }

                Main.Character.allDiggers.setLevelMaxAffordable(sorted[i]);

                //Main.LogDebug($"Digger {sorted[i]} level: {Main.Character.diggers.diggers[sorted[i]].curLevel} active:{Main.Character.diggers.diggers[sorted[i]].active}");

                allEquipped &= Main.Character.diggers.diggers[sorted[i]].active;
            }

            UpdateCheapestDigger();
            return allEquipped;
        }

        private static bool CanAcquireOrHasLock(LockType requestor)
        {
            if (CurrentLock == requestor)
            {
                return true;
            }

            if (CurrentLock == LockType.None)
            {
                return true;
            }

            return false;
        }

        internal static void RecapDiggers()
        {
            var gross = Main.Character.grossGoldPerSecond();
            var sub = 0.0;
            for (var i = Main.Character.diggers.diggers.Count - 1; i >= 0; i--)
            {
                if (Main.Character.diggers.diggers[i].active)
                {
                    SetLevelMaxAffordable(i, gross - sub);
                    sub += Main.Character.allDiggers.drain(i);
                }
            }
            UpgradeCheapestDigger();
            Main.Character.allDiggers.refreshMenu();
        }

        private static void SetLevelMaxAffordable(int id, double cap)
        {
            if (id < 0 || id > Main.Character.diggers.diggers.Count)
                return;
            var curLevel = Main.Character.diggers.diggers[id].curLevel;
            Main.Character.diggers.diggers[id].curLevel = 0L;
            if (Main.Character.goldPerSecond() < Main.Character.allDiggers.drain(id, 1, true))
            {
                Main.Character.diggers.diggers[id].curLevel = curLevel;
            }
            else
            {
                var num1 = cap;
                var num2 = Main.Character.allDiggers.baseGPSDrain[id];
                var a = Main.Character.allDiggers.gpsGrowthRate[id];
                var num3 = Math.Min((long)(Math.Log(num1 / num2, Math.E) / Math.Log(a, Math.E)), Main.Character.diggers.diggers[id].maxLevel);
                if (num3 < 0L)
                    num3 = 0L;
                if (num3 > Main.Character.diggers.diggers[id].maxLevel)
                    num3 = Main.Character.diggers.diggers[id].maxLevel;
                Main.Character.diggers.diggers[id].curLevel = num3;
                if (Main.Character.diggers.diggers[id].curLevel == 0L && Main.Character.diggers.diggers[id].active)
                {
                    var num4 = 0;
                    while (num4 < Main.Character.diggers.activeDiggers.Count)
                        ++num4;
                    Main.Character.diggers.diggers[id].active = false;
                    Main.Character.diggers.activeDiggers.RemoveAt(Main.Character.diggers.activeDiggers.IndexOf(id));
                }
                if (Main.Character.grossGoldPerSecond() < Main.Character.allDiggers.totalGPSDrain())
                {
                    Main.Character.diggers.diggers[id].curLevel = curLevel;
                }
                else if (!Main.Character.diggers.diggers[id].active && Main.Character.diggers.diggers[id].curLevel > 0L && Main.Character.diggers.activeDiggers.Count < Main.Character.allDiggers.maxDiggerSlots())
                    Main.Character.allDiggers.activateDigger(id);
            }
        }

        internal static void RestoreDiggers()
        {
            Main.Character.allDiggers.clearAllActiveDiggers();
            EquipDiggers(_savedDiggers);
        }

        internal static void UpdateCheapestDigger()
        {
            if (!Main.Settings.UpgradeDiggers) return;
            _cheapestDigger = -1;
            for (var i = 0; i < Main.Character.diggers.diggers.Count; i++)
            {
                if (_cheapestDigger == -1)
                {
                    _cheapestDigger = i;
                }
                if (Main.Character.allDiggers.upgradeCost(i) < Main.Character.allDiggers.upgradeCost(_cheapestDigger))
                {
                    _cheapestDigger = i;
                }
            }
        }

        internal static void UpgradeCheapestDigger()
        {
            if (!Main.Settings.UpgradeDiggers) return;
            if (_cheapestDigger == -1) return;
            if (Main.Character.allDiggers.upgradeCost(_cheapestDigger) + Main.Settings.MoneyPitThreshold < Main.Character.realGold)
            {
                Main.Log("Upgrading Digger " + _cheapestDigger);
                Main.Character.allDiggers.upgradeMaxLevel(_cheapestDigger);
            }
            else
            {
                return;
            }

            UpdateCheapestDigger();
            UpgradeCheapestDigger();
        }
    }
}

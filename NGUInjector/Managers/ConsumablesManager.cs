using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace NGUInjector.Managers
{
    static class ConsumablesManager
    {
        private static Character _character = Main.Character;
        private static ArbitraryController _arbitraryController = Main.ArbitraryController;

        private static string[] _lastConsumables = new string[0];
        private static double _lastTime = 0;

        internal enum ConsumableType { EPOTA, EPOTB, EPOTC, MPOTA, MPOTB, MPOTC, R3POTA, R3POTB, R3POTC, EBARBAR, MBARBAR, MUFFIN, LC, SLC, MAYO }

        internal class Consumable
        {
            public string Name { get; set; }
            public long Cost { get; set; }
            public double? Time { get; set; }

            public Func<int> GetCount;
            public Func<bool?> GetIsActive;
            public Func<double?> GetTimeLeft;

            private Action _buy;
            private string _useMethod;

            public Consumable(string name, long cost, double? time, Func<int> getCount, Func<bool?> getIsActive, Func<double?> getTimeLeft, Action buy, string useMethodName)
            {
                Name = name;
                Cost = cost;
                Time = time;

                GetCount = getCount;
                GetIsActive = getIsActive;
                GetTimeLeft = getTimeLeft;

                _buy = buy;
                _useMethod = useMethodName;
            }

            public bool HasEnoughCount(int quantity)
            {
                return GetCount() >= quantity;
            }

            public bool HasEnoughAP(int quantity, out long amountNeeded)
            {
                amountNeeded = Cost * quantity;
                return _character.arbitrary.curArbitraryPoints >= amountNeeded;
            }

            public bool Buy(int quantity, out long amountNeeded)
            {
                if (!HasEnoughAP(quantity, out amountNeeded))
                {
                    Main.Log($"ConsumablesManager - Not enough AP to buy {quantity} {Name} ({_character.arbitrary.curArbitraryPoints:N0} out of {amountNeeded:N0} AP)!");
                    return false;
                }

                Main.Log($"ConsumablesManager - Buying {quantity} {Name} for {Cost * quantity} AP");

                for (int i = 0; i < quantity; i++)
                {
                    if (HasEnoughAP(1, out _))
                    {
                        _buy();
                    }
                    else
                    {
                        Main.Log($"ConsumablesManager - Unexpected error, ran out of AP before buying {quantity} {Name}");
                        return false;
                    }
                }

                return true;
            }

            public bool Use(int quantity)
            {
                if (!HasEnoughCount(quantity))
                {
                    Main.Log($"ConsumablesManager - Not enough {Name}! Owned: {GetCount()}, Needed:{quantity}");
                    return false;
                }

                if (GetIsActive().HasValue)
                {
                    if (!Time.HasValue)
                    {
                        Main.Log($"ConsumablesManager - Eating {quantity} {Name} (active until next rebirth)");
                    }
                    else
                    {
                        Main.Log($"ConsumablesManager - Eating {quantity} {Name} (active for {GetTimeDisplay(TimeSpan.FromSeconds(quantity * Time.Value))} and at least until next rebirth)");
                    }
                }
                else
                {
                    Main.Log($"ConsumablesManager - Eating {quantity} {Name} (active for {GetTimeDisplay(TimeSpan.FromSeconds(quantity * Time.Value))})");
                }

                for (int i = 0; i < quantity; i++)
                {
                    if (HasEnoughCount(1))
                    {
                        var useMethod = _arbitraryController.GetType().GetMethod(_useMethod,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        useMethod?.Invoke(_arbitraryController, null);
                    }
                    else
                    {
                        Main.Log($"ConsumablesManager - Unexpected error, ran out of {Name} before consuming {quantity}");
                        return false;
                    }
                }

                return true;
            }
        }

        public static string GetTimeDisplay(TimeSpan time)
        {
            string display = "";
            if (time.TotalDays >= 1)
            {
                display = $"{Math.Floor(time.TotalDays)} day{(time.TotalDays > 1 ? "s" : "")} ";
            }
            display += time.ToString(@"hh\:mm\:ss");
            return display;
        }

        internal static readonly Dictionary<ConsumableType, Consumable> Consumables = new Dictionary<ConsumableType, Consumable>
        {
            {ConsumableType.EPOTA,
                new Consumable("EPOT-A", _arbitraryController.energyPotion1Cost(), 60*60,
                    () => _character.arbitrary.energyPotion1Count, () => null, () => _character.arbitrary.energyPotion1Time.totalseconds,
                    _arbitraryController.buyEnergyPotion1AP, "useEnergyPotion1") },

            {ConsumableType.EPOTB,
                new Consumable("EPOT-B", _arbitraryController.energyPotion2Cost(), null,
                    () => _character.arbitrary.energyPotion2Count, () => _character.arbitrary.energyPotion2InUse, () => null,
                    _arbitraryController.buyEnergyPotion2AP, "useEnergyPotion2") },

            {ConsumableType.EPOTC,
                new Consumable("EPOT-C", _arbitraryController.energyPotion3Cost(), 60*60*24,
                    () => _character.arbitrary.energyPotion3Count, () => null, () => _character.arbitrary.energyPotion1Time.totalseconds,
                    _arbitraryController.buyEnergyPotion3, "useEnergyPotion3") },


            {ConsumableType.MPOTA,
                new Consumable("MPOT-A", _arbitraryController.magicPotion1Cost(), 60*60,
                    () => _character.arbitrary.magicPotion1Count, () => null, () => _character.arbitrary.magicPotion1Time.totalseconds,
                    _arbitraryController.buyMagicPotion1AP, "useMagicPotion1") },

            {ConsumableType.MPOTB,
                new Consumable("MPOT-B", _arbitraryController.magicPotion2Cost(), null,
                    () => _character.arbitrary.magicPotion2Count, () => _character.arbitrary.magicPotion2InUse, () => null,
                    _arbitraryController.buyMagicPotion2AP, "useMagicPotion2") },

            {ConsumableType.MPOTC,
                new Consumable("MPOT-C", _arbitraryController.magicPotion3Cost(), 60*60*24,
                    () => _character.arbitrary.magicPotion3Count, () => null, () => _character.arbitrary.magicPotion1Time.totalseconds,
                    _arbitraryController.buyMagicPotion3, "useMagicPotion3") },


            {ConsumableType.R3POTA,
                new Consumable("R3POT-A", _arbitraryController.res3Potion1Cost(), 60*60,
                    () => _character.arbitrary.res3Potion1Count, () => null, () => _character.arbitrary.res3Potion1Time.totalseconds,
                    _arbitraryController.buyRes3Potion1, "useRes3Potion1") },

            {ConsumableType.R3POTB,
                new Consumable("R3POT-B", _arbitraryController.res3Potion2Cost(), null,
                    () => _character.arbitrary.res3Potion2Count, () => _character.arbitrary.res3Potion2InUse, () => null,
                    _arbitraryController.buyRes3Potion2, "useRes3Potion2") },

            {ConsumableType.R3POTC,
                new Consumable("R3POT-C", _arbitraryController.res3Potion3Cost(), 60*60*24,
                    () => _character.arbitrary.res3Potion3Count, () => null, () => _character.arbitrary.res3Potion1Time.totalseconds,
                    _arbitraryController.buyRes3Potion3, "useRes3Potion3") },


            {ConsumableType.EBARBAR,
                new Consumable("EBARBAR", _arbitraryController.energyBarBar1Cost(), 60*60,
                    () => _character.arbitrary.energyBarBar1Count, () => null, () => _character.arbitrary.energyBarBar1Time.totalseconds,
                    _arbitraryController.buyEnergyBarBar1AP, "useEnergyBarBar1") },

            {ConsumableType.MBARBAR,
                new Consumable("MBARBAR", _arbitraryController.magicBarBar1Cost(), 60*60,
                    () => _character.arbitrary.magicBarBar1Count, () => null, () => _character.arbitrary.magicBarBar1Time.totalseconds,
                    _arbitraryController.buyMagicBarBar1AP, "useMagicBarBar1") },


            {ConsumableType.MUFFIN,
                new Consumable("MUFFIN", _arbitraryController.macguffinBooster1Cost(), 60*60*24,
                    () => _character.arbitrary.macGuffinBooster1Count, () => _character.arbitrary.macGuffinBooster1InUse, () => _character.arbitrary.macGuffinBooster1Time.totalseconds,
                    _arbitraryController.buyMacguffinBooster1AP, "useMacguffinBooster1") },


            {ConsumableType.LC,
                new Consumable("LC", _arbitraryController.lootCharm1Cost(), 60*30,
                    () => _character.arbitrary.lootCharm1Count, () => null, () => _character.arbitrary.lootcharm1Time.totalseconds,
                    _arbitraryController.buyLootCharm1AP, "useLootCharm1") },

            {ConsumableType.SLC,
                new Consumable("SLC", _arbitraryController.lootCharm2Cost(), 60*60*12,
                    () => _character.arbitrary.lootCharm2Count, () => null, () => _character.arbitrary.lootcharm1Time.totalseconds,
                    _arbitraryController.buyLootCharm2AP, "useLootCharm2") },


            {ConsumableType.MAYO,
                new Consumable("MAYO", _arbitraryController.mayoSpeedConsumableCost(), 60*60*24,
                    () => _character.arbitrary.mayoSpeedPotCount, () => null, () => _character.arbitrary.mayoSpeedPotTime.totalseconds,
                    _arbitraryController.buyMayoSpeedConsumableAP, "useMayoSpeedPot") }
        };

        internal static void EatConsumables(string[] consumables, double time, int[] quantity)
        {
            if (Enumerable.SequenceEqual(consumables, _lastConsumables) && (Math.Abs(time - _lastTime) < 1))
            {
                // We already did this set of consumables, wait for next one
                return;
            }

            for (int i = 0; i < consumables.Length; i++)
            {
                string consumableName = consumables[i];

                var matchingConsumables = Consumables.Where(c => c.Value.Name == consumableName);
                if (matchingConsumables.Count() != 1)
                {
                    Main.Log($"ConsumablesManager - Invalid consumable name: {consumableName}");
                    continue;
                }

                Consumable consumable = matchingConsumables.Single().Value;

                if (ShouldUse(consumable, quantity[i], time, out int? temp) && temp > 0)
                {
                    int quantityToUse = temp.Value;

                    if (!consumable.HasEnoughCount(quantityToUse))
                    {
                        int quantityToBuy = quantityToUse - consumable.GetCount();
                        bool hasEnoughAP = consumable.HasEnoughAP(quantityToBuy, out long amountNeeded);

                        Main.Log($"ConsumablesManager - Not enough {consumable.Name}, need to buy {quantityToBuy} more");

                        if (Main.Settings.AutoBuyConsumables && hasEnoughAP)
                        {
                            consumable.Buy(quantityToBuy, out _);
                        }
                        else
                        {
                            string message = $"ConsumablesManager - ";
                            message += !hasEnoughAP 
                                ? $"Not enough AP to purchase {quantityToBuy} {consumable.Name} ({_character.arbitrary.curArbitraryPoints:N0} out of {amountNeeded:N0} AP)!" 
                                : $"Not enough {consumable.Name} ({consumable.GetCount()} out of {quantityToUse})! Buy more manually, or turn on AutoBuyConsumables in settings.json";

                            Main.Log(message);
                            continue;
                        }
                    }

                    consumable.Use(quantityToUse);
                }
            }

            Array.Resize(ref _lastConsumables, consumables.Length);
            _lastConsumables = consumables;
            _lastTime = time;
        }

        private static bool ShouldUse(Consumable consumable, int quantity, double breakpointTime, out int? quantityToUse)
        {
            quantityToUse = null;

            //This will be null for all timed consumables except muffin
            //Muffin is a special case where it will show as "Active" until the first rebirth after activating
            //Afterwards, it will show the remaining time left (if any) of 24 hours since use
            bool? isActive = consumable.GetIsActive();
            bool isTimed = consumable.Time.HasValue;
            bool isRebirth = consumable.GetIsActive().HasValue;

            //Consumables which aren't timed but last as long as the rebirth
            if (isRebirth)
            {
                if (isActive.Value)
                {
                    Main.Log($"ConsumablesManager - {consumable.Name} is already active");
                    return false;
                }

                //If MacGuffin Muffin isnt active, fall through to the time check
                //Otherwise only use one
                if (!isTimed)
                {
                    quantityToUse = 1;
                    if (quantity > 1)
                    {
                        Main.Log($"ConsumablesManager - Rebirth based consumable detected! Only consuming {quantityToUse} {consumable.Name} instead of {quantity}");
                    }
                    return true;
                }
            }

            //All other timed consumables
            double totalConsumableTime = (consumable.Time ?? 0) * quantity;
            double timeAtConsumableEnd = breakpointTime + totalConsumableTime;

            if ((_character.rebirthTime.totalseconds + 60) >= timeAtConsumableEnd)
            {
                Main.Log($"ConsumablesManager - Reload detected! {consumable.Name} configured to expire at {GetTimeDisplay(TimeSpan.FromSeconds(timeAtConsumableEnd))} which is before or near the current time of {GetTimeDisplay(TimeSpan.FromSeconds(_character.rebirthTime.totalseconds))}");
                return false;
            }

            if (_character.rebirthTime.totalseconds < breakpointTime)
            {
                Main.Log($"ConsumablesManager - Unexpected scenario with {consumable.Name}! Current rebirth of {GetTimeDisplay(TimeSpan.FromSeconds(_character.rebirthTime.totalseconds))} has not yet reached the breakpoint of {GetTimeDisplay(TimeSpan.FromSeconds(breakpointTime))}");
                return false;
            }

            double timeLeft = (consumable.GetTimeLeft() ?? 0);

            if (!Main.Settings.ConsumeIfAlreadyRunning && timeLeft > 0)
            {
                Main.Log($"ConsumablesManager - {consumable.Name} is already running, enable \"Use Consumables if already running\" to enable consumption of currently running consumables");
                return false;
            }

            double timeSinceBreakpoint = _character.rebirthTime.totalseconds - breakpointTime;
            double expectedTimeLeft = totalConsumableTime - timeSinceBreakpoint;
            if ((timeLeft + 60) >= expectedTimeLeft)
            {
                Main.Log($"ConsumablesManager - Active buffs detected! {consumable.Name} will expire at {GetTimeDisplay(TimeSpan.FromSeconds(_character.rebirthTime.totalseconds + timeLeft))} which is beyond or near the expected end time of {GetTimeDisplay(TimeSpan.FromSeconds(_character.rebirthTime.totalseconds + expectedTimeLeft))}");
                return false;
            }

            double amountOfTimeToActivate = expectedTimeLeft - timeLeft;
            quantityToUse = (int)Math.Round(amountOfTimeToActivate / consumable.Time.Value, 0, MidpointRounding.AwayFromZero);
            if (quantityToUse != quantity)
            {
                if (quantityToUse == 0)
                {
                    Main.Log($"ConsumablesManager - Reload or previously active buffs detected! Using {consumable.Name} would go beyond the breakpoint's configured limit");
                }
                else if (quantityToUse > 0)
                {
                    Main.Log($"ConsumablesManager - Reload or previously active buffs detected! Only consuming {quantityToUse} {consumable.Name} instead of {quantity}");
                }
            }

            if (isRebirth && quantityToUse > 1)
            {
                quantityToUse = 1;
                Main.Log($"ConsumablesManager - Rebirth based consumable detected! Only consuming {quantityToUse} {consumable.Name} instead of {quantity}");
            }

            return (quantityToUse ?? 0) > 0;
        }

        //private static bool HasEnoughConsumable(string consumable, int quantity)
        //{
        //    int owned = GetOwnedConsumableCount(consumable);
        //    Main.Log($"ConsumablesManager - Owned {consumable}s: {owned} , Needed: {quantity}");
        //    return owned >= quantity;
        //}

        //private static bool HasEnoughAP(string consumable, int quantity)
        //{
        //    bool enough = false;
        //    ConsumableValues.TryGetValue(consumable, out int price);

        //    if (price > 0)
        //    {
        //        enough = _character.arbitrary.curArbitraryPoints > (price * quantity);
        //        if (!enough)
        //        {
        //            Main.Log($"ConsumablesManager - Not enough AP[{_character.arbitrary.curArbitraryPoints}] to buy consumable, need {price * quantity} AP for {quantity} {consumable}");
        //        }
        //    }

        //    return enough;
        //}

        //private static int GetOwnedConsumableCount(string consumable)
        //{
        //    switch (consumable)
        //    {
        //        case "EPOT-A":
        //            return _character.arbitrary.energyPotion1Count;
        //        case "EPOT-B":
        //            return _character.arbitrary.energyPotion2Count;
        //        case "EPOT-C":
        //            return _character.arbitrary.energyPotion3Count;
        //        case "MPOT-A":
        //            return _character.arbitrary.magicPotion1Count;
        //        case "MPOT-B":
        //            return _character.arbitrary.magicPotion2Count;
        //        case "MPOT-C":
        //            return _character.arbitrary.magicPotion3Count;
        //        case "R3POT-A":
        //            return _character.arbitrary.res3Potion1Count;
        //        case "R3POT-B":
        //            return _character.arbitrary.res3Potion2Count;
        //        case "R3POT-C":
        //            return _character.arbitrary.res3Potion3Count;
        //        case "EBARBAR":
        //            return _character.arbitrary.energyBarBar1Count;
        //        case "MBARBAR":
        //            return _character.arbitrary.magicBarBar1Count;
        //        case "MUFFIN":
        //            return _character.arbitrary.macGuffinBooster1Count;
        //        case "LC":
        //            return _character.arbitrary.lootCharm1Count;
        //        case "SLC":
        //            return _character.arbitrary.lootCharm2Count;
        //        case "MAYO":
        //            return _character.arbitrary.mayoSpeedPotCount;
        //        default:
        //            break;
        //    }
        //    return -1;
        //}

        //private static void BuyConsumable(string consumable, int count)
        //{
        //    for (int i = 0; i < count; i++)
        //    {
        //        switch (consumable)
        //        {
        //            case "EPOT-A":
        //                _character.arbitrary.energyPotion1Count++;
        //                break;
        //            case "EPOT-B":
        //                _character.arbitrary.energyPotion2Count++;
        //                break;
        //            case "EPOT-C":
        //                _character.arbitrary.energyPotion3Count++;
        //                break;
        //            case "MPOT-A":
        //                _character.arbitrary.magicPotion1Count++;
        //                break;
        //            case "MPOT-B":
        //                _character.arbitrary.magicPotion2Count++;
        //                break;
        //            case "MPOT-C":
        //                _character.arbitrary.magicPotion3Count++;
        //                break;
        //            case "R3POT-A":
        //                _character.arbitrary.res3Potion1Count++;
        //                break;
        //            case "R3POT-B":
        //                _character.arbitrary.res3Potion2Count++;
        //                break;
        //            case "R3POT-C":
        //                _character.arbitrary.res3Potion3Count++;
        //                break;
        //            case "EBARBAR":
        //                _character.arbitrary.energyBarBar1Count++;
        //                break;
        //            case "MBARBAR":
        //                _character.arbitrary.magicBarBar1Count++;
        //                break;
        //            case "MUFFIN":
        //                _character.arbitrary.macGuffinBooster1Count++;
        //                break;
        //            case "LC":
        //                _character.arbitrary.lootCharm1Count++;
        //                break;
        //            case "SLC":
        //                _character.arbitrary.lootCharm2Count++;
        //                break;
        //            case "MAYO":
        //                _character.arbitrary.mayoSpeedPotCount++;
        //                break;
        //            default:
        //                break;
        //        }

        //        ConsumablePrices.TryGetValue(consumable, out int price);
        //        _character.arbitrary.curArbitraryPoints -= price;
        //    }
        //}

        //private static void UseConsumables(string consumable, int count)
        //{
        //    Main.Log($"ConsumablesManager - Eating {count} {consumable}");

        //    for (int i = 0; i < count; i++)
        //    {
        //        switch (consumable)
        //        {
        //            case "EPOT-A":
        //                _character.arbitrary.energyPotion1Time.advanceTime(60 * 60);
        //                _character.arbitrary.energyPotion1Count--;
        //                break;
        //            case "EPOT-B":
        //                if (!_character.arbitrary.energyPotion2InUse)
        //                {
        //                    _character.arbitrary.energyPotion2InUse = true;
        //                    _character.arbitrary.energyPotion2Count--;
        //                }
        //                else
        //                {
        //                    Main.Log($"ConsumablesManager - Energy Potion Beta already active, not eating");
        //                }
        //                break;
        //            case "EPOT-C":
        //                _character.arbitrary.energyPotion1Time.advanceTime(60 * 60 * 24);
        //                _character.arbitrary.energyPotion3Count--;
        //                break;
        //            case "MPOT-A":
        //                _character.arbitrary.magicPotion1Time.advanceTime(60 * 60);
        //                _character.arbitrary.magicPotion1Count--;
        //                break;
        //            case "MPOT-B":
        //                if (!_character.arbitrary.magicPotion2InUse)
        //                {
        //                    _character.arbitrary.magicPotion2InUse = true;
        //                    _character.arbitrary.magicPotion2Count--;
        //                }
        //                else
        //                {
        //                    Main.Log($"ConsumablesManager - Magic Potion Beta already active, not eating");
        //                }
        //                break;
        //            case "MPOT-C":
        //                _character.arbitrary.magicPotion1Time.advanceTime(60 * 60 * 24);
        //                _character.arbitrary.magicPotion3Count--;
        //                break;
        //            case "R3POT-A":
        //                _character.arbitrary.res3Potion1Time.advanceTime(60 * 60);
        //                _character.arbitrary.res3Potion1Count--;
        //                break;
        //            case "R3POT-B":
        //                if (!_character.arbitrary.res3Potion2InUse)
        //                {
        //                    _character.arbitrary.res3Potion2InUse = true;
        //                    _character.arbitrary.res3Potion2Count--;
        //                }
        //                else
        //                {
        //                    Main.Log($"ConsumablesManager - R3 Potion Beta already active, not eating");
        //                }
        //                break;
        //            case "R3POT-C":
        //                _character.arbitrary.res3Potion1Time.advanceTime(60 * 60 * 24);
        //                _character.arbitrary.res3Potion3Count--;
        //                break;
        //            case "EBARBAR":
        //                _character.arbitrary.energyBarBar1Time.advanceTime(60 * 60);
        //                _character.arbitrary.energyBarBar1Count--;
        //                break;
        //            case "MBARBAR":
        //                _character.arbitrary.magicBarBar1Time.advanceTime(60 * 60);
        //                _character.arbitrary.magicBarBar1Count--;
        //                break;
        //            case "MUFFIN":
        //                // boolean _character.arbitrary.macGuffinBooster1InUse
        //                // The boolean doesn't seem to work, so going by remaining time instead
        //                if (_character.arbitrary.macGuffinBooster1Time.totalseconds < 1)
        //                {
        //                    _character.arbitrary.macGuffinBooster1Time.advanceTime(60 * 60 * 24);
        //                    _character.arbitrary.macGuffinBooster1Count--;
        //                }
        //                else
        //                {
        //                    Main.Log($"ConsumablesManager - Macguffin Muffin already active, not eating");
        //                }
        //                break;
        //            case "LC":
        //                _character.arbitrary.lootcharm1Time.advanceTime(30 * 60);
        //                _character.arbitrary.lootCharm1Count--;
        //                break;
        //            case "SLC":
        //                _character.arbitrary.lootcharm1Time.advanceTime(60 * 60 * 12);
        //                _character.arbitrary.lootCharm2Count--;
        //                break;
        //            case "MAYO":
        //                if (_character.arbitrary.mayoSpeedPotTime.totalseconds < 1)
        //                {
        //                    _character.arbitrary.mayoSpeedPotTime.advanceTime(60 * 60 * 24);
        //                    _character.arbitrary.mayoSpeedPotCount--;
        //                }
        //                else
        //                {
        //                    Main.Log($"ConsumablesManager - Mayo already active, not eating");
        //                }
        //                break;
        //            default:
        //                Main.Log($"ConsumablesManager - Unknown consumable: {consumable}");
        //                break;
        //        }
        //    }
        //}

        //private static bool IsValidConsumable(string consumable)
        //{
        //    return ConsumablePrices.ContainsKey(consumable);
        //}

        internal static void resetLastConsumables()
        {
            _lastConsumables = new string[0];
            _lastTime = 0;
        }
    }
}
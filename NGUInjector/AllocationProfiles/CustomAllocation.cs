using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using NGUInjector.AllocationProfiles.BreakpointTypes;
using NGUInjector.AllocationProfiles.RebirthStuff;
using NGUInjector.Managers;
using SimpleJSON;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace NGUInjector.AllocationProfiles
{
    [Serializable]
    internal class CustomAllocation : AllocationProfile
    {
        private BreakpointWrapper _wrapper;
        private AllocationBreakPoint _currentMagicBreakpoint;
        private AllocationBreakPoint _currentEnergyBreakpoint;
        private AllocationBreakPoint _currentR3Breakpoint;
        private GearBreakpoint _currentGearBreakpoint;
        private DiggerBreakpoint _currentDiggerBreakpoint;
        private WandoosBreakpoint _currentWandoosBreakpoint;
        private NGUDiffBreakpoint _currentNguBreakpoint;
        private ConsumablesBreakpoint _currentConsumablesBreakpoint;
        private bool _hasGearSwapped;
        private bool _hasDiggerSwapped;
        private bool _hasWandoosSwapped;
        private bool _hasNGUSwapped;
        private bool _didConsumeConsumables;
        private readonly string _allocationPath;
        private readonly string _profileName;

        internal bool IsAllocationRunning;

        public CustomAllocation(string profilesDir, string profile)
        {
            _allocationPath = Path.Combine(profilesDir, profile + ".json");
            _profileName = profile;
        }

        internal void ReloadAllocation()
        {
            if (File.Exists(_allocationPath))
            {
                try
                {
                    var text = File.ReadAllText(_allocationPath);
                    var parsed = JSON.Parse(text);
                    var breakpoints = parsed["Breakpoints"];
                    _wrapper = new BreakpointWrapper { Breakpoints = new Breakpoints() };
                    var rb = breakpoints["Rebirth"];
                    var rbtime = breakpoints["RebirthTime"];
                    if (rb == null)
                    {
                        _wrapper.Breakpoints.Rebirth = rbtime == null ? new NoRebirth() : BaseRebirth.CreateRebirth(ParseTime(rbtime), "time", new string[0]);
                    }
                    else
                    {
                        if (rb["Type"] == null || rb["Target"] == null)
                            _wrapper.Breakpoints.Rebirth = new NoRebirth();

                        var type = rb["Type"].Value.ToUpper();
                        var target = type == "TIME" ? ParseTime(rb["Target"]) : rb["Target"].AsDouble;
                        _wrapper.Breakpoints.Rebirth = BaseRebirth.CreateRebirth(target, type, rb["Challenges"].AsArray.Children.Select(x => x.Value.ToUpper()).ToArray());
                    }

                    _wrapper.Breakpoints.ConsumablesBreakpoints = breakpoints["Consumables"].Children.Select(bp => new ConsumablesBreakpoint
                    {
                        Time = ParseTime(bp["Time"]),
                        Consumables = ParseConsumableItemNames(bp["Items"].AsArray.Children.Select(x => x.Value.ToUpper()).ToArray()),
                        Quantity = GetConsumablesQuantities(bp["Items"].AsArray.Children.Select(x => x.Value.ToUpper()).ToArray())
                    }).OrderByDescending(x => x.Time).ToArray();


                    _wrapper.Breakpoints.Magic = breakpoints["Magic"].Children.Select(bp => new AllocationBreakPoint
                    {
                        Time = ParseTime(bp["Time"]),
                        Priorities = BaseBreakpoint.ParseBreakpointArray(bp["Priorities"].AsArray.Children.Select(x => x.Value.ToUpper())
                            .ToArray(), ResourceType.Magic, rbtime).Where(x => x != null).ToArray()
                    }).OrderByDescending(x => x.Time).ToArray();

                    _wrapper.Breakpoints.Energy = breakpoints["Energy"].Children.Select(bp => new AllocationBreakPoint
                    {
                        Time = ParseTime(bp["Time"]),
                        Priorities = BaseBreakpoint.ParseBreakpointArray(bp["Priorities"].AsArray.Children.Select(x => x.Value.ToUpper())
                            .ToArray(), ResourceType.Energy, rbtime).Where(x => x != null).ToArray()
                    }).OrderByDescending(x => x.Time).ToArray();

                    _wrapper.Breakpoints.R3 = breakpoints["R3"].Children.Select(bp => new AllocationBreakPoint
                    {
                        Time = ParseTime(bp["Time"]),
                        Priorities = BaseBreakpoint.ParseBreakpointArray(bp["Priorities"].AsArray.Children.Select(x => x.Value.ToUpper())
                            .ToArray(), ResourceType.R3, rbtime).Where(x => x != null).ToArray()
                    }).OrderByDescending(x => x.Time).ToArray();

                    _wrapper.Breakpoints.Gear = breakpoints["Gear"].Children
                        .Select(bp => new GearBreakpoint
                        {
                            Time = ParseTime(bp["Time"]),
                            Gear = bp["ID"].AsArray.Children.Select(x => x.AsInt).ToArray()
                        })
                        .OrderByDescending(x => x.Time).ToArray();

                    _wrapper.Breakpoints.Diggers = breakpoints["Diggers"].Children
                        .Select(bp => new DiggerBreakpoint
                        {
                            Time = ParseTime(bp["Time"]),
                            Diggers = bp["List"].AsArray.Children.Select(x => x.AsInt).ToArray()
                        }).OrderByDescending(x => x.Time).ToArray();

                    _wrapper.Breakpoints.Wandoos = breakpoints["Wandoos"].Children
                        .Select(bp => new WandoosBreakpoint { Time = ParseTime(bp["Time"]), OS = bp["OS"].AsInt })
                        .OrderByDescending(x => x.Time).ToArray();

                    _wrapper.Breakpoints.NGUBreakpoints = breakpoints["NGUDiff"].Children
                        .Select(bp => new NGUDiffBreakpoint { Time = ParseTime(bp["Time"]), Diff = bp["Diff"].AsInt })
                        .Where(x => x.Diff <= 2).OrderByDescending(x => x.Time).ToArray();

                    Main.Log(BuildAllocationString());

                    _currentDiggerBreakpoint = null;
                    _currentEnergyBreakpoint = null;
                    _currentGearBreakpoint = null;
                    _currentWandoosBreakpoint = null;
                    _currentMagicBreakpoint = null;
                    _currentR3Breakpoint = null;
                    _currentNguBreakpoint = null;
                    _currentConsumablesBreakpoint = null;

                    this.DoAllocations();
                }
                catch (Exception e)
                {
                    Main.Log("Failed to load allocation file. Resave to reload");
                    Main.Log(e.Message);
                    Main.Log(e.StackTrace);
                    _wrapper = new BreakpointWrapper
                    {
                        Breakpoints =
                        {
                            Rebirth = new NoRebirth(), R3 = new AllocationBreakPoint[0],
                            Diggers = new DiggerBreakpoint[0], Energy = new AllocationBreakPoint[0],
                            Gear = new GearBreakpoint[0], Magic = new AllocationBreakPoint[0],
                            NGUBreakpoints = new NGUDiffBreakpoint[0], Wandoos = new WandoosBreakpoint[0],
                            ConsumablesBreakpoints = new ConsumablesBreakpoint[0]
                        }
                    };

                    _currentDiggerBreakpoint = null;
                    _currentEnergyBreakpoint = null;
                    _currentGearBreakpoint = null;
                    _currentWandoosBreakpoint = null;
                    _currentMagicBreakpoint = null;
                    _currentR3Breakpoint = null;
                    _currentNguBreakpoint = null;
                    _currentConsumablesBreakpoint = null;
                }
            }
            else
            {
                var emptyAllocation = @"{
    ""Breakpoints"": {
      ""Magic"": [
        {
          ""Time"": 0,
          ""Priorities"": []
        }
      ],
      ""Energy"": [
        {
          ""Time"": 0,
          ""Priorities"": []
        }
      ],
    ""R3"": [
        {
          ""Time"": 0,
          ""Priorities"": []
        }
      ],
      ""Gear"": [
        {
          ""Time"": 0,
          ""ID"": []
        }
      ],
      ""Wandoos"": [
        {
          ""Time"": 0,
          ""OS"": 0
        }
      ],
      ""Diggers"": [
        {
          ""Time"": 0,
          ""List"": []
        }
      ],
      ""NGUDiff"": [
        {
          ""Time"": 0,
          ""Diff"": 0
        }
      ],
      ""RebirthTime"": -1
    }
  }
        ";

                Main.Log("Created empty allocation profile. Please update allocation.json");
                using (var writer = new StreamWriter(File.Open(_allocationPath, FileMode.CreateNew)))
                {
                    writer.WriteLine(emptyAllocation);
                    writer.Flush();
                }
            }
        }

        private string[] ParseConsumableItemNames(string[] itemNames)
        {
            string[] items = new string[itemNames.Length];
            int i = 0;

            foreach (string item in itemNames)
            {
                String value = item;
                if (value.Contains(":"))
                {
                    value = item.Substring(0, item.IndexOf(":"));
                }

                items[i++] = value;
            }

            return items;
        }

        private double ParseTime(JSONNode timeNode)
        {
            var time = 0;

            if (timeNode.IsObject)
            {
                foreach (var N in timeNode)
                {
                    if (N.Value.IsNumber)
                    {
                        switch (N.Key.ToLower())
                        {
                            case "h":
                                time += 60 * 60 * N.Value.AsInt;
                                break;
                            case "m":
                                time += 60 * N.Value.AsInt;
                                break;
                            case "s":
                            default:
                                time += N.Value.AsInt;
                                break;
                        }
                    }
                }
            }

            if (timeNode.IsNumber)
            {
                time = timeNode.AsInt;
            }

            return time;
        }

        private string BuildAllocationString()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Loaded Custom Allocation from profile '{_profileName}'");
            builder.AppendLine($"{_wrapper.Breakpoints.Energy.Length} Energy Breakpoints");
            builder.AppendLine($"{_wrapper.Breakpoints.Magic.Length} Magic Breakpoints");
            builder.AppendLine($"{_wrapper.Breakpoints.R3.Length} R3 Breakpoints");
            builder.AppendLine($"{_wrapper.Breakpoints.Gear.Length} Gear Breakpoints");
            builder.AppendLine($"{_wrapper.Breakpoints.Diggers.Length} Digger Breakpoints");
            builder.AppendLine($"{_wrapper.Breakpoints.Wandoos.Length} Wandoos Breakpoints");
            builder.AppendLine($"{_wrapper.Breakpoints.NGUBreakpoints.Length} NGU Difficulty Breakpoints");
            builder.AppendLine($"{_wrapper.Breakpoints.ConsumablesBreakpoints.Length} Consumable Breakpoints");
            var rb = _wrapper.Breakpoints.Rebirth;
            if (rb is NoRebirth)
            {
                builder.AppendLine($"Rebirth Disabled.");
            }
            else if (rb is NumberRebirth nrb)
            {
                builder.AppendLine($"Rebirthing when number bonus is {nrb.MultTarget}x previous number");
            }
            else if (rb is TimeRebirth trb)
            {
                builder.AppendLine($"Rebirthing at {trb.RebirthTime} seconds");
            }
            else if (rb is BossNumRebirth brb)
            {
                builder.AppendLine($"Rebirthing when number allows you +{brb.NumBosses} bosses");
            }

            if (rb.ChallengeTargets.Length > 0)
            {
                builder.AppendLine(
                    $"Challenge targets: {string.Join(",", rb.ChallengeTargets.Select(x => x.ToString()).ToArray())}");
            }

            return builder.ToString();
        }

        internal void SwapNGUDiff()
        {
            var bp = GetCurrentNGUDiffBreakpoint();
            if (bp == null)
                return;

            if (bp.Time != _currentNguBreakpoint.Time)
            {
                _hasNGUSwapped = false;
            }

            if (_hasNGUSwapped)
                return;

            if (bp.Diff == 0)
            {
                _character.settings.nguLevelTrack = difficulty.normal;
                if (_character.settings.nguLevelTrack == difficulty.normal)
                {
                    _hasNGUSwapped = true;
                }
            }
            else if (bp.Diff == 1 && (_character.settings.rebirthDifficulty == difficulty.evil ||
                                      _character.settings.rebirthDifficulty == difficulty.sadistic))
            {
                _character.settings.nguLevelTrack = difficulty.evil;
                if (_character.settings.nguLevelTrack == difficulty.evil)
                {
                    _hasNGUSwapped = true;
                }
            }
            else if (bp.Diff == 2 && _character.settings.rebirthDifficulty == difficulty.sadistic)
            {
                _character.settings.nguLevelTrack = difficulty.sadistic;
                if (_character.settings.nguLevelTrack == difficulty.sadistic)
                {
                    _hasNGUSwapped = true;
                }
            }

            _character.NGUController.refreshMenu();
        }

        internal void SwapOS()
        {
            var bp = GetCurrentWandoosBreakpoint();
            if (bp == null)
                return;

            if (bp.Time != _currentWandoosBreakpoint.Time)
            {
                _hasWandoosSwapped = false;
            }

            if (_hasWandoosSwapped) return;

            _hasWandoosSwapped = true;
            if (bp.OS == 0 && _character.wandoos98.os == OSType.wandoos98) return;
            if (bp.OS == 1 && _character.wandoos98.os == OSType.wandoosMEH) return;
            if (bp.OS == 2 && _character.wandoos98.os == OSType.wandoosXL) return;

            var id = bp.OS;
            if (id == 0)
            {
                var controller = Main.Character.wandoos98Controller;
                var type = controller.GetType().GetField("nextOS",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                type?.SetValue(controller, id);

                typeof(Wandoos98Controller)
                    .GetMethod("setOSType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.Invoke(controller, null);
            }

            if (id == 1 && _character.inventory.itemList.jakeComplete)
            {
                var controller = Main.Character.wandoos98Controller;
                var type = controller.GetType().GetField("nextOS",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                type?.SetValue(controller, id);
                typeof(Wandoos98Controller)
                    .GetMethod("setOSType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.Invoke(controller, null);
            }

            if (id == 2 && _character.wandoos98.XLLevels > 0)
            {
                var controller = Main.Character.wandoos98Controller;
                var type = controller.GetType().GetField("nextOS",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                type?.SetValue(controller, id);
                typeof(Wandoos98Controller)
                    .GetMethod("setOSType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.Invoke(controller, null);
            }

            _character.wandoos98Controller.refreshMenu();
        }

        public void DoRebirth()
        {
            if (_wrapper == null)
                return;

            if (_wrapper.Breakpoints.Rebirth.RebirthAvailable())
            {
                if (_character.bossController.isFighting || _character.bossController.nukeBoss)
                {
                    Main.Log("Delaying rebirth while boss fight is in progress");
                    return;
                }
            }
            else
            {
                return;
            }

            if (_wrapper.Breakpoints.Rebirth.DoRebirth())
            {
                _currentDiggerBreakpoint = null;
                _currentEnergyBreakpoint = null;
                _currentGearBreakpoint = null;
                _currentWandoosBreakpoint = null;
                _currentMagicBreakpoint = null;
                _currentR3Breakpoint = null;
                _currentNguBreakpoint = null;
                _currentConsumablesBreakpoint = null;
                ConsumablesManager.resetLastConsumables();
            }
        }

        public void CastBloodSpells()
        {
            if (!Main.Settings.CastBloodSpells)
                return;

            if (_wrapper.Breakpoints.Rebirth is TimeRebirth trb && trb.RebirthTime > 0 && Main.Settings.AutoRebirth)
            {
                if (trb.RebirthTime - _character.rebirthTime.totalseconds < 30 * 60)// && !rebirth)
                {
                    return;
                }
            }

            BloodMagicManager.CastGuffB(false);
            BloodMagicManager.CastGuffA(false);
            BloodMagicManager.CastIronPill(false);
        }

        public override void AllocateEnergy()
        {
            if (_wrapper == null)
                return;

            var bp = GetCurrentBreakpoint(true);
            if (bp == null)
                return;

            if (bp.Time != _currentEnergyBreakpoint.Time)
            {
                _currentEnergyBreakpoint = bp;
            }

            var temp = bp.Priorities.Where(x => x.IsValid()).ToList();
            if (temp.Count == 0)
                return;

            bool shouldRetry = true;
            while (shouldRetry)
            {
                var successList = new List<BaseBreakpoint>();
                shouldRetry = false;
                var prioCount = temp.Count(x => !x.IsCapPrio());

                if (temp.Any(x => x is BasicTrainingBP))
                    _character.removeAllEnergy();
                else
                    _character.removeMostEnergy();

                var toAdd = (long)Math.Ceiling((double)_character.idleEnergy / prioCount);
                SetInput(toAdd);

                foreach (var prio in temp)
                {
                    if (!prio.IsCapPrio())
                    {
                        prioCount--;
                    }

                    if (prio.Allocate())
                    {
                        successList.Add(prio);
                        toAdd = (long)Math.Ceiling((double)_character.idleEnergy / prioCount);
                        SetInput(toAdd);
                    }
                    else
                    {
                        shouldRetry = true;
                    }
                }
                temp = successList;
                shouldRetry &= temp.Any();
            }

            _character.NGUController.refreshMenu();
            _character.wandoos98Controller.refreshMenu();
            _character.advancedTrainingController.refresh();
            _character.timeMachineController.updateMenu();
            _character.allOffenseController.refresh();
            _character.allDefenseController.refresh();
            Main.WishManager.UpdateWishMenu();
            _character.augmentsController.updateMenu();
        }

        public override void AllocateMagic()
        {
            if (_wrapper == null)
                return;

            var bp = GetCurrentBreakpoint(false);
            if (bp == null)
                return;

            if (bp.Time != _currentMagicBreakpoint.Time)
            {
                _currentMagicBreakpoint = bp;
            }

            var temp = bp.Priorities.Where(x => x.IsValid()).ToList();
            if (temp.Count == 0)
                return;

            bool shouldRetry = true;
            while (shouldRetry)
            {
                var successList = new List<BaseBreakpoint>();
                shouldRetry = false;

                var prioCount = temp.Count(x => !x.IsCapPrio());

                _character.removeMostMagic();
                var toAdd = (long)Math.Ceiling((double)_character.magic.idleMagic / prioCount);
                SetInput(toAdd);

                var nextBP = GetNextBreakpoint(false, bp.Time);
                foreach (var prio in temp)
                {
                    if (!prio.IsCapPrio())
                    {
                        prioCount--;
                    }

                    prio.NextBreakpointTime = nextBP?.Time;
                    if (prio.Allocate())
                    {
                        successList.Add(prio);
                        toAdd = (long)Math.Ceiling((double)_character.magic.idleMagic / prioCount);
                        SetInput(toAdd);
                    }
                    else
                    {
                        shouldRetry = true;
                    }
                }
                temp = successList;
                shouldRetry &= temp.Any();
            }

            _character.timeMachineController.updateMenu();
            _character.bloodMagicController.updateMenu();
            _character.NGUController.refreshMenu();
            _character.wandoos98Controller.refreshMenu();
            Main.WishManager.UpdateWishMenu();
        }

        public override void AllocateR3()
        {
            if (_wrapper == null)
                return;

            var bp = GetCurrentR3Breakpoint();
            if (bp == null)
                return;

            if (bp.Time != _currentR3Breakpoint.Time)
            {
                _currentR3Breakpoint = bp;
            }

            var temp = bp.Priorities.Where(x => x.IsValid()).ToList();
            if (temp.Count == 0)
                return;

            var prioCount = temp.Count(x => !x.IsCapPrio() && !(x is HackBP)) + (temp.Any(x => x is HackBP && !x.IsCapPrio()) ? 1 : 0);
            _character.removeAllRes3();
            var toAdd = (long)Math.Ceiling((double)_character.res3.idleRes3 / prioCount);
            SetInput(toAdd);

            var hackAllocated = false;

            foreach (var prio in temp)
            {
                switch (prio)
                {
                    case HackBP _ when hackAllocated:
                        continue;
                    case HackBP _:
                        hackAllocated = true;
                        break;
                }

                if (!prio.IsCapPrio())
                {
                    prioCount--;
                }

                if (prio.Allocate())
                {
                    toAdd = (long)Math.Ceiling((double)_character.res3.idleRes3 / prioCount);
                    SetInput(toAdd);
                }
            }

            _character.hacksController.refreshMenu();
            Main.WishManager.UpdateWishMenu();
        }

        public override void EquipGear()
        {
            if (_wrapper == null)
                return;
            var bp = GetCurrentGearBreakpoint();
            if (bp == null)
                return;

            if (bp.Time != _currentGearBreakpoint.Time)
            {
                _hasGearSwapped = false;
            }

            if (_hasGearSwapped) return;

            if (!LoadoutManager.CanSwap()) return;
            _hasGearSwapped = true;
            _currentGearBreakpoint = bp;
            LoadoutManager.ChangeGear(bp.Gear);
            Main.InventoryController.assignCurrentEquipToLoadout(0);
        }

        public override void EquipDiggers()
        {
            if (_wrapper == null)
                return;
            var bp = GetCurrentDiggerBreakpoint();
            if (bp == null)
                return;

            if (bp.Time != _currentDiggerBreakpoint.Time)
            {
                _hasDiggerSwapped = false;
            }

            if (_hasDiggerSwapped) return;

            if (!DiggerManager.CanSwap()) return;

            //Main.LogDebug($"Setting diggers from bp {bp.Time}: {string.Join(",", bp.Diggers.Select(x => x.ToString()))}");
            
            _hasDiggerSwapped = DiggerManager.TryEquipDiggers(bp.Diggers);
            
            //Main.LogDebug($"Result: {_hasDiggerSwapped}");

            if (_hasDiggerSwapped)
            {
                Main.Log($"Equipping Diggers: {string.Join(",", bp.Diggers.Select(x => x.ToString()))}");
                _currentDiggerBreakpoint = bp;
            }
            else
            {
                //Main.LogDebug($"Not all configured diggers enabled, retry at next loop...");
            }

            _character.allDiggers.refreshMenu();
        }

        public override void ConsumeConsumables()
        {
            if (_wrapper == null)
                return;
            var bp = GetCurrentConsumablesBreakpoint();
            if (bp == null)
                return;
            if (bp.Time != _currentConsumablesBreakpoint.Time)
            {
                _didConsumeConsumables = false;
            }

            if (_didConsumeConsumables) return;
            _didConsumeConsumables = true;
            _currentConsumablesBreakpoint = bp;
            ConsumablesManager.EatConsumables(bp.Consumables, bp.Time, bp.Quantity);
        }

        private AllocationBreakPoint GetCurrentBreakpoint(bool energy)
        {
            var bps = energy ? _wrapper?.Breakpoints?.Energy : _wrapper?.Breakpoints?.Magic;
            if (bps == null)
                return null;

            AllocationBreakPoint bp = GetBreakpoint(energy, _character.rebirthTime.totalseconds);

            if (energy && _currentEnergyBreakpoint == null)
            {
                _currentEnergyBreakpoint = bp;
            }

            if (!energy && _currentMagicBreakpoint == null)
            {
                _currentMagicBreakpoint = bp;
            }

            return bp;
        }

        private AllocationBreakPoint GetBreakpoint(bool energy, double time)
        {
            var bps = energy ? _wrapper?.Breakpoints?.Energy : _wrapper?.Breakpoints?.Magic;
            if (bps == null)
                return null;

            return bps.OrderByDescending(x => x.Time).FirstOrDefault(x => x.Time <= time);
        }

        private AllocationBreakPoint GetNextBreakpoint(bool energy, double time)
        {
            var bps = energy ? _wrapper?.Breakpoints?.Energy : _wrapper?.Breakpoints?.Magic;
            if (bps == null)
                return null;

            return bps.OrderByDescending(x => x.Time).LastOrDefault(x => x.Time > time);
        }

        private AllocationBreakPoint GetCurrentR3Breakpoint()
        {
            var bps = _wrapper?.Breakpoints?.NGUBreakpoints;
            if (bps == null)
                return null;
            foreach (var b in _wrapper.Breakpoints.R3)
            {
                var rbTime = _character.rebirthTime.totalseconds;
                if (rbTime > b.Time)
                {
                    if (_currentR3Breakpoint == null)
                    {
                        _currentR3Breakpoint = b;
                    }

                    return b;
                }
            }

            _currentR3Breakpoint = null;
            return null;
        }

        private GearBreakpoint GetCurrentGearBreakpoint()
        {
            var bps = _wrapper?.Breakpoints?.Gear;
            if (bps == null)
                return null;
            foreach (var b in bps)
            {
                if (_character.rebirthTime.totalseconds > b.Time)
                {
                    if (_currentGearBreakpoint == null)
                    {
                        _hasGearSwapped = false;
                        _currentGearBreakpoint = b;
                    }

                    return b;
                }
            }

            _currentGearBreakpoint = null;
            return null;
        }

        private DiggerBreakpoint GetCurrentDiggerBreakpoint()
        {
            var bps = _wrapper?.Breakpoints?.Diggers;
            if (bps == null)
                return null;

            if (_character.challenges.timeMachineChallenge.inChallenge)
                return null;
            if (!_character.buttons.brokenTimeMachine.interactable)
                return null;
            if (_character.machine.realBaseGold == 0.0)
                return null;

            foreach (var b in bps)
            {
                if (_character.rebirthTime.totalseconds > b.Time)
                {
                    if (_currentDiggerBreakpoint == null || _character.challenges.trollChallenge.inChallenge)
                    {
                        _hasDiggerSwapped = false;
                        _currentDiggerBreakpoint = b;
                    }

                    return b;
                }
            }

            _currentDiggerBreakpoint = null;
            return null;
        }

        private NGUDiffBreakpoint GetCurrentNGUDiffBreakpoint()
        {
            var bps = _wrapper?.Breakpoints?.NGUBreakpoints;
            if (bps == null)
                return null;
            foreach (var b in bps)
            {
                if (_character.rebirthTime.totalseconds > b.Time)
                {
                    if (_currentNguBreakpoint == null)
                    {
                        _hasNGUSwapped = false;
                        _currentNguBreakpoint = b;
                    }

                    return b;
                }
            }

            _currentNguBreakpoint = null;
            return null;
        }

        private WandoosBreakpoint GetCurrentWandoosBreakpoint()
        {
            var bps = _wrapper?.Breakpoints?.Wandoos;
            if (bps == null)
                return null;

            foreach (var b in bps)
            {
                if (_character.rebirthTime.totalseconds > b.Time)
                {
                    if (_currentWandoosBreakpoint == null)
                    {
                        _hasWandoosSwapped = false;
                        _currentWandoosBreakpoint = b;
                    }

                    return b;
                }
            }

            _currentWandoosBreakpoint = null;
            return null;
        }

        private ConsumablesBreakpoint GetCurrentConsumablesBreakpoint()
        {
            var bps = _wrapper?.Breakpoints?.ConsumablesBreakpoints;
            if (bps == null)
                return null;
            foreach (var b in bps)
            {
                if (_character.rebirthTime.totalseconds > b.Time)
                {
                    if (_currentConsumablesBreakpoint == null)
                    {
                        _didConsumeConsumables = false;
                        _currentConsumablesBreakpoint = b;
                    }

                    return b;
                }
            }

            _currentConsumablesBreakpoint = null;
            return null;
        }

        private void SetInput(float val)
        {
            _character.energyMagicPanel.energyRequested.text = val.ToString("000000000000000000");
            _character.energyMagicPanel.validateInput();
        }

        private int[] GetConsumablesQuantities(string[] consumableTypes)
        {
            int[] quantities = new int[consumableTypes.Length];
            int i = 0;

            foreach (string consumable in consumableTypes)
            {
                int count = 1;

                if (consumable.Contains(":")) // "EPOT-A:5" = 5 energy A potions
                {
                    string[] split = consumable.Split(':');
                    if (!int.TryParse(split[1], out count))
                    {
                        count = 1;
                    }
                }

                quantities[i++] = count;
            }

            return quantities;
        }
    }

    [Serializable]
    internal class BreakpointWrapper
    {
        [SerializeField] public Breakpoints Breakpoints;
    }

    [Serializable]
    internal class Breakpoints
    {
        [SerializeField] public AllocationBreakPoint[] Magic;
        [SerializeField] public AllocationBreakPoint[] Energy;
        [SerializeField] public AllocationBreakPoint[] R3;
        [SerializeField] public GearBreakpoint[] Gear;
        [SerializeField] public DiggerBreakpoint[] Diggers;
        [SerializeField] public WandoosBreakpoint[] Wandoos;
        [SerializeField] public BaseRebirth Rebirth;
        [SerializeField] public NGUDiffBreakpoint[] NGUBreakpoints;
        [SerializeField] public ConsumablesBreakpoint[] ConsumablesBreakpoints;

    }

    [Serializable]
    internal class AllocationBreakPoint
    {
        [SerializeField] public double Time;
        [SerializeField] public BaseBreakpoint[] Priorities;
    }

    [Serializable]
    public class GearBreakpoint
    {
        public double Time;
        public int[] Gear;
    }

    [Serializable]
    public class DiggerBreakpoint
    {
        public double Time;
        public int[] Diggers;
    }

    [Serializable]
    public class WandoosBreakpoint
    {
        public double Time;
        public int OS;
    }

    [Serializable]
    public class NGUDiffBreakpoint
    {
        public double Time;
        public int Diff;
    }

    [Serializable]
    internal class ConsumablesBreakpoint
    {
        [SerializeField] public double Time;
        [SerializeField] public string[] Consumables;
        [SerializeField] public int[] Quantity;
    }

}

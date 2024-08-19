using NGUInjector.AllocationProfiles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static NGUInjector.Main;
using static NGUInjector.Managers.CombatHelpers;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskBand;

namespace NGUInjector.Managers
{
    internal class CombatManager
    {
        private readonly Character _character;
        private readonly PlayerController _pc;
        private bool _isFighting = false;
        private float _fightTimer = 0;
        private string _enemyName;

        public CombatManager()
        {
            _character = Main.Character;
            _pc = Main.PlayerController;
        }

        internal void UpdateFightTimer(float diff)
        {
            _fightTimer += diff;
        }

        private void DoCombat(bool fastCombat, bool smartBeastMode)
        {
            if (!_pc.canUseMove || !_pc.moveCheck())
                return;

            CombatAI combatAI = new CombatAI(_character, fastCombat, smartBeastMode);

            if (combatAI.DoPreCombat())
            {
                return;
            }

            if (combatAI.DoCombatBuffs())
            {
                return;
            }

            combatAI.DoCombat();
        }

        internal static bool IsZoneUnlocked(int zone)
        {
            return zone <= Main.Character.adventureController.zoneDropdown.options.Count - 2;
        }

        internal void MoveToZone(int zone)
        {
            if (_character.adventure.zone != zone)
            {
                _isFighting = false;
                _fightTimer = 0;
            }
            CurrentCombatZone = zone;
            _character.adventureController.zoneSelector.changeZone(zone);
        }

        private bool CheckBeastModeToggle(bool beastMode, out bool beastModeWasToggled)
        {
            beastModeWasToggled = false;
            //Beast mode checks
            if (BeastModeUnlocked())
            {
                bool needToToggle = BeastModeActive() != beastMode;

                //If the button is inaccessible, we need to stay in manual mode until we can press it
                if (needToToggle)
                {
                    beastModeWasToggled = CastBeastMode();
                    return true;
                }
            }

            return false;
        }

        internal void IdleZone(int zone, bool bossOnly, bool recoverHealth, bool beastMode)
        {
            if (zone == -1 && _character.adventure.zone != -1)
            {
                MoveToZone(-1);
                return;
            }

            bool needsToHeal = recoverHealth && !HasFullHP();

            //If we have no enemy and we were fighting, the fight has ended, update combat flags and release any gear locks. Move to safe zone if we need to heal.
            if (_character.adventureController.currentEnemy == null)
            {
                if (_isFighting)
                {
                    if (_fightTimer > 1)
                    {
                        LogCombat($"{_enemyName} killed in {_fightTimer:00.0}s");
                    }

                    _isFighting = false;
                    _fightTimer = 0;

                    if (LoadoutManager.CurrentLock == LockType.Gold)
                    {
                        Log("Gold Loadout kill done. Turning off setting and swapping gear");
                        Settings.DoGoldSwap = false;
                        LoadoutManager.RestoreGear();
                        LoadoutManager.ReleaseLock();
                        MoveToZone(-1);
                        return;
                    }
                }
                else
                {
                    _fightTimer = 0;
                }

                if (_character.adventure.zone != -1 && needsToHeal)
                {
                    MoveToZone(-1);
                    return;
                }
            }

            //If we need to toggle beast mode, wait in the safe zone in manual mode until beast mode is enabled
            if (CheckBeastModeToggle(beastMode, out bool beastModeWasToggled))
            {
                if (!beastModeWasToggled)
                {
                    if (_character.adventure.zone != -1)
                    {
                        MoveToZone(-1);
                    }
                    if (_character.adventure.autoattacking)
                    {
                        _character.adventureController.idleAttackMove.setToggle();
                    }
                }
                return;
            }

            //Enable idle attack if its not on
            if (!_character.adventure.autoattacking)
            {
                _character.adventureController.idleAttackMove.setToggle();
                return;
            }

            //Check if we're in not in the right zone and not in safe zone, if not move to safe zone first
            if (_character.adventure.zone != zone && _character.adventure.zone != -1)
            {
                MoveToZone(-1);
            }

            //If we're in safe zone, recover health if needed.
            if (_character.adventure.zone == -1 && needsToHeal)
            {
                return;
            }

            //Move to the zone
            if (_character.adventure.zone != zone)
            {
                MoveToZone(zone);
                return;
            }

            //Wait for an enemy to spawn
            if (_character.adventureController.currentEnemy == null)
            {
                return;
            }

            //Skip blacklisted enemies
            if (zone < 1000 && Settings.BlacklistedBosses.Contains(_character.adventureController.currentEnemy.spriteID))
            {
                MoveToZone(-1);
                MoveToZone(zone);
                return;
            }

            //Skip regular enemies if we're in bossOnly mode
            if (bossOnly && zone < 1000 && !ZoneHelpers.ZoneIsTitan(zone))
            {
                var ec = _character.adventureController.currentEnemy.enemyType;
                if (ec != enemyType.boss && !ec.ToString().Contains("bigBoss"))
                {
                    MoveToZone(-1);
                    MoveToZone(zone);
                    return;
                }
            }

            //We have a valid enemy and we're ready to fight, allow idle combat to continue
            _isFighting = true;
            _enemyName = _character.adventureController.currentEnemy.name;
        }

        internal void ManualZone(int zone, bool bossOnly, bool recoverHealth, bool precastBuffs, bool fastCombat, bool beastMode, bool smartBeastMode)
        {
            if (fastCombat)
            {
                smartBeastMode = false;
            }

            if (zone == -1 && _character.adventure.zone != -1)
            {
                MoveToZone(-1);
                return;
            }

            //If we havent unlocked any attacks yet, use the Idle loop, otherwise turn off idle mode
            if (_character.training.attackTraining[1] == 0)
            {
                IdleZone(zone, bossOnly, recoverHealth, beastMode);
                return;
            }
            else if (_character.adventure.autoattacking)
            {
                _character.adventureController.idleAttackMove.setToggle();
                return;
            }

            bool needsToPrecast = precastBuffs &&
                (
                    (ChargeUnlocked() && !ChargeReady()) ||
                    (ParryUnlocked() && (!ParryReady() || !ParryActive())) ||
                    (smartBeastMode && BeastModeUnlocked() && (!BeastModeReady() || !BeastModeActive())) ||
                    (MegaBuffUnlocked() && !MegaBuffReady()) ||
                    (UltimateBuffUnlocked() && !UltimateBuffReady()) ||
                    (DefensiveBuffUnlocked() && !DefensiveBuffReady())
                );

            bool needsToHeal = recoverHealth && !HasFullHP();

            //If we have no enemy and we were fighting, the fight has ended, update combat flags and release any gear locks. Move to safe zone if we need to heal or precast.
            if (_character.adventureController.currentEnemy == null)
            {
                if (_isFighting)
                {
                    if (_fightTimer > 1)
                    {
                        LogCombat($"{_enemyName} killed in {_fightTimer:00.0}s");
                    }

                    _isFighting = false;
                    _fightTimer = 0;

                    if (LoadoutManager.CurrentLock == LockType.Gold)
                    {
                        Log("Gold Loadout kill done. Turning off setting and swapping gear");
                        Settings.DoGoldSwap = false;
                        LoadoutManager.RestoreGear();
                        LoadoutManager.ReleaseLock();
                        MoveToZone(-1);
                        return;
                    }
                }
                else
                {
                    _fightTimer = 0;
                }

                if (_character.adventure.zone != -1 && (needsToPrecast || needsToHeal))
                {
                    MoveToZone(-1);
                    return;
                }
            }

            //If we need to toggle beast mode, just do the normal combat loop until the cooldown is ready
            if (!smartBeastMode)
            {
                CheckBeastModeToggle(beastMode, out bool beastModeWasToggled);
                if (beastModeWasToggled)
                {
                    return;
                }
            }

            //Check if we're in not in the right zone and not in safe zone, if not move to safe zone first
            if (_character.adventure.zone != zone && _character.adventure.zone != -1)
            {
                MoveToZone(-1);
            }

            //If we're in safe zone, precast buffs and recover health if needed.
            if (_character.adventure.zone == -1)
            {
                if (needsToPrecast)
                {
                    if (ParryUnlocked() && !ParryActive())
                    {
                        if (CastParry()) return;
                    }

                    if (smartBeastMode && BeastModeUnlocked() && !BeastModeActive())
                    {
                        if (CastBeastMode()) return;
                    }

                    return;
                }

                if (needsToHeal)
                {
                    if (ParryUnlocked() && !ParryActive())
                    {
                        if (CastParry()) return;
                    }

                    return;
                }
            }

            //Move to the zone
            if (_character.adventure.zone != zone)
            {
                MoveToZone(zone);
                return;
            }

            //Wait for an enemy to spawn
            if (_character.adventureController.currentEnemy == null)
            {
                if (!precastBuffs && bossOnly)
                {
                    if (!ParryActive())
                    {
                        if (CastParry())
                        {
                            return;
                        }
                    }

                    if (GetHPPercentage() < .75)
                    {
                        if (CastHeal())
                            return;
                    }
                }

                if (fastCombat)
                {
                    if (GetHPPercentage() < .75)
                    {
                        if (CastHeal())
                            return;
                    }

                    if (GetHPPercentage() < .60)
                    {
                        if (CastHyperRegen())
                            return;
                    }
                }

                return;
            }

            //Skip blacklisted enemies
            if (zone < 1000 && Settings.BlacklistedBosses.Contains(_character.adventureController.currentEnemy.spriteID))
            {
                MoveToZone(-1);
                MoveToZone(zone);
                return;
            }

            //Skip regular enemies if we're in bossOnly mode
            if (bossOnly && zone < 1000 && !ZoneHelpers.ZoneIsTitan(zone))
            {
                var ec = _character.adventureController.currentEnemy.enemyType;
                if (ec != enemyType.boss && !ec.ToString().Contains("bigBoss"))
                {
                    MoveToZone(-1);
                    MoveToZone(zone);
                    return;
                }
            }

            //We have a valid enemy and we're ready to fight. Run through our manual combat routine.
            _isFighting = true;
            _enemyName = _character.adventureController.currentEnemy.name;

            DoCombat(fastCombat, smartBeastMode);
        }
    }
}

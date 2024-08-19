using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.Remoting.Messaging;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;

namespace NGUInjector.Managers
{
    internal static class CombatHelpers
    {
        private static int _currentCombatZone = -1;
        internal static int CurrentCombatZone
        {
            get { return IsCurrentlyGoldSniping || IsCurrentlyQuesting || IsCurrentlyAdventuring ? _currentCombatZone : -1; }
            set { _currentCombatZone = value; }
        }

        internal static bool IsCurrentlyGoldSniping { get; set; }
        internal static bool IsCurrentlyQuesting { get; set; }
        internal static bool IsCurrentlyAdventuring { get; set; }

        #region Healing
        internal static bool HasFullHP()
        {
            return Math.Abs(Main.Character.totalAdvHP() - Main.Character.adventure.curHP) < 5;
        }

        internal static float GetHPPercentage()
        {
            return Main.Character.adventure.curHP / Main.Character.totalAdvHP();
        }

        internal static bool HealUnlocked()
        {
            return Main.Character.training.defenseTraining[1] >= 10000;
        }

        internal static bool HealReady()
        {
            return Main.Character.adventureController.healMove.button.IsInteractable();
        }

        internal static bool CastHeal()
        {
            if (HealReady())
            {
                Main.Character.adventureController.healMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool HyperRegenUnlocked()
        {
            return Main.Character.settings.hasHyperRegen;
        }

        internal static bool HyperRegenReady()
        {
            return Main.Character.adventureController.hyperRegenMove.button.IsInteractable();
        }

        internal static bool CastHyperRegen()
        {
            if (HyperRegenReady())
            {
                Main.Character.adventureController.hyperRegenMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool OhShitUnlocked()
        {
            return Main.Character.wishes.wishes[58].level >= 1 && ParalyzeUnlocked() && HealUnlocked() && HyperRegenUnlocked();
        }

        internal static bool OhShitReady()
        {
            return Main.Character.adventureController.ohShitMove.button.IsInteractable();
        }

        internal static bool CastOhShit()
        {
            if (OhShitReady())
            {
                Main.Character.adventureController.ohShitMove.doMove();
                return true;
            }

            return false;
        }
        #endregion

        #region Regular Buffs
        internal static bool DefensiveBuffUnlocked()
        {
            return Main.Character.training.defenseTraining[0] >= 5000;
        }

        internal static bool DefensiveBuffReady()
        {
            return Main.Character.adventureController.defenseBuffMove.button.IsInteractable();
        }

        internal static bool DefenseBuffActive(out float? timeLeft)
        {
            bool isActive = Main.PlayerController.defenseBuffTime > 0 && Main.PlayerController.defenseBuffTime < Main.Character.defenseBuffDuration();

            timeLeft = isActive ? (float?)(Main.Character.defenseBuffDuration() - Main.PlayerController.defenseBuffTime) : null;

            return isActive;
        }

        internal static bool CastDefensiveBuff()
        {
            if (DefensiveBuffReady())
            {
                Main.Character.adventureController.defenseBuffMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool OffensiveBuffReady()
        {
            return Main.Character.adventureController.offenseBuffMove.button.IsInteractable();
        }

        internal static bool CastOffensiveBuff()
        {
            if (OffensiveBuffReady())
            {
                Main.Character.adventureController.offenseBuffMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool UltimateBuffUnlocked()
        {
            return Main.Character.training.defenseTraining[4] >= 25000;
        }

        internal static bool UltimateBuffReady()
        {
            return Main.Character.adventureController.ultimateBuffMove.button.IsInteractable();
        }

        internal static bool UltimateBuffActive(out float? timeLeft)
        {
            bool isActive = Main.PlayerController.ultimateBuffTime > 0 && Main.PlayerController.ultimateBuffTime < Main.Character.ultimateBuffDuration();

            timeLeft = isActive ? (float?)(Main.Character.ultimateBuffDuration() - Main.PlayerController.ultimateBuffTime) : null;

            return isActive;
        }

        internal static bool CastUltimateBuff()
        {
            if (UltimateBuffReady())
            {
                Main.Character.adventureController.ultimateBuffMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool MegaBuffUnlocked()
        {
            return Main.Character.wishes.wishes[8].level >= 1 && UltimateBuffUnlocked();
        }

        internal static bool MegaBuffReady()
        {
            return Main.Character.adventureController.megaBuffMove.button.IsInteractable();
        }

        internal static bool CastMegaBuff()
        {
            if (MegaBuffReady())
            {
                Main.Character.adventureController.megaBuffMove.doMove();
                return true;
            }

            return false;
        }
        #endregion

        #region Defensive Cooldowns
        internal static bool BlockReady()
        {
            return Main.Character.adventureController.blockMove.button.IsInteractable();
        }

        internal static bool BlockActive(out float? timeLeft)
        {
            bool isActive = Main.PlayerController.isBlocking;

            timeLeft = isActive ? (float?)(3.0f - Main.PlayerController.blockTime) : null;

            return isActive;
        }

        internal static bool CastBlock()
        {
            if (BlockReady())
            {
                Main.Character.adventureController.blockMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool ParalyzeUnlocked()
        {
            return Main.Character.allChallenges.hasParalyze();
        }

        internal static bool ParalyzeReady()
        {
            return Main.Character.adventureController.paralyzeMove.button.IsInteractable();
        }

        internal static bool EnemyIsParalyzed(EnemyAI eai, out float? timeLeft)
        {
            var paralyzeTime = eai.GetPV<float>("paralyzeTime");

            bool isActive = paralyzeTime > 0;

            timeLeft = isActive ? (float?)(paralyzeTime) : null;

            return isActive;
        }

        internal static bool CastParalyze(bool useOhShitInstead)
        {
            if(useOhShitInstead && CastOhShit())
            {
                return true;
            }

            if (ParalyzeReady())
            {
                Main.Character.adventureController.paralyzeMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool ParryUnlocked()
        {
            return Main.Character.training.attackTraining[2] >= 15000;
        }

        internal static bool ParryReady()
        {
            return Main.Character.adventureController.parryMove.button.IsInteractable();
        }

        internal static bool ParryActive()
        {
            return Main.PlayerController.isParrying;
        }

        internal static bool CastParry()
        {
            if (ParryReady())
            {
                Main.Character.adventureController.parryMove.doMove();
                return true;
            }

            return false;
        }
        #endregion

        #region Other Cooldowns and Secondary Buffs
        internal static bool ChargeUnlocked()
        {
            return Main.Character.training.defenseTraining[3] >= 20000;
        }

        internal static bool ChargeReady()
        {
            return Main.Character.adventureController.chargeMove.button.IsInteractable();
        }

        internal static bool ChargeActive()
        {
            return Main.PlayerController.chargeFactor > 1.05;
        }

        internal static bool CastCharge()
        {
            if (ChargeReady() && !ChargeActive())
            {
                Main.Character.adventureController.chargeMove.doMove();
                return true;
            }

            return false;
        }

        internal static bool BeastModeUnlocked()
        {
            return Main.Character.adventureController.hasBeastMode();
        }

        internal static bool BeastModeReady()
        {
            return Main.Character.adventureController.beastModeMove.button.IsInteractable();
        }

        internal static bool BeastModeActive()
        {
            return Main.Character.adventure.beastModeOn;
        }

        internal static bool CastBeastMode()
        {
            if (BeastModeReady())
            {
                Main.Character.adventureController.beastModeMove.doMove();
                return true;
            }

            return false;
        }
        #endregion

        #region Attacks
        internal static bool PierceReady()
        {
            return Main.Character.adventureController.pierceMove.button.IsInteractable();
        }

        internal static bool UltimateAttackReady()
        {
            return Main.Character.adventureController.ultimateAttackMove.button.IsInteractable();
        }

        internal static bool Move69Ready()
        {
            return Main.Character.adventure.move69Unlocked && Main.Move69.button.IsInteractable() && Main.Character.adventure.move69Used < 69;
        }
        #endregion
    }
}

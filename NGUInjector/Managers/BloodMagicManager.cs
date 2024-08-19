using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGUInjector.Managers
{
    internal static class BloodMagicManager
    {
        private enum FailureReason { Disabled, OnCooldown, BelowMinimum, BelowThreshold }

        private static string GetFailureMessage(FailureReason reason, double minBlood, double calculatedPower, int threshold)
        {
            string msg = string.Empty;

            switch (reason)
            {
                case FailureReason.Disabled:
                    msg = "Disabled";
                    break;
                case FailureReason.OnCooldown:
                    msg = "On cooldown";
                    break;
                case FailureReason.BelowMinimum:
                    msg = $"Below minimum blood threshold of {minBlood}";
                    break;
                case FailureReason.BelowThreshold:
                    msg = $"Below configured power threshold ({Math.Round(calculatedPower, 2)} of {threshold}) and not force cast on rebirth";
                    break;
                default:
                    msg = "Unknown";
                    break;
            }

            return msg;
        }

        internal static void CastGuffB(bool rebirth)
        {
            if (!Main.Settings.CastBloodSpells)
                return;

            double bloodPoints = Main.Character.bloodMagic.bloodPoints;
            double minBlood = Main.Character.bloodSpells.minMacguffin2Blood();

            long mcguffB = 0;
            FailureReason reason = FailureReason.Disabled;

            if (Main.Settings.BloodMacGuffinBThreshold > 0)
            {
                if (Main.Character.adventure.itopod.perkLevel[73] >= 1L &&
                    Main.Character.settings.rebirthDifficulty >= difficulty.evil)
                {
                    if (Main.Character.bloodMagic.macguffin2Time.totalseconds > Main.Character.bloodSpells.macguffin2Cooldown)
                    {
                        if (bloodPoints >= minBlood)
                        {
                            var a = bloodPoints / minBlood;
                            mcguffB = (int)(Math.Log(a, 20.0) + 1.0);

                            if (Main.Settings.BloodMacGuffinBThreshold <= mcguffB || (rebirth && Main.Settings.BloodMacGuffinBOnRebirth))
                            {
                                Main.Character.bloodSpells.castMacguffin2Spell();
                                Main.LogPitSpin("Casting Blood Spell MacGuffin B power @ " + mcguffB);
                                return;
                            }
                            else
                            {
                                reason = FailureReason.BelowThreshold;
                            }
                        }
                        else
                        {
                            reason = FailureReason.BelowMinimum;
                        }
                    }
                    else
                    {
                        reason = FailureReason.OnCooldown;
                    }
                }
            }

            if (rebirth && reason != FailureReason.Disabled)
            {
                string msg = $"Casting Failed Blood Spell MacGuffin B - {GetFailureMessage(reason, minBlood, mcguffB, Main.Settings.BloodMacGuffinBThreshold)}";
                Main.Log(msg);
            }
        }

        internal static void CastGuffA(bool rebirth)
        {
            if (!Main.Settings.CastBloodSpells)
                return;

            double bloodPoints = Main.Character.bloodMagic.bloodPoints;
            double minBlood = Main.Character.bloodSpells.minMacguffin1Blood();

            long mcguffA = 0;
            FailureReason reason = FailureReason.Disabled;

            if (Main.Settings.BloodMacGuffinAThreshold > 0)
            {
                if (Main.Character.adventure.itopod.perkLevel[72] >= 1L)
                {
                    if (Main.Character.bloodMagic.macguffin1Time.totalseconds > Main.Character.bloodSpells.macguffin1Cooldown)
                    {
                        if (bloodPoints >= minBlood)
                        {
                            var a = bloodPoints / minBlood;
                            mcguffA = (int)((Math.Log(a, 10.0) + 1.0) * Main.Character.wishesController.totalBloodGuffbonus());

                            if (Main.Settings.BloodMacGuffinAThreshold <= mcguffA || (rebirth && Main.Settings.BloodMacGuffinBOnRebirth))
                            {
                                Main.Character.bloodSpells.castMacguffin1Spell();
                                Main.LogPitSpin("Casting Blood Spell MacGuffin A power @ " + mcguffA);
                                return;
                            }
                            else
                            {
                                reason = FailureReason.BelowThreshold;
                            }
                        }
                        else
                        {
                            reason = FailureReason.BelowMinimum;
                        }
                    }
                    else
                    {
                        reason = FailureReason.OnCooldown;
                    }
                }
            }

            if (rebirth && reason != FailureReason.Disabled)
            {
                string msg = $"Casting Failed Blood Spell MacGuffin A - {GetFailureMessage(reason, minBlood, mcguffA, Main.Settings.BloodMacGuffinAThreshold)}";
                Main.Log(msg);
            }
        }

        internal static void CastIronPill(bool rebirth)
        {
            if (!Main.Settings.CastBloodSpells)
                return;

            double bloodPoints = Main.Character.bloodMagic.bloodPoints;
            double minBlood = Main.Character.bloodSpells.minAdventureBlood();

            float iron = 0;
            FailureReason reason = FailureReason.Disabled;

            if (Main.Settings.IronPillThreshold > 0)
            {
                if (Main.Character.bloodMagic.adventureSpellTime.totalseconds > Main.Character.bloodSpells.adventureSpellCooldown)
                {
                    if (bloodPoints >= minBlood)
                    {
                        iron = (float)Math.Floor(Math.Pow(bloodPoints, 0.25));
                        if (Main.Character.settings.rebirthDifficulty >= difficulty.evil)
                        {
                            iron *= Main.Character.adventureController.itopod.ironPillBonus();
                        }

                        if (Main.Settings.IronPillThreshold <= iron || (rebirth && Main.Settings.IronPillOnRebirth))
                        {
                            Main.Character.bloodSpells.castAdventurePowerupSpell();
                            Main.LogPitSpin("Casting Blood Spell Iron Pill power @ " + iron);
                            return;
                        }
                        else
                        {
                            reason = FailureReason.BelowThreshold;
                        }
                    }
                    else
                    {
                        reason = FailureReason.BelowMinimum;
                    }
                }
                else
                {
                    reason = FailureReason.OnCooldown;
                }
            }

            if (rebirth && reason != FailureReason.Disabled)
            {
                string msg = $"Casting Failed Blood Spell Iron Pill - {GetFailureMessage(reason, minBlood, iron, Main.Settings.IronPillThreshold)}";
                Main.Log(msg);
            }
        }
    }
}

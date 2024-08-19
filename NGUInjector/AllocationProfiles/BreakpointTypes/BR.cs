﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGUInjector.AllocationProfiles.BreakpointTypes
{
    internal class BR : BaseBreakpoint
    {
        internal int RebirthTime { get; set; }

        protected override bool Unlocked()
        {
            return Character.buttons.bloodMagic.interactable;
        }

        protected override bool TargetMet()
        {
            return false;
        }

        internal override bool Allocate()
        {
            return CastRituals(Index, NextBreakpointTime) > 0;
        }

        protected override bool CorrectResourceType()
        {
            return Type == ResourceType.Magic;
        }

        private long CastRituals(int secondsToRun, double? nextBreakpointTime)
        {
            var allocationLeft = (long)MaxAllocation;
            long totalAllocated = 0;

            for (var i = Character.bloodMagic.ritual.Count - 1; i >= 0; i--)
            {
                if (allocationLeft <= 0)
                    break;
                if (Character.magic.idleMagic == 0)
                    break;
                if (i >= Character.bloodMagicController.ritualsUnlocked())
                    continue;

                var goldCost = Character.bloodMagicController.bloodMagics[i].baseCost * Character.totalDiscount();
                bool shouldSkip = false;

                //Ritual costs too much gold
                if (goldCost > Character.realGold && Character.bloodMagic.ritual[i].progress <= 0.0)
                {
                    shouldSkip = true;
                }
                else
                {
                    var tLeft = RitualTimeLeft(i, allocationLeft);
                    var completeTime = Character.rebirthTime.totalseconds + tLeft;

                    //Ritual will not finish before rebirth
                    if (RebirthTime > 0 && Main.Settings.AutoRebirth && completeTime > RebirthTime)
                    {
                        shouldSkip = true;
                    }
                    else
                    {
                        //If the runtime is explicitly set, ignore the breakpoint logic
                        if (secondsToRun > 0)
                        {
                            //The time left is more than the configured number of seconds to run
                            if (tLeft > secondsToRun)
                            {
                                shouldSkip = true;
                            }
                        }
                        else
                        {
                            //The ritual wont finish before the next breakpoint
                            if (nextBreakpointTime.HasValue && completeTime > nextBreakpointTime)
                            {
                                shouldSkip = true;
                            }
                            //The time left is more than an hour
                            else if (tLeft > 3600)
                            {
                                shouldSkip = true;
                            }
                        }
                    }
                }

                if (shouldSkip)
                {
                    if (Character.bloodMagic.ritual[i].magic > 0)
                    {
                        Character.bloodMagicController.bloodMagics[i].removeAllMagic();
                    }

                    continue;
                }

                var cap = CalculateMaxAllocation(i, allocationLeft);
                SetInput(cap);
                Character.bloodMagicController.bloodMagics[i].add();
                totalAllocated += cap;
                allocationLeft -= cap;
            }

            return totalAllocated;
        }

        private float RitualProgressPerTick(int id, long remaining)
        {
            var num1 = 0.0;
            if (Character.settings.rebirthDifficulty == difficulty.normal)
                num1 = remaining * (double)Character.totalMagicPower() / 50000.0 /
                       Character.bloodMagicController.normalSpeedDividers[id];
            else if (Character.settings.rebirthDifficulty == difficulty.evil)
                num1 = remaining * (double)Character.totalMagicPower() / 50000.0 /
                       Character.bloodMagicController.evilSpeedDividers[id];
            else if (Character.settings.rebirthDifficulty == difficulty.sadistic)
                num1 = remaining * (double)Character.totalMagicPower() /
                       Character.bloodMagicController.sadisticSpeedDividers[id];
            if (Character.settings.rebirthDifficulty >= difficulty.sadistic)
                num1 /= Character.bloodMagicController.bloodMagics[id].sadisticDivider();
            var num2 = num1 * Character.bloodMagicController.bloodMagics[id].totalBloodMagicSpeedBonus();
            if (num2 <= -3.40282346638529E+38)
                num2 = 0.0;
            if (num2 >= 3.40282346638529E+38)
                num2 = 3.40282346638529E+38;
            return (float)num2;
        }

        public float RitualTimeLeft(int id, long remaining)
        {
            return (float)((1.0 - Character.bloodMagic.ritual[id].progress) /
                           RitualProgressPerTick(id, remaining) / 50.0);
        }

        private long CalculateMaxAllocation(int id, long remaining)
        {
            var num1 = Character.bloodMagicController.bloodMagics[id].capValue();
            if (remaining > num1)
            {
                return num1;
            }

            var num2 = (long)((double)num1 / Math.Ceiling((double)num1 / (double)remaining)) + 1L;
            return num2;
        }
    }
}

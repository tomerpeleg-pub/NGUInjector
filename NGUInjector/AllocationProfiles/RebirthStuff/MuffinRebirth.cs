using NGUInjector.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NGUInjector.AllocationProfiles.RebirthStuff
{
    internal class MuffinRebirth : TimeRebirth
    {
        private double _24h = 60 * 60 * 24;
        private bool _shouldMuffin = true;
        private ConsumablesManager.Consumable _muffinConsumable;

        internal bool BalanceTime = false;
        internal double MuffinMinuteBuffer = 60;

        public MuffinRebirth()
        {
            RebirthTime = _24h;
            ConsumablesManager.Consumables.TryGetValue(ConsumablesManager.ConsumableType.MUFFIN, out _muffinConsumable);
        }

        internal override bool RebirthAvailable()
        {
            RebirthTime = _24h;
            _shouldMuffin = true;

            if (!Main.Settings.AutoRebirth || _muffinConsumable == null)
            {
                _shouldMuffin = false;
                return base.RebirthAvailable();
            }

            //Do 24 hour Rebirths if we have any current challenges, we havent yet unlocked 24hr muffins from TC 2, or if the 5 O'Clock Shadow Perk or Beast Fertilizer Quirk aren't maxed
            if (CharObj.challenges.inChallenge || AnyChallengesValid() ||
                Main.Character.allChallenges.trollChallenge.sadisticCompletions() < 2 ||
                Main.Character.adventure.itopod.perkLevel[21] < Main.Character.adventureController.itopod.maxLevel[21] || Main.Character.beastQuest.quirkLevel[13] < Main.Character.beastQuestPerkController.maxLevel[13])
            {
                _shouldMuffin = false;
            }

            //Do 24 hour Rebirths if we don't have any muffins and aren't configured to purchase more or don't have the AP to purchase more
            if (_muffinConsumable.GetCount() == 0 && (!Main.Settings.AutoBuyConsumables || !_muffinConsumable.HasEnoughAP(1, out _)))
            {
                _shouldMuffin = false;
            }

            //Cycle between 24 and 23 hours  (24h -> Activate Muffin if possible -> Rebirth -> 23h -> Rebirth)
            if (_shouldMuffin)
            {
                double longTime = _24h + (BalanceTime ? (MuffinMinuteBuffer * 60) : 0);
                double shortTime = _24h - (MuffinMinuteBuffer * 60);

                bool muffinIsActive = (_muffinConsumable.GetIsActive() ?? false);
                double muffinTimeLeft = (_muffinConsumable.GetTimeLeft() ?? 0);

                if (muffinTimeLeft > 0 && !muffinIsActive)
                {
                    RebirthTime = shortTime;
                }
                else
                {
                    RebirthTime = longTime;
                }
            }

            return base.RebirthAvailable();
        }

        protected override bool PreRebirth()
        {
            if (base.PreRebirth())
                return true;

            if (_muffinConsumable == null)
            {
                return false;
            }

            //Try to eat a muffin if it has completely expired
            if (_shouldMuffin && _muffinConsumable != null && !(_muffinConsumable.GetIsActive() ?? false) && (_muffinConsumable.GetTimeLeft() ?? 0) <= 0)
            {
                if (_muffinConsumable.GetCount() <= 0)
                {
                    if (Main.Settings.AutoBuyConsumables)
                    {
                        _muffinConsumable.Buy(1, out _);
                    }
                    else
                    {
                        Main.Log("No muffins available for rebirth and auto-purchase consumables disabled");
                    }
                }

                if (_muffinConsumable.GetCount() > 0)
                {
                    _muffinConsumable.Use(1);
                }
            }

            return false;
        }
    }
}

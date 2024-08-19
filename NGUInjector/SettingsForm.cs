using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using NGUInjector.AllocationProfiles;
using NGUInjector.Managers;

namespace NGUInjector
{
    public partial class SettingsForm : Form
    {
        private class ItemControlGroup
        {
            public ListBox ItemList { get; }
            public NumericUpDown ItemBox { get; }
            public ErrorProvider ErrorProvider { get; }
            public Label ItemLabel { get; }

            public Func<int[]> GetSettings { get; }
            public Action<int[]> SaveSettings { get; }

            public int MinVal { get; }
            public int MaxVal { get; }

            public bool CheckIsEquipment { get; }
            public Func<int, string> GetDisplayName { get; }


            public ItemControlGroup(ListBox itemList, NumericUpDown itemBox, ErrorProvider errorProvider, Label itemLabel, Func<int[]> getSettings, Action<int[]> saveSettings)
            {
                ItemList = itemList;
                ItemBox = itemBox;
                ErrorProvider = errorProvider;
                if (ItemList != null)
                {
                    ErrorProvider.SetIconAlignment(ItemList, ErrorIconAlignment.BottomRight);
                }
                ItemLabel = itemLabel;

                GetSettings = getSettings;
                SaveSettings = saveSettings;

                MinVal = 1;
                MaxVal = Consts.MAX_GEAR_ID;

                CheckIsEquipment = true;
                GetDisplayName = (id) => Main.Character.itemInfo.itemName[id];
            }
            public ItemControlGroup(ListBox itemList, NumericUpDown itemBox, ErrorProvider errorProvider, Label itemLabel, Func<int[]> getSettings, Action<int[]> saveSettings, int minVal, int maxVal, bool checkIsEquipment, Func<int, string> getDisplayName)
                : this(itemList, itemBox, errorProvider, itemLabel, getSettings, saveSettings)
            {
                MinVal = minVal;
                MaxVal = maxVal;

                CheckIsEquipment = checkIsEquipment;
                GetDisplayName = getDisplayName;
            }

            public void ClearError()
            {
                SetError("");
            }

            public void SetError(string message)
            {
                if (ErrorProvider != null)
                {
                    if (ItemList != null)
                    {
                        ErrorProvider.SetError(ItemList, message);
                    }
                    else
                    {
                        ErrorProvider.SetError(ItemBox, message);
                    }
                }
            }
        }

        internal class VersionedTitan
        {
            public int Zone { get; private set; }
            public string Name { get; private set; }
            public Func<bool> IsUnlocked { get; private set; }
            public Func<int> GetKills { get; private set; }
            public Func<int> GetVersion { get; private set; }
            public Action<int> SetVersion { get; private set; }

            public VersionedTitan(int zone, string name, int bossId)
            {
                Zone = zone;
                Name = name;

                var adv = Main.Character.adventure;

                var unlockField = adv.GetType().GetField($"titan{bossId}Unlocked",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var bossKillsField = adv.GetType().GetField($"boss{bossId}Kills",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var versionField = adv.GetType().GetField($"titan{bossId}Version",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                IsUnlocked = () => 
                {
                    var unlockObj = unlockField?.GetValue(adv);
                    if (unlockObj == null)
                        return false;
                    return (bool)unlockObj;
                };

                GetKills = () =>
                {
                    var bossKillsObj = bossKillsField?.GetValue(adv);
                    if (bossKillsObj == null)
                        return 0;

                    return (int)bossKillsObj;
                };

                GetVersion = () =>
                {
                    var versionObj = versionField?.GetValue(adv);
                    if (versionObj == null)
                        return 0;
                    return (int)versionObj;
                };
                SetVersion = (v) =>
                {
                    versionField?.SetValue(adv, v);
                };
            }

            public override string ToString()
            {
                return $"Zone:{Zone} | Name:{Name} | Unlocked?{IsUnlocked()} | Kills:{GetKills()} | Version:{GetVersion()}";
            }
        }

        internal readonly Dictionary<int, string> CubePriorityList;
        internal readonly Dictionary<int, string> CombatModeList;

        internal readonly Dictionary<int, string> TitanVersionList;
        internal readonly List<VersionedTitan> VersionedTitanList;

        internal readonly Dictionary<int, string> ZoneList;
        internal readonly Dictionary<int, string> SpriteEnemyList;
        private bool _initializing = true;

        private ItemControlGroup _yggControls;
        private ItemControlGroup _priorityControls;
        private ItemControlGroup _blacklistControls;
        private ItemControlGroup _titanControls;
        private ItemControlGroup _goldControls;
        private ItemControlGroup _questControls;
        private ItemControlGroup _wishControls;
        private ItemControlGroup _wishBlacklistControls;
        private ItemControlGroup _pitControls;
        private ItemControlGroup _cookingControls;

        //TODO: Implement missing manual features:
        //      NEW Tab: Cooking (ManageCooking flag, ManageCookingLoadout flag Cooking gear loadout)
        //DONE:
        //      General Tab: AutoBuyAdv flag, ManageConsumables flag, AutoBuyConsumables flag
        //      Inventory Tab: BoostPriority listbox with arrows to sort
        //      Wishes Tab: WishBlacklist listbox, BlacklistWish button, RemoveBlacklistedWish button
        //      Cards Tab: SortCards flag, CardSortOptions listbox, CardSortOrder listbox with arrows to move items between the listboxes and up/down in the order listbox
        //                 > Also Add ASC/DESC versions of everything but the Type:xxxx options
        public SettingsForm()
        {
            try
            {
                InitializeComponent();

                // Populate our data sources
                CubePriorityList = new Dictionary<int, string> { { 0, "None" }, { 1, "Balanced" }, { 2, "Softcap" }, { 3, "Power" }, { 4, "Toughness" } };

                CombatModeList = new Dictionary<int, string> { { 0, "Manual" }, { 1, "Idle" } };

                TitanVersionList = new Dictionary<int, string> { { 0, "Easy" }, { 1, "Normal" }, { 2, "Hard" }, { 3, "Brutal" } };

                VersionedTitanList = new List<VersionedTitan>
                {
                    { new VersionedTitan(19, ZoneHelpers.ZoneList[19], 6) },
                    { new VersionedTitan(23, ZoneHelpers.ZoneList[23], 7) },
                    { new VersionedTitan(26, ZoneHelpers.ZoneList[26], 8) },
                    { new VersionedTitan(30, ZoneHelpers.ZoneList[30], 9) },
                    { new VersionedTitan(34, ZoneHelpers.ZoneList[34], 10) },
                    { new VersionedTitan(38, ZoneHelpers.ZoneList[38], 11) },
                    { new VersionedTitan(42, ZoneHelpers.ZoneList[42], 12) }
                };

                ZoneList = new Dictionary<int, string>(ZoneHelpers.ZoneList);

                SpriteEnemyList = new Dictionary<int, string>();
                foreach (var x in Main.Character.adventureController.enemyList)
                {
                    foreach (var enemy in x)
                    {
                        try
                        {
                            SpriteEnemyList.Add(enemy.spriteID, enemy.name);
                        }
                        catch
                        {
                            //pass
                        }
                    }
                }

                List<string> cardRarities = Enum.GetNames(typeof(rarity)).ToList();
                string[] cardBonusTypes = Enum.GetNames(typeof(cardBonus)).Where(x => x != "none").ToArray();
                var cardSortOptions = new List<string> { "RARITY", "TIER", "COST", "PROTECTED", "CHANGE", "VALUE", "NORMALVALUE" };
                foreach (string sortOption in cardSortOptions)
                {
                    CardSortOptions.Items.Add(sortOption);
                    CardSortOptions.Items.Add($"{sortOption}-ASC");
                }
                foreach (string bonus in cardBonusTypes)
                {
                    CardSortOptions.Items.Add($"TYPE:{bonus}");
                    CardSortOptions.Items.Add($"TYPE-ASC:{bonus}");
                }

                cardRarities.Insert(0, "Don't trash");
                TrashQuality.Items.AddRange(cardRarities.ToArray());

                object[] arr = new object[31];
                for (int i = 0; i < arr.Length; i++) arr[i] = i;
                TrashTier.Items.AddRange(arr);

                DontCastSelection.Items.AddRange(cardBonusTypes);
                DontCastSelection.SelectedIndex = 0;

                CubePriority.DataSource = new BindingSource(CubePriorityList, null);
                CubePriority.ValueMember = "Key";
                CubePriority.DisplayMember = "Value";

                CombatMode.DataSource = new BindingSource(CombatModeList, null);
                CombatMode.ValueMember = "Key";
                CombatMode.DisplayMember = "Value";

                TitanVersions.DataSource = new BindingSource(TitanVersionList, null);
                TitanVersions.ValueMember = "Key";
                TitanVersions.DisplayMember = "Value";

                TitansWithVersion.DataSource = new BindingSource(VersionedTitanList, null);
                TitansWithVersion.ValueMember = "Zone";
                TitansWithVersion.DisplayMember = "Name";

                for (int i = TitansWithVersion.Items.Count - 1; i >= 0; i--)
                {
                    var titan = TitansWithVersion.Items[i] as VersionedTitan;
                    if (titan.IsUnlocked() && titan.GetKills() > 0)
                    {
                        TitansWithVersion.SelectedIndex = i;
                        TitanVersions.SelectedIndex = titan.GetVersion();
                        break;
                    }
                }

                TitanCombatMode.DataSource = new BindingSource(CombatModeList, null);
                TitanCombatMode.ValueMember = "Key";
                TitanCombatMode.DisplayMember = "Value";

                ITOPODCombatMode.DataSource = new BindingSource(CombatModeList, null);
                ITOPODCombatMode.ValueMember = "Key";
                ITOPODCombatMode.DisplayMember = "Value";

                QuestCombatMode.DataSource = new BindingSource(CombatModeList, null);
                QuestCombatMode.ValueMember = "Key";
                QuestCombatMode.DisplayMember = "Value";

                CombatTargetZone.DataSource = new BindingSource(ZoneList, null);
                CombatTargetZone.ValueMember = "Key";
                CombatTargetZone.DisplayMember = "Value";

                //Remove ITOPOD for non combat zones
                ZoneList.Remove(1000);
                ZoneList.Remove(-1);

                EnemyBlacklistZone.ValueMember = "Key";
                EnemyBlacklistZone.DisplayMember = "Value";
                EnemyBlacklistZone.DataSource = new BindingSource(ZoneList.Where(x => !ZoneHelpers.TitanZones.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value), null);
                EnemyBlacklistZone.SelectedIndex = 0;

                numberErrProvider.SetIconAlignment(BloodNumberThreshold, ErrorIconAlignment.MiddleRight);
                moneyPitThresholdError.SetIconAlignment(MoneyPitThreshold, ErrorIconAlignment.MiddleRight);

                yggLoadoutItem.TextChanged += yggLoadoutItem_TextChanged;
                priorityBoostItemAdd.TextChanged += priorityBoostItemAdd_TextChanged;
                blacklistAddItem.TextChanged += blacklistAddItem_TextChanged;
                titanAddItem.TextChanged += titanAddItem_TextChanged;
                GoldItemBox.TextChanged += GoldItemBox_TextChanged;
                QuestLoadoutItem.TextChanged += QuestLoadoutBox_TextChanged;
                WishAddInput.TextChanged += WishAddInput_TextChanged;
                WishBlacklistAddInput.TextChanged += WishBlacklistAddInput_TextChanged;
                MoneyPitInput.TextChanged += MoneyPitInput_TextChanged;
                CookingLoadoutItem.TextChanged += CookingLoadoutBox_TextChanged;

                _yggControls = new ItemControlGroup(yggdrasilLoadoutBox, yggLoadoutItem, yggErrorProvider, yggItemLabel, () => Main.Settings.YggdrasilLoadout, (settings) => Main.Settings.YggdrasilLoadout = settings);
                _priorityControls = new ItemControlGroup(priorityBoostBox, priorityBoostItemAdd, invPrioErrorProvider, priorityBoostLabel, () => Main.Settings.PriorityBoosts, (settings) => Main.Settings.PriorityBoosts = settings);
                _blacklistControls = new ItemControlGroup(blacklistBox, blacklistAddItem, invBlacklistErrProvider, blacklistLabel, () => Main.Settings.BoostBlacklist, (settings) => Main.Settings.BoostBlacklist = settings);
                _titanControls = new ItemControlGroup(titanLoadout, titanAddItem, titanErrProvider, titanLabel, () => Main.Settings.TitanLoadout, (settings) => Main.Settings.TitanLoadout = settings);
                _goldControls = new ItemControlGroup(GoldLoadout, GoldItemBox, goldErrorProvider, GoldItemLabel, () => Main.Settings.GoldDropLoadout, (settings) => Main.Settings.GoldDropLoadout = settings);
                _questControls = new ItemControlGroup(questLoadoutBox, QuestLoadoutItem, questErrorProvider, questItemLabel, () => Main.Settings.QuestLoadout, (settings) => Main.Settings.QuestLoadout = settings);
                _wishControls = new ItemControlGroup(WishPriority, WishAddInput, wishErrorProvider, AddWishLabel, () => Main.Settings.WishPriorities, (settings) => Main.Settings.WishPriorities = settings, 0, Consts.MAX_WISH_ID, false, (id) => Main.Character.wishesController.properties[id].wishName);
                _wishBlacklistControls = new ItemControlGroup(WishBlacklist, WishBlacklistAddInput, wishBlacklistErrorProvider, AddWishBlacklistLabel, () => Main.Settings.WishBlacklist, (settings) => Main.Settings.WishBlacklist = settings, 0, Consts.MAX_WISH_ID, false, (id) => Main.Character.wishesController.properties[id].wishName);
                _pitControls = new ItemControlGroup(MoneyPitLoadout, MoneyPitInput, moneyPitErrorProvider, MoneyPitLabel, () => Main.Settings.MoneyPitLoadout, (settings) => Main.Settings.MoneyPitLoadout = settings);
                _cookingControls = new ItemControlGroup(cookingLoadoutBox, CookingLoadoutItem, cookingErrorProvider, cookingItemLabel, () => Main.Settings.CookingLoadout, (settings) => Main.Settings.CookingLoadout = settings);

                TryItemBoxTextChanged(_yggControls, out _);
                TryItemBoxTextChanged(_priorityControls, out _);
                TryItemBoxTextChanged(_blacklistControls, out _);
                TryItemBoxTextChanged(_titanControls, out _);
                TryItemBoxTextChanged(_goldControls, out _);
                TryItemBoxTextChanged(_questControls, out _);
                TryItemBoxTextChanged(_wishControls, out _);
                TryItemBoxTextChanged(_wishBlacklistControls, out _);
                TryItemBoxTextChanged(_pitControls, out _);
                TryItemBoxTextChanged(_cookingControls, out _);

                UseTitanCombat_CheckedChanged(this, null);

                boostPrioUpButton.Text = char.ConvertFromUtf32(8593);
                boostPrioDownButton.Text = char.ConvertFromUtf32(8595);

                prioUpButton.Text = char.ConvertFromUtf32(8593);
                prioDownButton.Text = char.ConvertFromUtf32(8595);

                WishUpButton.Text = char.ConvertFromUtf32(8593);
                WishDownButton.Text = char.ConvertFromUtf32(8595);

                CardSortUp.Text = char.ConvertFromUtf32(8593);
                CardSortDown.Text = char.ConvertFromUtf32(8595);

                VersionLabel.Text = $"Version: {Main.Version}";
            }
            catch (Exception ex)
            {
                Main.LogDebug($"{ex.Message}:\n{ex.StackTrace}");
            }
        }

        internal void SetTitanGoldBox(SavedSettings newSettings)
        {
            TitanGoldTargets.Items.Clear();
            for (var i = 0; i < ZoneHelpers.TitanZones.Length; i++)
            {
                var text =
                    $"{ZoneList[ZoneHelpers.TitanZones[i]]}";
                if (newSettings.TitanGoldTargets[i])
                {
                    text = $"{text} ({(newSettings.TitanMoneyDone[i] ? "Done" : "Waiting")})";
                }
                var item = new ListViewItem
                {
                    Tag = i,
                    Checked = newSettings.TitanGoldTargets[i],
                    Text = text,
                    BackColor = newSettings.TitanGoldTargets[i]
                        ? newSettings.TitanMoneyDone[i] ? Color.LightGreen : Color.Yellow
                        : Color.White
                };
                TitanGoldTargets.Items.Add(item);
            }

            TitanGoldTargets.Columns[0].Width = -1;
        }

        internal void SetTitanSwapBox(SavedSettings newSettings)
        {
            TitanSwapTargets.Items.Clear();
            for (var i = 0; i < ZoneHelpers.TitanZones.Length; i++)
            {
                var text =
                    $"{ZoneList[ZoneHelpers.TitanZones[i]]}";
                var item = new ListViewItem
                {
                    Tag = i,
                    Checked = newSettings.TitanSwapTargets[i],
                    Text = text,
                };
                TitanSwapTargets.Items.Add(item);
            }

            TitanSwapTargets.Columns[0].Width = TitanSwapTargets.Width - 4 - SystemInformation.VerticalScrollBarWidth;
        }

        internal void SetSnipeZone(ComboBox control, int setting)
        {
            control.SelectedIndex = setting >= 1000 ? 43 : setting + 1;
        }

        internal void UpdateFromSettings(SavedSettings newSettings)
        {
            _initializing = true;
            AutoDailySpin.Checked = newSettings.AutoSpin;
            AutoFightBosses.Checked = newSettings.AutoFight;
            AutoITOPOD.Checked = newSettings.AutoQuestITOPOD;
            AutoMoneyPit.Checked = newSettings.AutoMoneyPit;
            MoneyPitThreshold.Text = $"{newSettings.MoneyPitThreshold:#.##E+00}";
            ManageEnergy.Checked = newSettings.ManageEnergy;
            ManageMagic.Checked = newSettings.ManageMagic;
            ManageGear.Checked = newSettings.ManageGear;
            ManageDiggers.Checked = newSettings.ManageDiggers;
            ManageWandoos.Checked = newSettings.ManageWandoos;
            AutoRebirth.Checked = newSettings.AutoRebirth;
            ManageConsumables.Checked = newSettings.ManageConsumables;
            ManageYggdrasil.Checked = newSettings.ManageYggdrasil;
            YggdrasilSwap.Checked = newSettings.SwapYggdrasilLoadouts;
            ManageInventory.Checked = newSettings.ManageInventory;
            ManageBoostConvert.Checked = newSettings.AutoConvertBoosts;
            SwapTitanLoadout.Checked = newSettings.SwapTitanLoadouts;
            BossesOnly.Checked = newSettings.SnipeBossOnly;
            PrecastBuffs.Checked = newSettings.PrecastBuffs;
            RecoverHealth.Checked = newSettings.RecoverHealth;
            FastCombat.Checked = newSettings.FastCombat;
            CombatMode.SelectedIndex = newSettings.CombatMode;
            SetSnipeZone(CombatTargetZone, newSettings.SnipeZone);
            AllowFallthrough.Checked = newSettings.AllowZoneFallback;
            QuestCombatMode.SelectedIndex = newSettings.QuestCombatMode;
            ManageQuests.Checked = newSettings.AutoQuest;
            ManageQuestLoadout.Checked = newSettings.ManageQuestLoadouts;
            AllowMajor.Checked = newSettings.AllowMajorQuests;
            AbandonMinors.Checked = newSettings.AbandonMinors;
            AbandonMinorThreshold.Value = newSettings.MinorAbandonThreshold;
            QuestFastCombat.Checked = newSettings.QuestFastCombat;
            QuestBeastMode.Checked = newSettings.QuestBeastMode;
            QuestSmartBeastMode.Checked = newSettings.QuestSmartBeastMode;
            UseGoldLoadout.Checked = newSettings.DoGoldSwap;
            AutoSpellSwap.Checked = newSettings.AutoSpellSwap;
            SpaghettiCap.Value = newSettings.SpaghettiThreshold;
            CounterfeitCap.Value = newSettings.CounterfeitThreshold;
            AutoBuyAdv.Checked = newSettings.AutoBuyAdventure;
            AutoBuyEM.Checked = newSettings.AutoBuyEM;
            AutoBuyConsumables.Checked = newSettings.AutoBuyConsumables;
            ConsumeIfRunning.Checked = newSettings.ConsumeIfAlreadyRunning;
            MasterEnable.Checked = newSettings.GlobalEnabled;
            CombatActive.Checked = newSettings.CombatEnabled;
            ManualMinor.Checked = newSettings.ManualMinors;
            ButterMajors.Checked = newSettings.UseButterMajor;
            ManageR3.Checked = newSettings.ManageR3;
            ButterMinors.Checked = newSettings.UseButterMinor;
            ActivateFruits.Checked = newSettings.ActivateFruits;
            BeastMode.Checked = newSettings.BeastMode;
            SmartBeastMode.Checked = newSettings.SmartBeastMode;
            CubePriority.SelectedIndex = newSettings.CubePriority;
            BloodNumberThreshold.Text = $"{newSettings.BloodNumberThreshold:#.##E+00}";
            ManageNGUDiff.Checked = newSettings.ManageNGUDiff;
            ManageGoldLoadouts.Checked = newSettings.ManageGoldLoadouts;
            CBlockMode.Checked = newSettings.GoldCBlockMode;
            ResnipeInput.Value = newSettings.ResnipeTime;
            OptimizeITOPOD.Checked = newSettings.OptimizeITOPODFloor;
            TargetITOPOD.Checked = newSettings.AdventureTargetITOPOD;
            TargetTitans.Checked = newSettings.AdventureTargetTitans;

            UseTitanCombat.Checked = newSettings.UseTitanCombat;
            TitanCombatMode.SelectedIndex = newSettings.TitanCombatMode;
            TitanPrecastBuffs.Checked = newSettings.TitanPrecastBuffs;
            TitanRecoverHealth.Checked = newSettings.TitanRecoverHealth;
            TitanFastCombat.Checked = newSettings.TitanFastCombat;
            TitanBeastMode.Checked = newSettings.TitanBeastMode;
            TitanSmartBeastMode.Checked = newSettings.TitanSmartBeastMode;
            TitanMoreBlockParry.Checked = newSettings.TitanMoreBlockParry;

            ITOPODCombatMode.SelectedIndex = newSettings.ITOPODCombatMode;
            ITOPODRecoverHP.Checked = newSettings.ITOPODRecoverHP;
            ITOPODBeastMode.Checked = newSettings.ITOPODBeastMode;
            ITOPODSmartBeastMode.Checked = newSettings.ITOPODSmartBeastMode;
            ITOPODFastCombat.Checked = newSettings.ITOPODFastCombat;
            ITOPODPrecastBuffs.Checked = newSettings.ITOPODPrecastBuffs;

            DisableOverlay.Checked = newSettings.DisableOverlay;
            UpgradeDiggers.Checked = newSettings.UpgradeDiggers;
            CastBloodSpells.Checked = newSettings.CastBloodSpells;

            IronPillThreshold.Value = newSettings.IronPillThreshold;
            GuffAThreshold.Value = newSettings.BloodMacGuffinAThreshold;
            GuffBThreshold.Value = newSettings.BloodMacGuffinBThreshold;
            YggSwapThreshold.Value = newSettings.YggSwapThreshold;

            IronPillOnRebirth.Checked = newSettings.IronPillOnRebirth;
            GuffAOnRebirth.Checked = newSettings.BloodMacGuffinAOnRebirth;
            GuffBOnRebirth.Checked = newSettings.BloodMacGuffinBOnRebirth;

            MoreBlockParry.Checked = newSettings.MoreBlockParry;
            WishSortOrder.Checked = newSettings.WishSortOrder;
            WishSortPriorities.Checked = newSettings.WishSortPriorities;

            SetTitanGoldBox(newSettings);
            SetTitanSwapBox(newSettings);

            balanceMayo.Checked = newSettings.ManageMayo;
            TrashCards.Checked = newSettings.TrashCards;
            TrashQuality.SelectedIndex = newSettings.CardsTrashQuality;
            TrashTier.SelectedIndex = newSettings.TrashCardCost;
            TrashAdventureCards.Checked = newSettings.TrashAdventureCards;
            TrashProtectedCards.Checked = newSettings.TrashProtectedCards;
            AutoCastCards.Checked = newSettings.AutoCastCards;
            TrashChunkers.Checked = newSettings.TrashChunkers;
            SortCards.Checked = newSettings.CardSortEnabled;

            ManageCooking.Checked = newSettings.ManageCooking;
            ManageCookingLoadout.Checked = newSettings.ManageCookingLoadouts;

            if (newSettings.DontCastCardType != null)
            {
                DontCastList.Items.Clear();
                DontCastList.Items.AddRange(newSettings.DontCastCardType);
            }

            if (newSettings.CardSortOrder != null)
            {
                CardSortList.Items.Clear();
                CardSortList.Items.AddRange(newSettings.CardSortOrder);
            }

            var temp = newSettings.YggdrasilLoadout.ToDictionary(x => x, x => Main.Character.itemInfo.itemName[x]);
            if (temp.Count > 0)
            {
                yggdrasilLoadoutBox.DataSource = null;
                yggdrasilLoadoutBox.DataSource = new BindingSource(temp, null);
                yggdrasilLoadoutBox.ValueMember = "Key";
                yggdrasilLoadoutBox.DisplayMember = "Value";
            }
            else
            {
                yggdrasilLoadoutBox.Items.Clear();
            }

            boostPriorityList.Items.Clear();
            if (!newSettings.BoostPriority.Any())
            {
                string[] tempBoostPrio = { "Power", "Toughness", "Special" };
                newSettings.BoostPriority = tempBoostPrio;
            }
            foreach (string priority in newSettings.BoostPriority)
            {
                boostPriorityList.Items.Add(priority);
            }

            temp = newSettings.PriorityBoosts.ToDictionary(x => x, x => Main.Character.itemInfo.itemName[x]);
            if (temp.Count > 0)
            {
                priorityBoostBox.DataSource = null;
                priorityBoostBox.DataSource = new BindingSource(temp, null);
                priorityBoostBox.ValueMember = "Key";
                priorityBoostBox.DisplayMember = "Value";
            }
            else
            {
                priorityBoostBox.Items.Clear();
            }

            temp = newSettings.BoostBlacklist.ToDictionary(x => x, x => Main.Character.itemInfo.itemName[x]);
            if (temp.Count > 0)
            {
                blacklistBox.DataSource = null;
                blacklistBox.DataSource = new BindingSource(temp, null);
                blacklistBox.ValueMember = "Key";
                blacklistBox.DisplayMember = "Value";
            }
            else
            {
                blacklistBox.Items.Clear();
            }

            temp = newSettings.TitanLoadout.ToDictionary(x => x, x => Main.Character.itemInfo.itemName[x]);
            if (temp.Count > 0)
            {
                titanLoadout.DataSource = null;
                titanLoadout.DataSource = new BindingSource(temp, null);
                titanLoadout.ValueMember = "Key";
                titanLoadout.DisplayMember = "Value";
            }
            else
            {
                titanLoadout.Items.Clear();
            }

            temp = newSettings.GoldDropLoadout.ToDictionary(x => x, x => Main.Character.itemInfo.itemName[x]);
            if (temp.Count > 0)
            {
                GoldLoadout.DataSource = null;
                GoldLoadout.DataSource = new BindingSource(temp, null);
                GoldLoadout.ValueMember = "Key";
                GoldLoadout.DisplayMember = "Value";
            }
            else
            {
                GoldLoadout.Items.Clear();
            }

            temp = newSettings.QuestLoadout.ToDictionary(x => x, x => Main.Character.itemInfo.itemName[x]);
            if (temp.Count > 0)
            {
                questLoadoutBox.DataSource = null;
                questLoadoutBox.DataSource = new BindingSource(temp, null);
                questLoadoutBox.ValueMember = "Key";
                questLoadoutBox.DisplayMember = "Value";
            }
            else
            {
                questLoadoutBox.Items.Clear();
            }

            temp = newSettings.MoneyPitLoadout.ToDictionary(x => x, x => Main.Character.itemInfo.itemName[x]);
            if (temp.Count > 0)
            {
                MoneyPitLoadout.DataSource = null;
                MoneyPitLoadout.DataSource = new BindingSource(temp, null);
                MoneyPitLoadout.ValueMember = "Key";
                MoneyPitLoadout.DisplayMember = "Value";
            }
            else
            {
                MoneyPitLoadout.Items.Clear();
            }

            temp = newSettings.WishPriorities.ToDictionary(x => x, x => Main.Character.wishesController.properties[x].wishName);
            if (temp.Count > 0)
            {
                WishPriority.DataSource = null;
                WishPriority.DataSource = new BindingSource(temp, null);
                WishPriority.ValueMember = "Key";
                WishPriority.DisplayMember = "Value";
            }
            else
            {
                WishPriority.Items.Clear();
            }

            temp = newSettings.WishBlacklist.ToDictionary(x => x, x => Main.Character.wishesController.properties[x].wishName);
            if (temp.Count > 0)
            {
                WishBlacklist.DataSource = null;
                WishBlacklist.DataSource = new BindingSource(temp, null);
                WishBlacklist.ValueMember = "Key";
                WishBlacklist.DisplayMember = "Value";
            }
            else
            {
                WishBlacklist.Items.Clear();
            }

            temp = newSettings.BlacklistedBosses.ToDictionary(x => x, x => SpriteEnemyList[x]);
            if (temp.Count > 0)
            {
                BlacklistedBosses.DataSource = null;
                BlacklistedBosses.DataSource = new BindingSource(temp, null);
                BlacklistedBosses.ValueMember = "Key";
                BlacklistedBosses.DisplayMember = "Value";
            }
            else
            {
                BlacklistedBosses.Items.Clear();
            }

            temp = newSettings.CookingLoadout.ToDictionary(x => x, x => Main.Character.itemInfo.itemName[x]);
            if (temp.Count > 0)
            {
                cookingLoadoutBox.DataSource = null;
                cookingLoadoutBox.DataSource = new BindingSource(temp, null);
                cookingLoadoutBox.ValueMember = "Key";
                cookingLoadoutBox.DisplayMember = "Value";
            }
            else
            {
                cookingLoadoutBox.Items.Clear();
            }

            Refresh();
            _initializing = false;
        }

        private bool TryGetValueFromNumericUpDown(NumericUpDown upDown, out int val)
        {
            return int.TryParse(upDown.Controls[0].Text, out val);
        }

        private bool TryItemBoxTextChanged(ItemControlGroup controls, out int val)
        {
            controls.ClearError();

            if (!TryGetValueFromNumericUpDown(controls.ItemBox, out val) || val < controls.MinVal || val > controls.MaxVal)
            {
                controls.ItemLabel.Text = "";
                return false;
            }

            var itemName = controls.GetDisplayName(val).Replace("<b><color=blue>[QUEST ITEM]</color></b>", "[QUEST ITEM]");
            bool isValid = true;

            if (controls.CheckIsEquipment)
            {
                var itemType = Main.Character.itemInfo.type[val];
                isValid = itemType == part.Head || itemType == part.Chest || itemType == part.Legs || itemType == part.Boots || itemType == part.Weapon || itemType == part.Accessory;
                if (!isValid)
                {
                    itemName += " (UNEQUIPPABLE)";
                }
            }
            controls.ItemLabel.Text = itemName;

            return isValid;
        }

        private void ItemBoxKeyDown(KeyEventArgs e, ItemControlGroup controls)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ItemListAdd(controls);
            }
        }

        private void ItemListAdd(ItemControlGroup controls)
        {
            controls.ClearError();

            if (!TryItemBoxTextChanged(controls, out int val))
            {
                controls.SetError("Invalid item id");
                return;
            }

            int[] currentSettings = controls.GetSettings();
            if (currentSettings.Contains(val))
            {
                return;
            }

            var temp = currentSettings.ToList();
            temp.Add(val);
            controls.SaveSettings(temp.ToArray());
        }

        private void ItemListRemove(ItemControlGroup controls)
        {
            controls.ClearError();

            var item = controls.ItemList.SelectedItem;
            if (item == null)
            {
                return;
            }

            var id = (KeyValuePair<int, string>)item;

            var temp = controls.GetSettings().ToList();
            temp.RemoveAll(x => x == id.Key);
            controls.SaveSettings(temp.ToArray());
        }

        private void ItemListUp(ItemControlGroup controls)
        {
            controls.ClearError();

            ItemListUp(controls.ItemList, controls.GetSettings(), controls.SaveSettings);

            //var index = controls.ItemList.SelectedIndex;
            //if (index == -1 || index == 0)
            //{
            //    return;
            //}

            //var temp = controls.GetSettings().ToList();
            //var item = temp[index];
            //temp.RemoveAt(index);
            //temp.Insert(index - 1, item);
            //controls.SaveSettings(temp.ToArray());

            //controls.ItemList.SelectedIndex = index - 1;
        }

        private void ItemListUp<T>(ListBox itemList, T[] settings, Action<T[]> saveSettings)
        {
            var index = itemList.SelectedIndex;
            if (index == -1 || index == 0)
            {
                return;
            }

            var temp = settings.ToList();
            var item = temp[index];
            temp.RemoveAt(index);
            temp.Insert(index - 1, item);
            saveSettings(temp.ToArray());

            itemList.SelectedIndex = index - 1;
        }

        private void ItemListDown(ItemControlGroup controls)
        {
            controls.ClearError();

            ItemListDown(controls.ItemList, controls.GetSettings(), controls.SaveSettings);

            //var index = controls.ItemList.SelectedIndex;
            //if (index == -1)
            //{
            //    return;
            //}

            //var temp = controls.GetSettings().ToList();
            //if (index == temp.Count - 1)
            //{
            //    return;
            //}

            //var item = temp[index];
            //temp.RemoveAt(index);
            //temp.Insert(index + 1, item);
            //controls.SaveSettings(temp.ToArray());

            //controls.ItemList.SelectedIndex = index + 1;
        }

        private void ItemListDown<T>(ListBox itemList, T[] settings, Action<T[]> saveSettings)
        {
            var index = itemList.SelectedIndex;
            if (index == -1)
            {
                return;
            }

            var temp = settings.ToList();
            if (index == temp.Count - 1)
            {
                return;
            }

            var item = temp[index];
            temp.RemoveAt(index);
            temp.Insert(index + 1, item);
            saveSettings(temp.ToArray());

            itemList.SelectedIndex = index + 1;
        }

        internal void UpdateProfileList(string[] profileList, string selectedProfile)
        {
            AllocationProfileFile.DataSource = null;
            AllocationProfileFile.DataSource = new BindingSource(profileList, null);
            AllocationProfileFile.SelectedItem = selectedProfile;
        }

        internal void UpdateProgressBar(int progress)
        {
            if (progress < 0)
                return;
            progressBar1.Value = progress;
        }

        private void MasterEnable_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.GlobalEnabled = MasterEnable.Checked;
        }

        private void AutoDailySpin_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AutoSpin = AutoDailySpin.Checked;
        }

        private void AutoMoneyPit_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AutoMoneyPit = AutoMoneyPit.Checked;
        }

        private void AutoITOPOD_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AutoQuestITOPOD = AutoITOPOD.Checked;
        }

        private void AutoFightBosses_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AutoFight = AutoFightBosses.Checked;
        }

        private void MoneyPitThresholdSave_Click(object sender, EventArgs e)
        {
            var newVal = MoneyPitThreshold.Text;
            if (double.TryParse(newVal, out var saved))
            {
                if (saved < 0)
                {
                    moneyPitThresholdError.SetError(MoneyPitThreshold, "Not a valid value");
                    return;
                }
                Main.Settings.MoneyPitThreshold = saved;
            }
            else
            {
                moneyPitThresholdError.SetError(MoneyPitThreshold, "Not a valid value");
            }
        }

        private void MoneyPitThreshold_TextChanged(object sender, EventArgs e)
        {
            moneyPitThresholdError.SetError(MoneyPitThreshold, "");
        }

        private void ManageEnergy_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ManageEnergy = ManageEnergy.Checked;
        }

        private void ManageMagic_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ManageMagic = ManageMagic.Checked;
        }

        private void ManageGear_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ManageGear = ManageGear.Checked;
        }

        private void ManageDiggers_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ManageDiggers = ManageDiggers.Checked;
        }

        private void ManageWandoos_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ManageWandoos = ManageWandoos.Checked;
        }

        private void AutoRebirth_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AutoRebirth = AutoRebirth.Checked;
        }

        private void ManageYggdrasil_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ManageYggdrasil = ManageYggdrasil.Checked;
        }

        private void YggdrasilSwap_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.SwapYggdrasilLoadouts = YggdrasilSwap.Checked;
        }

        private void yggLoadoutItem_TextChanged(object sender, EventArgs e)
        {
            TryItemBoxTextChanged(_yggControls, out _);
        }

        private void yggLoadoutItem_KeyDown(object sender, KeyEventArgs e)
        {
            ItemBoxKeyDown(e, _yggControls);
        }

        private void yggAddButton_Click(object sender, EventArgs e)
        {
            ItemListAdd(_yggControls);
        }

        private void yggRemoveButton_Click(object sender, EventArgs e)
        {
            ItemListRemove(_yggControls);
        }

        private void ManageInventory_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ManageInventory = ManageInventory.Checked;
        }

        private void ManageBoostConvert_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AutoConvertBoosts = ManageBoostConvert.Checked;
        }

        private void boostPrioUpButton_Click(object sender, EventArgs e)
        {
            ItemListUp(boostPriorityList, Main.Settings.BoostPriority, (settings) => Main.Settings.BoostPriority = settings);
        }

        private void boostPrioDownButton_Click(object sender, EventArgs e)
        {
            ItemListDown(boostPriorityList, Main.Settings.BoostPriority, (settings) => Main.Settings.BoostPriority = settings);
        }

        private void priorityBoostItemAdd_TextChanged(object sender, EventArgs e)
        {
            TryItemBoxTextChanged(_priorityControls, out _);
        }

        private void priorityBoostItemAdd_KeyDown(object sender, KeyEventArgs e)
        {
            ItemBoxKeyDown(e, _priorityControls);
        }

        private void priorityBoostAdd_Click(object sender, EventArgs e)
        {
            ItemListAdd(_priorityControls);
        }

        private void priorityBoostRemove_Click(object sender, EventArgs e)
        {
            ItemListRemove(_priorityControls);
        }

        private void prioUpButton_Click(object sender, EventArgs e)
        {
            ItemListUp(_priorityControls);
        }

        private void prioDownButton_Click(object sender, EventArgs e)
        {
            ItemListDown(_priorityControls);
        }

        private void blacklistAddItem_TextChanged(object sender, EventArgs e)
        {
            TryItemBoxTextChanged(_blacklistControls, out _);
        }

        private void blacklistAddItem_KeyDown(object sender, KeyEventArgs e)
        {
            ItemBoxKeyDown(e, _blacklistControls);
        }

        private void blacklistAdd_Click(object sender, EventArgs e)
        {
            ItemListAdd(_blacklistControls);
        }

        private void blacklistRemove_Click(object sender, EventArgs e)
        {
            ItemListRemove(_blacklistControls);
        }

        private void SwapTitanLoadout_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.SwapTitanLoadouts = SwapTitanLoadout.Checked;
        }

        private void ManageQuestLoadout_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ManageQuestLoadouts = ManageQuestLoadout.Checked;
        }

        private void titanAddItem_TextChanged(object sender, EventArgs e)
        {
            TryItemBoxTextChanged(_titanControls, out _);
        }

        private void titanAddItem_KeyDown(object sender, KeyEventArgs e)
        {
            ItemBoxKeyDown(e, _titanControls);
        }

        private void titanAdd_Click(object sender, EventArgs e)
        {
            ItemListAdd(_titanControls);
        }

        private void titanRemove_Click(object sender, EventArgs e)
        {
            ItemListRemove(_titanControls);
        }

        private void CombatActive_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.CombatEnabled = CombatActive.Checked;
        }

        private void BossesOnly_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.SnipeBossOnly = BossesOnly.Checked;
        }

        private void PrecastBuffs_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.PrecastBuffs = PrecastBuffs.Checked;
        }

        private void RecoverHealth_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.RecoverHealth = RecoverHealth.Checked;
        }

        private void FastCombat_CheckedChanged(object sender, EventArgs e)
        {
            SmartBeastMode.Enabled = CombatMode.SelectedIndex == 0 && !FastCombat.Checked;
            if (_initializing) return;
            Main.Settings.FastCombat = FastCombat.Checked;
        }

        private void CombatMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected = CombatMode.SelectedIndex;

            bool isManualMode = selected == 0;
            PrecastBuffs.Enabled = isManualMode;
            FastCombat.Enabled = isManualMode;
            MoreBlockParry.Enabled = isManualMode;
            SmartBeastMode.Enabled = isManualMode && !FastCombat.Checked;

            if (_initializing) return;
            Main.Settings.CombatMode = selected;
        }

        private void CombatTargetZone_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            var selected = CombatTargetZone.SelectedItem;
            var item = (KeyValuePair<int, string>)selected;
            Main.Settings.SnipeZone = item.Key;
        }

        private void AllowFallthrough_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AllowZoneFallback = AllowFallthrough.Checked;
        }

        private void UseGoldLoadout_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.DoGoldSwap = UseGoldLoadout.Checked;
        }

        private void GoldItemBox_TextChanged(object sender, EventArgs e)
        {
            TryItemBoxTextChanged(_goldControls, out _);
        }

        private void GoldItemBox_KeyDown(object sender, KeyEventArgs e)
        {
            ItemBoxKeyDown(e, _goldControls);
        }

        private void GoldLoadoutAdd_Click(object sender, EventArgs e)
        {
            ItemListAdd(_goldControls);
        }

        private void GoldLoadoutRemove_Click(object sender, EventArgs e)
        {
            ItemListRemove(_goldControls);
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        private void ManageQuests_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AutoQuest = ManageQuests.Checked;
        }

        private void AllowMajor_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AllowMajorQuests = AllowMajor.Checked;
        }

        private void AbandonMinors_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AbandonMinors = AbandonMinors.Checked;
        }

        private void AbandonMinorThreshold_ValueChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            if (TryGetValueFromNumericUpDown(AbandonMinorThreshold, out int val))
            {
                Main.Settings.MinorAbandonThreshold = val;
            }
        }

        private void QuestFastCombat_CheckedChanged(object sender, EventArgs e)
        {
            QuestSmartBeastMode.Enabled = QuestCombatMode.SelectedIndex == 0 && !QuestFastCombat.Checked;
            if (_initializing) return;
            Main.Settings.QuestFastCombat = QuestFastCombat.Checked;
        }

        private void QuestBeastMode_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.QuestBeastMode = QuestBeastMode.Checked;
            if (QuestBeastMode.Checked)
            {
                Main.Settings.QuestSmartBeastMode = QuestSmartBeastMode.Checked = false;
            }
        }

        private void QuestSmartBeastMode_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.QuestSmartBeastMode = QuestSmartBeastMode.Checked;
            if (QuestSmartBeastMode.Checked)
            {
                Main.Settings.QuestBeastMode = QuestBeastMode.Checked = false;
            }
        }

        private void QuestLoadoutBox_TextChanged(object sender, EventArgs e)
        {
            TryItemBoxTextChanged(_questControls, out _);
        }

        private void QuestLoadoutItem_KeyDown(object sender, KeyEventArgs e)
        {
            ItemBoxKeyDown(e, _questControls);
        }

        private void questAddButton_Click(object sender, EventArgs e)
        {
            ItemListAdd(_questControls);
        }

        private void questRemoveButton_Click(object sender, EventArgs e)
        {
            ItemListRemove(_questControls);
        }

        private void AutoSpellSwap_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AutoSpellSwap = AutoSpellSwap.Checked;
        }

        private void SaveSpellCapButton_Click(object sender, EventArgs e)
        {
            if (_initializing) return;

            int val;
            if (TryGetValueFromNumericUpDown(SpaghettiCap, out val))
            {
                Main.Settings.SpaghettiThreshold = val;
            }
            if (TryGetValueFromNumericUpDown(CounterfeitCap, out val))
            {
                Main.Settings.CounterfeitThreshold = val;
            }
            if (TryGetValueFromNumericUpDown(IronPillThreshold, out val))
            {
                Main.Settings.IronPillThreshold = val;
            }
            if (TryGetValueFromNumericUpDown(GuffAThreshold, out val))
            {
                Main.Settings.BloodMacGuffinAThreshold = val;
            }
            if (TryGetValueFromNumericUpDown(GuffBThreshold, out val))
            {
                Main.Settings.BloodMacGuffinBThreshold = val;
            }

            var newVal = BloodNumberThreshold.Text;
            if (double.TryParse(newVal, out var saved))
            {
                if (saved < 0)
                {
                    numberErrProvider.SetError(BloodNumberThreshold, "Not a valid value");
                    return;
                }
                Main.Settings.BloodNumberThreshold = saved;
            }
            else
            {
                numberErrProvider.SetError(BloodNumberThreshold, "Not a valid value");
            }
        }

        private void BloodNumberThreshold_TextChanged(object sender, EventArgs e)
        {
            numberErrProvider.SetError(BloodNumberThreshold, "");
        }

        private void TestButton_Click(object sender, EventArgs e)
        {
            //var c = Main.Character;
            //for (var i = 0; i <= 13; i++)
            //{
            //    CustomAllocation.
            //}
        }

        private void AutoBuyEM_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AutoBuyEM = AutoBuyEM.Checked;
        }

        private void IdleMinor_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ManualMinors = ManualMinor.Checked;
        }

        private void UseButter_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.UseButterMajor = ButterMajors.Checked;
        }

        private void ManageR3_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ManageR3 = ManageR3.Checked;
        }

        private void ButterMinors_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.UseButterMinor = ButterMinors.Checked;
        }

        private void ActivateFruits_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ActivateFruits = ActivateFruits.Checked;
        }

        private void WishAddInput_TextChanged(object sender, EventArgs e)
        {
            TryItemBoxTextChanged(_wishControls, out _);
        }

        private void WishAddInput_KeyDown(object sender, KeyEventArgs e)
        {
            ItemBoxKeyDown(e, _wishControls);
        }

        private void AddWishButton_Click(object sender, EventArgs e)
        {
            ItemListAdd(_wishControls);
        }

        private void RemoveWishButton_Click(object sender, EventArgs e)
        {
            ItemListRemove(_wishControls);
        }

        private void WishUpButton_Click(object sender, EventArgs e)
        {
            ItemListUp(_wishControls);
        }

        private void WishDownButton_Click(object sender, EventArgs e)
        {
            ItemListDown(_wishControls);
        }

        private void WishBlacklistAddInput_TextChanged(object sender, EventArgs e)
        {
            TryItemBoxTextChanged(_wishBlacklistControls, out _);
        }

        private void WishBlacklistAddInput_KeyDown(object sender, KeyEventArgs e)
        {
            ItemBoxKeyDown(e, _wishBlacklistControls);
        }

        private void AddWishBlacklistButton_Click(object sender, EventArgs e)
        {
            ItemListAdd(_wishBlacklistControls);
        }

        private void RemoveWishBlacklistButton_Click(object sender, EventArgs e)
        {
            ItemListRemove(_wishBlacklistControls);
        }

        private void BeastMode_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.BeastMode = BeastMode.Checked;
            if (BeastMode.Checked)
            {
                Main.Settings.SmartBeastMode = SmartBeastMode.Checked = false;
            }
        }

        private void SmartBeastMode_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.SmartBeastMode = SmartBeastMode.Checked;
            if (SmartBeastMode.Checked)
            {
                Main.Settings.BeastMode = BeastMode.Checked = false;
            }
        }

        private void CubePriority_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.CubePriority = CubePriority.SelectedIndex;
        }

        private void ManageNGUDiff_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ManageNGUDiff = ManageNGUDiff.Checked;
        }

        private void ChangeProfileFile_Click(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AllocationFile = AllocationProfileFile.SelectedItem.ToString();
            Main.LoadAllocation();
        }

        private void TitanGoldTargets_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_initializing) return;
            var temp = Main.Settings.TitanGoldTargets.ToArray();
            temp[(int)e.Item.Tag] = e.Item.Checked;
            Main.Settings.TitanGoldTargets = temp;
        }

        private void ResetTitanStatus_Click(object sender, EventArgs e)
        {
            if (_initializing) return;
            var temp = new bool[ZoneHelpers.TitanZones.Length];
            Main.Settings.TitanMoneyDone = temp;
        }

        private void ManageGoldLoadouts_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ManageGoldLoadouts = ManageGoldLoadouts.Checked;
        }

        private void SaveResnipeButton_Click(object sender, EventArgs e)
        {
            if (TryGetValueFromNumericUpDown(ResnipeInput, out int val))
            {
                Main.Settings.ResnipeTime = val;
            }
        }

        private void CBlockMode_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.GoldCBlockMode = CBlockMode.Checked;
        }

        private void HarvestSafety_CheckedChanged(object sender, EventArgs e)
        {
            HarvestAllButton.Enabled = HarvestSafety.Checked;
        }

        private void HarvestAllButton_Click(object sender, EventArgs e)
        {
            if (Main.Settings.SwapYggdrasilLoadouts && Main.Settings.YggdrasilLoadout.Length > 0 && YggdrasilManager.AnyHarvestable())
            {
                if (!LoadoutManager.TryYggdrasilSwap() || !DiggerManager.TryYggSwap())
                {
                    Main.Log("Unable to harvest now");
                    return;
                }

                YggdrasilManager.HarvestAll();
            }
        }

        private void OptimizeITOPOD_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.OptimizeITOPODFloor = OptimizeITOPOD.Checked;
        }

        private void TargetITOPOD_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AdventureTargetITOPOD = TargetITOPOD.Checked;
        }

        private void TargetTitans_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AdventureTargetTitans = TargetTitans.Checked;
        }

        private void TitanSwapTargets_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_initializing) return;
            var temp = Main.Settings.TitanSwapTargets.ToArray();
            temp[(int)e.Item.Tag] = e.Item.Checked;
            Main.Settings.TitanSwapTargets = temp;
        }

        private void UnloadSafety_CheckedChanged(object sender, EventArgs e)
        {
            UnloadButton.Enabled = UnloadSafety.Checked;
        }

        private void UnloadButton_Click(object sender, EventArgs e)
        {
            Loader.Unload();
        }

        private void ITOPODCombatMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected = ITOPODCombatMode.SelectedIndex;

            bool isManualMode = selected == 0;
            ITOPODPrecastBuffs.Enabled = isManualMode;
            ITOPODFastCombat.Enabled = isManualMode;
            ITOPODSmartBeastMode.Enabled = isManualMode && !ITOPODFastCombat.Checked;

            if (_initializing) return;
            Main.Settings.ITOPODCombatMode = selected;
        }

        private void ITOPODPrecastBuffs_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ITOPODPrecastBuffs = ITOPODPrecastBuffs.Checked;
        }

        private void ITOPODRecoverHP_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ITOPODRecoverHP = ITOPODRecoverHP.Checked;
        }

        private void ITOPODFastCombat_CheckedChanged(object sender, EventArgs e)
        {
            ITOPODSmartBeastMode.Enabled = ITOPODCombatMode.SelectedIndex == 0 && !ITOPODFastCombat.Checked;
            if (_initializing) return;
            Main.Settings.ITOPODFastCombat = ITOPODFastCombat.Checked;
        }

        private void ITOPODBeastMode_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ITOPODBeastMode = ITOPODBeastMode.Checked;
            if (ITOPODBeastMode.Checked)
            {
                Main.Settings.ITOPODSmartBeastMode = ITOPODSmartBeastMode.Checked = false;
            }
        }

        private void ITOPODSmartBeastMode_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ITOPODSmartBeastMode = ITOPODSmartBeastMode.Checked;
            if (ITOPODSmartBeastMode.Checked)
            {
                Main.Settings.ITOPODBeastMode = ITOPODBeastMode.Checked = false;
            }
        }

        private void DisableOverlay_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.DisableOverlay = DisableOverlay.Checked;
        }

        private void MoneyPitInput_TextChanged(object sender, EventArgs e)
        {
            TryItemBoxTextChanged(_pitControls, out _);
        }

        private void MoneyPitInput_KeyDown(object sender, KeyEventArgs e)
        {
            ItemBoxKeyDown(e, _pitControls);
        }

        private void MoneyPitAdd_Click(object sender, EventArgs e)
        {
            ItemListAdd(_pitControls);
        }

        private void MoneyPitRemove_Click(object sender, EventArgs e)
        {
            ItemListRemove(_pitControls);
        }

        private void UpgradeDiggers_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.UpgradeDiggers = UpgradeDiggers.Checked;
        }

        private void CastBloodSpells_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.CastBloodSpells = CastBloodSpells.Checked;
        }

        private void IronPillOnRebirth_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.IronPillOnRebirth = IronPillOnRebirth.Checked;
        }

        private void GuffAOnRebirth_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.BloodMacGuffinAOnRebirth = GuffAOnRebirth.Checked;
        }

        private void GuffBOnRebirth_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.BloodMacGuffinBOnRebirth = GuffBOnRebirth.Checked;
        }

        private void QuestCombatMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected = QuestCombatMode.SelectedIndex;

            bool isManualMode = selected == 0;
            QuestFastCombat.Enabled = isManualMode;
            QuestSmartBeastMode.Enabled = isManualMode && !QuestFastCombat.Checked;

            if (_initializing) return;
            Main.Settings.QuestCombatMode = selected;
        }

        private void YggSwapThreshold_ValueChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            if (TryGetValueFromNumericUpDown(YggSwapThreshold, out int val))
            {
                Main.Settings.YggSwapThreshold = val;
            }
        }

        private void MoreBlockParry_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.MoreBlockParry = MoreBlockParry.Checked;
        }

        private void EnemyBlacklistZone_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected = EnemyBlacklistZone.SelectedItem;
            var item = (KeyValuePair<int, string>)selected;
            var values = Main.Character.adventureController.enemyList[item.Key]
                .Select(x => new KeyValuePair<int, string>(x.spriteID, x.name)).ToList();
            EnemyBlacklistNames.DataSource = null;
            EnemyBlacklistNames.ValueMember = "Key";
            EnemyBlacklistNames.DisplayMember = "Value";
            EnemyBlacklistNames.DataSource = values;
        }

        private void BlacklistRemoveEnemyButton_Click(object sender, EventArgs e)
        {
            var item = BlacklistedBosses.SelectedItem;
            if (item == null)
                return;

            var id = (KeyValuePair<int, string>)item;

            var temp = Main.Settings.BlacklistedBosses.ToList();
            temp.RemoveAll(x => x == id.Key);
            Main.Settings.BlacklistedBosses = temp.ToArray();
        }

        private void BlacklistAddEnemyButton_Click(object sender, EventArgs e)
        {
            var item = EnemyBlacklistNames.SelectedItem;
            if (item == null) return;

            var id = (KeyValuePair<int, string>)item;
            var temp = Main.Settings.BlacklistedBosses.ToList();
            temp.Add(id.Key);
            Main.Settings.BlacklistedBosses = temp.ToArray();
        }

        private void BoostAvgReset_Click(object sender, EventArgs e)
        {
            Main.reference.ResetBoostProgress();
        }

        private void WishSortPriorities_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.WishSortPriorities = WishSortPriorities.Checked;
        }

        private void WishSortOrder_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.WishSortOrder = WishSortOrder.Checked;
        }

        private void manageMayo_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ManageMayo = balanceMayo.Checked;
        }

        private void TrashCards_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.TrashCards = TrashCards.Checked;
        }

        private void TrashQuality_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.CardsTrashQuality = TrashQuality.SelectedIndex;
        }

        private void AutoCastCards_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AutoCastCards = AutoCastCards.Checked;
        }

        private void OpenSettingsFolder_Click(object sender, EventArgs e)
        {
            Process.Start(Main.GetSettingsDir());
        }

        private void ProfileEditButton_Click(object sender, EventArgs e)
        {
            var filename = Main.Settings.AllocationFile + ".json";
            var path = Path.Combine(Main.GetProfilesDir(), filename);
            Process.Start(path);
        }

        private void OpenProfileFolder_Click(object sender, EventArgs e)
        {
            Process.Start(Main.GetProfilesDir());
        }

        private void trashAdventureCards_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.TrashAdventureCards = TrashAdventureCards.Checked;
        }

        private void TrashProtectedCards_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.TrashProtectedCards = TrashProtectedCards.Checked;
        }

        private void trashCardCost_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.TrashCardCost = (int)TrashTier.SelectedItem;
        }

        private void DontCastAdd_Click(object sender, EventArgs e)
        {
            if (DontCastSelection.SelectedItem != null && !DontCastList.Items.Contains(DontCastSelection.SelectedItem))
            {
                DontCastList.Items.Add(DontCastSelection.SelectedItem);
                Main.Settings.DontCastCardType = DontCastList.Items.Cast<string>().ToArray();
            }
        }

        private void DontCastRemove_Click(object sender, EventArgs e)
        {
            if (DontCastList.SelectedItem != null)
            {
                DontCastList.Items.RemoveAt(DontCastList.SelectedIndex);
                Main.Settings.DontCastCardType = DontCastList.Items.Cast<string>().ToArray();
            }
        }

        private void TrashChunkers_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.TrashChunkers = TrashChunkers.Checked;
        }

        private void UseTitanCombat_CheckedChanged(object sender, EventArgs e)
        {
            bool useTitanCombat = UseTitanCombat.Checked;
            var selected = TitanCombatMode.SelectedIndex;
            bool isManualMode = selected == 0;

            TitanCombatMode.Enabled = useTitanCombat;
            TitanPrecastBuffs.Enabled = useTitanCombat && isManualMode;
            TitanRecoverHealth.Enabled = useTitanCombat;
            TitanFastCombat.Enabled = useTitanCombat && isManualMode;
            TitanBeastMode.Enabled = useTitanCombat;
            TitanSmartBeastMode.Enabled = useTitanCombat && isManualMode && !TitanFastCombat.Checked;
            TitanMoreBlockParry.Enabled = useTitanCombat && isManualMode;

            if (_initializing) return;
            Main.Settings.UseTitanCombat = useTitanCombat;
        }

        private void TitansWithVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            VersionedTitan titan = TitansWithVersion.SelectedItem as VersionedTitan;
            TitanVersions.SelectedIndexChanged -= new System.EventHandler(this.TitanVersions_SelectedIndexChanged);

            if (titan.IsUnlocked())
            {
                TitanVersions.Enabled = true;
                TitanVersions.SelectedIndex = titan.GetVersion();
            }
            else
            {
                TitanVersions.SelectedIndex = 0;
                TitanVersions.Enabled = false;
            }

            TitanVersions.SelectedIndexChanged += new System.EventHandler(this.TitanVersions_SelectedIndexChanged);
        }

        private void TitanVersions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializing) return;

            VersionedTitan titan = TitansWithVersion.SelectedItem as VersionedTitan;
            if (titan.IsUnlocked())
            {
                titan.SetVersion(TitanVersions.SelectedIndex);
                Main.Character.adventureController.updateTitanDifficultyUI();
            }
        }

        private void SetTitanVersion_Click(object sender, EventArgs e)
        {
        }

        private void TitanCombatMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool useTitanCombat = UseTitanCombat.Checked;
            var selected = TitanCombatMode.SelectedIndex;
            bool isManualMode = selected == 0;

            TitanPrecastBuffs.Enabled = useTitanCombat && isManualMode;
            TitanFastCombat.Enabled = useTitanCombat && isManualMode;
            TitanSmartBeastMode.Enabled = useTitanCombat && isManualMode && !TitanFastCombat.Checked;
            TitanMoreBlockParry.Enabled = useTitanCombat && isManualMode;

            if (_initializing) return;
            Main.Settings.TitanCombatMode = selected;
        }

        private void TitanPrecastBuffs_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.TitanPrecastBuffs = TitanPrecastBuffs.Checked;
        }

        private void TitanRecoverHealth_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.TitanRecoverHealth = TitanRecoverHealth.Checked;
        }

        private void TitanFastCombat_CheckedChanged(object sender, EventArgs e)
        {
            TitanSmartBeastMode.Enabled = TitanCombatMode.SelectedIndex == 0 && !TitanFastCombat.Checked;
            if (_initializing) return;
            Main.Settings.TitanFastCombat = TitanFastCombat.Checked;
        }

        private void TitanBeastMode_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.TitanBeastMode = TitanBeastMode.Checked;
            if (TitanBeastMode.Checked)
            {
                Main.Settings.TitanSmartBeastMode = TitanSmartBeastMode.Checked = false;
            }
        }

        private void TitanSmartBeastMode_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.TitanSmartBeastMode = TitanSmartBeastMode.Checked;
            if (TitanSmartBeastMode.Checked)
            {
                Main.Settings.TitanBeastMode = TitanBeastMode.Checked = false;
            }
        }

        private void TitanMoreBlockParry_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.TitanMoreBlockParry = TitanMoreBlockParry.Checked;
        }

        private void ManageConsumables_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ManageConsumables = ManageConsumables.Checked;
        }

        private void AutoBuyAdv_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AutoBuyAdventure = AutoBuyAdv.Checked;
        }

        private void AutoBuyConsumables_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.AutoBuyConsumables = AutoBuyConsumables.Checked;
        }

        private void ConsumeIfRunning_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ConsumeIfAlreadyRunning = ConsumeIfRunning.Checked;
        }

        private void SortCards_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.CardSortEnabled = SortCards.Checked;
        }

        private void CardSortAdd_Click(object sender, EventArgs e)
        {
            if (CardSortOptions.SelectedItem != null && !CardSortList.Items.Contains(CardSortOptions.SelectedItem))
            {
                CardSortList.Items.Add(CardSortOptions.SelectedItem);
                Main.Settings.CardSortOrder = CardSortList.Items.Cast<string>().ToArray();
            }
        }

        private void CardSortRemove_Click(object sender, EventArgs e)
        {
            if (CardSortList.SelectedItem != null)
            {
                CardSortList.Items.RemoveAt(CardSortList.SelectedIndex);
                Main.Settings.CardSortOrder = CardSortList.Items.Cast<string>().ToArray();
            }
        }

        private void CardSortUp_Click(object sender, EventArgs e)
        {
            ItemListUp(CardSortList, Main.Settings.CardSortOrder, (settings) => Main.Settings.CardSortOrder = settings);
        }

        private void CardSortDown_Click(object sender, EventArgs e)
        {
            ItemListDown(CardSortList, Main.Settings.CardSortOrder, (settings) => Main.Settings.CardSortOrder = settings);
        }

        private void LocateWalderp_Click(object sender, EventArgs e)
        {
            if (Main.Character.waldoUnlocker.currentMenu >= 0)
            {
                Main.Character.menuSwapper.swapMenu(Main.Character.waldoUnlocker.currentMenu);
            }
        }

        private void ManageCooking_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ManageCooking = ManageCooking.Checked;
        }

        private void ManageCookingLoadout_CheckedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            Main.Settings.ManageCookingLoadouts = ManageCookingLoadout.Checked;
        }

        private void CookingLoadoutBox_TextChanged(object sender, EventArgs e)
        {
            TryItemBoxTextChanged(_cookingControls, out _);
        }

        private void CookingLoadoutItem_KeyDown(object sender, KeyEventArgs e)
        {
            ItemBoxKeyDown(e, _cookingControls);
        }

        private void cookingAddButton_Click(object sender, EventArgs e)
        {
            ItemListAdd(_cookingControls);
        }

        private void cookingRemoveButton_Click(object sender, EventArgs e)
        {

            ItemListRemove(_cookingControls);
        }
    }
}

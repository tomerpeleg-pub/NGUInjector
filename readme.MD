# NGUInjector

NGUInjector is an automation platform for the steam version of NGU Idle.

# Instructions

Releases can be found in the releases section of the github page [here](https://github.com/JConquistador/NGUInjector/releases). Do not download the source code archive, instead download the zip file with the release version in the name (dist_3.6.3.zip for example). Extract the downloaded archive to a directory of your choice. To start automation, run inject.bat in the extracted folder when NGU Idle is open.

You'll know the injection has worked if you see the following in the upper left portion of NGU.

![Injected](https://i.imgur.com/JIF2pl4.png)

To upgrade to a new release, unload the injector from the General Settings page and then inject the new version.

# Configuration

**IMPORTANT NOTE** For those upgrading from an older version of NGUInjector (< 3.7.0), your configuration folder will be recreated in the directory shown below. Your old settings and profiles are NOT gone. Simply copy the old NGUInjector folder from the desktop to the new location.

After injecting the dll, a new folder will be created in your AppData directory called NGUInjector (full path is %UserProfile%\AppData\LocalLow\NGUInjector). Settings files will be automatically written to this directory. The following files are of interest:

- **settings.json** - Contains settings used by the application. Most settings can be configured from the in-game menu.
- **zoneOverrides.json** - Override the default values for Idle/Manual Power/Toughness for gold sniping

A profiles folder will be created with the following files:

- **default.json** - Breakpoints for assigning gear, magic, energy, wandoos OS, rebirth time, and diggers. See the allocation section for more information

More profiles can be added to this folder at your discretion.

A logs folder will also be created with the following files:

- **loot.log** - Logs all the loot dropped by enemies.
- **inject.log** - Logs general information about the injector
- **combat.log** - Logs information from the combat algorithm
- **allocation.log** - Logs information about allocating energy/magic/gear/diggers
- **pitspin.log** - Logs important things like fruit harvests, money pit, daily spin. This log file will not be overwritten.

Saving settings.json, zoneOverrides.json, or any profile will automatically reload the settings in the Injector. Reloading the game isn't necessary

# Settings.json Configurations

Settings in the GUI can be managed directly in the underlying settings file if desired. This can be helpful when pasting in loadouts from the Gear Optimizer. To access these settings, open the **settings.json** file in your AppData NGUInjector folder mentioned above.

There should be only one setting not found in the GUI and *must* be managed directly:

- **_debugAllocation** - Will enable debug logging to a separate file of various allocation decisions made by the injector

# Log Parsing

Discord user WookieeNo1 has made a PowerShell script to prettify the log files. If you are interested in this, check out the github here: https://github.com/WookieeNo1/NGUIdleInjector-Pretty-Print

This is what it looks like once you set it up:
![loot.log](https://i.imgur.com/68fVGk7.png)
![pitspin.log](https://i.imgur.com/nTMJ5jP.png)
![inject.log](https://i.imgur.com/7tlf4qH.png)

# Settings Configuration

An in game menu can be opened using the F1 button.

![General](https://i.imgur.com/5k0KmJm.png)

- **Master Switch** - If turned off, will disable all automation
- **Disable Overlay** - If switched on, will disable the in-game overlay.

- **Auto Move to ITOPOD** - Automatically move to ITOPOD when nothing else is happening in automation.
- **Auto Fight Bosses** - Automatically nuke/fight bosses.
- **Auto Buy Energy/Magic/R3** - Automatically spend EXP on Energy/Magic/R3 based on the custom caps set by the user on their respective tabs. Will buy stats all at once after the total of all custom caps are met (including Adventure Stats if enabled).
- **Auto Buy Adventure Stats** - Automatically spend EXP on Adventure Atats based on the custom caps set by the user in the "Adventure Stats" tab. Will buy stats all at once after the total of all custom caps are met (including Energy/Magic/R3 if enabled).

- **Auto Buy Consumables** - Set this to true to have the injector auto purchase any consumables your profile is configured to use that you do not own. Will only buy consumables if you have the AP to purchase them.
- **Use Consumables if already running** - If this is false, consumables in the consumable breakpoint will NOT be used if the consumable in question has time remaining. See section on Consumables below for more details.

- **Open Settings Folder** - Opens a folder to the NGUInjector folder which contains the settings, profiles and logs.

- **Unload** - Allows you to unload the injector from the game. Check the box next to the unload button and then hit unload

![Allocation](https://i.imgur.com/FvoQLZF.png)

- **Allocation Profile** - Allows you to quickly switch allocation profiles. Any profiles stored in the profiles folder will be listed here. Press "Change profile" to load the profile selected in the dropdown. Press "Edit" to open the selected profile in a text editor. Press "Open Profile Folder" to open the folder containing all profiles.

- **Manage Energy** - Manage energy based on breakpoints in your selected allocation file
- **Manage Magic** - Manage magic based on breakpoints in your selected allocation file
- **Manage R3** - Manage resource 3 based on breakpoints in your selected allocation file

- **Manage Gear** - Manage gear based on breakpoints in your selected allocation file
- **Manage NGU Difficulty** - Manage NGU difficulty based on breakpoints in your selected allocation file

- **Manage Diggers** - Manage diggers based on breakpoints in your selected allocation file
- **Auto Upgrade Diggers** - Automatically upgrades your cheapest diggers when you have money available. Respects your money pit threshold and wont buy underneath it.

- **Manage Wandoos OS** - Manage wandoos OS based on breakpoints in your selected allocation file

- **Manage Consumables** - Set this to true to have injector manage consumables via breakpoints in your allocation file. See section on Consumables below for more details.

- **Auto Rebirth** - Automatically rebirth after the time in your selected allocation file is reached.

- **Change Auto Spells** - Manage Spoopy Spell casting
- **Spaghetti %** - Percent to cast Blood Spaghetti until
- **Number Bonus** - Number to cast Blood NUMBER Boost until
- **Counterfeit %** - Percent to cast Counterfeit Gold until

- **Cast Blood Spells** - Cast blood spells automatically. Will try every 10 seconds or at rebirth
- **Iron Pill** - Amount of +stats to cast Iron Pill at.
- **Guff A** - Number of +levels to cast Guff A at
- **Guff B** - Number of +levels to cast Guff B at
- **Cast on Rebirth Options** - If selected will use all available blood on the selected options if cooldown and threshold requirements are met. Priority is Guff B > Guff A > Iron Pill. Will use remaining blood on Number if any remains.

![Yggdrasil](https://i.imgur.com/EksfVj0.png)

- **Manage Yggdrasil** - Automatically harvest fruits.
- **Activate Fruits** - Reactivate fruits you haven't bought the exp perk for
- **Swap Loadout for Yggdrasil** - If checked, equip the loadout specified in YggdrasilLoadout before harvesting
- **Minimum Fruit Level for Swap** - Don't equip Yggdrasil loadout for fruits below this level.
- **Loadout** - Edit the loadout used for harvest yggdrasil. Add items using the box in the bottom.
- **Harvest All Now** - Allows you to do a Yggdrasil swap and immediately harvest all fruits. Check the box next to the button to enable it. Good for manual rebirths

![Inventory](https://i.imgur.com/4smaL33.png)

- **Manage Inventory** - When enabled, the following features will be enabled
  - **Auto Boost Merging** - If a boost is locked, boosts will automatically be merged until the boost hits 100.
  - **Auto Merging** - Equipped items and items in inventory will automatically be merged, as well as MacGuffins. The highest level item will be selected for merging automatically. A locked item will always be preferred.
  - **Auto Boosting** - Automatically boost items. Will start with items in the PriorityBoosts setting, followed by equipped items, locked items in inventory and finally infinity cube.
  - **Auto Filtering** - If improved loot filter is purchased, will automatically filter items when they hit level 100
  - **Auto Convertible Management** - Will automatically transform non-locked pendants/lootys and other items
  - **Quest Item Management** - Will try to merge a quest item to level 100 if a locked one is found. Otherwise will automatically turn in quest items
- **Manage Boost Conversion** - If all normal 100 Level Challenges are completed, will calculate needed boosts for items in boostIds and then switch auto conversion appropriately. If a locked non-level 100 boost is found in your inventory, will lock conversion to that type first to boost it to 100. Will also show you the hourly average of boost progress in the inject.log file.
- **Cube Boost Priority** - How to boost cube.
  - **None** - Will turn off boost conversion
  - **Balanced** - Will keep raw power and toughness on cube equal
  - **Softcap** - Will keep percentage of the power and toughness softcaps on cube equal. For instance if the power softcap is 3x the toughness softcap, cube raw power will be 3x cube raw toughness.
  - **Power** - Will only put power into the cube
  - **Toughness** - Will only put toughness into the cube
- **Gear Boost Priority** - The priority in which items should be boosted. Defaults to Power > Toughness > Special.
- **Priority Boosts** - Edit the items you wish to boost before others. Add items using the box in the bottom. Order is respected.
- **Blacklisted Items** - Edit the items you wish to never be boosted. Add items using the box in the bottom. Supersedes Priority list if the same item is in both lists.
- **Reset Boost Average** - Resets the running average of cube/item boosting

![Titans](https://i.imgur.com/VpF8Wwf.png)

- **Manage Titans** - Turns on combat and gear swapping for titans.
- **Use Titan Combat Settings** - If enabled will override the default combat settings in the Adventure tab.
- **Set difficulty** - Use this to change the difficulties of titans which have different versions. Disabled until the titan has been killed at least once.
- **Loadout** - The gear to equip when autokilling or fighting titans.
- **Titans to Manage** - Mark titans you wish to fight and swap gear for
- **Combat Mode** - If set to manual, will execute a custom combat routine. If set to idle, will just idle the zone. *WALDERP, THE GODMOTHER, and THE EXILE will ALWAYS use manual combat regardless of this setting*
- **Precast Buffs** - If enabled, will cast charge and parry in safe zone before each titan attempt and wait for the cooldowns to recharge. Ignored during Idle combat mode.
- **Recover HP** - If enabled, will recover HP in safe zone between titan kills attempts.
- **Fast Combat** - If enabled, will just quickly attack and skip buffs altogether. Block will still be used for the special attacks of THE GODMOTHER and THE EXILE. Ignored during Idle combat mode.
- **Beast Mode** - Whether to use beast mode or not while in combat. Will deselect Smart Beast Mode option if selected.
- **Smart Beast Mode** - Will enable beast mode if Defensive or Ultimate Buff is active, Piercing or Ultimate Attacks can be used AND beast mode can be turned off again before that buff expires. Will deselect Beast Mode option if selected. Ignored during Idle combat mode or if Fast Combat is enabled.
- **Block/Parry with Buffs** - If checked, combat will use block/parry along with offensive/defensive/ultimate buffs. Ignored during Idle combat mode.
- **Locate Walderp** - If Walderp is hiding, will take you to the menu he's currently in.

![Adventure](https://i.imgur.com/91ri7DG.png)

- **Combat Enabled** - Turns on auto combat to go to target zone and kill stuff.

- **Precast Buffs** - If enabled, will cast charge and parry in safe zone before each combat and wait for the cooldowns to recharge. Ignored during Idle combat mode.
- **Recover HP** - If enabled, will recover HP in safe zone between kills
- **Fast Combat** - If enabled, will just quickly attack and skip buffs altogether. Block will still be used for the special attacks of Rapid, Charge, or Exploder enemy types if available. Ignored during Idle combat mode.
- **Beast Mode** - Whether to use beast mode or not while in combat. Will deselect Smart Beast Mode option if selected.
- **Smart Beast Mode** - Will enable beast mode if Defensive or Ultimate Buff is active, Piercing or Ultimate Attacks can be used AND beast mode can be turned off again before that buff expires. Will deselect Beast Mode option if selected. Ignored during Idle combat mode or if Fast Combat is enabled.
- **Block/Parry with Buffs** - If checked, combat will use block/parry along with offensive/defensive/ultimate buffs. Ignored during Idle combat mode.
- **Combat Mode** - If set to manual, will execute a custom combat routine. If set to idle, will just idle the zone. *WALDERP and THE GODMOTHER will ALWAYS use manual combat regardless of this setting*

- **Target Zone** - The target zone to snipe/do combat in. ITOPOD is at the bottom of the list. If the target zone is a titan zone, will idle in ITOPOD until the Titan is about to spawn. Also, will use combat settings/gear defined in the Titans tab even if the Manage Titans flag is disabled.
- **Bosses Only** - If enabled, will only fight bosses when doing combat. Good for zone sniping.
- **Allow Fallthrough** - If set to true, combat will use the highest unlocked zone you have until the target zone is unlocked.
- **ITOPOD** - Equivalent to setting target zone to ITOPOD, just a quicker option
- **Titans** - Will move you to each Titan enabled in the Titans and/or Gold tabs when the Titan is about to spawn. Higher level titans will be fought first, Gold loadouts take priority. Uses the non-ITOPOD combat settings unless Titan Combat Settings are enabled in the Titans tab.

- **ITOPOD Settings** - Settings below this apply only to ITOPOD combat
- **Combat Mode** - If set to manual, will execute a custom combat routine. If set to idle, will just idle the zone.
- **Precast Buffs** - If enabled, will cast charge and parry in safe zone before each combat and wait for the cooldowns to recharge. Ignored during Idle combat mode.
- **Recover HP** - If enabled, will recover HP in safe zone between kills
- **Fast Combat** - If enabled, will just quickly attack and skip buffs altogether. Block will still be used for the special attacks of Rapid, Charge, or Exploder enemy types if available. Ignored during Idle combat mode.
- **Beast Mode** - Whether to use beast mode or not while in combat. Will deselect Smart Beast Mode option if selected.
- **Smart Beast Mode** - Will enable beast mode if Defensive or Ultimate Buff is active, Piercing or Ultimate Attacks can be used AND beast mode can be turned off again before that buff expires. Will deselect Beast Mode option if selected. Ignored during Idle combat mode or if Fast Combat is enabled.
- **Optimize ITOPOD Floor** - Will optimize your ITOPOD floor every 10 seconds. Does nothing if Lazy Floor Shifter is on

- **Blacklisted Enemies** - Allows you to blacklist enemies when searching for enemies in a zone.

![Gold](https://i.imgur.com/CAZvwq9.png)

- **Enable Gold Management** - If turned off, will disable all gold swap logic
- **Do Gold Snipe** - If checked, will immediately equip gold loadout gear and perform a boss snipe in the best calculated zone based on the zone stats and gold loadout power/toughness.
- **Resnipe Time** - Set this value to re-enable gold snipe at a certain time during your rebirth. Best set to after AT is completed in order to get a higher zone.
- **CBlock Mode** - If checked, will automatically gold snipe every new zone that is unlocked. Useful for challenge blocks where zones sporadically become available.
- **Loadout** - Items to use when swapping for gold loadout.
- **Titans to Gold Snipe** - Check the box next to titans you want to swap to gold loadouts for. Leave all boxes unchecked in order to never use gold loadouts for titans. If a titan is green, gold snipe has already been completed for this titan.
- **Reset Titan Status** - Immediately mark all titan gold swaps as incomplete

![Quests](https://i.imgur.com/URjSuL1.png)

- **Auto Quest** - If enabled, quests will automatically be turned in. Behavior varies based on the Allow Major Quests option
- **Allow Major Quests**
  - If this option is enabled, when major quests are available they will be accepted and automation will manually farm the items from the appropriate zone
  - If this option is disabled, major quests will not be accepted.
  - When no major quests are available, minor quests will be accepted and idled.
  - Manual questing will pause while titan combat is occurring
  - Manual questing will NOT occur if a zone is currently being sniped in the Adventure tab.
- **Use Butter on Majors** - If enabled will attempt to butter major quests
- **Manual Minors** - If enabled will manual minor quests as well as majors
- **Use Butter on Minors** - If enabled will attempt to butter minor quests
- **Abandon Minor Quests** - If enabled and minor quest progress is below the minor quest threshold, the minor quest will be abandoned and a major quest will be picked up.
- **Abandon Minor Quest Threshold** - Quest progress before which its acceptable to abandon minor quests.
- **Swap Loadout for Quests** - Equip the gear in Quest Loadout when engaged in Manual questing
- **Loadout** - The gear to equip when in a manual quest.
- **Combat Mode** - If engaged in manual questing and this option is set to manual, will perform a combat routine in the quest zone. Otherwise will idle the zone.
- **Fast Combat** - If enabled will skip all buffs during combat. Ignored during Idle combat mode.
- **Beast Mode** - Whether to use beast mode or not while in combat. Will deselect Smart Beast Mode option if selected.
- **Smart Beast Mode** - Will enable beast mode if Defensive or Ultimate Buff is active, Piercing or Ultimate Attacks can be used AND beast mode can be turned off again before that buff expires. Will deselect Beast Mode option if selected. Ignored during Idle combat mode or if Fast Combat is enabled.

![Wishes](https://i.imgur.com/g8nsInJ.png)

- **Sort Wishes by Completion Time** - Each wish has a raw speed value that determines how fast it progresses. By default wishes are assigned to wish slots based on this speed (fastest wishes first). If this is checked, wishes will be assigned to slots by the remaining time to complete the current level of the wish instead. For example Wish 9 and Wish 10 both have the same raw speed, if this option is unchecked, Wish 9 will go to max level before any levels are gained in Wish 10. But since each additional level adds more time to the raw speed, if this option is checked, Wish 9 and 10 will gain levels equally.
- **Priority Wishes** - The wishes to prioritize for the available wish slots. If there are open slots remaining after all prioritized wishes are assigned, other available wishes will be assigned to open slots ordered by the method outlined above.
- **Sort Priority Wishes** - If unchecked, will assign wishes to wish slots based on their order in the Priority list. If checked, will order by the method outlined above.
- **Blacklist Wishes** - Assign wishes you never want to cast here. Any which in this list will be ignored when working out the wish priorities. Supersedes Priority list if the same wish is in both lists.

![The Pit](https://i.imgur.com/vXvkQPT.png)

- **Auto Daily Spin** - Automatically do daily spin and log the result to the loot.log file
- **Auto Money Pit** - Automatically throw money into the money pit if the Money Pit Threshold is met. Logs the result to the loot.log file
- **Money Pit Threshold** - The amount of gold to wait for to throw gold into the money pit. Accepts scientific notation (ex: 1E+28)
- **Loadout** - Items to equip before throwing money in the pit

![Cards](https://i.imgur.com/b5G9O1Z.png)

- **Manage Mayo** - Prioritize mayo production to be able to cast the first card in your deck asap. If you have no cards, it will equalize all mayo types.
- **Auto Cast Cards** - Cast any card that is not trashed according to your settings.
- **Sort Cards** - Set this to true to have the injector sort cards based on the priorities given in the card Sort Order.
- **Card Sort Order** - A list of priorities to sort the cards by. The cards will be sorted by the first priority, then any cards that share an identical value for the first priority will be sorted by the second priority, and so on. The available priority options are:
  - **"RARITY"** - The card rarity ("Good"/"Okay", etc.). (high->low)
  - **"TIER"** - The card tier. (high->low)
  - **"COST"** - The total mayo cost. (high->low)
  - **"PROTECTED"** - Whether the card is protected. (protected, then not protected)
  - **"CHANGE"** - The actual change in the bonus. (e.g. if you're current total Wish bonus is 200%, and the card bonus is 2%, the change will be 1%). (high->low)
  - **"VALUE"** - The CHANGE value divided by the card cost. (high->low)
  - **"NORMALVALUE"** - The VALUE, normalized by the card bonus type. (e.g. Gold always gives higher changes than PP, so this will try to account for that). (high->low)
  - **"TYPE:xxxx"** - The card bonus type. Replace 'xxxx' with a specific card bonus type, i.e. one of: "energyNGUSpeed", "magicNGUSpeed", "wandoosSpeed", "augSpeed", "TMSpeed", "hackSpeed", "wishSpeed", "atkDefStats", "adventureStat", "dropChance", "goldDrop", "dayCareSpeed", "PP", "QP".

The "-ASC" versions of the sort options will sort in the opposite direction.  
**Example:**
```
   ["PROTECTED", "TYPE:adventureStat", "TYPE:hackSpeed", "COST-ASC", "VALUE"]
```
This will place any protected cards first. For all the protected cards, it will prioritise advStat, then hackSpeed above other types. All the protected advStat cards are then ordered by the lowest cost, then highest value. Same for protected hackSpeed cards. Same for all other protected cards. The unprotected cards are then sorted in the same manner as the protected cards.

- **Auto Trash Cards** - Automatically yeet cards according to the settings below.
- **Trash Adventure Cards** - If checked, adventure cards are yeeted according to your settings. If not checked, no adventure cards are ever yeeted, regarless of quality/cost/type settings.
- **Trash Protected Cards** - If checked, protected cards are yeeted according to your settings. If not checked, no protected cards are ever yeeted, regarless of quality/cost/type settings. **Note:** Big Chonker cards *always* spawn as protected and ignore this flag if the Trash Chonkers flag is enabled.
- **Trash Cards of XXX quality/cost and below** - Yeet cards which are at or below the specified quality/cost.
- **Trash Chonkers** - If checked, BigChonker level cards will be yeeted when their type is on the Always Trash List. If not checked, BigChonker cards will not be yeeted unless they are below the configured quality or cost thresholds.
- **Trash All Cards of Type** Will trash ALL of these card types regardless of the quality/cost settings.

![Cooking](https://i.imgur.com/2pgJoUZ.png)

- **Manage Cooking** - Set this to true to have the injector optimize your cooking efficiency
- **Swap Loadout for Cooking** - Set this to true to have the injector swap to a Cooking loadout when eating meals
- **Loadout** - Items to equip before eating

# Allocation

Allocation profiles can be found in the profiles folder and contain time breakpoints for configuring your gear, diggers, energy allocation and magic allocation. Sample allocation files can be found in the sampleprofile folder.

[Sample 24 Hour Profile](https://github.com/rvazarkar/NGUInjector/blob/master/NGUInjector/SampleProfiles/24hr_with_gear.json).

The time portion of every breakpoint refers to rebirth time in seconds. Time can be defined as a simple number (ex: 86400) or as a JSON object:

```
"Time": {
    "h": 1,
    "m": 30,
    "s": 20
},
```

Priorities can be modified with an optional percentage cap by adding a : and a percent. As an example:

```
CAPWISH-0:30
```

This will cap wish 0 but limit the maximum amount allocated to 30% of your current cap. When applied to non-cap priorities, it will limit to % of idle resource. Priorities will respect targets and not allocate to priorities that are target capped.

## Energy

An energy breakpoint is structured as follows:

```
"Energy": [
    {
        "Time": 0,
        "Priorities": ["CAPNGU-0", "CAPWAN", "AT-1", "NGU-1"]
    }
]

```

Priorities come in 2 types - cap and non-cap. Any priority that has -X after it is 0 indexed.

A cap priority will use as much idle energy as possible and hit the highest BB breakpoint possible for the next 10 seconds. A non-cap priority will take a divider of your idle energy based on number of non-cap priorities and attempt to hit the highest BB breakpoint possible for the next 10 seconds. When a priority is calculated, it will push excess energy to later priorities.

In the above example the following actions will be taken:

- NGU-0 (NGU Augments) will be capped
- Wandoos energy will be capped
- Remaining energy will be split between AT-1 (Advanced Training Power) and NGU-1 (NGU Wandoos)

Available cap priorities for Energy are as follows:

- CAPNGU-X (0-8) - Calculate a cap for the NGU
- CAPALLNGU - Calculate a cap for every NGU starting from 0
- CAPAT-X (0-4) - Calculate a cap for the AT and attempt to BB it.
- CAPALLAT - Calculate caps for all ATs and BB them.
- CAPBESTAUG - Attempts to allocate to the best augment possible. Before pairs are unlocked, will allocate to highest augment that is easily doable. After will try to optimize.
- CAPWAN - Calculate a cap for wandoos energy
- CAPTM - Calculate a cap for energy time machine and attempt to BB it.
- CAPBT-X (0-11) - Calculate a cap for basic training
- CAPALLBT - Caps all basic trainings
- CAPAUG-X (0-13) - Calculate a cap for augments and attempt to BB it
- CAPWISH-X (0-X) - Allocate energy to wishes. X is the wish slot you wish to allocate to. If you only have 1 slot, WISH-0 will be the one you use. Wishes will follow the priority set in the WishPriorities setting and then work through cheapest wishes

Available non-cap priorities for Energy are as follows:

- ALLNGU - Allocate energy to every NGU
- NGU-X (0-8) - Allocate energy to NGU
- AT-X (0-4) - Allocate energy to AT
- ALLAT - Allocate energy to all AT
- AUG-X (0-13) - Allocate energy to augment
- BESTAUG - Attempts to allocate to the best augment possible. Before pairs are unlocked, will allocate to highest augment that is easily doable. After will try to optimize.
- WAN - Allocate energy to energy wandoos
- TM - Allocate energy to energy time machine
- BT-X (0-11) - Allocate energy to basic training
- WISH-X (0-X) - Allocate energy to wishes. X is the wish slot you wish to allocate to. If you only have 1 slot, WISH-0 will be the one you use. Wishes will follow the priority set in the WishPriorities setting and then work through cheapest wishes

More information on allocation indexes can be found on the [wiki](https://github.com/rvazarkar/NGUInjector/wiki/Allocation-Indexes)

## Magic

A magic breakpoint is structured as follows:

```
"Magic": [
    {
        "Time": 0,
        "Priorities": ["CAPNGU-0", "CAPWAN", "BR", "NGU-1"]
    }
]

```

Priorities come in 2 types - cap and non-cap. Any priority that has -X after it is 0 indexed.

A cap priority will use as much idle energy as possible and hit the highest BB breakpoint possible for the next 10 seconds. A non-cap priority will take a divider of your idle energy based on number of non-cap priorities and attempt to hit the highest BB breakpoint possible for the next 10 seconds. When a priority is calculated, it will push excess energy to later priorities.

In the above example the following actions will be taken:

- NGU-0 (NGU Yggdrasil) will be capped
- Wandoos magic will be capped
- Magic rituals will be capped from highest to lowest, with rituals taking more than 1 hour skipped
- Remaining magic will be allocated to NGU-1 (NGU Exp)

Available cap priorities for Magic are as follows:

- CAPNGU-X (0-6) - Use the cap button for the NGU
- CAPALLNGU - Use the cap button for every NGU starting from 0
- CAPWAN - Use the cap button for wandoos magic
- CAPTM - Calculate a cap for magic time machine and attempt to BB it.
- CAPRIT-X - Calculate a cap for the ritual and allocate
- CAPWISH-X (0-X) - Allocate magic to wishes. X is the wish slot you wish to allocate to. If you only have 1 slot, WISH-0 will be the one you use. Wishes will follow the priority set in the WishPriorities setting and then work through cheapest wishes
- BR - Cast rituals from highest to lowest ignoring rituals you cant afford or will take more than an hour
- BR-X - Cast rituals from highest to lowest that will finish before time specified by X. BR-3600 will cast rituals that will end before the 1 hour mark from your current time.

Available non-cap priorities for Magic are as follows:

- NGU-X (0-6) - Allocate energy to NGU
- WAN - Allocate energy to magic wandoos
- TM - Allocate energy to magic time machine
- RIT-X - Allocate energy to ritual
- WISH-X (0-X) - Allocate magic to wishes. X is the wish slot you wish to allocate to. If you only have 1 slot, WISH-0 will be the one you use. Wishes will follow the priority set in the WishPriorities setting and then work through cheapest wishes

More information on allocation indexes can be found on the [wiki](https://github.com/rvazarkar/NGUInjector/wiki/Allocation-Indexes)

## R3

An R3 breakpoint is structured as follows:

```
"R3": [
    {
        "Time": 0,
        "Priorities": ["HACK-1"]
    }
]

```

Available priorities for R3 are as follows:

- (CAP)HACK-X (0-14) - Allocate R3 to hacks. Will only ever allocate to the first hack in your priority list that hasn't met its target.
- (CAP)ALLHACK - Adds every hack from 0 to 14 as a priority
- (CAP)WISH-X (0-X) - Allocate R3 to wishes. X is the wish slot you wish to allocate to. If you only have 1 slot, WISH-0 will be the one you use. Wishes will follow the priority set in the WishPriorities setting and then work through cheapest wishes

## Gear

A gear breakpoint is structured as follows:

```
"Gear": [
    {
        "Time": 0,
        "ID": [189, 442, 160, 441, 148, 169, 139, 184, 187, 185, 186, 188]
    }
]

```

The list of IDs is the IDs of the gear desired.

You can dump your loadouts from Gear Optimizer using the method found on [this wiki page](https://github.com/rvazarkar/NGUInjector/wiki/Dump-Equipment-from-GO)

## Diggers

A digger breakpoint is structured as follows:

```
"Diggers": [
    {
        "Time": 3650,
        "List": [3, 4, 5, 8]
    }
]

```

The list of diggers is 0 indexed. Drop chance digger is 0, EXP digger is 11. More information can be found on the [wiki](https://github.com/rvazarkar/NGUInjector/wiki/Allocation-Indexes)

## Wandoos

A wandoos breakpoint is structured as follows:

```
"Wandoos": [
    {
        "Time": 0,
        "OS": 1
    }
]

```

The OS is 0 indexed. Wandoos 98 is 0, Wandoos MEH is 1, Wandoos XL is 2

## NGU Difficulty

A NGU Difficulty breakpoint is structured as follows:

```
"NGUDiff": [
    {
    "Time": 0,
    "Diff": 0
    }
]
```

The difficulty is 0 indexed. Normal NGUs is 0, Evil NGUs is 1, Sadistic NGUs is 2

## Rebirth

Rebirth Breakpoints can be written 2 ways. A simple time based breakpoint is written as follows:

```
"RebirthTime": -1
```

A setting of -1 means no rebirths. Otherwise rebirth will be performed when the time in seconds is reached.

More complex rebirth breakpoints can be done as follows:

```
"Rebirth": {
    "Type": "Time",
    "Target": {
    "h": 24,
    "m": 1
    },
    "Challenges": []
},
```

The following types are available:

- **Time** - Rebirths when a certain time passes
- **Number** - Rebirths when your number will be OldNumber \* _Target_
- **Bosses** - Rebirths when you can defeat _Target_ additional bosses
- **Muffin** - Rebirths to optimize Muffin consumable usage. Will cycle between 24 hour and (24 hour - _Target_ minutes) Time rebirths. _Target_ must be at least 1 and no more than 60. Enable "Auto Buy Consumables" to auto purchase Muffins if none are available. Will simply behave as a 24 hour Time rebirth without Muffin use if any of the following conditions are met:
  - Currently in a challenge
  - Able to rebirth into any challenge defined in the Rebirth Breakpoint
  - Has not yet beaten Sadistic Troll Challenge 2
  - 5 O'Clock Shadow Perk or Beast Fertilizer Quirk aren't maxed
  - Do not have any Muffins and cannot buy more (or is configured to not buy more)
- **TimeBalancedMuffin** - Same as the Muffin rebirth except will also add the _Target_ minutes to the 24 hour rebirth to try to keep rebirths at the same time each day. _Target_ must be at least 1 and no more than 15.

The following example will rebirth when your number is 10x your previous number:

```
"Rebirth": {
    "Type": "Number",
    "Target": 10,
    "Challenges": []
},
```

The following example will rebirth when you can kill 5 more bosses than your last rebirth:

```
"Rebirth": {
    "Type": "Bosses",
    "Target": 5,
    "Challenges": []
},
```

The following example will perform "Muffin" rebirth cycles. If no muffin is active will activate a muffin and rebirth at 24:00:00, followed by a rebirth at 23:00:00:

```
"Rebirth": {
    "Type": "Muffin",
    "Target": 60,
    "Challenges": []
},
```

The following example will perform "Muffin" rebirth cycles. If no muffin is active will activate a muffin and rebirth at 24:05:00, followed by a rebirth at 23:55:00:

```
"Rebirth": {
    "Type": "TimeBalancedMuffin",
    "Target": 5,
    "Challenges": []
},
```

Additionally, you may specify challenges to rebirth into. The following string correspond to challenges:

- **BASIC** - Basic
- **NOAUG** - No Augments
- **24HR** - 24 Hour
- **100LC** - 100 Level Challenge
- **NOEC** - No Equipment
- **TC** - Troll Challenge
- **NORB** - No Rebirth
- **LSC** - Laser Sword Challenge
- **BLIND** - Blind Challenge
- **NONGU** - No NGU
- **NOTM** - No Time Machine

Challenges must be given a number afterwards to specify which challenge is being completed. Challenges start at 1. Ex:

```
"Challenges": ["BASIC-1", "BASIC-2", "TC-1","NOEQ-1"]
```

## Consumables

A Consumables breakpoint is structured as follows:

```
"Consumables": [
    {
    "Time": 0,
    "Items": ["EPOT-B", "MPOT-B"]
    }
]
```

Consumables are indexed as follows:

- **EPOT-A** - Energy Potion alpha
- **EPOT-B** - Energy Potion beta
- **EPOT-C** - Energy Potion delta
- **MPOT-A** - Magic Potion alpha
- **MPOT-B** - Magic Potion beta
- **MPOT-C** - Magic Potion delta
- **R3POT-A** - R3 Potion alpha
- **R3POT-B** - R3 Potion beta
- **R3POT-C** - R3 Potion delta
- **EBARBAR** - Energy Bar Bar
- **MBARBAR** - Magic Bar Bar
- **MUFFIN** - MacGuffin Muffin
- **LC** - Lucky Charm
- **SLC** - Super Lucky Charm
- **MAYO** - Mayo Infuser

Consumables can be modified with an optional number to use by adding a : and a number. As an example:

```
EPOT-A:2
```

This will use two Energy Potion alphas instead of one. Consumables which are active until the next rebirth (beta potions and Muffins) ignore this modification and will only ever consume one.

**IMPORTANT NOTE: BE AWARE OF THE LIMITATIONS OF CONSUMABLE BREAKPOINTS**

The injector cannot remember consumable usage across sessions or know if consumables were used manually. Therefore, if you restart NGU, restart the injector, or make any changes to/reload the profile, it **will** run your current Consumables breakpoint again. The injector will try to compensate by estimating the amount of time a consumable is expected to run and only use consumables when appropriate. But it is not perfect and there are limitations.

**_Example_**:
Let's say you have it set to use a two Lucky Charms 1 hour after rebirth.

If the profile initializes before 01:00:00 into the current rebirth, the breakpoint will execute normally at 01:00:00 and if lucky charm is not currently running, you'll use the two Lucky Charms.

However if the profile is inialized AFTER this point (due to restarting the game or reloading the profile in some way) or the consumable is running when the breakpoint executes, there are a few things which can happen:

- First we determine at what point the Lucky Charms *would* have expired if activated normally. In this case it would be two 30m charms activated at 01:00:00, so the expected end time is 02:00:00
- If the current rebirth time is within 1 minute of the expected end time or later, the consumable will not be used
  - For example, if the profile is loaded at 01:59:00 or later, no lucky charms will be used
- If however the current rebirth time is between the breakpoint time and the expected end time, we check to see if the boost is currently running and get the expected time left in the consumable by subtracting the current time from the total expected time the boost should run
  - For example, if the profile is loaded at 01:35:00, the expected time left in the boost would be 25 minutes
- If the boost is running 
  - If the "Use Consumables if already running" option is not selected, the consumable will not be used
  - If the time left on the boost is within 1 minute of the expected end time or later, the consumable will not be used
    - For example if there are 24 minutes or more left in the boost and the expected amount of time left is 25 minutes, no lucky charms will be used
- If we make it to this point, check to see how many consumables should be used (if any)
  - Get the total amount of time we need to add to the consumable timer by subtracting the time left (if any) from the total expected time left that the boost should be running
    - For example if the profile is loaded at 01:35:00 and there are 0 minutes left in the boost time, the amount of time we'd need to add is 25 minutes (so the lucky charm expires at 02:00:00). If there is 15 minutes left in the boost time we'd need to add 10 minutes.
  - Get the number of consumables to use by dividing the amount of time to add by the amount of time each consumable adds to the timer and rounding the result to get as close to the expiration as possible
    - For example if we need to add 25 minutes to the boost time, we'd consume 1 of the 2 lucky charms: 25/30 = .833 => 1. If we need to add 10 minutes to the boost time we'd consume 0 of the 2 lucky charms: 10/30 = .333 => 0

Beta potions and MacGuffin Muffins only activate if not already active. MacGuffin Muffin is a special case in that it lasts until the next rebirth AND for 24 hours, whichever is longer. If MacGuffin Muffin has gone through a rebirth, it will no longer be "Active" and will use the same timing logic as other non-beta consumables.

There is **NO** consideration for the possibility that a consumable was used manually, two of the same consumable being in the same breakpoint, multiple breakpoints overlapping, or the fact that alpha and delta potions share a timer. Injector will evaluate whether to use each consumable independant of any other configured consumables or external consumable use.

Be mindful of these limitations or your AP will be sad. No one likes sad AP.

# Zone Stat Overrides

The optimal zone for gold sniping is calculated using a set of values from pins that show stats necessary to do each zone. If the manual threshold is met for a zone, the script will snipe a boss without fast combat. If the idle threshold is met, the script will snipe a boss using fast combat. Beast Mode will always be turned off for this.

The stats for the zones can be manually overriden using the `zoneOverrides.json` file in the user's directory. For the default stats used see [here](https://github.com/rvazarkar/NGUInjector/wiki/Default-Zone-Stats-for-Sniping)

# Other Features

## Quicksave + NGUSav.ES JSON

Pressing the F3 button will dump a NGU save file as well as a ngusav.es JSON file in the AppData NGUInjector folder, which can be loaded into Gear Optimizer.

## Quickload

Pressing the F7 button will load the save created by the quicksave function

## Dump Equipment to Log

Pressing the F5 button will dump your current equipment to the log file for use in allocation files

## Monitor Loot Log File

The adventure log will automatically be monitored and loot dropped will be written to loot.log in the AppData NGUInjector folder.

## Monitor Money Pit/Daily Spin/Yggdrasil Results

The pitspin.log file will contain the results of each automatic money pit, daily spin, and yggdrasil harvest and will persist across sessions.

## Quick Disable/Enable

Pressing the F2 button will globally disable all features of the app until re-enabled.

## Turn on Autoattack in ITOPOD

Have you forgotten to turn on auto attack before and left yourself AFK in ITOPOD doing nothing? Me too! Pressing the F4 button will toggle the script to automatically turn auto attack on if you're in ITOPOD and its not on.

## Auto Merge Boost to 100

Lock a boost in your inventory and the script will automatically merge boosts on to that boost until it hits level 100!

# Acknowledgements

SharpMonoInjector can be found [here](https://github.com/warbler/SharpMonoInjector). This wouldn't be possible without the excellent work done there.
Obviously 4G for making NGU Idle.
Altizar on discord for WISH logic, cooking logic and some other fixes
Ingwar on discord for allocation profile logic and some other fixes

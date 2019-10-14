﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;
using NativeUI;
using System.Windows.Forms;


namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// the nativeUI-implementing class
    /// -------------thanks to the NativeUI developers!-------------
    /// </summary>
    public class MenuScript
    {
        private readonly MenuPool menuPool;
        private readonly UIMenu zonesMenu, gangMenu, memberMenu, carMenu, warAttackStrengthMenu;
        private UIMenu gangOptionsSubMenu, warOptionsSubMenu, weaponsMenu, specificGangMemberRegSubMenu, specificCarRegSubMenu,
            modSettingsSubMenu;
        private Ped closestPed;
        private int memberStyle = 0, memberColor = 0;
        private int healthUpgradeCost, armorUpgradeCost, accuracyUpgradeCost, gangValueUpgradeCost, curZoneValueUpgradeCost,
            warLightAtkCost, warMedAtkCost, warLargeAtkCost, warMassAtkCost;
        private readonly Dictionary<BuyableWeapon, UIMenuCheckboxItem> buyableWeaponCheckboxesDict =
            new Dictionary<BuyableWeapon, UIMenuCheckboxItem>();
        private readonly Dictionary<VehicleColor, UIMenuItem> carColorEntries =
            new Dictionary<VehicleColor, UIMenuItem>();
        private int playerGangOriginalBlipColor = 0;
        private readonly Dictionary<string, int> blipColorEntries = new Dictionary<string, int>
        {
            {"white", 0 },
            {"white-2", 4 },
            {"white snowy", 13 },
            {"red", 1 },
            {"red-2", 6 },
            {"dark red", 76 },
            {"green", 2 },
            {"green-2", 11 },
            {"dark green", 25 },
            {"darker green", 52 },
            {"turquoise", 15 },
            {"blue", 3 },
            {"light blue", 18 },
            {"dark blue", 38 },
            {"darker blue", 54 },
            {"purple", 7 },
            {"purple-2", 19 },
            {"dark purple", 27 },
            {"dark purple-2", 83 },
            {"very dark purple", 58 },
            {"orange", 17 },
            {"orange-2", 51 },
            {"orange-3", 44 },
            {"gray", 20 },
            {"light gray", 39 },
            {"brown", 21 },
            {"beige", 56 },
            {"pink", 23 },
            {"pink-2", 8 },
            {"smooth pink", 41 },
            {"strong pink", 48 },
            {"black", 40 }, //as close as it gets
            {"yellow", 66 },
            {"gold-ish", 28 },
            {"yellow-2", 46 },
            {"light yellow", 33 },
        };
        private UIMenuItem healthButton, armorButton, accuracyButton, takeZoneButton, upgradeGangValueBtn, upgradeZoneValueBtn,
            openGangMenuBtn, openZoneMenuBtn, mindControlBtn, addToGroupBtn, carBackupBtn, paraBackupBtn,
            warLightAtkBtn, warMedAtkBtn, warLargeAtkBtn, warMassAtkBtn;

        private int ticksSinceLastCarBkp = 5000, ticksSinceLastParaBkp = 5000;

        public enum DesiredInputType
        {
            none,
            enterGangName,
            changeKeyBinding
        }

        public static MenuScript Instance { get; private set; }

        public ModOptions ModOptions { get; set; }

        public UIMenuListItem AggOption { get; private set; }

        public DesiredInputType CurInputType { get; set; } = DesiredInputType.none;
        public ModKeyBindings.ChangeableKeyBinding TargetKeyBindToChange { get; set; } = ModKeyBindings.ChangeableKeyBinding.AddGroupBtn;

        public MenuScript()
        {
            Instance = this;
            zonesMenu = new UIMenu("Gang Mod", "Zone Controls");
            memberMenu = new UIMenu("Gang Mod", "Gang Member Registration Controls");
            carMenu = new UIMenu("Gang Mod", "Gang Vehicle Registration Controls");
            gangMenu = new UIMenu("Gang Mod", "Gang Controls");
            warAttackStrengthMenu = new UIMenu("Gang Mod", "Gang War Attack Options");
            menuPool = new MenuPool();
            menuPool.Add(zonesMenu);
            menuPool.Add(gangMenu);
            menuPool.Add(memberMenu);
            menuPool.Add(carMenu);
            menuPool.Add(warAttackStrengthMenu);

            AddGangTakeoverButton();
            AddGangWarAtkOptions();
            AddZoneUpgradeButton();
            AddAbandonZoneButton();
            AddSaveZoneButton();
            //AddZoneCircleButton();

            AddMemberStyleChoices();
            AddSaveMemberButton();
            AddNewPlayerGangMemberButton();
            AddNewEnemyMemberSubMenu();
            AddRemoveGangMemberButton();
            AddRemoveFromAllGangsButton();
            AddMakeFriendlyToPlayerGangButton();

            AddCallBackupBtns();

            AddSaveVehicleButton();
            AddRegisterPlayerVehicleButton();
            AddRegisterEnemyVehicleButton();
            AddRemovePlayerVehicleButton();
            AddRemoveVehicleEverywhereButton();

            //UpdateBuyableWeapons();
            AddWarOptionsSubMenu();
            AddGangOptionsSubMenu();
            AddModSettingsSubMenu();

            zonesMenu.RefreshIndex();
            gangMenu.RefreshIndex();
            memberMenu.RefreshIndex();
            warAttackStrengthMenu.RefreshIndex();

            AggOption.Index = (int)ModOptions.MemberAIOptions.GangMemberAggressiveness;

            //add mouse click as another "select" button
            menuPool.SetKey(UIMenu.MenuControls.Select, Control.PhoneSelect);
            InstructionalButton clickButton = new InstructionalButton(Control.PhoneSelect, "Select");
            zonesMenu.AddInstructionalButton(clickButton);
            gangMenu.AddInstructionalButton(clickButton);
            memberMenu.AddInstructionalButton(clickButton);
            warAttackStrengthMenu.AddInstructionalButton(clickButton);

            ticksSinceLastCarBkp = ModOptions.BackupOptions.TicksCooldownBackupCar;
            ticksSinceLastParaBkp = ModOptions.BackupOptions.TicksCooldownParachutingMember;
        }

        #region menu opening methods
        public void OpenGangMenu()
        {
            if (!menuPool.IsAnyMenuOpen())
            {
                UpdateUpgradeCosts();
                //UpdateBuyableWeapons();
                gangMenu.Visible = !gangMenu.Visible;
            }
        }

        public void OpenContextualRegistrationMenu()
        {
            if (!menuPool.IsAnyMenuOpen())
            {
                if (MindControl.CurrentPlayerCharacter.CurrentVehicle == null)
                {
                    closestPed = World.GetClosestPed(MindControl.CurrentPlayerCharacter.Position + MindControl.CurrentPlayerCharacter.ForwardVector * 6.0f, 5.5f);
                    if (closestPed != null)
                    {
                        UI.ShowSubtitle("ped selected!");
                        World.AddExplosion(closestPed.Position, ExplosionType.Steam, 1.0f, 0.1f);
                    }
                    else
                    {
                        UI.ShowSubtitle("Couldn't find a ped in front of you! You have selected yourself.");
                        closestPed = MindControl.CurrentPlayerCharacter;
                        World.AddExplosion(closestPed.Position, ExplosionType.Extinguisher, 1.0f, 0.1f);
                    }

                    memberMenu.Visible = !memberMenu.Visible;
                }
                else
                {
                    UI.ShowSubtitle("vehicle selected!");
                    carMenu.Visible = !carMenu.Visible;
                }
                RefreshNewEnemyMenuContent();
            }
        }

        public void OpenZoneMenu()
        {
            if (!menuPool.IsAnyMenuOpen())
            {
                ZoneManager.instance.OutputCurrentZoneInfo();
                UpdateZoneUpgradeBtn();
                zonesMenu.Visible = !zonesMenu.Visible;
            }
        }
        #endregion


        public void Tick()
        {
            menuPool.ProcessMenus();

            if (CurInputType == DesiredInputType.enterGangName)
            {
                int inputFieldSituation = Function.Call<int>(Hash.UPDATE_ONSCREEN_KEYBOARD);
                if (inputFieldSituation == 1)
                {
                    string typedText = Function.Call<string>(Hash.GET_ONSCREEN_KEYBOARD_RESULT);
                    if (typedText != "none" && GangManager.GetGangByName(typedText) == null)
                    {
                        ZoneManager.instance.GiveGangZonesToAnother(GangManager.PlayerGang.name, typedText);
                        GangManager.PlayerGang.name = typedText;
                        GangManager.SaveGangData();

                        UI.ShowSubtitle("Your gang is now known as the " + typedText);
                    }
                    else
                    {
                        UI.ShowSubtitle("That name is not allowed, sorry! (It may be in use already)");
                    }

                    CurInputType = DesiredInputType.none;
                }
                else if (inputFieldSituation == 2 || inputFieldSituation == 3)
                {
                    CurInputType = DesiredInputType.none;
                }
            }

            //countdown for next backups
            ticksSinceLastCarBkp++;
            if (ticksSinceLastCarBkp > ModOptions.BackupOptions.TicksCooldownBackupCar)
                ticksSinceLastCarBkp = ModOptions.BackupOptions.TicksCooldownBackupCar;
            ticksSinceLastParaBkp++;
            if (ticksSinceLastParaBkp > ModOptions.BackupOptions.TicksCooldownParachutingMember)
                ticksSinceLastParaBkp = ModOptions.BackupOptions.TicksCooldownParachutingMember;
        }

        private void UpdateUpgradeCosts()
        {
            Gang playerGang = GangManager.PlayerGang;
            healthUpgradeCost = GangCalculations.CalculateHealthUpgradeCost(playerGang.memberHealth);
            armorUpgradeCost = GangCalculations.CalculateArmorUpgradeCost(playerGang.memberArmor);
            accuracyUpgradeCost = GangCalculations.CalculateAccuracyUpgradeCost(playerGang.memberAccuracyLevel);
            gangValueUpgradeCost = GangCalculations.CalculateGangValueUpgradeCost(playerGang.baseTurfValue);

            healthButton.Text = "Upgrade Member Health - " + healthUpgradeCost.ToString();
            armorButton.Text = "Upgrade Member Armor - " + armorUpgradeCost.ToString();
            accuracyButton.Text = "Upgrade Member Accuracy - " + accuracyUpgradeCost.ToString();
            upgradeGangValueBtn.Text = "Upgrade Gang Base Strength - " + gangValueUpgradeCost.ToString();
        }

        private void UpdateTakeOverBtnText()
        {
            takeZoneButton.Description = "Makes the zone you are in become part of your gang's turf. If it's not controlled by any gang, it will instantly become yours for a price of $" +
                ModOptions.ZoneOptions.BaseCostToTakeTurf.ToString() + ". If it belongs to another gang, a battle will begin!";
        }

        private void UpdateZoneUpgradeBtn()
        {
            string curZoneName = World.GetZoneName(MindControl.CurrentPlayerCharacter.Position);
            TurfZone curZone = ZoneManager.instance.GetZoneByName(curZoneName);
            if (curZone == null)
            {
                upgradeZoneValueBtn.Text = "Upgrade current zone - (Not takeable)";
            }
            else
            {
                curZoneValueUpgradeCost = GangCalculations.CalculateTurfValueUpgradeCost(curZone.value);
                upgradeZoneValueBtn.Text = "Upgrade current zone - " + curZoneValueUpgradeCost.ToString();
            }

        }

        private void FillCarColorEntries()
        {
            foreach (GangColorTranslation colorList in ModOptions.GangColorsOptions.SimilarColors)
            {
                for (int i = 0; i < colorList.VehicleColors.Count; i++)
                {
                    carColorEntries.Add(colorList.VehicleColors[i], new UIMenuItem(colorList.VehicleColors[i].ToString(), "Colors can be previewed if you are inside a vehicle. Click or press enter to confirm the gang color change."));
                }

            }

            if (ModOptions.GangColorsOptions.ExtraPlayerExclusiveColors == null)
            {
                ModOptions.GangColorsOptions.SetColorTranslationDefaultValues();
            }
            //and the extra colors, only chooseable by the player!
            foreach (VehicleColor extraColor in ModOptions.GangColorsOptions.ExtraPlayerExclusiveColors)
            {
                carColorEntries.Add(extraColor, new UIMenuItem(extraColor.ToString(), "Colors can be previewed if you are inside a vehicle. Click or press enter to confirm the gang color change."));
            }
        }

        public void RefreshKeyBindings()
        {
            openGangMenuBtn.Text = "Gang Control Key - " + ModOptions.Keys.OpenGangMenuKey.ToString();
            openZoneMenuBtn.Text = "Zone Control Key - " + ModOptions.Keys.OpenZoneMenuKey.ToString();
            addToGroupBtn.Text = "Add or Remove Member from Group - " + ModOptions.Keys.AddToGroupKey.ToString();
            mindControlBtn.Text = "Take Control of Member - " + ModOptions.Keys.MindControlKey.ToString();
        }

        #region Zone Menu Stuff

        private void AddSaveZoneButton()
        {
            UIMenuItem saveZoneBtn = new UIMenuItem("Add Current Zone to Takeables", "Makes the zone you are in become takeable by gangs and sets your position as the zone's reference position (if toggled, this zone's blip will show here).");
            zonesMenu.AddItem(saveZoneBtn);
            zonesMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == saveZoneBtn)
                {
                    string curZoneName = World.GetZoneName(MindControl.CurrentPlayerCharacter.Position);
                    TurfZone curZone = ZoneManager.instance.GetZoneByName(curZoneName);
                    if (curZone == null)
                    {
                        //add a new zone then
                        curZone = new TurfZone(curZoneName);
                    }

                    //update the zone's blip position even if it already existed
                    curZone.zoneBlipPosition = MindControl.CurrentPlayerCharacter.Position;
                    ZoneManager.instance.UpdateZoneData(curZone);
                    UI.ShowSubtitle("Zone Data Updated!");
                }
            };

        }

        private void AddGangTakeoverButton()
        {
            takeZoneButton = new UIMenuItem("Take current zone",
                "Makes the zone you are in become part of your gang's turf. If it's not controlled by any gang, it will instantly become yours for a price of $" +
                ModOptions.ZoneOptions.BaseCostToTakeTurf.ToString() + ". If it belongs to another gang, a battle will begin!");
            zonesMenu.AddItem(takeZoneButton);
            zonesMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == takeZoneButton)
                {
                    string curZoneName = World.GetZoneName(MindControl.CurrentPlayerCharacter.Position);
                    TurfZone curZone = ZoneManager.instance.GetZoneByName(curZoneName);
                    if (curZone == null)
                    {
                        UI.ShowSubtitle("this zone isn't marked as takeable.");
                    }
                    else
                    {
                        if (curZone.ownerGangName == "none")
                        {
                            if (MindControl.AddOrSubtractMoneyToProtagonist(-ModOptions.ZoneOptions.BaseCostToTakeTurf))
                            {
                                GangManager.PlayerGang.TakeZone(curZone);
                                UI.ShowSubtitle("This zone is " + GangManager.PlayerGang.name + " turf now!");
                            }
                            else
                            {
                                UI.ShowSubtitle("You don't have the resources to take over a neutral zone.");
                            }
                        }
                        else
                        {
                            if (curZone.ownerGangName == GangManager.PlayerGang.name)
                            {
                                UI.ShowSubtitle("Your gang already owns this zone.");
                            }
                            else
                            {
                                zonesMenu.Visible = !zonesMenu.Visible;
                                UpdateGangWarAtkOptions(curZone);
                                warAttackStrengthMenu.Visible = true;
                            }
                        }
                    }
                }
            };
        }

        private void UpdateGangWarAtkOptions(TurfZone targetZone)
        {
            Gang enemyGang = GangManager.GetGangByName(targetZone.ownerGangName);
            Gang playerGang = GangManager.PlayerGang;
            int defenderNumbers = GangCalculations.CalculateDefenderReinforcements(enemyGang, targetZone);
            warLightAtkCost = GangCalculations.CalculateAttackCost(playerGang, GangWarManager.AttackStrength.light);
            warMedAtkCost = GangCalculations.CalculateAttackCost(playerGang, GangWarManager.AttackStrength.medium);
            warLargeAtkCost = GangCalculations.CalculateAttackCost(playerGang, GangWarManager.AttackStrength.large);
            warMassAtkCost = GangCalculations.CalculateAttackCost(playerGang, GangWarManager.AttackStrength.massive);

            warLightAtkBtn.Text = "Light Attack - " + warLightAtkCost.ToString();
            warMedAtkBtn.Text = "Medium Attack - " + warMedAtkCost.ToString();
            warLargeAtkBtn.Text = "Large Attack - " + warLargeAtkCost.ToString();
            warMassAtkBtn.Text = "Massive Attack - " + warMassAtkCost.ToString();

            warLightAtkBtn.Description = GetReinforcementsComparisonMsg(GangWarManager.AttackStrength.light, defenderNumbers);
            warMedAtkBtn.Description = GetReinforcementsComparisonMsg(GangWarManager.AttackStrength.medium, defenderNumbers);
            warLargeAtkBtn.Description = GetReinforcementsComparisonMsg(GangWarManager.AttackStrength.large, defenderNumbers);
            warMassAtkBtn.Description = GetReinforcementsComparisonMsg(GangWarManager.AttackStrength.massive, defenderNumbers);

        }

        private static string GetReinforcementsComparisonMsg(GangWarManager.AttackStrength atkStrength, int defenderNumbers)
        {
            return string.Concat("We will have ",
                GangCalculations.CalculateAttackerReinforcements(GangManager.PlayerGang, atkStrength), " members against their ",
                defenderNumbers.ToString());
        }

        private void AddGangWarAtkOptions()
        {

            Gang playerGang = GangManager.PlayerGang;

            warLightAtkBtn = new UIMenuItem("Attack", "Attack. (Text set elsewhere)"); //those are updated when this menu is opened (UpdateGangWarAtkOptions)
            warMedAtkBtn = new UIMenuItem("Attack", "Attack.");
            warLargeAtkBtn = new UIMenuItem("Attack", "Attack.");
            warMassAtkBtn = new UIMenuItem("Attack", "Attack.");
            UIMenuItem cancelBtn = new UIMenuItem("Cancel", "Cancels the attack. No money is lost for canceling.");
            warAttackStrengthMenu.AddItem(warLightAtkBtn);
            warAttackStrengthMenu.AddItem(warMedAtkBtn);
            warAttackStrengthMenu.AddItem(warLargeAtkBtn);
            warAttackStrengthMenu.AddItem(warMassAtkBtn);
            warAttackStrengthMenu.AddItem(cancelBtn);

            warAttackStrengthMenu.OnItemSelect += (sender, item, index) =>
            {
                string curZoneName = World.GetZoneName(MindControl.CurrentPlayerCharacter.Position);
                TurfZone curZone = ZoneManager.instance.GetZoneByName(curZoneName);
                if (item == warLightAtkBtn)
                {
                    if (TryStartWar(warLightAtkCost, curZone, GangWarManager.AttackStrength.light)) warAttackStrengthMenu.Visible = false;
                }
                if (item == warMedAtkBtn)
                {
                    if (TryStartWar(warMedAtkCost, curZone, GangWarManager.AttackStrength.medium)) warAttackStrengthMenu.Visible = false;
                }
                if (item == warLargeAtkBtn)
                {
                    if (TryStartWar(warLargeAtkCost, curZone, GangWarManager.AttackStrength.large)) warAttackStrengthMenu.Visible = false;
                }
                if (item == warMassAtkBtn)
                {
                    if (TryStartWar(warMassAtkCost, curZone, GangWarManager.AttackStrength.massive)) warAttackStrengthMenu.Visible = false;
                }
                else
                {
                    warAttackStrengthMenu.Visible = false;
                }
            };
        }

        private static bool TryStartWar(int atkCost, TurfZone targetZone, GangWarManager.AttackStrength atkStrength)
        {
            if (targetZone.ownerGangName == GangManager.PlayerGang.name)
            {
                UI.ShowSubtitle("You can't start a war against your own gang! (You probably have changed zones after opening this menu)");
                return false;
            }


            if (MindControl.AddOrSubtractMoneyToProtagonist(-atkCost, true))
            {
                if (!GangWarManager.instance.StartWar(GangManager.GetGangByName(targetZone.ownerGangName), targetZone, GangWarManager.WarType.attackingEnemy, atkStrength))
                {
                    UI.ShowSubtitle("A war is already in progress.");
                    return false;
                }
                else
                {
                    MindControl.AddOrSubtractMoneyToProtagonist(-atkCost);
                }
                return true;
            }
            else
            {
                UI.ShowSubtitle("You don't have the resources to start a battle of this size.");
                return false;
            }
        }

        private void AddZoneUpgradeButton()
        {
            upgradeZoneValueBtn = new UIMenuItem("Upgrade current zone",
                "Increases this zone's level. This level affects the income provided, the reinforcements available in a war and the presence of police in that zone. The zone's level is reset when it is taken by another gang. The level limit is configurable via the ModOptions file.");
            zonesMenu.AddItem(upgradeZoneValueBtn);
            zonesMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == upgradeZoneValueBtn)
                {
                    string curZoneName = World.GetZoneName(MindControl.CurrentPlayerCharacter.Position);
                    TurfZone curZone = ZoneManager.instance.GetZoneByName(curZoneName);
                    if (curZone == null)
                    {
                        UI.ShowSubtitle("this zone isn't marked as takeable.");
                    }
                    else
                    {
                        if (curZone.ownerGangName == GangManager.PlayerGang.name)
                        {
                            if (MindControl.AddOrSubtractMoneyToProtagonist(-curZoneValueUpgradeCost, true))
                            {
                                if (curZone.value >= ModOptions.ZoneOptions.MaxTurfLevel)
                                {
                                    UI.ShowSubtitle("This zone's level is already maxed!");
                                }
                                else
                                {
                                    curZone.value++;
                                    ZoneManager.instance.IsDirty = true;
                                    UI.ShowSubtitle("Zone level increased!");
                                    MindControl.AddOrSubtractMoneyToProtagonist(-curZoneValueUpgradeCost);
                                    UpdateZoneUpgradeBtn();

                                }
                            }
                            else
                            {
                                UI.ShowSubtitle("You don't have the resources to upgrade this zone.");
                            }
                        }
                        else
                        {
                            UI.ShowSubtitle("You can only upgrade zones owned by your gang!");
                        }

                    }
                }
            };
        }

        private void AddAbandonZoneButton()
        {
            UIMenuItem newButton = new UIMenuItem("Abandon Zone", "If the zone you are in is controlled by your gang, it instantly becomes neutral. You receive part of the money used for upgrading the zone.");
            zonesMenu.AddItem(newButton);
            zonesMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    string curZoneName = World.GetZoneName(MindControl.CurrentPlayerCharacter.Position);
                    TurfZone curZone = ZoneManager.instance.GetZoneByName(curZoneName);
                    if (curZone == null)
                    {
                        UI.ShowSubtitle("This zone hasn't been marked as takeable.");
                    }
                    else
                    {
                        if (curZone.ownerGangName == GangManager.PlayerGang.name)
                        {
                            if (ModOptions.NotificationsEnabled)
                            {
                                UI.Notify(string.Concat("The ", curZone.ownerGangName, " have abandoned ",
                                    curZone.zoneName, ". It has become a neutral zone again."));
                            }
                            curZone.ownerGangName = "none";
                            curZone.value = 0;

                            int valueDifference = curZone.value - GangManager.PlayerGang.baseTurfValue;
                            if (valueDifference > 0)
                            {
                                MindControl.AddOrSubtractMoneyToProtagonist
                                (ModOptions.ZoneOptions.BaseCostToUpgradeSingleTurfLevel * valueDifference);
                            }

                            UI.ShowSubtitle(curZone.zoneName + " is now neutral again.");
                            ZoneManager.instance.UpdateZoneData(curZone);
                        }
                        else
                        {
                            UI.ShowSubtitle("Your gang does not own this zone.");
                        }
                    }
                }
            };
        }

        #endregion

        #region register Member/Vehicle Stuff

        private void AddMemberStyleChoices()
        {
            List<dynamic> memberStyles = new List<dynamic>
            {
                "Business",
                "Street",
                "Beach",
                "Special"
            };

            List<dynamic> memberColors = new List<dynamic>
            {
                "White",
                "Black",
                "Red",
                "Green",
                "Blue",
                "Yellow",
                "Gray",
                "Pink",
                "Purple"
            };

            UIMenuListItem styleList = new UIMenuListItem("Member Dressing Style", memberStyles, 0, "The way the selected member is dressed. Used by the AI when picking members (if the AI gang's chosen style is the same as this member's, it may choose this member).");
            UIMenuListItem colorList = new UIMenuListItem("Member Color", memberColors, 0, "The color the member will be assigned to. Used by the AI when picking members (if the AI gang's color is the same as this member's, it may choose this member).");
            memberMenu.AddItem(styleList);
            memberMenu.AddItem(colorList);
            memberMenu.OnListChange += (sender, item, index) =>
            {
                if (item == styleList)
                {
                    memberStyle = item.Index;
                }
                else if (item == colorList)
                {
                    memberColor = item.Index;
                }

            };

        }

        private void AddSaveMemberButton()
        {
            UIMenuItem newButton = new UIMenuItem("Save Potential Member for future AI gangs", "Saves the selected ped as a potential gang member with the specified data. AI gangs will be able to choose him\\her.");
            memberMenu.AddItem(newButton);
            memberMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01)
                    {
                        if (PotentialGangMember.AddMemberAndSavePool(new FreemodePotentialGangMember(closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                        {
                            UI.ShowSubtitle("Potential freemode member added!");
                        }
                        else
                        {
                            UI.ShowSubtitle("A similar potential member already exists.");
                        }
                    }
                    else
                    {
                        if (PotentialGangMember.AddMemberAndSavePool(new PotentialGangMember(closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                        {
                            UI.ShowSubtitle("Potential member added!");
                        }
                        else
                        {
                            UI.ShowSubtitle("A similar potential member already exists.");
                        }
                    }

                }
            };
        }

        private void AddNewPlayerGangMemberButton()
        {
            UIMenuItem newButton = new UIMenuItem("Save ped type for your gang", "Saves the selected ped type as a member of your gang, with the specified data. The selected ped himself won't be a member, however.");
            memberMenu.AddItem(newButton);
            memberMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01)
                    {
                        if (GangManager.PlayerGang.AddMemberVariation(new FreemodePotentialGangMember
                       (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                        {
                            UI.ShowSubtitle("Freemode Member added successfully!");
                        }
                        else
                        {
                            UI.ShowSubtitle("Your gang already has a similar member.");
                        }
                    }
                    else
                    {
                        if (GangManager.PlayerGang.AddMemberVariation(new PotentialGangMember
                       (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                        {
                            UI.ShowSubtitle("Member added successfully!");
                        }
                        else
                        {
                            UI.ShowSubtitle("Your gang already has a similar member.");
                        }
                    }

                }
            };
        }

        private void AddNewEnemyMemberSubMenu()
        {
            specificGangMemberRegSubMenu = menuPool.AddSubMenu(memberMenu, "Save ped type for a specific enemy gang...");

            specificGangMemberRegSubMenu.OnItemSelect += (sender, item, index) =>
            {
                Gang pickedGang = GangManager.GetGangByName(item.Text);
                if (pickedGang != null)
                {
                    if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01)
                    {
                        if (pickedGang.AddMemberVariation(new FreemodePotentialGangMember
                       (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                        {
                            UI.ShowSubtitle("Freemode Member added successfully!");
                        }
                        else
                        {
                            UI.ShowSubtitle("That gang already has a similar member.");
                        }
                    }
                    else
                    {
                        if (pickedGang.AddMemberVariation(new PotentialGangMember
                       (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                        {
                            UI.ShowSubtitle("Member added successfully!");
                        }
                        else
                        {
                            UI.ShowSubtitle("That gang already has a similar member.");
                        }
                    }
                }
            };

        }

        /// <summary>
        /// removes all options and then adds all gangs that are not controlled by the player as chooseable options in the "Save for a specific gang" submenus
        /// </summary>
        public void RefreshNewEnemyMenuContent()
        {
            specificGangMemberRegSubMenu.Clear();
            specificCarRegSubMenu.Clear();

            List<Gang> gangsList = GangManager.GangData.Gangs;

            for (int i = 0; i < gangsList.Count; i++)
            {
                if (!gangsList[i].isPlayerOwned)
                {
                    specificGangMemberRegSubMenu.AddItem(new UIMenuItem(gangsList[i].name));
                    specificCarRegSubMenu.AddItem(new UIMenuItem(gangsList[i].name));
                }
            }

            specificGangMemberRegSubMenu.RefreshIndex();
            specificCarRegSubMenu.RefreshIndex();

        }

        private void AddRemoveGangMemberButton()
        {
            UIMenuItem newButton = new UIMenuItem("Remove ped type from respective gang", "If the selected ped type was a member of a gang, it will no longer be. The selected ped himself will still be a member, however. This works for your own gang and for the enemies.");
            memberMenu.AddItem(newButton);
            memberMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    Gang ownerGang = GangManager.GetGangByRelGroup(closestPed.RelationshipGroup);
                    if (ownerGang == null)
                    {
                        UI.ShowSubtitle("The ped doesn't seem to be in a gang.", 8000);
                        return;
                    }
                    if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01)
                    {
                        if (ownerGang.RemoveMemberVariation(new FreemodePotentialGangMember
                        (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                        {
                            UI.ShowSubtitle("Member removed successfully!");
                        }
                        else
                        {
                            UI.ShowSubtitle("The ped doesn't seem to be in a gang.", 8000);
                        }
                    }
                    else
                    {
                        if (ownerGang.RemoveMemberVariation(new PotentialGangMember
                        (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                        {
                            UI.ShowSubtitle("Member removed successfully!");
                        }
                        else
                        {
                            UI.ShowSubtitle("The ped doesn't seem to be in a gang.", 8000);
                        }
                    }
                }
            };
        }

        private void AddRemoveFromAllGangsButton()
        {
            UIMenuItem newButton = new UIMenuItem("Remove ped type from all gangs and pool", "Removes the ped type from all gangs and from the member pool, which means future gangs also won't try to use this type. The selected ped himself will still be a gang member, however.");
            memberMenu.AddItem(newButton);
            memberMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01)
                    {
                        FreemodePotentialGangMember memberToRemove = new FreemodePotentialGangMember
                   (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor);

                        if (PotentialGangMember.RemoveMemberAndSavePool(memberToRemove))
                        {
                            UI.ShowSubtitle("Ped type removed from pool! (It might not be the only similar ped in the pool)");
                        }
                        else
                        {
                            UI.ShowSubtitle("Ped type not found in pool.");
                        }

                        for (int i = 0; i < GangManager.GangData.Gangs.Count; i++)
                        {
                            GangManager.GangData.Gangs[i].RemoveMemberVariation(memberToRemove);
                        }
                    }
                    else
                    {
                        PotentialGangMember memberToRemove = new PotentialGangMember
                   (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor);

                        if (PotentialGangMember.RemoveMemberAndSavePool(memberToRemove))
                        {
                            UI.ShowSubtitle("Ped type removed from pool! (It might not be the only similar ped in the pool)");
                        }
                        else
                        {
                            UI.ShowSubtitle("Ped type not found in pool.");
                        }

                        for (int i = 0; i < GangManager.GangData.Gangs.Count; i++)
                        {
                            GangManager.GangData.Gangs[i].RemoveMemberVariation(memberToRemove);
                        }
                    }
                }
            };
        }

        private void AddMakeFriendlyToPlayerGangButton()
        {
            UIMenuItem newButton = new UIMenuItem("Make Ped friendly to your gang", "Makes the selected ped (and everyone from his group) and your gang become allies. Can't be used with cops or gangs from this mod! NOTE: this only lasts until scripts are loaded again");
            memberMenu.AddItem(newButton);
            memberMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    int closestPedRelGroup = closestPed.RelationshipGroup;
                    //check if we can really become allies with this guy
                    if (closestPedRelGroup != Function.Call<int>(Hash.GET_HASH_KEY, "COP"))
                    {
                        //he can still be from one of the gangs! we should check

                        if (GangManager.GetGangByRelGroup(closestPedRelGroup) != null)
                        {
                            UI.ShowSubtitle("That ped is a gang member! Gang members cannot be marked as allies");
                            return;
                        }

                        //ok, we can be allies
                        Gang playerGang = GangManager.PlayerGang;
                        World.SetRelationshipBetweenGroups(Relationship.Respect, playerGang.relationGroupIndex, closestPedRelGroup);
                        World.SetRelationshipBetweenGroups(Relationship.Respect, closestPedRelGroup, playerGang.relationGroupIndex);
                        UI.ShowSubtitle("That ped's group is now an allied group!");
                    }
                    else
                    {
                        UI.ShowSubtitle("That ped is a cop! Cops cannot be marked as allies");
                    }
                }
            };
        }

        private void AddSaveVehicleButton()
        {
            UIMenuItem newButton = new UIMenuItem("Register Vehicle as usable by AI Gangs", "Makes the vehicle type you are driving become chooseable as one of the types used by AI gangs.");
            carMenu.AddItem(newButton);
            carMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                    if (curVehicle != null)
                    {
                        if (PotentialGangVehicle.AddVehicleAndSavePool(new PotentialGangVehicle(curVehicle.Model.Hash)))
                        {
                            UI.ShowSubtitle("Vehicle added to pool!");
                        }
                        else
                        {
                            UI.ShowSubtitle("That vehicle has already been added to the pool.");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You are not inside a vehicle.");
                    }
                }
            };
        }

        private void AddRegisterPlayerVehicleButton()
        {
            UIMenuItem newButton = new UIMenuItem("Register Vehicle for your Gang", "Makes the vehicle type you are driving become one of the default types used by your gang.");
            carMenu.AddItem(newButton);
            carMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                    if (curVehicle != null)
                    {
                        if (GangManager.PlayerGang.AddGangCar(new PotentialGangVehicle(curVehicle.Model.Hash)))
                        {
                            UI.ShowSubtitle("Gang vehicle added!");
                        }
                        else
                        {
                            UI.ShowSubtitle("That vehicle is already registered for your gang.");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You are not inside a vehicle.");
                    }
                }
            };
        }

        private void AddRegisterEnemyVehicleButton()
        {
            specificCarRegSubMenu = menuPool.AddSubMenu(carMenu, "Register vehicle for a specific enemy gang...");

            specificCarRegSubMenu.OnItemSelect += (sender, item, index) =>
            {
                Gang pickedGang = GangManager.GetGangByName(item.Text);
                if (pickedGang != null)
                {
                    Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                    if (curVehicle != null)
                    {
                        if (pickedGang.AddGangCar(new PotentialGangVehicle(curVehicle.Model.Hash)))
                        {
                            UI.ShowSubtitle("Gang vehicle added!");
                        }
                        else
                        {
                            UI.ShowSubtitle("That vehicle is already registered for that gang.");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You are not inside a vehicle.");
                    }
                }
            };
        }

        private void AddRemovePlayerVehicleButton()
        {
            UIMenuItem newButton = new UIMenuItem("Remove Vehicle Type from your Gang", "Removes the vehicle type you are driving from the possible vehicle types for your gang.");
            carMenu.AddItem(newButton);
            carMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                    if (curVehicle != null)
                    {
                        if (GangManager.PlayerGang.RemoveGangCar(new PotentialGangVehicle(curVehicle.Model.Hash)))
                        {
                            UI.ShowSubtitle("Gang vehicle removed!");
                        }
                        else
                        {
                            UI.ShowSubtitle("That vehicle is not registered for your gang.");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You are not inside a vehicle.");
                    }
                }
            };
        }

        private void AddRemoveVehicleEverywhereButton()
        {
            UIMenuItem newButton = new UIMenuItem("Remove Vehicle Type from all gangs and pool", "Removes the vehicle type you are driving from the possible vehicle types for all gangs, including yours. Existing gangs will also stop using that car and get another one if needed.");
            carMenu.AddItem(newButton);
            carMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                    if (curVehicle != null)
                    {
                        PotentialGangVehicle removedVehicle = new PotentialGangVehicle(curVehicle.Model.Hash);

                        if (PotentialGangVehicle.RemoveVehicleAndSavePool(removedVehicle))
                        {
                            UI.ShowSubtitle("Vehicle type removed from pool!");
                        }
                        else
                        {
                            UI.ShowSubtitle("Vehicle type not found in pool.");
                        }

                        for (int i = 0; i < GangManager.GangData.Gangs.Count; i++)
                        {
                            GangManager.GangData.Gangs[i].RemoveGangCar(removedVehicle);
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You are not inside a vehicle.");
                    }
                }
            };
        }

        #endregion

        #region Gang Control Stuff

        public void CallCarBackup(bool showMenu = true)
        {
            if (ticksSinceLastCarBkp < ModOptions.BackupOptions.TicksCooldownBackupCar)
            {
                UI.ShowSubtitle("You must wait before calling for car backup again! (This is configurable)");
                return;
            }
            if (MindControl.AddOrSubtractMoneyToProtagonist(-ModOptions.BackupOptions.CostToCallBackupCar, true))
            {
                Gang playergang = GangManager.PlayerGang;
                if (ZoneManager.instance.GetZonesControlledByGang(playergang.name).Count > 0)
                {
                    Math.Vector3 destPos = MindControl.SafePositionNearPlayer;

                    Math.Vector3 spawnPos = SpawnManager.instance.FindGoodSpawnPointForCar(destPos);

                    SpawnedDrivingGangMember spawnedVehicle = SpawnManager.instance.SpawnGangVehicle(GangManager.PlayerGang,
                            spawnPos, destPos, true, true);
                    if (spawnedVehicle != null)
                    {
                        if (showMenu)
                        {
                            gangMenu.Visible = !gangMenu.Visible;
                        }
                        ticksSinceLastCarBkp = 0;
                        MindControl.AddOrSubtractMoneyToProtagonist(-ModOptions.BackupOptions.CostToCallBackupCar);
                        UI.ShowSubtitle("A vehicle is on its way!", 1000);
                    }
                    else
                    {
                        UI.ShowSubtitle("There are too many gang members around or you haven't registered any member or car.");
                    }
                }
                else
                {
                    UI.ShowSubtitle("You need to have control of at least one territory in order to call for backup.");
                }
            }
            else
            {
                UI.ShowSubtitle("You need $" + ModOptions.BackupOptions.CostToCallBackupCar.ToString() + " to call a vehicle!");
            }
        }

        private void AddCallBackupBtns()
        {
            carBackupBtn = new UIMenuItem("Call Backup Vehicle ($" + ModOptions.BackupOptions.CostToCallBackupCar.ToString() + ")", "Calls one of your gang's vehicles to your position. All passengers leave the vehicle once it arrives.");
            paraBackupBtn = new UIMenuItem("Call Parachuting Member ($" + ModOptions.BackupOptions.CostToCallParachutingMember.ToString() + ")", "Calls a gang member who parachutes to your position (member survival not guaranteed!).");

            gangMenu.AddItem(carBackupBtn);
            gangMenu.AddItem(paraBackupBtn);
            gangMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == carBackupBtn)
                {
                    CallCarBackup();

                }

                if (item == paraBackupBtn)
                {
                    if (ticksSinceLastParaBkp < ModOptions.BackupOptions.TicksCooldownParachutingMember)
                    {
                        UI.ShowSubtitle("You must wait before calling for parachuting backup again! (This is configurable)");
                        return;
                    }

                    if (MindControl.AddOrSubtractMoneyToProtagonist(-ModOptions.BackupOptions.CostToCallParachutingMember, true))
                    {
                        Gang playergang = GangManager.PlayerGang;
                        //only allow spawning if the player has turf
                        if (ZoneManager.instance.GetZonesControlledByGang(playergang.name).Count > 0)
                        {
                            Ped spawnedPed = SpawnManager.instance.SpawnParachutingMember(GangManager.PlayerGang,
                       MindControl.CurrentPlayerCharacter.Position + Math.Vector3.WorldUp * 50, MindControl.SafePositionNearPlayer);
                            if (spawnedPed != null)
                            {
                                ticksSinceLastParaBkp = 0;
                                MindControl.AddOrSubtractMoneyToProtagonist(-ModOptions.BackupOptions.CostToCallParachutingMember);
                                gangMenu.Visible = !gangMenu.Visible;
                            }
                            else
                            {
                                UI.ShowSubtitle("There are too many gang members around or you haven't registered any member.");
                            }
                        }
                        else
                        {
                            UI.ShowSubtitle("You need to have control of at least one territory in order to call for backup.");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You need $" + ModOptions.BackupOptions.CostToCallParachutingMember.ToString() + " to call a parachuting member!");
                    }

                }
            };


        }

        private void AddWarOptionsSubMenu()
        {
            warOptionsSubMenu = menuPool.AddSubMenu(gangMenu, "War Options Menu");

            UIMenuItem skipWarBtn = new UIMenuItem("Skip current War",
               "If a war is currently occurring, it will instantly end, and its outcome will be defined by the strength and reinforcements of the involved gangs and a touch of randomness.");
            UIMenuItem resetAlliedSpawnBtn = new UIMenuItem("Set allied spawn points to your region",
                "If a war is currently occurring, your gang members will keep spawning at the 3 allied spawn points for as long as you've got reinforcements. This option sets all 3 spawn points to your location: one exactly where you are and 2 nearby.");
            UIMenuItem resetEnemySpawnBtn = new UIMenuItem("Force reset enemy spawn points",
                "If a war is currently occurring, the enemy spawn points will be randomly set to a nearby location. Use this if they end up spawning somewhere unreachable.");

            warOptionsSubMenu.AddItem(skipWarBtn);
            warOptionsSubMenu.AddItem(resetAlliedSpawnBtn);

            UIMenuItem[] setSpecificSpawnBtns = new UIMenuItem[3];
            for (int i = 0; i < setSpecificSpawnBtns.Length; i++)
            {
                setSpecificSpawnBtns[i] = new UIMenuItem(string.Concat("Set allied spawn point ", (i + 1).ToString(), " to your position"),
                    string.Concat("If a war is currently occurring, your gang members will keep spawning at the 3 allied spawn points for as long as you've got reinforcements. This option sets spawn point number ",
                        (i + 1).ToString(), " to your exact location."));
                warOptionsSubMenu.AddItem(setSpecificSpawnBtns[i]);
            }

            warOptionsSubMenu.AddItem(resetEnemySpawnBtn);

            warOptionsSubMenu.OnItemSelect += (sender, item, index) =>
            {
                if (GangWarManager.instance.isOccurring)
                {
                    if (item == skipWarBtn)
                    {

                        GangWarManager.instance.EndWar(GangWarManager.instance.SkipWar(0.9f));
                    }
                    else

                    if (item == resetAlliedSpawnBtn)
                    {
                        if (GangWarManager.instance.playerNearWarzone)
                        {
                            GangWarManager.instance.ForceSetAlliedSpawnPoints(MindControl.SafePositionNearPlayer);
                        }
                        else
                        {
                            UI.ShowSubtitle("You must be in the contested zone or close to the war blip before setting the spawn point!");
                        }
                    }
                    else

                    if (item == resetEnemySpawnBtn)
                    {
                        if (GangWarManager.instance.playerNearWarzone)
                        {
                            if (GangWarManager.instance.ReplaceEnemySpawnPoint())
                            {
                                UI.ShowSubtitle("Enemy spawn point reset succeeded!");
                            }
                            else
                            {
                                UI.ShowSubtitle("Enemy spawn point reset failed (try again)!");
                            }
                        }
                        else
                        {
                            UI.ShowSubtitle("You must be in the contested zone or close to the war blip before resetting spawn points!");
                        }
                    }
                    else
                    {
                        for (int i = 0; i < setSpecificSpawnBtns.Length; i++)
                        {
                            if (item == setSpecificSpawnBtns[i])
                            {
                                if (GangWarManager.instance.playerNearWarzone)
                                {
                                    GangWarManager.instance.SetSpecificAlliedSpawnPoint(i, MindControl.SafePositionNearPlayer);
                                }
                                else
                                {
                                    UI.ShowSubtitle("You must be in the contested zone or close to the war blip before setting the spawn point!");
                                }

                                break;
                            }

                        }
                    }


                }
                else
                {
                    UI.ShowSubtitle("There is no war in progress.");
                }

            };

            warOptionsSubMenu.RefreshIndex();
        }

        private void AddGangOptionsSubMenu()
        {
            gangOptionsSubMenu = menuPool.AddSubMenu(gangMenu, "Gang Customization/Upgrades Menu");

            AddGangUpgradesMenu();
            AddGangWeaponsMenu();
            AddSetCarColorMenu();
            AddSetBlipColorMenu();
            AddRenameGangButton();

            gangOptionsSubMenu.RefreshIndex();
        }

        private void AddGangUpgradesMenu()
        {
            UIMenu upgradesMenu = menuPool.AddSubMenu(gangOptionsSubMenu, "Gang Upgrades...");

            //upgrade buttons
            healthButton = new UIMenuItem("Upgrade Member Health - " + healthUpgradeCost.ToString(), "Increases gang member starting and maximum health. The cost increases with the amount of upgrades made. The limit is configurable via the ModOptions file.");
            armorButton = new UIMenuItem("Upgrade Member Armor - " + armorUpgradeCost.ToString(), "Increases gang member starting body armor. The cost increases with the amount of upgrades made. The limit is configurable via the ModOptions file.");
            accuracyButton = new UIMenuItem("Upgrade Member Accuracy - " + accuracyUpgradeCost.ToString(), "Increases gang member firing accuracy. The cost increases with the amount of upgrades made. The limit is configurable via the ModOptions file.");
            upgradeGangValueBtn = new UIMenuItem("Upgrade Gang Base Strength - " + gangValueUpgradeCost.ToString(), "Increases the level territories have after you take them. This level affects the income provided, the reinforcements available in a war and reduces general police presence. The limit is configurable via the ModOptions file.");
            upgradesMenu.AddItem(healthButton);
            upgradesMenu.AddItem(armorButton);
            upgradesMenu.AddItem(accuracyButton);
            upgradesMenu.AddItem(upgradeGangValueBtn);
            upgradesMenu.RefreshIndex();

            upgradesMenu.OnItemSelect += (sender, item, index) =>
            {
                Gang playerGang = GangManager.PlayerGang;

                if (item == healthButton)
                {
                    if (MindControl.AddOrSubtractMoneyToProtagonist(-healthUpgradeCost, true))
                    {
                        if (playerGang.memberHealth < ModOptions.MemberUpgradeOptions.MaxGangMemberHealth)
                        {
                            playerGang.memberHealth += ModOptions.MemberUpgradeOptions.GetHealthUpgradeIncrement();
                            if (playerGang.memberHealth > ModOptions.MemberUpgradeOptions.MaxGangMemberHealth)
                            {
                                playerGang.memberHealth = ModOptions.MemberUpgradeOptions.MaxGangMemberHealth;
                            }
                            MindControl.AddOrSubtractMoneyToProtagonist(-healthUpgradeCost);
                            GangManager.SaveGangData();
                            UI.ShowSubtitle("Member health upgraded!");
                        }
                        else
                        {
                            UI.ShowSubtitle("Your members' health is at its maximum limit (it can be configured in the ModOptions file)");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You don't have enough money to buy that upgrade.");
                    }
                }

                if (item == armorButton)
                {
                    if (MindControl.AddOrSubtractMoneyToProtagonist(-armorUpgradeCost, true))
                    {
                        if (playerGang.memberArmor < ModOptions.MemberUpgradeOptions.MaxGangMemberArmor)
                        {
                            playerGang.memberArmor += ModOptions.MemberUpgradeOptions.GetArmorUpgradeIncrement();
                            if (playerGang.memberArmor > ModOptions.MemberUpgradeOptions.MaxGangMemberArmor)
                            {
                                playerGang.memberArmor = ModOptions.MemberUpgradeOptions.MaxGangMemberArmor;
                            }
                            MindControl.AddOrSubtractMoneyToProtagonist(-armorUpgradeCost);
                            GangManager.SaveGangData();
                            UI.ShowSubtitle("Member armor upgraded!");
                        }
                        else
                        {
                            UI.ShowSubtitle("Your members' armor is at its maximum limit (it can be configured in the ModOptions file)");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You don't have enough money to buy that upgrade.");
                    }
                }

                if (item == accuracyButton)
                {
                    if (MindControl.AddOrSubtractMoneyToProtagonist(-accuracyUpgradeCost, true))
                    {
                        if (playerGang.memberAccuracyLevel < ModOptions.MemberUpgradeOptions.MaxGangMemberAccuracy)
                        {
                            playerGang.memberAccuracyLevel += ModOptions.MemberUpgradeOptions.GetAccuracyUpgradeIncrement();
                            if (playerGang.memberAccuracyLevel > ModOptions.MemberUpgradeOptions.MaxGangMemberAccuracy)
                            {
                                playerGang.memberAccuracyLevel = ModOptions.MemberUpgradeOptions.MaxGangMemberAccuracy;
                            }
                            MindControl.AddOrSubtractMoneyToProtagonist(-accuracyUpgradeCost);
                            GangManager.SaveGangData();
                            UI.ShowSubtitle("Member accuracy upgraded!");
                        }
                        else
                        {
                            UI.ShowSubtitle("Your members' accuracy is at its maximum limit (it can be configured in the ModOptions file)");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You don't have enough money to buy that upgrade.");
                    }
                }

                if (item == upgradeGangValueBtn)
                {
                    if (MindControl.AddOrSubtractMoneyToProtagonist(-gangValueUpgradeCost, true))
                    {
                        if (playerGang.baseTurfValue < ModOptions.ZoneOptions.MaxTurfLevel)
                        {
                            playerGang.baseTurfValue++;
                            if (playerGang.baseTurfValue > ModOptions.ZoneOptions.MaxTurfLevel)
                            {
                                playerGang.baseTurfValue = ModOptions.ZoneOptions.MaxTurfLevel;
                            }
                            MindControl.AddOrSubtractMoneyToProtagonist(-gangValueUpgradeCost);
                            GangManager.SaveGangData();
                            UI.ShowSubtitle("Gang Base Strength upgraded!");
                        }
                        else
                        {
                            UI.ShowSubtitle("Your Gang Base Strength is at its maximum limit (it can be configured in the ModOptions file)");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You don't have enough money to buy that upgrade.");
                    }
                }

                UpdateUpgradeCosts();

            };

        }

        private void AddGangWeaponsMenu()
        {
            weaponsMenu = menuPool.AddSubMenu(gangOptionsSubMenu, "Gang Weapons Menu");

            Gang playerGang = GangManager.PlayerGang;

            gangOptionsSubMenu.OnMenuChange += (oldMenu, newMenu, forward) =>
            {
                if (newMenu == weaponsMenu)
                {
                    RefreshBuyableWeaponsMenuContent();
                }
            };

            weaponsMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                foreach (KeyValuePair<BuyableWeapon, UIMenuCheckboxItem> kvp in buyableWeaponCheckboxesDict)
                {
                    if (kvp.Value == item)
                    {
                        if (playerGang.gangWeaponHashes.Contains(kvp.Key.WepHash))
                        {
                            playerGang.gangWeaponHashes.Remove(kvp.Key.WepHash);
                            MindControl.AddOrSubtractMoneyToProtagonist(kvp.Key.Price);
                            GangManager.SaveGangData();
                            UI.ShowSubtitle("Weapon Removed!");
                            item.Checked = false;
                        }
                        else
                        {
                            if (MindControl.AddOrSubtractMoneyToProtagonist(-kvp.Key.Price))
                            {
                                playerGang.gangWeaponHashes.Add(kvp.Key.WepHash);
                                GangManager.SaveGangData();
                                UI.ShowSubtitle("Weapon Bought!");
                                item.Checked = true;
                            }
                            else
                            {
                                UI.ShowSubtitle("You don't have enough money to buy that weapon for your gang.");
                                item.Checked = false;
                            }
                        }

                        break;
                    }
                }

            };
        }

        /// <summary>
        /// removes all options and then adds all gangs that are not controlled by the player as chooseable options in the "Save ped type for a specific gang..." submenu
        /// </summary>
        public void RefreshBuyableWeaponsMenuContent()
        {
            weaponsMenu.Clear();

            buyableWeaponCheckboxesDict.Clear();

            List<BuyableWeapon> weaponsList = ModOptions.WeaponOptions.BuyableWeapons;

            Gang playerGang = GangManager.PlayerGang;

            for (int i = 0; i < weaponsList.Count; i++)
            {
                UIMenuCheckboxItem weaponCheckBox = new UIMenuCheckboxItem
                        (string.Concat(weaponsList[i].WepHash.ToString(), " - ", weaponsList[i].Price.ToString()),
                        playerGang.gangWeaponHashes.Contains(weaponsList[i].WepHash));
                buyableWeaponCheckboxesDict.Add(weaponsList[i], weaponCheckBox);
                weaponsMenu.AddItem(weaponCheckBox);
            }

            weaponsMenu.RefreshIndex();
        }

        private void AddSetCarColorMenu()
        {
            FillCarColorEntries();

            UIMenu carColorsMenu = menuPool.AddSubMenu(gangOptionsSubMenu, "Gang Vehicle Colors Menu");

            Gang playerGang = GangManager.PlayerGang;

            VehicleColor[] carColorsArray = carColorEntries.Keys.ToArray();
            UIMenuItem[] colorButtonsArray = carColorEntries.Values.ToArray();

            for (int i = 0; i < colorButtonsArray.Length; i++)
            {
                carColorsMenu.AddItem(colorButtonsArray[i]);
            }

            carColorsMenu.RefreshIndex();

            carColorsMenu.OnIndexChange += (sender, index) =>
            {
                Vehicle playerVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                if (playerVehicle != null)
                {
                    playerVehicle.PrimaryColor = carColorsArray[index];
                }
            };

            carColorsMenu.OnItemSelect += (sender, item, checked_) =>
            {
                for (int i = 0; i < carColorsArray.Length; i++)
                {
                    if (item == carColorEntries[carColorsArray[i]])
                    {
                        playerGang.vehicleColor = carColorsArray[i];
                        GangManager.SaveGangData(false);
                        UI.ShowSubtitle("Gang vehicle color changed!");
                        break;
                    }
                }

            };
        }

        private void AddSetBlipColorMenu()
        {

            UIMenu blipColorsMenu = menuPool.AddSubMenu(gangOptionsSubMenu, "Gang Blip Colors Menu");

            Gang playerGang = GangManager.PlayerGang;

            string[] blipColorNamesArray = blipColorEntries.Keys.ToArray();
            int[] colorCodesArray = blipColorEntries.Values.ToArray();

            for (int i = 0; i < colorCodesArray.Length; i++)
            {
                blipColorsMenu.AddItem(new UIMenuItem(blipColorNamesArray[i], "The color change can be seen immediately on turf blips. Click or press enter after selecting a color to save the color change."));
            }

            blipColorsMenu.RefreshIndex();

            blipColorsMenu.OnIndexChange += (sender, index) =>
            {
                GangManager.PlayerGang.blipColor = colorCodesArray[index];
                ZoneManager.instance.RefreshZoneBlips();
            };

            gangOptionsSubMenu.OnMenuChange += (oldMenu, newMenu, forward) =>
            {
                if (newMenu == blipColorsMenu)
                {
                    playerGangOriginalBlipColor = GangManager.PlayerGang.blipColor;
                }
            };

            blipColorsMenu.OnMenuClose += (sender) =>
            {
                GangManager.PlayerGang.blipColor = playerGangOriginalBlipColor;
                ZoneManager.instance.RefreshZoneBlips();
            };

            blipColorsMenu.OnItemSelect += (sender, item, checked_) =>
            {
                for (int i = 0; i < blipColorNamesArray.Length; i++)
                {
                    if (item.Text == blipColorNamesArray[i])
                    {
                        GangManager.PlayerGang.blipColor = colorCodesArray[i];
                        playerGangOriginalBlipColor = colorCodesArray[i];
                        GangManager.SaveGangData(false);
                        UI.ShowSubtitle("Gang blip color changed!");
                        break;
                    }
                }

            };
        }

        private void AddRenameGangButton()
        {
            UIMenuItem newButton = new UIMenuItem("Rename Gang", "Resets your gang's name.");
            gangOptionsSubMenu.AddItem(newButton);
            gangOptionsSubMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    gangOptionsSubMenu.Visible = !gangOptionsSubMenu.Visible;
                    Function.Call(Hash.DISPLAY_ONSCREEN_KEYBOARD, false, "FMMC_KEY_TIP12N", "", GangManager.PlayerGang.name, "", "", "", 30);
                    CurInputType = DesiredInputType.enterGangName;
                }
            };
        }

        #endregion

        #region Mod Settings

        private void AddModSettingsSubMenu()
        {
            modSettingsSubMenu = menuPool.AddSubMenu(gangMenu, "Mod Settings Menu");

            AddNotificationsToggle();
            AddMemberAggressivenessControl();
            AddEnableAmbientSpawnToggle();
            AddAiExpansionToggle();
            AddShowMemberBlipsToggle();
            AddMeleeOnlyToggle();
            AddEnableWarVersusPlayerToggle();
            AddEnableCarTeleportToggle();
            AddGangsStartWithPistolToggle();
            AddKeyBindingMenu();
            AddGamepadControlsToggle();
            AddForceAIGangsTickButton();
            AddForceAIAttackButton();
            AddReloadOptionsButton();
            AddResetWeaponOptionsButton();
            AddResetOptionsButton();

            modSettingsSubMenu.RefreshIndex();
        }

        private void AddNotificationsToggle()
        {
            UIMenuCheckboxItem notifyToggle = new UIMenuCheckboxItem("Notifications enabled?", ModOptions.NotificationsEnabled, "Enables/disables the displaying of messages whenever a gang takes over a zone.");

            modSettingsSubMenu.AddItem(notifyToggle);
            modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == notifyToggle)
                {
                    ModOptions.NotificationsEnabled = checked_;
                    ModOptions.SaveOptions(false);
                }

            };
        }

        private void AddMemberAggressivenessControl()
        {
            List<dynamic> aggModes = new List<dynamic>
            {
                "V. Aggressive",
                "Aggressive",
                "Defensive"
            };

            AggOption = new UIMenuListItem("Member Aggressiveness", aggModes, (int)ModOptions.MemberAIOptions.GangMemberAggressiveness, "This controls how aggressive members from all gangs will be. Very aggressive members will shoot at cops and other gangs on sight, aggressive members will shoot only at other gangs on sight and defensive members will only shoot when one of them is attacked or aimed at.");
            modSettingsSubMenu.AddItem(AggOption);
            modSettingsSubMenu.OnListChange += (sender, item, index) =>
            {
                if (item == AggOption)
                {
                    ModOptions.MemberAIOptions.SetMemberAggressiveness((MemberAIOptions.AggressivenessMode)index);
                }
            };
        }

        private void AddAiExpansionToggle()
        {
            UIMenuCheckboxItem aiToggle = new UIMenuCheckboxItem("Prevent AI Gangs' Expansion?", ModOptions.GangAIOptions.PreventAIExpansion, "If checked, AI Gangs won't start wars or take neutral zones.");

            modSettingsSubMenu.AddItem(aiToggle);
            modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == aiToggle)
                {
                    ModOptions.GangAIOptions.PreventAIExpansion = checked_;
                    ModOptions.SaveOptions(false);
                }

            };
        }

        private void AddMeleeOnlyToggle()
        {
            UIMenuCheckboxItem meleeToggle = new UIMenuCheckboxItem("Gang members use melee weapons only?", ModOptions.SpawningOptions.MembersSpawnWithMeleeOnly, "If checked, all gang members will spawn with melee weapons only, even if they purchase firearms or are set to start with pistols.");

            modSettingsSubMenu.AddItem(meleeToggle);
            modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == meleeToggle)
                {
                    ModOptions.SpawningOptions.MembersSpawnWithMeleeOnly = checked_;
                    ModOptions.SaveOptions(false);
                }

            };
        }

        private void AddEnableWarVersusPlayerToggle()
        {
            UIMenuCheckboxItem warToggle = new UIMenuCheckboxItem("Enemy gangs can attack your turf?", ModOptions.WarOptions.WarAgainstPlayerEnabled, "If unchecked, enemy gangs won't start a war against you, but you will still be able to start a war against them.");

            modSettingsSubMenu.AddItem(warToggle);
            modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == warToggle)
                {
                    ModOptions.WarOptions.WarAgainstPlayerEnabled = checked_;
                    ModOptions.SaveOptions(false);
                }

            };
        }

        private void AddEnableAmbientSpawnToggle()
        {
            UIMenuCheckboxItem spawnToggle = new UIMenuCheckboxItem("Ambient member spawning?", ModOptions.SpawningOptions.AmbientSpawningEnabled, "If enabled, members from the gang which owns the zone you are in will spawn once in a while. This option does not affect member spawning via backup calls or gang wars.");

            modSettingsSubMenu.AddItem(spawnToggle);
            modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == spawnToggle)
                {
                    ModOptions.SpawningOptions.AmbientSpawningEnabled = checked_;
                    ModOptions.SaveOptions(false);
                }

            };
        }

        private void AddShowMemberBlipsToggle()
        {
            UIMenuCheckboxItem blipToggle = new UIMenuCheckboxItem("Show Member and Car Blips?", ModOptions.SpawningOptions.ShowGangMemberBlips, "If disabled, members and cars won't spawn with blips attached to them. (This option only affects those that spawn after the option is set)");

            modSettingsSubMenu.AddItem(blipToggle);
            modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == blipToggle)
                {
                    ModOptions.SpawningOptions.ShowGangMemberBlips = checked_;
                    ModOptions.SaveOptions(false);
                }

            };
        }

        private void AddEnableCarTeleportToggle()
        {
            UIMenuCheckboxItem spawnToggle = new UIMenuCheckboxItem("Backup cars can teleport to always arrive?", ModOptions.SpawningOptions.ForceSpawnCars, "If enabled, backup cars, after taking too long to get to the player, will teleport close by. This will only affect friendly vehicles.");

            modSettingsSubMenu.AddItem(spawnToggle);
            modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == spawnToggle)
                {
                    ModOptions.SpawningOptions.ForceSpawnCars = checked_;
                    ModOptions.SaveOptions(false);
                }

            };
        }

        private void AddGangsStartWithPistolToggle()
        {
            UIMenuCheckboxItem pistolToggle = new UIMenuCheckboxItem("Gangs start with Pistols?", ModOptions.GangsOptions.GangsStartWithPistols, "If checked, all gangs, except the player's, will start with pistols. Pistols will not be given to gangs already in town.");

            modSettingsSubMenu.AddItem(pistolToggle);
            modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == pistolToggle)
                {
                    ModOptions.GangsOptions.GangsStartWithPistols = checked_;
                    ModOptions.SaveOptions(false);
                }

            };
        }

        private void AddGamepadControlsToggle()
        {
            UIMenuCheckboxItem padToggle = new UIMenuCheckboxItem("Use joypad controls?", ModOptions.Keys.JoypadControls, "Enables/disables the use of joypad commands to recruit members (pad right), call backup (pad left) and output zone info (pad up). Commands are used while aiming. All credit goes to zixum.");

            modSettingsSubMenu.AddItem(padToggle);
            modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == padToggle)
                {
                    ModOptions.Keys.JoypadControls = checked_;
                    if (checked_)
                    {
                        UI.ShowSubtitle("Joypad controls activated. Remember to disable them when not using a joypad, as it is possible to use the commands with mouse/keyboard as well");
                    }
                    ModOptions.SaveOptions(false);
                }

            };
        }

        private void AddKeyBindingMenu()
        {
            UIMenu bindingsMenu = menuPool.AddSubMenu(modSettingsSubMenu, "Key Bindings...");

            //the buttons
            openGangMenuBtn = new UIMenuItem("Gang Control Key - " + ModOptions.Keys.OpenGangMenuKey.ToString(), "The key used to open the Gang/Mod Menu. Used with shift to open the Member Registration Menu. Default is B.");
            openZoneMenuBtn = new UIMenuItem("Zone Control Key - " + ModOptions.Keys.OpenZoneMenuKey.ToString(), "The key used to check the current zone's name and ownership. Used with shift to open the Zone Menu and with control to toggle zone blip display modes. Default is N.");
            addToGroupBtn = new UIMenuItem("Add or Remove Member from Group - " + ModOptions.Keys.AddToGroupKey.ToString(), "The key used to add/remove the targeted friendly gang member to/from your group. Members of your group will follow you. Default is H.");
            mindControlBtn = new UIMenuItem("Take Control of Member - " + ModOptions.Keys.MindControlKey.ToString(), "The key used to take control of the targeted friendly gang member. Pressing this key while already in control of a member will restore protagonist control. Default is J.");
            bindingsMenu.AddItem(openGangMenuBtn);
            bindingsMenu.AddItem(openZoneMenuBtn);
            bindingsMenu.AddItem(addToGroupBtn);
            bindingsMenu.AddItem(mindControlBtn);
            bindingsMenu.RefreshIndex();

            bindingsMenu.OnItemSelect += (sender, item, index) =>
            {
                UI.ShowSubtitle("Press the new key for this command.");
                CurInputType = DesiredInputType.changeKeyBinding;

                if (item == openGangMenuBtn)
                {
                    TargetKeyBindToChange = ModKeyBindings.ChangeableKeyBinding.GangMenuBtn;
                }
                if (item == openZoneMenuBtn)
                {
                    TargetKeyBindToChange = ModKeyBindings.ChangeableKeyBinding.ZoneMenuBtn;
                }
                if (item == addToGroupBtn)
                {
                    TargetKeyBindToChange = ModKeyBindings.ChangeableKeyBinding.AddGroupBtn;
                }
                if (item == mindControlBtn)
                {
                    TargetKeyBindToChange = ModKeyBindings.ChangeableKeyBinding.MindControlBtn;
                }
            };
        }

        private void AddForceAIGangsTickButton()
        {
            UIMenuItem newButton = new UIMenuItem("Run an Update on all AI Gangs", "Makes all AI Gangs try to upgrade themselves and/or invade other territories immediately. Their normal updates, which happen from time to time (configurable in the ModOptions file), will still happen normally after this.");
            modSettingsSubMenu.AddItem(newButton);
            modSettingsSubMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    GangManager.ForceTickAIGangs();
                }
            };
        }

        private void AddForceAIAttackButton()
        {
            UIMenuItem newButton = new UIMenuItem("Force an AI Gang to Attack this zone", "If you control the current zone, makes a random AI Gang attack it, starting a war. The AI gang won't spend money to make this attack.");
            modSettingsSubMenu.AddItem(newButton);
            modSettingsSubMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    GangAI enemyAttackerAI = RandoMath.GetRandomElementFromList(GangManager.EnemyGangs);
                    if (enemyAttackerAI != null)
                    {
                        TurfZone curZone = ZoneManager.instance.GetCurrentTurfZone();
                        if (curZone != null)
                        {
                            if (curZone.ownerGangName == GangManager.PlayerGang.name)
                            {
                                if (!GangWarManager.instance.StartWar(enemyAttackerAI.WatchedGang, curZone,
                                    GangWarManager.WarType.defendingFromEnemy, GangWarManager.AttackStrength.medium))
                                {
                                    UI.ShowSubtitle("Couldn't start a war. Is a war already in progress?");
                                }
                            }
                            else
                            {
                                UI.ShowSubtitle("The zone you are in is not controlled by your gang.");
                            }
                        }
                        else
                        {
                            UI.ShowSubtitle("The zone you are in has not been marked as takeable.");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("There aren't any enemy gangs in San Andreas!");
                    }
                }
            };
        }

        private void AddReloadOptionsButton()
        {
            UIMenuItem newButton = new UIMenuItem("Reload Mod Options", "Reload the settings defined by the ModOptions file. Use this if you tweaked the ModOptions file while playing for its new settings to take effect.");
            modSettingsSubMenu.AddItem(newButton);
            modSettingsSubMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    ModOptions.LoadOptions();
                    GangManager.ResetGangUpdateIntervals();
                    GangManager.AdjustGangsToModOptions();
                    UpdateUpgradeCosts();
                    carBackupBtn.Text = "Call Backup Vehicle ($" + ModOptions.BackupOptions.CostToCallBackupCar.ToString() + ")";
                    this.paraBackupBtn.Text = "Call Parachuting Member ($" + ModOptions.BackupOptions.CostToCallParachutingMember.ToString() + ")";
                    UpdateTakeOverBtnText();
                }
            };
        }

        private void AddResetWeaponOptionsButton()
        {
            UIMenuItem newButton = new UIMenuItem("Reset Weapon List and Prices to Defaults", "Resets the weapon list in the ModOptions file back to the default values. The new options take effect immediately.");
            modSettingsSubMenu.AddItem(newButton);
            modSettingsSubMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    ModOptions.WeaponOptions.BuyableWeapons.Clear();
                    ModOptions.WeaponOptions.SetWeaponListDefaultValues();
                    ModOptions.SaveOptions(false);
                    UpdateUpgradeCosts();
                }
            };
        }

        private void AddResetOptionsButton()
        {
            UIMenuItem newButton = new UIMenuItem("Reset Mod Options to Defaults", "Resets all the options in the ModOptions file back to the default values (except the possible gang first and last names). The new options take effect immediately.");
            modSettingsSubMenu.AddItem(newButton);
            modSettingsSubMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    ModOptions.SetAllValuesToDefault();
                    UpdateUpgradeCosts();
                    UpdateTakeOverBtnText();
                }
            };
        }

        #endregion
    }
}

using GTA.Native;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NativeUI;
using System.Drawing;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// the script is responsble for ticking and detecting input for the more sensitive scripts.
    /// some, like the gang war manager, are still ticking on their own.
    /// this one exists to make sure the sensitive ones start running in the correct order
    /// </summary>
    internal class ModCore : Script
    {
        public MenuScript menuScript;
        public ZoneManager zoneManagerScript;

        public static int curGameTime;

        public ModOptions ModOptions;

        public ModCore()
        {
            curGameTime = Game.GameTime;

            ModOptions = new ModOptions();

            zoneManagerScript = new ZoneManager();
            GangManager.Init(ModOptions);
            menuScript = new MenuScript();

            //classes below are all singletons, so no need to hold their ref here
            new SpawnManager(ModOptions.SpawningOptions, ModOptions.WeaponOptions, ModOptions.MemberAIOptions);

            this.Aborted += OnAbort;

            this.KeyUp += OnKeyUp;
            this.Tick += OnTick;

            bool successfulInit = GangMemberUpdater.Initialize();

            while (successfulInit == false)
            {
                Yield();
                successfulInit = GangMemberUpdater.Initialize();
            }

            successfulInit = GangVehicleUpdater.Initialize();

            while (successfulInit == false)
            {
                Yield();
                successfulInit = GangVehicleUpdater.Initialize();
            }

            Logger.Log("mod started!", 2);
        }

        private void OnTick(object sender, EventArgs e)
        {
            curGameTime = Game.GameTime;
            GangManager.Tick();
            MindControl.Tick();
            menuScript.Tick();

            //war stuff that should happen every frame
            if (GangWarManager.instance.shouldDisplayReinforcementsTexts)
            {
                GangWarManager.instance.alliedNumText.Draw();
                GangWarManager.instance.enemyNumText.Draw();

                if (ModOptions.WarOptions.EmptyZoneDuringWar)
                {
                    Function.Call(Hash.SET_PED_DENSITY_MULTIPLIER_THIS_FRAME, 0);
                    Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0);
                }

            }

            //zix attempt controller recruit
            if (ModOptions.Keys.JoypadControls)
            {
                if (Game.IsControlPressed(GTA.Control.Aim) || Game.IsControlPressed(GTA.Control.AccurateAim))
                {
                    if (Game.IsControlJustPressed(GTA.Control.ScriptPadRight))
                    {
                        RecruitGangMember();
                    }

                    if (Game.IsControlJustPressed(GTA.Control.ScriptPadLeft))
                    {
                        menuScript.CallCarBackup(false);
                    }

                    if (Game.IsControlJustPressed(GTA.Control.ScriptPadUp))
                    {
                        zoneManagerScript.OutputCurrentZoneInfo();
                    }
                }
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (menuScript.CurInputType == MenuScript.DesiredInputType.changeKeyBinding)
            {
                if (e.KeyCode != Keys.Enter)
                {
                    ModOptions.Keys.SetKey(menuScript.TargetKeyBindToChange, e.KeyCode, ModOptions);
                    menuScript.CurInputType = MenuScript.DesiredInputType.none;
                    menuScript.RefreshKeyBindings();
                }
            }
            else
            {

                if (e.KeyCode == ModOptions.Keys.OpenGangMenuKey)
                {
                    //note: numpad keys dont seem to go along well with shift
                    if (e.Modifiers == Keys.None)
                    {
                        menuScript.OpenGangMenu();
                    }
                    else if (e.Modifiers == Keys.Shift)
                    {
                        menuScript.OpenContextualRegistrationMenu();
                    }
                }
                else if (e.KeyCode == ModOptions.Keys.OpenZoneMenuKey)
                {
                    if (e.Modifiers == Keys.None)
                    {
                        zoneManagerScript.OutputCurrentZoneInfo();
                    }
                    else if (e.Modifiers == Keys.Shift)
                    {
                        zoneManagerScript.OutputCurrentZoneInfo();
                        menuScript.OpenZoneMenu();
                    }
                    else if (e.Modifiers == Keys.Control)
                    {
                        zoneManagerScript.ChangeBlipDisplay();
                    }

                }
                else if (e.KeyCode == ModOptions.Keys.AddToGroupKey)
                {
                    RecruitGangMember();
                }
                else if (e.KeyCode == ModOptions.Keys.MindControlKey)
                {
                    MindControl.TryBodyChange();
                }
                else if (e.KeyCode == Keys.Space)
                {
                    if (MindControl.HasChangedBody)
                    {
                        MindControl.RespawnIfPossible();
                    }
                }
            }

        }

        /// <summary>
        /// adds a friendly member the player is aiming at to the player's group, or tells a friendly vehicle to behave like a backup vehicle
        /// </summary>
        public void RecruitGangMember()
        {
            RaycastResult hit;
            if (MindControl.CurrentPlayerCharacter.IsInVehicle())
            {
                hit = World.Raycast(GameplayCamera.Position, GameplayCamera.Direction, 250, IntersectOptions.Everything,
                    MindControl.CurrentPlayerCharacter.CurrentVehicle);
            }
            else
            {
                hit = World.Raycast(GameplayCamera.Position, GameplayCamera.Direction, 250, IntersectOptions.Everything);
            }

            if (hit.HitEntity != null)
            {
                List<Ped> playerGangMembers;

                if (hit.HitEntity.Model.IsVehicle)
                {
                    Vehicle hitVeh = (Vehicle)hit.HitEntity;
                    //only do vehicle-related stuff if we're not inside said vehicle!
                    if (!MindControl.CurrentPlayerCharacter.IsInVehicle(hitVeh))
                    {
                        List<SpawnedDrivingGangMember> playerGangDrivers = SpawnManager.instance.GetSpawnedDriversOfGang(GangManager.PlayerGang);
                        for (int i = 0; i < playerGangDrivers.Count; i++)
                        {
                            if (playerGangDrivers[i].vehicleIAmDriving != null &&
                                (playerGangDrivers[i].vehicleIAmDriving == hitVeh))
                            {
                                //car should now behave as a backup vehicle: come close and drop passengers if player is on foot, follow player if not
                                playerGangDrivers[i].playerAsDest = true;
                                playerGangDrivers[i].deliveringCar = true;
                                playerGangDrivers[i].destination = Math.Vector3.WorldEast; //just something that isn't zero will do to wake the driver up
                                playerGangDrivers[i].Update();
                                UI.Notification.Show("Car told to back you up!");
                                return;
                            }
                        }

                        //we've hit a vehicle that's not marked as a gang vehicle!
                        //maybe it's no longer persistent and refs have already been cleared
                        //in any case, try to find gang members inside it and tell them to leave
                        playerGangMembers = SpawnManager.instance.GetSpawnedPedsOfGang(GangManager.PlayerGang);
                        for (int i = 0; i < playerGangMembers.Count; i++)
                        {
                            if (playerGangMembers[i].IsInVehicle(hitVeh) && playerGangMembers[i].IsAlive)
                            {

                                if (playerGangMembers[i].IsInGroup)
                                {
                                    Function.Call(Hash.REMOVE_PED_FROM_GROUP, playerGangMembers[i]);
                                    UI.Notification.Show("A member has left your group");
                                }
                                else
                                {
                                    int playergrp = Function.Call<int>(Hash.GET_PLAYER_GROUP, Game.Player);
                                    playerGangMembers[i].Task.ClearAll();
                                    Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, playerGangMembers[i], playergrp);
                                    if (playerGangMembers[i].IsInGroup)
                                    {
                                        UI.Notification.Show("A member has joined your group");
                                    }
                                    else
                                    {
                                        playerGangMembers[i].Task.LeaveVehicle();
                                    }
                                }
                            }
                        }
                    }


                }
                else if (hit.HitEntity.Model.IsPed)
                {
                    //maybe we're just targeting a ped then?
                    playerGangMembers = SpawnManager.instance.GetSpawnedPedsOfGang(GangManager.PlayerGang);
                    for (int i = 0; i < playerGangMembers.Count; i++)
                    {
                        if (playerGangMembers[i] == hit.HitEntity)
                        {
                            if (playerGangMembers[i].IsInGroup)
                            {
                                Function.Call(Hash.REMOVE_PED_FROM_GROUP, playerGangMembers[i]);
                                UI.Notification.Show("A member has left your group");
                            }
                            else
                            {
                                int playergrp = Function.Call<int>(Hash.GET_PLAYER_GROUP, Game.Player);
                                playerGangMembers[i].Task.ClearAll();
                                Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, playerGangMembers[i], playergrp);
                                if (playerGangMembers[i].IsInGroup)
                                {
                                    UI.Notification.Show("A member has joined your group");
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }

        private void OnAbort(object sender, EventArgs e)
        {
            UI.Notification.Show("Gang and Turf mod: removing blips. If you didn't press Insert, please check your log and report any errors.");
            zoneManagerScript.ChangeBlipDisplay(ZoneManager.ZoneBlipDisplay.none);
            if (MindControl.HasChangedBody)
            {
                MindControl.RestorePlayerBody();
            }
            SpawnManager.instance.RemoveAllMembers();

            Logger.Log("mod aborted!", 2);
        }
    }
}

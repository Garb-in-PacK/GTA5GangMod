﻿using GTA.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// the script is responsble for ticking and detecting input for the more sensitive scripts.
    /// some, like the gang war manager, are still ticking on their own.
    /// this one exists to make sure these ones start running in the correct order
    /// </summary>
    class ModCore : Script
    {
        public GangManager gangManagerScript;
        public MenuScript menuScript;
        public ZoneManager zoneManagerScript;

        public ModCore()
        {
            gangManagerScript = new GangManager();
            zoneManagerScript = new ZoneManager();
            menuScript = new MenuScript();

            this.KeyUp += onKeyUp;
            this.Tick += OnTick;
        }

        void OnTick(object sender, EventArgs e)
        {
            gangManagerScript.Tick();
            menuScript.Tick();
        }

        private void onKeyUp(object sender, KeyEventArgs e)
        {
            if(menuScript.curInputType == MenuScript.desiredInputType.changeKeyBinding)
            {
                if(e.KeyCode != Keys.Enter)
                {
                    ModOptions.instance.SetKey(menuScript.targetKeyBindToChange, e.KeyCode);
                    menuScript.curInputType = MenuScript.desiredInputType.none;
                    menuScript.RefreshKeyBindings();
                }
            }
            else
            {
                if (e.KeyCode == ModOptions.instance.openGangMenuKey)
                {
                    //numpad keys dont seem to go along well with shift
                    if (e.Modifiers == Keys.None)
                    {
                        menuScript.OpenGangMenu();
                    }
                    else if (e.Modifiers == Keys.Shift)
                    {
                        menuScript.OpenContextualRegistrationMenu();
                    }
                }
                else if (e.KeyCode == ModOptions.instance.openZoneMenuKey)
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
                else if (e.KeyCode == ModOptions.instance.addToGroupKey)
                {
                    Ped[] playerGangMembers = gangManagerScript.GetSpawnedMembersOfGang(gangManagerScript.GetPlayerGang());
                    for (int i = 0; i < playerGangMembers.Length; i++)
                    {
                        if (Game.Player.IsTargetting(playerGangMembers[i]))
                        {
                            int playergrp = Function.Call<int>(Hash.GET_PLAYER_GROUP, Game.Player);

                            if (playerGangMembers[i].IsInGroup)
                            {
                                Function.Call(Hash.REMOVE_PED_FROM_GROUP, playerGangMembers[i]);
                                UI.Notify("A member has left your group");
                            }
                            else
                            {
                                playerGangMembers[i].Task.ClearAll();
                                Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, playerGangMembers[i], playergrp);
                                UI.Notify("A member has joined your group");
                            }
                            break;
                        }
                    }

                }
                else if (e.KeyCode == ModOptions.instance.mindControlKey)
                {
                    gangManagerScript.TryBodyChange();
                }
                else if (e.KeyCode == Keys.Space)
                {
                    if (gangManagerScript.hasChangedBody)
                    {
                        gangManagerScript.RespawnIfPossible();
                    }
                }
            }
            
        }
        
    }
}

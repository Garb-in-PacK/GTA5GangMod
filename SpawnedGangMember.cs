﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTA
{
    public class SpawnedGangMember : UpdatedClass
    {
        
        public Ped watchedPed;

        public override void Update()
        {

            if (RandomUtil.RandomBool() || Game.Player.IsTargetting(watchedPed))
            {
                watchedPed.Task.FightAgainstHatedTargets(100);
            }

            if (watchedPed.IsInCombat)
            {
                foreach (Ped member in GangManager.instance.GetSpawnedMembersOfGang
                    (GangManager.instance.GetGangByRelGroup(watchedPed.RelationshipGroup)))
                {
                    member.Task.FightAgainstHatedTargets(200);
                }
            }
            else
            {
                if (watchedPed.RelationshipGroup == GangManager.instance.GetPlayerGang().relationGroupIndex)
                {

                    if (!watchedPed.IsInCombat)
                    {
                        //help the player if he's in trouble and we're not
                        foreach (Ped ped in World.GetNearbyPeds(Game.Player.Character.Position, 100))
                        {
                            if (ped.IsInCombat && (ped.RelationshipGroup != GangManager.instance.GetPlayerGang().relationGroupIndex &&
                                ped.RelationshipGroup != Game.Player.Character.RelationshipGroup))
                            {
                                watchedPed.Task.FightAgainst(ped);
                                break;
                            }
                        }
                    }

                }
            }

            if (!watchedPed.IsAlive)
            {
                watchedPed.CurrentBlip.Remove();
                this.watchedPed = null;
            }
        }

        public SpawnedGangMember(Ped watchedPed)
        {
            this.watchedPed = watchedPed;
            this.ticksBetweenUpdates = 600;
        }

    }
}
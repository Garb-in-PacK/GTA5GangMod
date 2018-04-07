﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using GTA;
using System.Windows.Forms;
using GTA.Native;
using System.Drawing;
using GTA.Math;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// this script controls most things related to gang behavior and relations.
    /// </summary>
    public class GangManager
    {
        public List<SpawnedGangMember> livingMembers;
        List<SpawnedDrivingGangMember> livingDrivingMembers;
        List<GangAI> enemyGangs;
        public GangData gangData;
        public static GangManager instance;

        public Gang PlayerGang
        {
            get
            {
                if(cachedPlayerGang == null)
                {
                    cachedPlayerGang = GetPlayerGang();
                }

                return cachedPlayerGang;
            }
        }

        private Gang cachedPlayerGang;

        private int ticksSinceLastReward = 0;

        /// <summary>
        /// the number of currently alive members.
        /// (the number of entries in LivingMembers isn't the same as this)
        /// </summary>
        public int livingMembersCount = 0;

        public SpawnedGangMember currentlyControlledMember = null;
        public bool hasDiedWithChangedBody = false;
        public Ped theOriginalPed;
        private int moneyFromLastProtagonist = 0;
        private int defaultMaxHealth = 200;

        //bools below are toggled true for one Tick if an Update function for the respective type was run
        private bool memberUpdateRanThisFrame = false;
        private bool vehUpdateRanThisFrame = false;
        private bool gangAIUpdateRanThisFrame = false;

        public delegate void SuccessfulMemberSpawnDelegate();

        #region setup/save stuff
        public class GangData
        {

            public GangData()
            {
                gangs = new List<Gang>();
            }

            public List<Gang> gangs;
        }
        public GangManager()
        {
            instance = this;

            livingMembers = new List<SpawnedGangMember>();
            livingDrivingMembers = new List<SpawnedDrivingGangMember>();
            enemyGangs = new List<GangAI>();

            new ModOptions(); //just start the options, we can call it by its instance later

            defaultMaxHealth = Game.Player.Character.MaxHealth;

            gangData = PersistenceHandler.LoadFromFile<GangData>("GangData");
            if (gangData == null)
            {
                gangData = new GangData();

                Gang playerGang = new Gang("Player's Gang", VehicleColor.BrushedGold, true);
                //setup gangs
                gangData.gangs.Add(playerGang);

                playerGang.blipColor = (int) BlipColor.Yellow;

                if (ModOptions.instance.gangsStartWithPistols)
                {
                    playerGang.gangWeaponHashes.Add(WeaponHash.Pistol);
                }

                CreateNewEnemyGang();
            }

            if (gangData.gangs.Count == 1 && ModOptions.instance.maxCoexistingGangs > 1)
            {
                //we're alone.. add an enemy!
                CreateNewEnemyGang();
            }

            SetUpGangRelations();
        }
        /// <summary>
        /// basically makes all gangs hate each other
        /// </summary>
        void SetUpGangRelations()
        {
            //set up the relationshipgroups
            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                gangData.gangs[i].relationGroupIndex = World.AddRelationshipGroup(gangData.gangs[i].name);
                
                //if the player owns this gang, we love him
                if (gangData.gangs[i].isPlayerOwned)
                {
                    World.SetRelationshipBetweenGroups(Relationship.Companion, gangData.gangs[i].relationGroupIndex, Game.Player.Character.RelationshipGroup);
                    World.SetRelationshipBetweenGroups(Relationship.Companion, Game.Player.Character.RelationshipGroup, gangData.gangs[i].relationGroupIndex);

                    ////also, make the player gang friendly to mission characters
                    //for(int missionIndex = 2; missionIndex < 9; missionIndex++)
                    //{
                    //    int specialHash = Function.Call<int>(Hash.GET_HASH_KEY, "MISSION" + missionIndex.ToString());
                    //    World.SetRelationshipBetweenGroups(Relationship.Respect, gangData.gangs[i].relationGroupIndex, specialHash);
                    //    World.SetRelationshipBetweenGroups(Relationship.Respect, specialHash, gangData.gangs[i].relationGroupIndex);
                    //}
                    
                }
                else
                {
                    //since we're checking each gangs situation...
                    //lets check if we don't have any member variation, which could be a problem
                    if (gangData.gangs[i].memberVariations.Count == 0)
                    {
                        GetMembersForGang(gangData.gangs[i]);
                    }

                    //lets also see if their colors are consistent
                    gangData.gangs[i].EnforceGangColorConsistency();

                    //if we're not player owned, we hate the player!
                    World.SetRelationshipBetweenGroups(Relationship.Hate, gangData.gangs[i].relationGroupIndex, Game.Player.Character.RelationshipGroup);
                    World.SetRelationshipBetweenGroups(Relationship.Hate, Game.Player.Character.RelationshipGroup, gangData.gangs[i].relationGroupIndex);
                    
                    //add this gang to the enemy gangs
                    //and start the AI for it
                    enemyGangs.Add(new GangAI(gangData.gangs[i]));
                }

            }

            //and the relations themselves
            for (int i = gangData.gangs.Count - 1; i > -1; i--)
            {
                for(int j = 0; j < i; j++)
                {
                    World.SetRelationshipBetweenGroups(Relationship.Hate, gangData.gangs[i].relationGroupIndex, gangData.gangs[j].relationGroupIndex);
                    World.SetRelationshipBetweenGroups(Relationship.Hate, gangData.gangs[j].relationGroupIndex, gangData.gangs[i].relationGroupIndex);
                }
            }

            //all gangs hate cops if set to very aggressive
            SetCopRelations(ModOptions.instance.gangMemberAggressiveness == ModOptions.gangMemberAggressivenessMode.veryAgressive);
        }

        public void SetCopRelations(bool hate)
        {
            int copHash = Function.Call<int>(Hash.GET_HASH_KEY, "COP");
            int relationLevel = 3; //neutral
            if (hate) relationLevel = 5; //hate

            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, relationLevel, copHash, gangData.gangs[i].relationGroupIndex);
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, relationLevel, gangData.gangs[i].relationGroupIndex, copHash);
            }
        }

        public void SaveGangData(bool notifySuccess = true)
        {
            PersistenceHandler.SaveToFile(gangData, "GangData", notifySuccess);
        }
        #endregion

        #region cleanup

        /// <summary>
        /// marks all living members as no longer needed and removes their blips, 
        /// as if everyone had died or were too far from the player
        /// </summary>
        public void RemoveAllMembers()
        {
            for (int i = 0; i < livingMembers.Count; i++)
            {
                livingMembers[i].Die();
            }

            for (int i = 0; i < livingDrivingMembers.Count; i++)
            {
                livingDrivingMembers[i].ClearAllRefs();
            }

        }

        #endregion


        public static bool debugAlwaysFalseBool = false;
        public void Tick()
        {
            debugAlwaysFalseBool = false;
            //tick living members...
            memberUpdateRanThisFrame = false;
            for (int i = 0; i < livingMembers.Count; i++)
            {
                if (livingMembers[i].watchedPed != null)
                {
                    livingMembers[i].ticksSinceLastUpdate++;

                    if (!memberUpdateRanThisFrame)
                    {
                        if (livingMembers[i].ticksSinceLastUpdate >= livingMembers[i].ticksBetweenUpdates)
                        {
                            //max is one update per frame in order to avoid crashes (it shouldn't make them dumb or anything)
                            livingMembers[i].Update();
                            livingMembers[i].ticksSinceLastUpdate = 0 - RandoMath.CachedRandom.Next(livingMembers[i].ticksBetweenUpdates / 3);
                            memberUpdateRanThisFrame = true;
                        }
                    }
                    
                }
            }

            //tick living driving members...
            vehUpdateRanThisFrame = false;
            for (int i = 0; i < livingDrivingMembers.Count; i++)
            {
                if (livingDrivingMembers[i].watchedPed != null && livingDrivingMembers[i].vehicleIAmDriving != null)
                {
                    livingDrivingMembers[i].ticksSinceLastUpdate++;
                    if (!vehUpdateRanThisFrame)
                    {
                        if (livingDrivingMembers[i].ticksSinceLastUpdate >= livingDrivingMembers[i].ticksBetweenUpdates)
                        {
                            //max is one vehicle update per frame in order to avoid crashes
                            livingDrivingMembers[i].Update();
                            livingDrivingMembers[i].ticksSinceLastUpdate = 0 - RandoMath.CachedRandom.Next(livingDrivingMembers[i].ticksBetweenUpdates / 3);
                            vehUpdateRanThisFrame = true;
                        }
                    }
                    
                }
            }

            TickGangs();

            if (HasChangedBody)
            {
                TickMindControl();
            }

            if (debugAlwaysFalseBool)
            {
                UI.Notify("the always false bool is true!");
            }
        }

        #region gang general control stuff

        /// <summary>
        /// this controls the gang AI decisions and rewards for the player and AI gangs
        /// </summary>
        void TickGangs()
        {
            gangAIUpdateRanThisFrame = false;
            for (int i = 0; i < enemyGangs.Count; i++)
            {
                enemyGangs[i].ticksSinceLastUpdate++;
                if (!gangAIUpdateRanThisFrame)
                {
                    if (enemyGangs[i].ticksSinceLastUpdate >= enemyGangs[i].ticksBetweenUpdates)
                    {
                        enemyGangs[i].ticksSinceLastUpdate = 0 - RandoMath.CachedRandom.Next(enemyGangs[i].ticksBetweenUpdates / 3);
                        enemyGangs[i].Update();

                        //lets also check if there aren't too many gangs around
                        //if there aren't, we might create a new one...
                        if (enemyGangs.Count < ModOptions.instance.maxCoexistingGangs - 1)
                        {
                            if (RandoMath.CachedRandom.Next(enemyGangs.Count) == 0)
                            {
                                Gang createdGang = CreateNewEnemyGang();
                                if (createdGang != null)
                                {
                                    enemyGangs.Add(new GangAI(createdGang));
                                }

                            }
                        }

                        gangAIUpdateRanThisFrame = true; //max is one update per tick
                    }
                }
               
            }

            ticksSinceLastReward++;
            if (ticksSinceLastReward >= ModOptions.instance.ticksBetweenTurfRewards)
            {
                ticksSinceLastReward = 0;
                //each gang wins money according to the amount of owned zones and their values
                for (int i = 0; i < enemyGangs.Count; i++)
                {
                    GiveTurfRewardToGang(enemyGangs[i].watchedGang);
                }

                //this also counts for the player's gang
                GiveTurfRewardToGang(PlayerGang);
            }
        }

        /// <summary>
        /// makes all AI gangs do an Update run immediately
        /// </summary>
        public void ForceTickAIGangs()
        {
            for (int i = 0; i < enemyGangs.Count; i++)
            {
                enemyGangs[i].Update();
            }
        }

        public Gang CreateNewEnemyGang(bool notifyMsg = true)
        {
            if(PotentialGangMember.MemberPool.memberList.Count <= 0)
            {
                UI.Notify("Enemy gang creation failed: bad/empty/not found memberPool file. Try adding peds as potential members for AI gangs");
                return null;
            }
            //set gang name from options
            string gangName = "Gang";
            do
            {
                gangName = string.Concat(RandoMath.GetRandomElementFromList(ModOptions.instance.possibleGangFirstNames), " ",
                RandoMath.GetRandomElementFromList(ModOptions.instance.possibleGangLastNames));
            } while (GetGangByName(gangName) != null);

            PotentialGangMember.memberColor gangColor = (PotentialGangMember.memberColor)RandoMath.CachedRandom.Next(9);

            //the new gang takes the wealthiest gang around as reference to define its starting money.
            //that does not mean it will be the new wealthiest one, hehe (but it may)
            Gang newGang = new Gang(gangName, RandoMath.GetRandomElementFromList(ModOptions.instance.GetGangColorTranslation(gangColor).vehicleColors),
                false, (int) (RandoMath.Max(Game.Player.Money, GetWealthiestGang().moneyAvailable) * (RandoMath.CachedRandom.Next(1,11) / 6.5f)));

            newGang.blipColor = RandoMath.GetRandomElementFromArray(ModOptions.instance.GetGangColorTranslation(gangColor).blipColors);

            GetMembersForGang(newGang);

            //relations...
            newGang.relationGroupIndex = World.AddRelationshipGroup(gangName);

            World.SetRelationshipBetweenGroups(Relationship.Hate, newGang.relationGroupIndex, Game.Player.Character.RelationshipGroup);
            World.SetRelationshipBetweenGroups(Relationship.Hate, Game.Player.Character.RelationshipGroup, newGang.relationGroupIndex);

            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                World.SetRelationshipBetweenGroups(Relationship.Hate, gangData.gangs[i].relationGroupIndex, newGang.relationGroupIndex);
                World.SetRelationshipBetweenGroups(Relationship.Hate, newGang.relationGroupIndex, gangData.gangs[i].relationGroupIndex);
            }

            gangData.gangs.Add(newGang);

            newGang.GetPistolIfOptionsRequire();

            SaveGangData();
            if (notifyMsg)
            {
                UI.Notify("The " + gangName + " have entered San Andreas!");
            }
            

            return newGang;
        }

        public void GetMembersForGang(Gang targetGang)
        {
            PotentialGangMember.memberColor gangColor = ModOptions.instance.TranslateVehicleToMemberColor(targetGang.vehicleColor);
            PotentialGangMember.dressStyle gangStyle = (PotentialGangMember.dressStyle)RandoMath.CachedRandom.Next(3);
            for (int i = 0; i < RandoMath.CachedRandom.Next(2, 6); i++)
            {
                PotentialGangMember newMember = PotentialGangMember.GetMemberFromPool(gangStyle, gangColor);
                if (newMember != null)
                {
                    targetGang.AddMemberVariation(newMember);
                }
                else
                {
                    break;
                }

            }
        }

        public void KillGang(GangAI aiWatchingTheGang)
        {
            UI.Notify("The " + aiWatchingTheGang.watchedGang.name + " have been wiped out!");
            enemyGangs.Remove(aiWatchingTheGang);
            gangData.gangs.Remove(aiWatchingTheGang.watchedGang);
            if(enemyGangs.Count == 0 && ModOptions.instance.maxCoexistingGangs > 1)
            {
                //create a new gang right away... but do it silently to not demotivate the player too much
                Gang createdGang = CreateNewEnemyGang(false);
                if (createdGang != null)
                {
                    enemyGangs.Add(new GangAI(createdGang));
                }
            }
            SaveGangData(false);
        }

        public void GiveTurfRewardToGang(Gang targetGang)
        {

            List<TurfZone> curGangZones = ZoneManager.instance.GetZonesControlledByGang(targetGang.name);
            if (targetGang.isPlayerOwned)
            {
                if (curGangZones.Count > 0)
                {
                    int rewardedCash = 0;

                    for (int i = 0; i < curGangZones.Count; i++)
                    {
                        int zoneReward = (int)((ModOptions.instance.baseRewardPerZoneOwned * 
                            (1 + ModOptions.instance.rewardMultiplierPerZone * curGangZones.Count)) +
                            ((curGangZones[i].value + 1) * ModOptions.instance.baseRewardPerZoneOwned * 0.25f) );

                        AddOrSubtractMoneyToProtagonist(zoneReward);
                        
                        rewardedCash += zoneReward;
                    }
                    Function.Call(Hash.PLAY_SOUND, -1, "Virus_Eradicated", "LESTER1A_SOUNDS", 0, 0, 1);
                    UI.Notify("Money won from controlled zones: " + rewardedCash.ToString());
                }
            }
            else
            {
                for (int j = 0; j < curGangZones.Count; j++)
                {
                    targetGang.moneyAvailable += (int)((curGangZones[j].value + 1) *
                        ModOptions.instance.baseRewardPerZoneOwned *
                        (1 + ModOptions.instance.rewardMultiplierPerZone * curGangZones.Count) * ModOptions.instance.extraProfitForAIGangsFactor);
                }

            }

        }


        /// <summary>
        /// when the player asks to reset mod options, we must reset these update intervals because they
        /// may have changed
        /// </summary>
        public void ResetGangUpdateIntervals()
        {
            for(int i = 0; i < enemyGangs.Count; i++)
            {
                enemyGangs[i].ResetUpdateInterval();
            }

            for (int i = 0; i < livingMembers.Count; i++)
            {
                livingMembers[i].ResetUpdateInterval();
            }
        }

        #endregion

        #region Gang Upgrade/War Calculations

        public static int CalculateHealthUpgradeCost(int currentMemberHealth)
        {
            return ModOptions.instance.baseCostToUpgradeHealth + (currentMemberHealth + 20) * (20 * (currentMemberHealth / 20) + 1);
        }

        public static int CalculateArmorUpgradeCost(int currentMemberArmor)
        {
            return ModOptions.instance.baseCostToUpgradeArmor + (currentMemberArmor + 20) * (50 * (currentMemberArmor / 25));
        }

        public static int CalculateAccuracyUpgradeCost(int currentMemberAcc)
        {
            return ((currentMemberAcc / 5) + 1) * ModOptions.instance.baseCostToUpgradeAccuracy;
        }

        public static int CalculateGangValueUpgradeCost(int currentGangValue)
        {
            return (currentGangValue + 1) * ModOptions.instance.baseCostToUpgradeGeneralGangTurfValue;
        }

        public static int CalculateTurfValueUpgradeCost(int currentTurfValue)
        {
            return (currentTurfValue + 1) * ModOptions.instance.baseCostToUpgradeSingleTurfValue;
        }

        public static int CalculateAttackCost(Gang attackerGang, GangWarManager.attackStrength attackType)
        {
            int attackTypeInt = (int) attackType;
            int pow2NonZeroAttackType = (attackTypeInt * attackTypeInt + 1);
            return ModOptions.instance.baseCostToTakeTurf + ModOptions.instance.baseCostToTakeTurf * attackTypeInt * attackTypeInt +
                attackerGang.GetFixedStrengthValue() * pow2NonZeroAttackType;
        }

        public static GangWarManager.attackStrength CalculateRequiredAttackStrength(Gang attackerGang, int defenderStrength)
        {
            GangWarManager.attackStrength requiredAtk = GangWarManager.attackStrength.light;

            int attackerGangStrength = attackerGang.GetFixedStrengthValue();

            for (int i = 0; i < 3; i++)
            {
                if (attackerGangStrength * (i * i + 1) > defenderStrength)
                {
                    break;
                }
                else
                {
                    requiredAtk++;
                }
            }

            return requiredAtk;
        }

        public static int CalculateAttackerReinforcements(Gang attackerGang, GangWarManager.attackStrength attackType)
        {
            return ModOptions.instance.extraKillsPerTurfValue * ((int) (attackType + 1) * (int) (attackType + 1)) +  ModOptions.instance.baseNumKillsBeforeWarVictory / 2 +
                attackerGang.GetReinforcementsValue() / 100;
        }

        public static int CalculateDefenderStrength(Gang defenderGang, TurfZone contestedZone)
        {
            return defenderGang.GetFixedStrengthValue() * contestedZone.value;
        }

        public static int CalculateDefenderReinforcements(Gang defenderGang, TurfZone targetZone)
        {
            return ModOptions.instance.extraKillsPerTurfValue * targetZone.value + ModOptions.instance.baseNumKillsBeforeWarVictory +
                defenderGang.GetReinforcementsValue() / 100;
        }

        /// <summary>
        /// uses the base reward for taking enemy turf (half if it was just a battle for defending)
        /// and the enemy strength (with variation) to define the "loot"
        /// </summary>
        /// <returns></returns>
        public static int CalculateBattleRewards(Gang ourEnemy, bool weWereAttacking)
        {
            int baseReward = ModOptions.instance.rewardForTakingEnemyTurf;
            if(weWereAttacking)
            {
                baseReward /= 2;
            }
            return baseReward + ourEnemy.GetGangVariedStrengthValue();
        }

        #endregion

        #region gang member mind control

        /// <summary>
        /// the addition to the tick methods when the player is in control of a member
        /// </summary>
        void TickMindControl()
        {
            if (Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player, true))
            {
                UI.ShowSubtitle("Your member has been arrested!");
                RestorePlayerBody();
                return;
            }

            if (!theOriginalPed.IsAlive)
            {
                RestorePlayerBody();
                Game.Player.Character.Kill();
                return;
            }

            if (Game.Player.Character.Health > 4000 && Game.Player.Character.Health != 4900)
            {
                Game.Player.Character.Armor -= (4900 - Game.Player.Character.Health);
            }

            Game.Player.Character.Health = 5000;

            if (Game.Player.Character.Armor <= 0) //dead!
            {
                if (!(Game.Player.Character.IsRagdoll) && hasDiedWithChangedBody)
                {
                    Game.Player.Character.Weapons.Select(WeaponHash.Unarmed, true);
                    Game.Player.Character.Task.ClearAllImmediately();
                    Game.Player.Character.CanRagdoll = true;
                    Function.Call((Hash)0xAE99FB955581844A, Game.Player.Character.Handle, -1, -1, 0, 0, 0, 0);
                    //Game.Player.Character.Euphoria.ShotFallToKnees.Start();
                }
                else
                {
                    if (!hasDiedWithChangedBody)
                    {
                        if (GangWarManager.instance.isOccurring)
                        {
                            GangWarManager.instance.OnAllyDeath();
                        }
                    }
                    hasDiedWithChangedBody = true;
                    //Game.Player.CanControlCharacter = false;
                    //Game.Player.Character.Euphoria.ShotFallToKnees.Start(20000);
                    Game.Player.Character.Weapons.Select(WeaponHash.Unarmed, true);
                    //in a war, this counts as a casualty in our team
                    
                    Function.Call((Hash)0xAE99FB955581844A, Game.Player.Character.Handle, -1, -1, 0, 0, 0, 0);
                    Game.Player.IgnoredByEveryone = true;
                }

                //RestorePlayerBody();
            }
        }

        /// <summary>
        /// attempts to change the player's body.
        /// if the player has already changed body, the original body is restored
        /// </summary>
        public void TryBodyChange()
        {
            if (!HasChangedBody)
            {
                List<Ped> playerGangMembers = GetSpawnedPedsOfGang(PlayerGang);
                for (int i = 0; i < playerGangMembers.Count; i++)
                {
                    if (Game.Player.IsTargetting(playerGangMembers[i]))
                    {
                        if (playerGangMembers[i].IsAlive)
                        {
                            theOriginalPed = Game.Player.Character;
                            //adds a blip to the protagonist so that we know where we left him
                            Blip protagonistBlip = theOriginalPed.AddBlip();
                            protagonistBlip.Sprite = BlipSprite.Creator;
                            Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, "Last Used Protagonist");
                            Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, protagonistBlip);
                            

                            defaultMaxHealth = theOriginalPed.MaxHealth;
                            moneyFromLastProtagonist = Game.Player.Money;
                            TakePedBody(playerGangMembers[i]);
                            break;
                        }
                    }
                }
            }
            else
            {
                RestorePlayerBody();
            }

        }

        void TakePedBody(Ped targetPed)
        {
            targetPed.Task.ClearAllImmediately();
            
            Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, targetPed, true, true);
            Game.Player.MaxArmor = targetPed.Armor + targetPed.MaxHealth;
            targetPed.Armor += targetPed.Health;
            targetPed.MaxHealth = 5000;
            targetPed.Health = 5000;
            currentlyControlledMember = GetTargetMemberAI(targetPed);

            Game.Player.CanControlCharacter = true;
        }

        /// <summary>
        /// makes the body the player was using become dead for real
        /// </summary>
        /// <param name="theBody"></param>
        void DiscardDeadBody(Ped theBody)
        {
            hasDiedWithChangedBody = false;
            theBody.IsInvincible = false;
            SpawnedGangMember bodyAI = GetTargetMemberAI(theBody);
            if(bodyAI != null)
            {
                bodyAI.Die();
            }
            theBody.Health = 0;
            theBody.MarkAsNoLongerNeeded();
            theBody.Kill();
        }

        /// <summary>
        /// takes control of a random gang member in the vicinity.
        /// if there isnt any, creates one parachuting.
        /// you can only respawn if you have died as a gang member
        /// </summary>
        public void RespawnIfPossible()
        {
            if (hasDiedWithChangedBody)
            {
                Ped oldPed = Game.Player.Character;

                List<Ped> respawnOptions = GetSpawnedPedsOfGang(PlayerGang);

                for(int i = 0; i < respawnOptions.Count; i++)
                {
                    if (respawnOptions[i].IsAlive)
                    {
                        //we have a new body then
                        TakePedBody(respawnOptions[i]);

                        DiscardDeadBody(oldPed);
                        return;
                    }
                }

                //lets parachute if no one is around
                SpawnedGangMember spawnedPara = GangManager.instance.SpawnGangMember(GangManager.instance.PlayerGang,
                   Game.Player.Character.Position + Vector3.WorldUp * 70);
                if (spawnedPara != null)
                {
                    TakePedBody(spawnedPara.watchedPed);
                    spawnedPara.watchedPed.Weapons.Give(WeaponHash.Parachute, 1, true, true);
                    DiscardDeadBody(oldPed);
                }


            }
        }

        public void RestorePlayerBody()
        {
            Ped oldPed = Game.Player.Character;
            //return to original body
            Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, theOriginalPed, true, true);
            Game.Player.MaxArmor = 100;
            theOriginalPed.CurrentBlip.Remove();
            theOriginalPed.MaxHealth = defaultMaxHealth;
            if (theOriginalPed.Health > theOriginalPed.MaxHealth) theOriginalPed.Health = theOriginalPed.MaxHealth;
            theOriginalPed.Task.ClearAllImmediately();
            
            if (hasDiedWithChangedBody)
            {
                oldPed.IsInvincible = false;
                oldPed.Health = 0;
                oldPed.MarkAsNoLongerNeeded();
                oldPed.Kill();
            }
            else
            {
                oldPed.Health = oldPed.Armor + 100;
                oldPed.RelationshipGroup = PlayerGang.relationGroupIndex;
                oldPed.Task.ClearAllImmediately();
                oldPed.Task.FightAgainstHatedTargets(80);
            }

            hasDiedWithChangedBody = false;
            Game.Player.Money = moneyFromLastProtagonist;
            Game.Player.IgnoredByEveryone = false;
            currentlyControlledMember = null;
        }

        public bool HasChangedBody
        {
            get
            {
                return currentlyControlledMember != null;
            }
        }

        /// <summary>
        /// adds the value, or checks if it's possible to do so, to the currently controlled protagonist
        /// (or the last controlled protagonist if the player is mind-controlling a member)
        /// </summary>
        /// <param name="valueToAdd"></param>
        /// <returns></returns>
        public bool AddOrSubtractMoneyToProtagonist(int valueToAdd, bool onlyCheck = false)
        {
            if (HasChangedBody)
            {
                if (valueToAdd > 0 || moneyFromLastProtagonist >= RandoMath.Abs(valueToAdd))
                {
                    if(!onlyCheck) moneyFromLastProtagonist += valueToAdd;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (valueToAdd > 0 || Game.Player.Money >= RandoMath.Abs(valueToAdd))
                {
                    if (!onlyCheck) Game.Player.Money += valueToAdd;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        #endregion

        #region getters
        public Gang GetGangByName(string name)
        {
            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                if (gangData.gangs[i].name == name)
                {
                    return gangData.gangs[i];
                }
            }
            return null;
        }

        public Gang GetGangByRelGroup(int relGroupIndex)
        {
            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                if (gangData.gangs[i].relationGroupIndex == relGroupIndex)
                {
                    return gangData.gangs[i];
                }
            }
            return null;
        }

        public GangAI GetGangAI(Gang targetGang)
        {
            for (int i = 0; i < enemyGangs.Count; i++)
            {
                if (enemyGangs[i].watchedGang == targetGang)
                {
                    return enemyGangs[i];
                }
            }
            return null;
        }

        /// <summary>
        /// returns the player's gang
        /// </summary>
        /// <returns></returns>
        private Gang GetPlayerGang()
        {
            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                if (gangData.gangs[i].isPlayerOwned)
                {
                    return gangData.gangs[i];
                }
            }
            return null;
        }

        public List<Ped> GetSpawnedPedsOfGang(Gang desiredGang)
        {
            List<Ped> returnedList = new List<Ped>();

            for (int i = 0; i < livingMembers.Count; i++)
            {
                if (livingMembers[i].watchedPed != null)
                {
                    if (livingMembers[i].watchedPed.RelationshipGroup == desiredGang.relationGroupIndex)
                    {
                        returnedList.Add(livingMembers[i].watchedPed);
                    }
                }
            }

            return returnedList;
        }

        /// <summary>
        /// gets all currently active spawned members of the desired gang.
        /// The onlyGetIfInsideVehicle will only add members who are inside vehicles to the returned list
        /// </summary>
        /// <param name="desiredGang"></param>
        /// <param name="onlyGetIfInsideVehicle"></param>
        /// <returns></returns>
        public List<SpawnedGangMember> GetSpawnedMembersOfGang(Gang desiredGang, bool onlyGetIfInsideVehicle = false)
        {
            List<SpawnedGangMember> returnedList = new List<SpawnedGangMember>();

            for (int i = 0; i < livingMembers.Count; i++)
            {
                if (livingMembers[i].myGang == desiredGang)
                {
                    if(onlyGetIfInsideVehicle)
                    {
                        if(Function.Call<bool>(Hash.IS_PED_IN_ANY_VEHICLE, livingMembers[i].watchedPed, false))
                        {
                            returnedList.Add(livingMembers[i]);
                        }
                    }
                    else
                    {
                        returnedList.Add(livingMembers[i]);
                    }
                    
                }
            }

            return returnedList;
        }

        /// <summary>
        /// gets all (alive) driver members of the desired gang.
        /// </summary>
        /// <param name="desiredGang"></param>
        /// <returns></returns>
        public List<SpawnedDrivingGangMember> GetSpawnedDriversOfGang(Gang desiredGang)
        {
            List<SpawnedDrivingGangMember> returnedList = new List<SpawnedDrivingGangMember>();

            for (int i = 0; i < livingDrivingMembers.Count; i++)
            {
                if (livingDrivingMembers[i].watchedPed != null)
                {
                    if(livingDrivingMembers[i].watchedPed.RelationshipGroup == desiredGang.relationGroupIndex)
                    {
                        returnedList.Add(livingDrivingMembers[i]);
                    }
                }
            }

            return returnedList;
        }

        /// <summary>
        /// gets the SpawnedGangMember object that is handling the target ped's AI, optionally returning null instead if the ped is dead
        /// </summary>
        /// <param name="targetPed"></param>
        /// <param name="onlyGetIfIsAlive"></param>
        /// <returns></returns>
        public SpawnedGangMember GetTargetMemberAI(Ped targetPed, bool onlyGetIfIsAlive = false)
        {
            if (targetPed == null) return null;
            if (!targetPed.IsAlive && onlyGetIfIsAlive) return null;
            for(int i = 0; i < livingMembers.Count; i++)
            {
                if(livingMembers[i].watchedPed == targetPed)
                {
                    return livingMembers[i];
                }
            }

            return null;
        }

        public SpawnedDrivingGangMember GetTargetMemberDrivingAI(Ped targetMember)
        {
            if (targetMember == null) return null;
            for (int i = 0; i < livingDrivingMembers.Count; i++)
            {
                if (livingDrivingMembers[i].watchedPed == targetMember)
                {
                    return livingDrivingMembers[i];
                }
            }

            return null;
        }

        /// <summary>
        /// returns gang members who are not from the gang provided
        /// </summary>
        /// <param name="myGang"></param>
        /// <returns></returns>
        public List<Ped> GetMembersNotFromMyGang(Gang myGang, bool includePlayer = true)
        {
            List<Ped> returnedList = new List<Ped>();

            for (int i = 0; i < livingMembers.Count; i++)
            {
                if (livingMembers[i].watchedPed != null)
                {
                    if (livingMembers[i] != currentlyControlledMember &&
                        livingMembers[i].watchedPed.RelationshipGroup != myGang.relationGroupIndex)
                    {
                        returnedList.Add(livingMembers[i].watchedPed);
                    }
                }
            }

            if(includePlayer && myGang != PlayerGang)
            {
                returnedList.Add(Game.Player.Character);
            }

            return returnedList;
        }

        public List<Ped> GetHostilePedsAround(Vector3 targetPos, Ped referencePed, float radius)
        {
            Ped[] detectedPeds = World.GetNearbyPeds(targetPos, radius);

            List<Ped> hostilePeds = new List<Ped>();

            foreach (Ped ped in detectedPeds)
            {
                if (referencePed.RelationshipGroup != ped.RelationshipGroup && ped.IsAlive)
                {
                    int pedRelation = (int) World.GetRelationshipBetweenGroups(ped.RelationshipGroup, referencePed.RelationshipGroup);
                    //if the relationship between them is hate or they were neutral and our reference ped has been hit by this ped...
                    if (pedRelation == 5 ||
                        (pedRelation >= 3 && referencePed.HasBeenDamagedBy(ped))) 
                    {
                        hostilePeds.Add(ped);
                    }
                }
               
            }
            return hostilePeds;
        }

        /// <summary>
        /// returns the gang with the most stocked money
        /// </summary>
        /// <returns></returns>
        public Gang GetWealthiestGang()
        {
            Gang pickedGang = null;

            for(int i = 0; i < gangData.gangs.Count; i++)
            {
                if (pickedGang != null) {
                    if (gangData.gangs[i].moneyAvailable > pickedGang.moneyAvailable)
                        pickedGang = gangData.gangs[i];
                }
                else
                {
                    pickedGang = gangData.gangs[i];
                }
            }

            return pickedGang;
        }

        #endregion

        #region spawner methods

        /// <summary>
        /// a good spawn point is one that is not too close and not too far from the player or referencePosition (according to the Mod Options)
        /// </summary>
        /// <returns></returns>
        public Vector3 FindGoodSpawnPointForMember(Vector3? referencePosition = null)
        {
            Vector3 chosenPos = Vector3.Zero;
            Vector3 referencePos = Game.Player.Character.Position;

            if(referencePosition != null)
            {
                referencePos = referencePosition.Value;
            }

            int attempts = 0;

            chosenPos = World.GetNextPositionOnSidewalk(referencePos + RandoMath.RandomDirection(true) *
                          ModOptions.instance.GetAcceptableMemberSpawnDistance());
            float distFromRef = World.GetDistance(referencePos, chosenPos);
            while ((distFromRef > ModOptions.instance.maxDistanceMemberSpawnFromPlayer ||
                distFromRef < ModOptions.instance.minDistanceMemberSpawnFromPlayer) && attempts <= 5)
            {
                // UI.Notify("too far"); or too close
                chosenPos = World.GetNextPositionOnSidewalk(referencePos + RandoMath.RandomDirection(true) * 
                    ModOptions.instance.GetAcceptableMemberSpawnDistance());
                distFromRef = World.GetDistance(referencePos, chosenPos);
                attempts++;
            }

            return chosenPos;
        }

        /// <summary>
        /// finds a spawn point that is close to the specified reference point and, optionally, far from the specified repulsor
        /// </summary>
        /// <returns></returns>
        public Vector3 FindCustomSpawnPoint(Vector3 referencePoint, float averageDistanceFromReference, float minDistanceFromReference, int maxAttempts = 10, Vector3? repulsor = null, float minDistanceFromRepulsor = 0)
        {
            Vector3 chosenPos = Vector3.Zero;

            int attempts = 0;

            chosenPos = World.GetNextPositionOnSidewalk(referencePoint + RandoMath.RandomDirection(true) *
                          averageDistanceFromReference);
            float distFromRef = World.GetDistance(referencePoint, chosenPos);
            while (((distFromRef > averageDistanceFromReference * 5 || (distFromRef < minDistanceFromReference)) ||
                (repulsor != null && World.GetDistance(repulsor.Value, chosenPos) < minDistanceFromRepulsor)) &&
                attempts <= maxAttempts)
            {
                // UI.Notify("too far"); or too close
                chosenPos = World.GetNextPositionOnSidewalk(referencePoint + RandoMath.RandomDirection(true) *
                    averageDistanceFromReference);
                distFromRef = World.GetDistance(referencePoint, chosenPos);
                attempts++;
            }

            return chosenPos;
        }

        public Vector3 FindGoodSpawnPointForCar()
        {
            Vector3 chosenPos = Vector3.Zero;
            Vector3 playerPos = Game.Player.Character.Position;

            int attempts = 0;

            chosenPos = World.GetNextPositionOnStreet
                          (playerPos + RandoMath.RandomDirection(true) *
                          ModOptions.instance.GetAcceptableCarSpawnDistance());
            float distFromPlayer = World.GetDistance(playerPos, chosenPos);

            while ((distFromPlayer > ModOptions.instance.maxDistanceCarSpawnFromPlayer ||
                distFromPlayer < ModOptions.instance.minDistanceCarSpawnFromPlayer) && attempts < 5)
            {
                // UI.Notify("too far"); or too close
                //just spawn it then, don't mind being on the street because the player might be on the mountains or the desert
                chosenPos = World.GetNextPositionOnSidewalk(playerPos + RandoMath.RandomDirection(true) *
                    ModOptions.instance.GetAcceptableCarSpawnDistance());
                distFromPlayer = World.GetDistance(playerPos, chosenPos);
                attempts++;
            }

            return chosenPos;
        }

        /// <summary>
        /// makes a few attempts to place the target vehicle on a street.
        /// if it fails, the vehicle is returned to its original position
        /// </summary>
        /// <param name="targetVehicle"></param>
        /// <param name="originalPos"></param>
        public void TryPlaceVehicleOnStreet(Vehicle targetVehicle, Vector3 originalPos)
        {
            targetVehicle.PlaceOnNextStreet();
            int attemptsPlaceOnStreet = 0;
            float distFromPlayer = World.GetDistance(Game.Player.Character.Position, targetVehicle.Position);
            while((distFromPlayer > ModOptions.instance.maxDistanceCarSpawnFromPlayer ||
                distFromPlayer < ModOptions.instance.minDistanceCarSpawnFromPlayer) && attemptsPlaceOnStreet < 3)
            {
                targetVehicle.Position = FindGoodSpawnPointForCar();
                targetVehicle.PlaceOnNextStreet();
                distFromPlayer = World.GetDistance(Game.Player.Character.Position, targetVehicle.Position);
                attemptsPlaceOnStreet++;
            }

            if(distFromPlayer > ModOptions.instance.maxDistanceCarSpawnFromPlayer ||
                distFromPlayer < ModOptions.instance.minDistanceCarSpawnFromPlayer)
            {
                targetVehicle.Position = originalPos;
            }
        }

        public SpawnedGangMember SpawnGangMember(Gang ownerGang, Vector3 spawnPos, SuccessfulMemberSpawnDelegate onSuccessfulMemberSpawn = null)
        {
            if(livingMembersCount >= ModOptions.instance.spawnedMemberLimit || spawnPos == Vector3.Zero || ownerGang.memberVariations == null)
            {
                //don't start spawning, we're on the limit already or we failed to find a good spawn point or we haven't started up our data properly yet
                return null;
            }
            if (ownerGang.memberVariations.Count > 0)
            {
                PotentialGangMember chosenMember =
                    RandoMath.GetRandomElementFromList(ownerGang.memberVariations);
                Ped newPed = World.CreatePed(chosenMember.modelHash, spawnPos);
                if(newPed != null)
                {
                    chosenMember.SetPedAppearance(newPed);

                    newPed.Accuracy = ownerGang.memberAccuracyLevel;
                    newPed.MaxHealth = ownerGang.memberHealth;
                    newPed.Health = ownerGang.memberHealth;
                    newPed.Armor = ownerGang.memberArmor;

                    newPed.Money = RandoMath.CachedRandom.Next(60);

                    //set the blip
                    newPed.AddBlip();
                    newPed.CurrentBlip.IsShortRange = true;
                    newPed.CurrentBlip.Scale = 0.65f;
                    Function.Call(Hash.SET_BLIP_COLOUR, newPed.CurrentBlip, ownerGang.blipColor);

                    //set blip name - got to use native, the c# blip.name returns error ingame
                    Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                    Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, ownerGang.name + " member");
                    Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, newPed.CurrentBlip);

                    bool hasDriveByGun = false; //used for when the member has to decide between staying inside a vehicle or not

                    //give a weapon
                    if (ownerGang.gangWeaponHashes.Count > 0)
                    {
                        //get one weap from each type... if possible AND we're not forcing melee only
                        newPed.Weapons.Give(ownerGang.GetListedGunFromOwnedGuns(ModOptions.instance.meleeWeapons), 1000, false, true);
                        if (!ModOptions.instance.membersSpawnWithMeleeOnly)
                        {
                            WeaponHash driveByGun = ownerGang.GetListedGunFromOwnedGuns(ModOptions.instance.driveByWeapons);
                            hasDriveByGun = driveByGun != WeaponHash.Unarmed;
                            newPed.Weapons.Give(driveByGun, 1000, false, true);
                            newPed.Weapons.Give(ownerGang.GetListedGunFromOwnedGuns(ModOptions.instance.primaryWeapons), 1000, false, true);

                            //and one extra
                            newPed.Weapons.Give(RandoMath.GetRandomElementFromList(ownerGang.gangWeaponHashes), 1000, false, true);
                        }
                    }

                    //set the relationship group
                    newPed.RelationshipGroup = ownerGang.relationGroupIndex;

                    newPed.NeverLeavesGroup = true;

                    newPed.BlockPermanentEvents = true;
                    newPed.StaysInVehicleWhenJacked = true;

                    Function.Call(Hash.SET_CAN_ATTACK_FRIENDLY, newPed, false, false); //cannot attack friendlies
                    Function.Call(Hash.SET_PED_COMBAT_ABILITY, newPed, 1); //average combat ability
                    Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, newPed, 0, 0); //clears the flee attributes?

                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, newPed, 46, true); // alwaysFight = true and canFightArmedWhenNotArmed. which one is which is unknown
                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, newPed, 5, true);
                    Function.Call(Hash.SET_PED_COMBAT_RANGE, newPed, 2); //combatRange = far
                   
                    newPed.CanSwitchWeapons = true;
                    newPed.CanWrithe = false; //no early dying

                    //enlist this new gang member in the spawned list!
                    SpawnedGangMember newMemberAI = null;

                    bool couldEnlistWithoutAdding = false;
                    for (int i = 0; i < livingMembers.Count; i++)
                    {
                        if (livingMembers[i].watchedPed == null)
                        {
                            livingMembers[i].AttachData(newPed, ownerGang, hasDriveByGun);
                            newMemberAI = livingMembers[i];
                            couldEnlistWithoutAdding = true;
                            break;
                        }
                    }
                    if (!couldEnlistWithoutAdding)
                    {
                        if (livingMembers.Count < ModOptions.instance.spawnedMemberLimit)
                        {
                            newMemberAI = new SpawnedGangMember(newPed, ownerGang, hasDriveByGun);
                            livingMembers.Add(newMemberAI);
                        }
                        else
                        {
                            newPed.Delete();
                            return null;
                        }
                    }

                    livingMembersCount++;
                    if (onSuccessfulMemberSpawn != null) onSuccessfulMemberSpawn();
                    return newMemberAI;
                }
                else
                {
                    return null;
                }
            }
            return null;
        }

        public SpawnedDrivingGangMember SpawnGangVehicle(Gang ownerGang, Vector3 spawnPos, Vector3 destPos, bool playerIsDest = false, SuccessfulMemberSpawnDelegate onSuccessfulPassengerSpawn = null)
        {
            if (livingMembersCount >= ModOptions.instance.spawnedMemberLimit || spawnPos == Vector3.Zero || ownerGang.carVariations == null)
            {
                //don't start spawning, we're on the limit already or we failed to find a good spawn point or we haven't started up our data properly yet
                return null;
            }

            if (ownerGang.carVariations.Count > 0)
            {
                Vehicle newVehicle = World.CreateVehicle(RandoMath.GetRandomElementFromList(ownerGang.carVariations).modelHash, spawnPos);
                if(newVehicle != null)
                {
                    newVehicle.PrimaryColor = ownerGang.vehicleColor;


                    SpawnedGangMember driver = SpawnGangMember(ownerGang, spawnPos, onSuccessfulMemberSpawn : onSuccessfulPassengerSpawn);
                    
                    if (driver != null)
                    {
                        driver.curStatus = SpawnedGangMember.memberStatus.inVehicle;
                        driver.watchedPed.SetIntoVehicle(newVehicle, VehicleSeat.Driver);

                        int passengerCount = newVehicle.PassengerSeats;
                        if (destPos == Vector3.Zero && passengerCount > 4) passengerCount = 4; //limit ambient passengers in order to have less impact in ambient spawning

                        for (int i = 0; i < passengerCount; i++)
                        {
                            SpawnedGangMember passenger = SpawnGangMember(ownerGang, spawnPos, onSuccessfulMemberSpawn: onSuccessfulPassengerSpawn);
                            if (passenger != null)
                            {
                                passenger.curStatus = SpawnedGangMember.memberStatus.inVehicle;
                                passenger.watchedPed.SetIntoVehicle(newVehicle, VehicleSeat.Any);
                            }
                        }

                        SpawnedDrivingGangMember driverAI = EnlistDrivingMember(driver.watchedPed, newVehicle, destPos, ownerGang == PlayerGang, playerIsDest);

                        newVehicle.AddBlip();
                        newVehicle.CurrentBlip.IsShortRange = true;

                        Function.Call(Hash.SET_BLIP_COLOUR, newVehicle.CurrentBlip, ownerGang.blipColor);

                        return driverAI;
                    }
                    else
                    {
                        newVehicle.Delete();
                        return null;
                    }
                }
                

            }

            return null;
        }

        public Ped SpawnParachutingMember(Gang ownerGang, Vector3 spawnPos, Vector3 destPos)
        {
            SpawnedGangMember spawnedPara = SpawnGangMember(ownerGang, spawnPos);
            if (spawnedPara != null)
            {
                spawnedPara.watchedPed.Task.ParachuteTo(destPos);
                return spawnedPara.watchedPed;
            }

            return null;
        }

        SpawnedDrivingGangMember EnlistDrivingMember(Ped pedToEnlist, Vehicle vehicleDriven, Vector3 destPos, bool friendlyToPlayer, bool playerIsDest = false)
        {
            SpawnedDrivingGangMember newDriverAI = null;

            bool couldEnlistWithoutAdding = false;
            for (int i = 0; i < livingDrivingMembers.Count; i++)
            {
                if (livingDrivingMembers[i].watchedPed == null)
                {
                    newDriverAI = livingDrivingMembers[i];
                    livingDrivingMembers[i].AttachData(pedToEnlist, vehicleDriven, destPos, friendlyToPlayer, playerIsDest);
                    couldEnlistWithoutAdding = true;
                    break;
                }
            }
            if (!couldEnlistWithoutAdding)
            {
                newDriverAI = new SpawnedDrivingGangMember(pedToEnlist, vehicleDriven, destPos, friendlyToPlayer, playerIsDest);
                livingDrivingMembers.Add(newDriverAI);
            }

            return newDriverAI;
        }
        #endregion
    }

}

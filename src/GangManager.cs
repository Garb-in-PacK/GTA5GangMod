using System;
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
    public class GangManager: IDirtableSaveable
    {
        public Gang PlayerGang
        {
            get
            {
                if (cachedPlayerGang == null)
                {
                    cachedPlayerGang = GetPlayerGang();
                }

                //if, somehow, we still don't have a player gang around, make a new one!
                if (cachedPlayerGang == null)
                {
                    cachedPlayerGang = CreateNewPlayerGang();
                }

                return cachedPlayerGang;
            }
        }

        public List<GangAI> EnemyGangs { get; private set; }
        public GangData GangData { get; private set; }

        private Gang cachedPlayerGang;

        private int timeLastReward = 0;

        private ModOptions ModOptions { get; set; }
        public bool IsDirty { get; set; }
        public bool NotifyNextSave { get; set; }

        /// <summary>
        /// toggled true for one Tick if a GangAI update was run
        /// </summary>
        private bool gangAIUpdateRanThisFrame = false;

        #region setup/save stuff

        /// <summary>
        /// makes all gang-related preparations, like loading the GangData file and setting relations
        /// </summary>
        public void Init(ModOptions modOptions)
        {
            ModOptions = modOptions ?? throw new ArgumentNullException(nameof(modOptions));

            EnemyGangs = new List<GangAI>();

            GangData = PersistenceHandler.LoadFromFile<GangData>("GangData");
            if (GangData == null)
            {
                GangData = new GangData();

                //setup initial gangs... the player's and an enemy
                CreateNewPlayerGang();

                CreateNewEnemyGang();
            }
            else
            {
                AdjustGangsToModOptions();
            }

            if (GangData.Gangs.Count == 1 && ModOptions.GangsOptions.MaxCoexistingGangs > 1)
            {
                //we're alone.. add an enemy!
                CreateNewEnemyGang();
            }

            SetUpAllGangs();

            timeLastReward = ModCore.curGameTime;

        }
        /// <summary>
        /// basically sets relationship groups for all gangs, makes them hate each other and starts the AI for enemy gangs.
        /// also runs a few consistency checks on the gangs, like if their stats are conforming to the limits defined in modoptions
        /// </summary>
        private void SetUpAllGangs()
        {
            //set up the relationshipgroups
            for (int i = 0; i < GangData.Gangs.Count; i++)
            {
                GangData.Gangs[i].relationGroup = World.AddRelationshipGroup(GangData.Gangs[i].name);

                //if the player owns this gang, we love him
                if (GangData.Gangs[i].isPlayerOwned)
                {
                    GangData.Gangs[i].relationGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Companion, true);
                }
                else
                {
                    //since we're checking each gangs situation...
                    //lets check if we don't have any member variation, which could be a problem
                    if (GangData.Gangs[i].memberVariations.Count == 0)
                    {
                        GetMembersForGang(GangData.Gangs[i]);
                    }

                    //lets also see if their colors are consistent
                    GangData.Gangs[i].EnforceGangColorConsistency();


                    //add this gang to the enemy gangs
                    //and start the AI for it
                    EnemyGangs.Add(new GangAI(GangData.Gangs[i]));
                }

            }

            //set gang relations...
            SetAllGangRelationsAccordingToAggrLevel(ModOptions.RelationOptions.GangMemberAggressiveness);
            //all gangs hate cops if set to very aggressive
            SetCopRelationsToAllGangs(ModOptions.RelationOptions.GangMemberAggressiveness == RelationOptions.AggressivenessMode.veryAggressive);
        }

        /// <summary>
        /// sets relations between gangs to a certain level according to the aggressiveness
        /// </summary>
        /// <param name="aggrLevel"></param>
        public void SetAllGangRelationsAccordingToAggrLevel(RelationOptions.AggressivenessMode aggrLevel)
        {
            Relationship targetRelationLevel = RelationOptions.AggrModeToRelationship(aggrLevel);
            Gang excludedGang = null;
            if (GangWarManager.instance.isOccurring)
            {
                excludedGang = GangWarManager.instance.enemyGang;
            }
            for (int i = GangData.Gangs.Count - 1; i > -1; i--)
            {
                for (int j = 0; j < i; j++)
                {
                    if ((GangData.Gangs[i] != excludedGang || GangData.Gangs[j] != PlayerGang) &&
                        (GangData.Gangs[j] != excludedGang || GangData.Gangs[i] != PlayerGang))
                    {
                        GangData.Gangs[i].relationGroup.SetRelationshipBetweenGroups(GangData.Gangs[j].relationGroup, targetRelationLevel, true);
                    }
                }
                if (!GangData.Gangs[i].isPlayerOwned && GangData.Gangs[i] != excludedGang)
                {
                    GangData.Gangs[i].relationGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, targetRelationLevel, true);
                }
            }
        }

        /// <summary>
        /// sets all gangs' relations with cops to either neutral or hate
        /// </summary>
        /// <param name="hate"></param>
        public void SetCopRelationsToAllGangs(bool hate)
        {
            int copHash = Function.Call<int>(Hash.GET_HASH_KEY, "COP");
            int relationLevel = 3; //neutral
            if (hate) relationLevel = 5; //hate

            for (int i = 0; i < GangData.Gangs.Count; i++)
            {
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, relationLevel, copHash, GangData.Gangs[i].relationGroup.Hash);
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, relationLevel, GangData.Gangs[i].relationGroup.Hash, copHash);
            }
        }

        /// <summary>
        /// sets relation values from all other gangs and cops towards this one according to mod options
        /// </summary>
        /// <param name="gang"></param>
        public void SetRelationsTowardGang(Gang gang)
        {
            if (gang == null) throw new ArgumentNullException(nameof(gang));

            Relationship targetRelationLevel = RelationOptions.AggrModeToRelationship(ModOptions.RelationOptions.GangMemberAggressiveness);

            gang.relationGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup,
                                                            gang.isPlayerOwned ? Relationship.Companion : targetRelationLevel,
                                                            true);

            for (int i = 0; i < GangData.Gangs.Count; i++)
            {
                if (GangData.Gangs[i] == gang) continue;

                gang.relationGroup.SetRelationshipBetweenGroups(GangData.Gangs[i].relationGroup,
                                                            targetRelationLevel,
                                                            true);
            }
        }

        /// <summary>
        /// saves gang data to GangData.xml
        /// </summary>
        /// <param name="notifySuccess"></param>
        public void SaveData(bool notifySuccess = true)
        {
            PersistenceHandler.SaveToFile(GangData, "GangData", notifySuccess);
        }
        #endregion


        public void Tick()
        {
            TickGangs();
        }

        #region gang general control stuff


        /// <summary>
        /// this controls the gang AI decisions and rewards for the player and AI gangs
        /// </summary>
        public void TickGangs()
        {
            gangAIUpdateRanThisFrame = false;
            for (int i = 0; i < EnemyGangs.Count; i++)
            {
                EnemyGangs[i].ticksSinceLastUpdate++;
                if (!gangAIUpdateRanThisFrame)
                {
                    if (EnemyGangs[i].ticksSinceLastUpdate >= EnemyGangs[i].ticksBetweenUpdates)
                    {
                        EnemyGangs[i].ticksSinceLastUpdate = 0 - RandoMath.CachedRandom.Next(EnemyGangs[i].ticksBetweenUpdates / 3);
                        EnemyGangs[i].Update();
                        //lets also check if there aren't too many gangs around
                        //if there aren't, we might create a new one...
                        if (EnemyGangs.Count < ModOptions.GangsOptions.MaxCoexistingGangs - 1)
                        {
                            if (RandoMath.CachedRandom.Next(EnemyGangs.Count) == 0)
                            {
                                Gang createdGang = CreateNewEnemyGang();
                                if (createdGang != null)
                                {
                                    EnemyGangs.Add(new GangAI(createdGang));
                                }

                            }
                        }

                        gangAIUpdateRanThisFrame = true; //max is one update per tick
                    }
                }

            }

            if (ModCore.curGameTime - timeLastReward > ModOptions.ZoneOptions.MsTimeBetweenTurfRewards)
            {
                timeLastReward = ModCore.curGameTime;
                for (int i = 0; i < EnemyGangs.Count; i++)
                {
                    GiveTurfRewardToGang(EnemyGangs[i].WatchedGang);
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
            for (int i = 0; i < EnemyGangs.Count; i++)
            {
                EnemyGangs[i].Update();
            }
        }

        /// <summary>
        /// creates a new "player's gang" (there should be only one!)
        /// and adds it to the gangdata gangs list
        /// </summary>
        /// <param name="notifyMsg"></param>
        /// <returns></returns>
        public Gang CreateNewPlayerGang(bool notifyMsg = true)
        {
            Gang playerGang = new Gang("Player's Gang", VehicleColor.BrushedGold, true);
            //setup gangs
            GangData.Gangs.Add(playerGang);

            playerGang.blipColor = (int)BlipColor.Yellow;

            if (ModOptions.GangsOptions.GangsStartWithPistols)
            {
                playerGang.gangWeaponHashes.Add(WeaponHash.Pistol);
            }

            if (notifyMsg)
            {
                UI.Notification.Show("Created new gang for the player!");
            }

            return playerGang;
        }

        /// <summary>
        /// attempts to create a new AI Gang with random members and name,
        /// using data from the memberPool and ModOptions
        /// </summary>
        /// <param name="notifyMsg"></param>
        /// <returns></returns>
		public Gang CreateNewEnemyGang(bool notifyMsg = true)
        {
            ModOptions.GangColorsOptions.EnforceColorsIntegrity();

            if (PotentialGangMember.MemberPool.memberList.Count <= 0)
            {
                UI.Notification.Show("Enemy gang creation failed: bad/empty/not found memberPool file. Try adding peds as potential members for AI gangs");
                return null;
            }
            //set gang name from options
            string gangName;
            do
            {
                gangName = string.Concat(RandoMath.GetRandomElementFromList(ModOptions.GangNamesOptions.PossibleGangFirstNames), " ",
                RandoMath.GetRandomElementFromList(ModOptions.GangNamesOptions.PossibleGangLastNames));
            } while (GetGangByName(gangName) != null);

            PotentialGangMember.MemberColor gangColor = (PotentialGangMember.MemberColor)RandoMath.CachedRandom.Next(9);

            //the new gang takes the wealthiest gang around as reference to define its starting money.
            //that does not mean it will be the new wealthiest one, hehe (but it may)
            Gang newGang = new Gang(gangName, RandoMath.GetRandomElementFromList(ModOptions.GangColorsOptions.GetGangColorTranslation(gangColor).VehicleColors),
                false, (int)(RandoMath.Max(Game.Player.Money, GetWealthiestGang().moneyAvailable) * (RandoMath.CachedRandom.Next(1, 11) / 6.5f)))
            {
                blipColor = RandoMath.GetRandomElementFromArray(ModOptions.GangColorsOptions.GetGangColorTranslation(gangColor).BlipColors)
            };

            GetMembersForGang(newGang);

            //relations...
            newGang.relationGroup = World.AddRelationshipGroup(gangName);


            SetRelationsTowardGang(newGang);

            GangData.Gangs.Add(newGang);

            newGang.GetPistolIfOptionsRequire();

            IsDirty = true;
            if (notifyMsg)
            {
                UI.Notification.Show("The " + gangName + " have entered San Andreas!");
            }


            return newGang;
        }

        public void GetMembersForGang(Gang targetGang)
        {
            if (targetGang == null) throw new ArgumentNullException(nameof(targetGang));

            PotentialGangMember.MemberColor gangColor = ModOptions.GangColorsOptions.TranslateVehicleToMemberColor(targetGang.vehicleColor);
            PotentialGangMember.DressStyle gangStyle = (PotentialGangMember.DressStyle)RandoMath.CachedRandom.Next(3);
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
            if (aiWatchingTheGang == null) throw new ArgumentNullException(nameof(aiWatchingTheGang));

            UI.Notification.Show("The " + aiWatchingTheGang.WatchedGang.name + " have been wiped out!");

            //save the fallen gang in a file
            AddGangToWipedOutList(aiWatchingTheGang.WatchedGang);
            GangData.Gangs.Remove(aiWatchingTheGang.WatchedGang);
            EnemyGangs.Remove(aiWatchingTheGang);
            if (EnemyGangs.Count == 0 && ModOptions.GangsOptions.MaxCoexistingGangs > 1)
            {
                //create a new gang right away... but do it silently to not demotivate the player too much
                Gang createdGang = CreateNewEnemyGang(false);
                if (createdGang != null)
                {
                    EnemyGangs.Add(new GangAI(createdGang));
                }
            }
            SaveData(false);
        }

        /// <summary>
        /// adds the gang to a xml file that contains a list of gangs that have been wiped out,
        ///  so that the player can reuse their data in the future
        /// </summary>
        /// <param name="gangToAdd"></param>
        public static void AddGangToWipedOutList(Gang gangToAdd)
        {
            List<Gang> WOList = PersistenceHandler.LoadFromFile<List<Gang>>("wipedOutGangsList");
            if (WOList == null)
            {
                WOList = new List<Gang>();
            }
            WOList.Add(gangToAdd);
            PersistenceHandler.SaveToFile(WOList, "wipedOutGangsList");
        }

        public void GiveTurfRewardToGang(Gang targetGang)
        {
            if (targetGang == null) throw new ArgumentNullException(nameof(targetGang));

            List<TurfZone> curGangZones = ZoneManager.instance.GetZonesControlledByGang(targetGang.name);
            int zonesCount = curGangZones.Count;
            if (targetGang.isPlayerOwned)
            {
                if (curGangZones.Count > 0)
                {
                    int rewardedCash = 0;

                    for (int i = 0; i < zonesCount; i++)
                    {
                        int zoneReward = GangCalculations.CalculateRewardForZone(curGangZones[i], zonesCount);

                        rewardedCash += zoneReward;
                    }

                    MindControl.AddOrSubtractMoneyToProtagonist(rewardedCash);
                    Function.Call(Hash.PLAY_SOUND, -1, "Virus_Eradicated", "LESTER1A_SOUNDS", 0, 0, 1);
                    UI.Notification.Show("Money won from controlled zones: " + rewardedCash.ToString());
                }
            }
            else
            {
                for (int i = 0; i < curGangZones.Count; i++)
                {
                    targetGang.moneyAvailable += (int)
                        (GangCalculations.CalculateRewardForZone(curGangZones[i], zonesCount) *
                        ModOptions.GangAIOptions.ExtraProfitForAIGangsFactor);
                }

            }

        }

        /// <summary>
        /// adjust gangs' stats and weapons in order to conform with the ModOptions file
        /// </summary>
        public void AdjustGangsToModOptions()
        {
            foreach (Gang g in GangData.Gangs)
            {
                g.AdjustStatsToModOptions();
                g.AdjustWeaponChoicesToModOptions();
            }
        }

        /// <summary>
        /// when the player asks to reset mod options, we must reset these update intervals because they
        /// may have changed
        /// </summary>
        public void ResetGangUpdateIntervals()
        {
            for (int i = 0; i < EnemyGangs.Count; i++)
            {
                EnemyGangs[i].ResetUpdateInterval();
            }

            SpawnManager.instance.ResetSpawnedsUpdateInterval();
        }

        #endregion

        #region getters

        /// <summary>
        /// returns the Gang with the provided name... or null, if none match
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Gang GetGangByName(string name)
        {
            for (int i = 0; i < GangData.Gangs.Count; i++)
            {
                if (GangData.Gangs[i].name == name)
                {
                    return GangData.Gangs[i];
                }
            }
            return null;
        }

        /// <summary>
        /// returns the gang using the specified relationGroup... or null for no matches
        /// </summary>
        /// <param name="relGroup"></param>
        /// <returns></returns>
		public Gang GetGangByRelGroup(RelationshipGroup relGroup)
        {
            for (int i = 0; i < GangData.Gangs.Count; i++)
            {
                if (GangData.Gangs[i].relationGroup == relGroup)
                {
                    return GangData.Gangs[i];
                }
            }
            return null;
        }

        /// <summary>
        /// returns the GangAI object for the targetGang (returns null for the player gang)
        /// </summary>
        /// <param name="targetGang"></param>
        /// <returns></returns>
		public GangAI GetGangAI(Gang targetGang)
        {
            for (int i = 0; i < EnemyGangs.Count; i++)
            {
                if (EnemyGangs[i].WatchedGang == targetGang)
                {
                    return EnemyGangs[i];
                }
            }
            return null;
        }

        /// <summary>
        /// returns the player's gang (it's better to use the PlayerGang property instead)
        /// </summary>
        /// <returns></returns>
        private Gang GetPlayerGang()
        {
            for (int i = 0; i < GangData.Gangs.Count; i++)
            {
                if (GangData.Gangs[i].isPlayerOwned)
                {
                    return GangData.Gangs[i];
                }
            }
            return null;
        }

        /// <summary>
        /// returns the gang with the most stocked money
        /// </summary>
        /// <returns></returns>
        public Gang GetWealthiestGang()
        {
            Gang pickedGang = null;

            for (int i = 0; i < GangData.Gangs.Count; i++)
            {
                if (pickedGang != null)
                {
                    if (GangData.Gangs[i].moneyAvailable > pickedGang.moneyAvailable)
                        pickedGang = GangData.Gangs[i];
                }
                else
                {
                    pickedGang = GangData.Gangs[i];
                }
            }

            return pickedGang;
        }

        public void LoadData()
        {
            throw new NotImplementedException();
        }

        #endregion

    }

}

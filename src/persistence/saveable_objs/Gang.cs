﻿using System;
using System.Collections.Generic;
using GTA.Native;
using System.Xml.Serialization;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// the Gang!
    /// gangs have names, colors, turf, money and stats about cars and gang peds
    /// </summary>
    public class Gang
    {
        public string name;
        public int blipColor;
        public VehicleColor vehicleColor;
        public int moneyAvailable;
        public bool isPlayerOwned = false;

        //the gang's relationshipgroup
        [XmlIgnore]
        public RelationshipGroup relationGroup;

        public int memberAccuracyLevel = 1;
        public int memberHealth = 10;
        public int memberArmor = 0;

        public int baseTurfValue = 0;

        //car stats - the models
        public List<PotentialGangVehicle> carVariations = new List<PotentialGangVehicle>();

        //acceptable ped model/texture/component combinations
        public List<PotentialGangMember> memberVariations = new List<PotentialGangMember>();

        public List<WeaponHash> gangWeaponHashes = new List<WeaponHash>();
        //the weapons the AI Gang will probably buy if they have enough cash
        public List<WeaponHash> preferredWeaponHashes = new List<WeaponHash>();

        /// <summary>
        /// what does this Gang give priority to when spending money?
        /// (all gangs should try to expand a little, but the expanders should go beyond that)
        /// </summary>
        public enum AIUpgradeTendency
        {
            toughMembers,
            bigGuns,
            toughTurf,
            moreExpansion
        }

        public AIUpgradeTendency upgradeTendency = AIUpgradeTendency.moreExpansion;

        public Gang(string name, VehicleColor color, bool isPlayerOwned, int moneyAvailable = -1)
        {
            this.name = name;
            this.vehicleColor = color;

            this.isPlayerOwned = isPlayerOwned;

            this.memberHealth = ModOptions.Instance.StartingGangMemberHealth;

            if (!isPlayerOwned)
            {
                upgradeTendency = (AIUpgradeTendency)RandoMath.CachedRandom.Next(4);
            }

            if (moneyAvailable <= 0)
            {
                this.moneyAvailable = RandoMath.CachedRandom.Next(5, 15) * ModOptions.Instance.BaseCostToTakeTurf; //this isnt used if this is the player's gang - he'll use his own money instead
            }
            else
            {
                this.moneyAvailable = moneyAvailable;
            }


        }

        public Gang()
        {

        }

        public void SetPreferredWeapons()
        {
            if (ModOptions.Instance.BuyableWeapons.Count == 0) return; //don't even start looking if there aren't any buyable weapons

            //add at least one of each type - melee, primary and drive-by
            preferredWeaponHashes.Add(ModOptions.Instance.GetWeaponFromListIfBuyable(ModOptions.Instance.MeleeWeapons));
            preferredWeaponHashes.Add(ModOptions.Instance.GetWeaponFromListIfBuyable(ModOptions.Instance.DriveByWeapons));
            preferredWeaponHashes.Add(ModOptions.Instance.GetWeaponFromListIfBuyable(ModOptions.Instance.PrimaryWeapons));

            //and some more for that extra variation
            for (int i = 0; i < RandoMath.CachedRandom.Next(2, 5); i++)
            {
                preferredWeaponHashes.Add(RandoMath.GetRandomElementFromList(ModOptions.Instance.BuyableWeapons).WepHash);
            }

            GangManager.SaveData(false);
        }

        /// <summary>
        ///this checks if the gangs member, blip and car colors are consistent, like black, black and black.
        ///if left unassigned, the blip color is 0 and the car color is metallic black:
        ///a sign that somethings wrong, because 0 is white blip color
        /// </summary>
        public void EnforceGangColorConsistency()
        {
            GangColorTranslation ourColor = ModOptions.Instance.GetGangColorTranslation(memberVariations[0].linkedColor);
            if ((blipColor == 0 && ourColor.BaseColor != PotentialGangMember.MemberColor.white) ||
                (vehicleColor == VehicleColor.MetallicBlack && ourColor.BaseColor != PotentialGangMember.MemberColor.black))
            {
                blipColor = RandoMath.GetRandomElementFromArray(ourColor.BlipColors);
                vehicleColor = RandoMath.GetRandomElementFromList(ourColor.VehicleColors);
                GangManager.SaveData(false);
            }
        }

        /// <summary>
        /// checks and adjusts (if necessary) this gang's levels in order to make it conform to the current modOptions
        /// </summary>
        public void AdjustStatsToModOptions()
        {
            memberHealth = RandoMath.TrimValue(memberHealth, ModOptions.Instance.StartingGangMemberHealth, ModOptions.Instance.MaxGangMemberHealth);
            memberArmor = RandoMath.TrimValue(memberArmor, 0, ModOptions.Instance.MaxGangMemberArmor);
            memberAccuracyLevel = RandoMath.TrimValue(memberAccuracyLevel, 0, ModOptions.Instance.MaxGangMemberAccuracy);
            baseTurfValue = RandoMath.TrimValue(baseTurfValue, 0, ModOptions.Instance.MaxTurfValue);

            GangManager.SaveData(false);
        }

        /// <summary>
        /// removes weapons from our preferred list if they're not in the buyables list...
        /// then checks if we have too few preferred guns, adding some more if that's the case
        /// </summary>
        public void AdjustWeaponChoicesToModOptions()
        {
            for (int i = preferredWeaponHashes.Count - 1; i >= 0; i--)
            {
                if (ModOptions.Instance.GetBuyableWeaponByHash(preferredWeaponHashes[i]) == null)
                {
                    gangWeaponHashes.Remove(preferredWeaponHashes[i]);
                    preferredWeaponHashes.RemoveAt(i);
                }
            }

            if (preferredWeaponHashes.Count <= 2)
            {
                SetPreferredWeapons();
            }

            GangManager.SaveData(false);
        }

        public bool AddMemberVariation(PotentialGangMember newMember)
        {
            for (int i = 0; i < memberVariations.Count; i++)
            {
                if (memberVariations[i].modelHash == newMember.modelHash &&
                        memberVariations[i].hairDrawableIndex == newMember.hairDrawableIndex &&
                        memberVariations[i].headDrawableIndex == newMember.headDrawableIndex &&
                        memberVariations[i].headTextureIndex == newMember.headTextureIndex &&
                        memberVariations[i].legsDrawableIndex == newMember.legsDrawableIndex &&
                        memberVariations[i].legsTextureIndex == newMember.legsTextureIndex &&
                        memberVariations[i].torsoDrawableIndex == newMember.torsoDrawableIndex &&
                        memberVariations[i].torsoTextureIndex == newMember.torsoTextureIndex)
                {
                    return false;
                }
            }

            memberVariations.Add(newMember);
            GangManager.SaveData(isPlayerOwned);
            return true;
        }

        public bool RemoveMemberVariation(PotentialGangMember sadMember) // :(
        {
            for (int i = 0; i < memberVariations.Count; i++)
            {
                if (memberVariations[i].headDrawableIndex == -1)
                {
                    if (memberVariations[i].modelHash == sadMember.modelHash &&
                       (memberVariations[i].legsDrawableIndex == -1 || memberVariations[i].legsDrawableIndex == sadMember.legsDrawableIndex) &&
                       (memberVariations[i].legsTextureIndex == -1 || memberVariations[i].legsTextureIndex == sadMember.legsTextureIndex) &&
                       (memberVariations[i].torsoDrawableIndex == -1 || memberVariations[i].torsoDrawableIndex == sadMember.torsoDrawableIndex) &&
                       (memberVariations[i].torsoTextureIndex == -1 || memberVariations[i].torsoTextureIndex == sadMember.torsoTextureIndex))
                    {
                        memberVariations.Remove(memberVariations[i]);

                        //get new members if we have none now and we're AI-controlled
                        if (memberVariations.Count == 0 && !isPlayerOwned)
                        {
                            GangManager.GetMembersForGang(this);
                        }

                        GangManager.SaveData();
                        return true;
                    }
                }
                else
                {
                    if (memberVariations[i].modelHash == sadMember.modelHash &&
                        memberVariations[i].hairDrawableIndex == sadMember.hairDrawableIndex &&
                        memberVariations[i].headDrawableIndex == sadMember.headDrawableIndex &&
                        memberVariations[i].headTextureIndex == sadMember.headTextureIndex &&
                       memberVariations[i].legsDrawableIndex == sadMember.legsDrawableIndex &&
                       memberVariations[i].legsTextureIndex == sadMember.legsTextureIndex &&
                       memberVariations[i].torsoDrawableIndex == sadMember.torsoDrawableIndex &&
                       memberVariations[i].torsoTextureIndex == sadMember.torsoTextureIndex)
                    {
                        memberVariations.Remove(memberVariations[i]);

                        //get new members if we have none now and we're AI-controlled
                        if (memberVariations.Count == 0 && !isPlayerOwned)
                        {
                            GangManager.GetMembersForGang(this);
                        }

                        GangManager.SaveData();
                        return true;
                    }
                }

            }

            return false;
        }

        public bool AddGangCar(PotentialGangVehicle newVehicleType)
        {
            for (int i = 0; i < carVariations.Count; i++)
            {
                if (newVehicleType.modelHash != carVariations[i].modelHash)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }

            carVariations.Add(newVehicleType);
            GangManager.SaveData();
            return true;
        }

        public bool RemoveGangCar(PotentialGangVehicle sadVehicle)
        {
            for (int i = 0; i < carVariations.Count; i++)
            {
                if (sadVehicle.modelHash != carVariations[i].modelHash)
                {
                    continue;
                }
                else
                {
                    carVariations.Remove(carVariations[i]);

                    //if we're AI and we're out of cars, get a replacement for this one
                    if (carVariations.Count == 0 && !isPlayerOwned)
                    {
                        carVariations.Add(PotentialGangVehicle.GetCarFromPool());
                    }

                    GangManager.SaveData();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// gives pistols to this gang if the gangsStartWithPistols mod option is toggled on
        /// </summary>
        public void GetPistolIfOptionsRequire()
        {
            if (ModOptions.Instance.GangsStartWithPistols)
            {
                if (!gangWeaponHashes.Contains(WeaponHash.Pistol))
                {
                    gangWeaponHashes.Add(WeaponHash.Pistol);
                }
            }
        }

        public void TakeZone(TurfZone takenZone, bool doNotify = true)
        {
            if (doNotify && ModOptions.Instance.NotificationsEnabled)
            {
                string notificationMsg = string.Concat("The ", name, " have taken ", takenZone.zoneName);
                if (takenZone.ownerGangName != "none")
                {
                    notificationMsg = string.Concat(notificationMsg, " from the ", takenZone.ownerGangName);
                }
                notificationMsg = string.Concat(notificationMsg, "!");
                UI.Notification.Show(notificationMsg);
            }
            takenZone.value = baseTurfValue;
            takenZone.ownerGangName = name;
            ZoneManager.instance.UpdateZoneData(takenZone);
        }

        /// <summary>
        /// when a gang fights against another, this value is used to influence the outcome.
        /// it varies a little, to give that extra chance to weaker gangs
        /// </summary>
        /// <returns></returns>
        public int GetGangVariedStrengthValue()
        {
            int weaponValue = 200;
            if (gangWeaponHashes.Count > 0)
            {
                BuyableWeapon randomWeap = ModOptions.Instance.GetBuyableWeaponByHash(RandoMath.GetRandomElementFromList(gangWeaponHashes));
                if (randomWeap != null)
                {
                    weaponValue = randomWeap.Price;
                }
            }
            return ZoneManager.instance.GetZonesControlledByGang(name).Count * 50 +
                weaponValue / 200 +
                memberAccuracyLevel * 10 +
                memberArmor +
                memberHealth;
        }

        /// <summary>
        /// this value doesn't have random variations. we use the gang's number of territories
        ///  and upgrades to define this. a high value is around 2000, 3000
        /// </summary>
        /// <returns></returns>
        public int GetFixedStrengthValue()
        {
            return ZoneManager.instance.GetZonesControlledByGang(name).Count * 40 +
                memberAccuracyLevel * 10 +
                memberArmor +
                memberHealth;
        }

        /// <summary>
        /// uses the number of territories and the gang's strength
        /// </summary>
        /// <returns></returns>
        public int GetReinforcementsValue()
        {
            return ZoneManager.instance.GetZonesControlledByGang(name).Count * 50 +
                baseTurfValue * 500;
        }

        public static int CompareGunsByPrice(WeaponHash x, WeaponHash y)
        {
            BuyableWeapon buyableX = ModOptions.Instance.GetBuyableWeaponByHash(x),
                buyableY = ModOptions.Instance.GetBuyableWeaponByHash(y);

            if (buyableX == null)
            {
                if (buyableY == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if (buyableY == null)
                {
                    return 1;
                }
                else
                {

                    return buyableY.Price.CompareTo(buyableX.Price);
                }
            }
        }

        public WeaponHash GetListedGunFromOwnedGuns(List<WeaponHash> targetList)
        {
            List<WeaponHash> possibleGuns = new List<WeaponHash>();
            for (int i = 0; i < gangWeaponHashes.Count; i++)
            {
                if (targetList.Contains(gangWeaponHashes[i]))
                {
                    possibleGuns.Add(gangWeaponHashes[i]);
                }
            }

            if (possibleGuns.Count > 0)
            {
                return RandoMath.GetRandomElementFromList(possibleGuns);
            }
            return WeaponHash.Unarmed;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;

namespace GTA.GangAndTurfMod
{
    public class GangAI : UpdatedClass
    {
        private List<TurfZone> myZones;

        public Gang WatchedGang { get; private set; }

        public override void Update()
        {
            Logger.Log("gang ai update: begin", 5);

            //everyone tries to expand before anything else;
            //that way, we don't end up with isolated gangs or some kind of peace
            myZones = ZoneManager.instance.GetZonesControlledByGang(WatchedGang.name);
            TryExpand();

            switch (WatchedGang.upgradeTendency)
            {
                case Gang.AIUpgradeTendency.bigGuns:
                    TryUpgradeGuns();
                    TryUpgradeGuns(); //yeah, we like guns
                    TryUpgradeZones();
                    if (RandoMath.RandomBool()) TryUpgradeMembers(); //but we're not buff
                    break;
                case Gang.AIUpgradeTendency.moreExpansion:
                    TryExpand();
                    TryExpand(); //that's some serious expansion
                    TryUpgradeGuns();
                    TryUpgradeMembers();
                    if (RandoMath.RandomBool()) TryUpgradeZones(); //...but zone strength isn't our priority
                    break;
                case Gang.AIUpgradeTendency.toughMembers:
                    TryUpgradeMembers();
                    TryUpgradeMembers();
                    TryUpgradeGuns();
                    if (RandoMath.RandomBool()) TryUpgradeZones(); //tough members, but tend to be few in number
                    break;
                case Gang.AIUpgradeTendency.toughTurf:
                    TryUpgradeZones();
                    TryUpgradeZones(); //lots of defenders!
                    TryUpgradeMembers();
                    if (RandoMath.RandomBool()) TryUpgradeGuns(); //...with below average guns
                    break;
            }

            //lets check our financial situation:
            //are we running very low on cash (unable to take even a neutral territory)?
            //do we have any turf? is any war going on?
            //if not, we no longer exist
            if (WatchedGang.moneyAvailable < ModOptions.Instance.BaseCostToTakeTurf)
            {
                if (!GangWarManager.instance.isOccurring)
                {
                    myZones = ZoneManager.instance.GetZonesControlledByGang(WatchedGang.name);
                    if (myZones.Count == 0)
                    {
                        if (ModOptions.Instance.GangsCanBeWipedOut)
                        {
                            GangManager.KillGang(this);
                        }
                        else
                        {
                            //we get some money then, at least to keep trying to fight
                            WatchedGang.moneyAvailable += (int)(ModOptions.Instance.BaseCostToTakeTurf * 5 * ModOptions.Instance.ExtraProfitForAIGangsFactor);
                        }

                    }
                }
            }

            Logger.Log("gang ai update: end", 5);

        }

        private void TryExpand()
        {
            //lets attack!
            //pick a random zone owned by us, get the closest hostile zone and attempt to take it
            //..but only if the player hasn't disabled expansion
            if (ModOptions.Instance.PreventAIExpansion) return;

            if (myZones.Count > 0)
            {
                TurfZone chosenZone = RandoMath.GetRandomElementFromList(myZones);
                TurfZone closestZoneToChosen = ZoneManager.instance.GetClosestZoneToTargetZone(chosenZone, true);
                TryTakeTurf(closestZoneToChosen);
            }
            else
            {
                //we're out of turf!
                //get a random zone (preferably neutral, since it's cheaper for the AI) and try to take it
                //but only sometimes, since we're probably on a tight spot
                TurfZone chosenZone = ZoneManager.instance.GetRandomZone(true);
                TryTakeTurf(chosenZone);
            }
        }

        private void TryUpgradeGuns()
        {
            //try to buy the weapons we like
            if (WatchedGang.preferredWeaponHashes.Count == 0)
            {
                WatchedGang.SetPreferredWeapons();
            }

            WeaponHash chosenWeapon = RandoMath.GetRandomElementFromList(WatchedGang.preferredWeaponHashes);

            if (!WatchedGang.gangWeaponHashes.Contains(chosenWeapon))
            {
                //maybe the chosen weapon can no longer be bought
                if (ModOptions.Instance.GetBuyableWeaponByHash(chosenWeapon) == null)
                {
                    WatchedGang.preferredWeaponHashes.Remove(chosenWeapon);
                    GangManager.
                    return;
                }

                if (WatchedGang.moneyAvailable >= ModOptions.Instance.GetBuyableWeaponByHash(chosenWeapon).Price)
                {
                    WatchedGang.moneyAvailable -= ModOptions.Instance.GetBuyableWeaponByHash(chosenWeapon).Price;
                    WatchedGang.gangWeaponHashes.Add(chosenWeapon);
                    GangManager.SaveData(false);
                }
            }
        }

        private void TryUpgradeMembers()
        {
            //since we've got some extra cash, lets upgrade our members!
            switch (RandoMath.CachedRandom.Next(3))
            {
                case 0: //accuracy!
                    if (WatchedGang.memberAccuracyLevel < ModOptions.Instance.MaxGangMemberAccuracy &&
                WatchedGang.moneyAvailable >= GangCalculations.CalculateAccuracyUpgradeCost(WatchedGang.memberAccuracyLevel))
                    {
                        WatchedGang.moneyAvailable -= GangCalculations.CalculateAccuracyUpgradeCost(WatchedGang.memberAccuracyLevel);
                        WatchedGang.memberAccuracyLevel += ModOptions.Instance.GetAccuracyUpgradeIncrement();
                        if (WatchedGang.memberAccuracyLevel > ModOptions.Instance.MaxGangMemberAccuracy)
                        {
                            WatchedGang.memberAccuracyLevel = ModOptions.Instance.MaxGangMemberAccuracy;
                        }

                        GangManager.SaveData(false);
                    }
                    break;
                case 1: //armor!
                    if (WatchedGang.memberArmor < ModOptions.Instance.MaxGangMemberArmor &&
                            WatchedGang.moneyAvailable >= GangCalculations.CalculateArmorUpgradeCost(WatchedGang.memberArmor))
                    {
                        WatchedGang.moneyAvailable -= GangCalculations.CalculateArmorUpgradeCost(WatchedGang.memberArmor);
                        WatchedGang.memberArmor += ModOptions.Instance.GetArmorUpgradeIncrement();

                        if (WatchedGang.memberArmor > ModOptions.Instance.MaxGangMemberArmor)
                        {
                            WatchedGang.memberArmor = ModOptions.Instance.MaxGangMemberArmor;
                        }

                        GangManager.SaveData(false);
                    }
                    break;

                default: //health!
                    if (WatchedGang.memberHealth < ModOptions.Instance.MaxGangMemberHealth &&
                            WatchedGang.moneyAvailable >= GangCalculations.CalculateHealthUpgradeCost(WatchedGang.memberHealth))
                    {
                        WatchedGang.moneyAvailable -= GangCalculations.CalculateHealthUpgradeCost(WatchedGang.memberHealth);
                        WatchedGang.memberHealth += ModOptions.Instance.GetHealthUpgradeIncrement();

                        if (WatchedGang.memberHealth > ModOptions.Instance.MaxGangMemberHealth)
                        {
                            WatchedGang.memberHealth = ModOptions.Instance.MaxGangMemberHealth;
                        }

                        GangManager.SaveData(false);
                    }
                    break;
            }

        }

        private void TryUpgradeZones()
        {
            int upgradeCost = GangCalculations.CalculateGangValueUpgradeCost(WatchedGang.baseTurfValue);
            //upgrade the whole gang strength if possible!
            //lets not get more upgrades here than the player. it may get too hard for the player to catch up otherwise
            if (WatchedGang.moneyAvailable >= upgradeCost &&
                WatchedGang.baseTurfValue <= GangManager.PlayerGang.baseTurfValue - 1)
            {
                WatchedGang.moneyAvailable -= upgradeCost;
                WatchedGang.baseTurfValue++;
                GangManager.SaveData(false);
                return;
            }
            //if we have enough money to upgrade a zone,
            //try upgrading our toughest zone... or one that we can afford upgrading
            int lastCheckedValue = ModOptions.Instance.MaxTurfValue;
            for (int i = 0; i < myZones.Count; i++)
            {
                if (myZones[i].value >= lastCheckedValue) continue; //we already know we can't afford upgrading from this turf level
                upgradeCost = GangCalculations.CalculateTurfValueUpgradeCost(myZones[i].value);
                if (WatchedGang.moneyAvailable >= upgradeCost)
                {
                    WatchedGang.moneyAvailable -= upgradeCost;
                    myZones[i].value++;
                    ZoneManager.instance.IsDirty = true;
                    return;
                }
                else
                {
                    lastCheckedValue = myZones[i].value;
                }
            }
        }

        private void TryTakeTurf(TurfZone targetZone)
        {
            if (targetZone == null || targetZone.ownerGangName == WatchedGang.name) return; //whoops, there just isn't any zone available for our gang
            if (targetZone.ownerGangName == "none")
            {
                //this zone is neutral, lets just take it
                if (WatchedGang.moneyAvailable >= ModOptions.Instance.BaseCostToTakeTurf)
                {
                    WatchedGang.moneyAvailable -= ModOptions.Instance.BaseCostToTakeTurf;
                    WatchedGang.TakeZone(targetZone);
                }
            }
            else
            {
                TryStartFightForZone(targetZone);
            }
        }

        /// <summary>
        /// if fighting is enabled and the targetzone is controlled by an enemy, attack it! ... But only if it's affordable.
        /// if we're desperate we do it anyway
        /// </summary>
        /// <param name="targetZone"></param>
        private void TryStartFightForZone(TurfZone targetZone)
        {
            Gang ownerGang = GangManager.GetGangByName(targetZone.ownerGangName);

            if (ownerGang == null)
            {
                Logger.Log("Gang with name " + targetZone.ownerGangName + " no longer exists; assigning all owned turf to 'none'", 1);
                ZoneManager.instance.GiveGangZonesToAnother(targetZone.ownerGangName, "none");

                //this zone was controlled by a gang that no longer exists. it is neutral now
                if (WatchedGang.moneyAvailable >= ModOptions.Instance.BaseCostToTakeTurf)
                {
                    WatchedGang.moneyAvailable -= ModOptions.Instance.BaseCostToTakeTurf;
                    WatchedGang.TakeZone(targetZone);
                }
            }
            else
            {
                if (GangWarManager.instance.isOccurring && GangWarManager.instance.warZone == targetZone)
                {
                    //don't mess with this zone then, it's a warzone
                    return;
                }
                //we check how well defended this zone is,
                //then figure out how large our attack should be.
                //if we can afford that attack, we do it
                int defenderStrength = GangCalculations.CalculateDefenderStrength(ownerGang, targetZone);
                GangWarManager.AttackStrength requiredStrength =
                    GangCalculations.CalculateRequiredAttackStrength(WatchedGang, defenderStrength);
                int atkCost = GangCalculations.CalculateAttackCost(WatchedGang, requiredStrength);

                if (WatchedGang.moneyAvailable < atkCost)
                {
                    if (myZones.Count == 0)
                    {
                        //if we're out of turf and cant afford a decent attack, lets just attack anyway
                        //we use a light attack and do it even if that means our money gets negative.
                        //this should make gangs get back in the game or be wiped out instead of just staying away
                        requiredStrength = GangWarManager.AttackStrength.light;
                        atkCost = GangCalculations.CalculateAttackCost(WatchedGang, requiredStrength);
                    }
                    else
                    {
                        return; //hopefully we can just find a cheaper fight
                    }
                }

                if (targetZone.ownerGangName == GangManager.PlayerGang.name)
                {
                    if (ModOptions.Instance.WarAgainstPlayerEnabled &&
                    GangWarManager.instance.CanStartWarAgainstPlayer)
                    {
                        //the player may be in big trouble now
                        WatchedGang.moneyAvailable -= atkCost;
                        GangWarManager.instance.StartWar(WatchedGang, targetZone, GangWarManager.WarType.defendingFromEnemy, requiredStrength);
                    }
                }
                else
                {
                    WatchedGang.moneyAvailable -= atkCost;
                    int attackStrength = GangCalculations.CalculateAttackerStrength(WatchedGang, requiredStrength);
                    //roll dices... favor the defenders a little here
                    if (attackStrength / RandoMath.CachedRandom.Next(1, 22) >
                        defenderStrength / RandoMath.CachedRandom.Next(1, 15))
                    {
                        WatchedGang.TakeZone(targetZone);
                        WatchedGang.moneyAvailable += (int)(GangCalculations.CalculateBattleRewards(ownerGang, targetZone.value, true) *
                            ModOptions.Instance.ExtraProfitForAIGangsFactor);
                    }
                    else
                    {
                        ownerGang.moneyAvailable += (int)(GangCalculations.CalculateBattleRewards(WatchedGang, (int)requiredStrength, false) *
                            ModOptions.Instance.ExtraProfitForAIGangsFactor);
                    }

                }
            }
        }

        /// <summary>
        /// if this gang seems to be new, makes it take up to 3 neutral zones
        /// </summary>
        private void DoInitialTakeover()
        {

            if (WatchedGang.gangWeaponHashes.Count > 0 || ZoneManager.instance.GetZonesControlledByGang(WatchedGang.name).Count > 2)
            {
                //we've been around for long enough to get weapons or get turf, abort
                return;
            }

            TurfZone chosenZone = ZoneManager.instance.GetRandomZone(true);

            if (chosenZone.ownerGangName == "none")
            {
                WatchedGang.TakeZone(chosenZone, false);
                //we took one, now we should spread the influence around it
                for (int i = 0; i < 3; i++)
                {
                    TurfZone nearbyZone = ZoneManager.instance.GetClosestZoneToTargetZone(chosenZone, true);
                    if (nearbyZone.ownerGangName == "none")
                    {
                        WatchedGang.TakeZone(nearbyZone, false);
                        //and use this new zone as reference from now on
                        chosenZone = nearbyZone;
                    }
                }
            }
            else
            {
                //no neutral turf available, abort!
                return;
            }
        }

        public void ResetUpdateInterval()
        {
            ticksBetweenUpdates = ModOptions.Instance.TicksBetweenGangAIUpdates + RandoMath.CachedRandom.Next(100);
            ticksSinceLastUpdate = ticksBetweenUpdates;
        }

        public GangAI(Gang watchedGang)
        {
            WatchedGang = watchedGang ?? throw new ArgumentNullException(nameof(watchedGang));
            ResetUpdateInterval();

            //have some turf for free! but only if you're new around here
            DoInitialTakeover();

            //do we have vehicles?
            if (WatchedGang.carVariations.Count == 0)
            {
                //get some vehicles!
                for (int i = 0; i < RandoMath.CachedRandom.Next(1, 4); i++)
                {
                    PotentialGangVehicle newVeh = PotentialGangVehicle.GetCarFromPool();
                    if (newVeh != null)
                    {
                        WatchedGang.AddGangCar(newVeh);
                    }
                }
            }
        }

    }
}

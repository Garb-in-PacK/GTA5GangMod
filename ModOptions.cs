﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;

namespace GTA
{
    [System.Serializable]
    public class ModOptions
    {
        public static ModOptions instance;

        public ModOptions()
        {
            instance = this;
            ModOptions loadedOptions = PersistenceHandler.LoadFromFile<ModOptions>("ModOptions");
            if(loadedOptions != null)
            {
                //get the loaded options
                this.possibleGangFirstNames = loadedOptions.possibleGangFirstNames;
                this.possibleGangLastNames = loadedOptions.possibleGangLastNames;
                this.maxGangMemberHealth = loadedOptions.maxGangMemberHealth;
                this.maxGangMemberArmor = loadedOptions.maxGangMemberArmor;
                this.ticksBetweenTurfRewards = loadedOptions.ticksBetweenTurfRewards;
                this.baseRewardPerZoneOwned = loadedOptions.baseRewardPerZoneOwned;
                this.buyableWeapons = loadedOptions.buyableWeapons;
                this.similarColors = loadedOptions.similarColors;
                this.wantedFactorWhenInGangTurf = loadedOptions.wantedFactorWhenInGangTurf;
            }
            else
            {
                PersistenceHandler.SaveToFile(this, "ModOptions");
            }
        }

        public int maxGangMemberHealth = 400;
        public int maxGangMemberArmor = 100;

        public int ticksBetweenTurfRewards = 50000;
        public int baseRewardPerZoneOwned = 500;

        public float wantedFactorWhenInGangTurf = 0.4f;

        //XMLserializer does not like dictionaries
        [System.Serializable]
        public class BuyableWeapon
        {
            public WeaponHash wepHash;
            public int price;

            public BuyableWeapon()
            {
                wepHash = WeaponHash.SNSPistol;
                price = 500;
            }

            public BuyableWeapon(WeaponHash wepHash, int price)
            {
                this.wepHash = wepHash;
                this.price = price;
            }
        }

        [System.Serializable]
        public class GangColorTranslation
        {
            public List<VehicleColor> vehicleColors;
            public PotentialGangMember.memberColor baseColor;

            public GangColorTranslation()
            {
                vehicleColors = new List<VehicleColor>();
            }

            public GangColorTranslation(PotentialGangMember.memberColor baseColor, List<VehicleColor> vehicleColors)
            {
                this.baseColor = baseColor;
                this.vehicleColors = vehicleColors;
            }
        }

        public BuyableWeapon GetBuyableWeaponByHash(WeaponHash wepHash)
        {
            for (int i = 0; i < buyableWeapons.Count; i++)
            {
                if (buyableWeapons[i].wepHash == wepHash)
                {
                    return buyableWeapons[i];
                }
            }

            return null;
        }

        public GangColorTranslation GetGangColorTranslation(PotentialGangMember.memberColor baseColor)
        {
            for (int i = 0; i < similarColors.Count; i++)
            {
                if (similarColors[i].baseColor == baseColor)
                {
                    return similarColors[i];
                }
            }

            return null;
        }

        public  List<BuyableWeapon> buyableWeapons = new List<BuyableWeapon>()
        {
            new BuyableWeapon(WeaponHash.AdvancedRifle, 20000),
            new BuyableWeapon(WeaponHash.APPistol, 5000),
            new BuyableWeapon(WeaponHash.AssaultRifle, 12000),
            new BuyableWeapon(WeaponHash.AssaultShotgun, 25000),
            new BuyableWeapon(WeaponHash.AssaultSMG, 19000),
            new BuyableWeapon(WeaponHash.BullpupRifle, 23000),
            new BuyableWeapon(WeaponHash.BullpupShotgun, 26500),
            new BuyableWeapon(WeaponHash.CarbineRifle, 15000),
            new BuyableWeapon(WeaponHash.CombatMG, 22000),
            new BuyableWeapon(WeaponHash.CombatPDW, 20500),
            new BuyableWeapon(WeaponHash.CombatPistol, 5000),
            new BuyableWeapon(WeaponHash.GrenadeLauncher, 28000),
            new BuyableWeapon(WeaponHash.Gusenberg, 10000),
            new BuyableWeapon(WeaponHash.HeavyPistol, 4000),
            new BuyableWeapon(WeaponHash.HeavyShotgun, 18000),
            new BuyableWeapon(WeaponHash.HeavySniper, 30000),
            new BuyableWeapon(WeaponHash.MachinePistol, 5500),
            new BuyableWeapon(WeaponHash.MarksmanPistol, 5000),
            new BuyableWeapon(WeaponHash.MarksmanRifle, 20000),
            new BuyableWeapon(WeaponHash.MG, 19000),
            new BuyableWeapon(WeaponHash.MicroSMG, 7000),
            new BuyableWeapon(WeaponHash.Minigun, 40000),
            new BuyableWeapon(WeaponHash.Musket, 5000),
            new BuyableWeapon(WeaponHash.Pistol, 1000),
            new BuyableWeapon(WeaponHash.Pistol50, 3000),
            new BuyableWeapon(WeaponHash.PumpShotgun, 10000),
            new BuyableWeapon(WeaponHash.Railgun, 50000),
            new BuyableWeapon(WeaponHash.RPG, 32000),
            new BuyableWeapon(WeaponHash.SawnOffShotgun, 8000),
            new BuyableWeapon(WeaponHash.SMG, 11000),
            new BuyableWeapon(WeaponHash.SniperRifle, 23000),
            new BuyableWeapon(WeaponHash.SNSPistol, 900),
            new BuyableWeapon(WeaponHash.VintagePistol, 5000)
        };


        public List<string> possibleGangFirstNames = new List<string>
        {
            "Rocket",
            "Magic",
            "High",
            "Miami",
            "Vicious",
            "Electric",
            "Bloody",
            "Brazilian",
            "French",
            "Italian",
            "Greek",
            "Roman",
            "Serious",
            "Japanese",
            "Holy",
            "Colombian",
            "American",
            "Sweet",
            "Cute",
            "Killer",
            "Merciless",
            "666",
            "Street",
            "Business",
            "Beach",
            "Night",
            "Laughing",
            "Watchful",
            "Vigilant",
            "Laser",
            "Swedish",
            "Fire",
            "Ice",
            "Snow",
            "Gothic",
            "Gold",
            "Silver",
            "Iron",
            "Steel",
            "Robot",
            "Chemical",
            "New Wave",
            "Nihilist",
            "Legendary",
            "Epic"
        };

        public List<string> possibleGangLastNames = new List<string>
        {
            "League",
            "Sword",
            "Landers",
            "Vice",
            "Gang",
            "Eliminators",
            "Kittens",
            "Cats",
            "Murderers",
            "Mob",
            "Invaders",
            "Mafia",
            "Skull",
            "Ghosts",
            "Dealers",
            "People",
            "Men",
            "Women",
            "Tigers",
            "Triad",
            "Watchers",
            "Vigilantes",
            "Militia",
            "Wolves",
            "Bears",
            "Infiltrators",
            "Barbarians",
            "Goths",
            "Gunners",
            "Hunters",
            "Sharks",
            "Unicorns",
            "Pegasi",
            "Industry Leaders",
            "Gangsters",
            "Hobos",
            "Hookers",
            "Reapers",
            "Dogs",
            "Soldiers",
            "Mobsters",
            "Company",
            "Friends",
            "Monsters",
            "Girls",
            "Guys",
            "Fighters",
        };

        public List<GangColorTranslation> similarColors = new List<GangColorTranslation>
        {
            new GangColorTranslation(PotentialGangMember.memberColor.black, new List<VehicleColor> {
                 VehicleColor.BrushedBlackSteel,
                VehicleColor.MatteBlack,
                VehicleColor.MetallicBlack,
                VehicleColor.MetallicGraphiteBlack,
                VehicleColor.UtilBlack,
                VehicleColor.WornBlack,
                VehicleColor.ModshopBlack1
            }
           ),
            new GangColorTranslation(PotentialGangMember.memberColor.blue, new List<VehicleColor> {
                 VehicleColor.Blue,
                VehicleColor.EpsilonBlue,
                VehicleColor.MatteBlue,
                VehicleColor.MatteDarkBlue,
                VehicleColor.MatteMidnightBlue,
                VehicleColor.MetaillicVDarkBlue,
                VehicleColor.MetallicBlueSilver,
                VehicleColor.MetallicBrightBlue,
                VehicleColor.MetallicDarkBlue,
                VehicleColor.MetallicDiamondBlue,
                VehicleColor.MetallicHarborBlue,
                VehicleColor.MetallicMarinerBlue,
                VehicleColor.UtilBlue,
                VehicleColor.MetallicUltraBlue
            }
           ),
            new GangColorTranslation(PotentialGangMember.memberColor.green, new List<VehicleColor> {
                 VehicleColor.Green,
                VehicleColor.HunterGreen,
                VehicleColor.MatteFoliageGreen,
                VehicleColor.MatteForestGreen,
                VehicleColor.MatteGreen,
                VehicleColor.MatteLimeGreen,
                VehicleColor.MetallicDarkGreen,
                VehicleColor.MetallicGreen,
                VehicleColor.MetallicRacingGreen,
                VehicleColor.UtilDarkGreen,
                VehicleColor.MetallicOliveGreen,
                VehicleColor.WornGreen,
            }
           ),
            new GangColorTranslation(PotentialGangMember.memberColor.pink, new List<VehicleColor> {
                 VehicleColor.HotPink,
                VehicleColor.MetallicVermillionPink,
            }
           ),
            new GangColorTranslation(PotentialGangMember.memberColor.purple, new List<VehicleColor> {
                 VehicleColor.MatteDarkPurple,
                VehicleColor.MattePurple,
                VehicleColor.MetallicPurple,
                VehicleColor.MetallicPurpleBlue,
            }
           ),
            new GangColorTranslation(PotentialGangMember.memberColor.red, new List<VehicleColor> {
                 VehicleColor.MatteDarkRed,
                VehicleColor.MatteRed,
                VehicleColor.MetallicBlazeRed,
                VehicleColor.MetallicCabernetRed,
                VehicleColor.MetallicCandyRed,
                VehicleColor.MetallicDesertRed,
                VehicleColor.MetallicFormulaRed,
                VehicleColor.MetallicGarnetRed,
                VehicleColor.MetallicGracefulRed,
                VehicleColor.MetallicLavaRed,
                VehicleColor.MetallicRed,
                VehicleColor.MetallicTorinoRed,
                VehicleColor.UtilBrightRed,
                VehicleColor.UtilGarnetRed,
                VehicleColor.UtilRed,
                VehicleColor.WornDarkRed,
                VehicleColor.WornGoldenRed,
                VehicleColor.WornRed,
            }
           ),
            new GangColorTranslation(PotentialGangMember.memberColor.white, new List<VehicleColor> {
                 VehicleColor.MatteWhite,
                VehicleColor.MetallicFrostWhite,
                VehicleColor.MetallicWhite,
                VehicleColor.PureWhite,
                VehicleColor.UtilOffWhite,
                VehicleColor.WornOffWhite,
                VehicleColor.WornWhite,
            }
           ),
            new GangColorTranslation(PotentialGangMember.memberColor.yellow, new List<VehicleColor> {
                 VehicleColor.MatteYellow,
                VehicleColor.MetallicRaceYellow,
                VehicleColor.MetallicTaxiYellow,
                VehicleColor.MetallicYellowBird,
                VehicleColor.WornTaxiYellow,
                VehicleColor.BrushedGold,
                VehicleColor.MetallicClassicGold,
                VehicleColor.PureGold,
                VehicleColor.MetallicGoldenBrown,
            }
           )
        };

        public void SaveOptions()
        {
            PersistenceHandler.SaveToFile<ModOptions>(this, "ModOptions");
        }
    }
}
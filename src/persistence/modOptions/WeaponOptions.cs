using GTA.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// contains options related to weapons: which weapons are melee? which ones are "sidearms"? which ones can gangs buy?
    /// </summary>
    [Serializable]
    public class WeaponOptions : IModOptionGroup
    {

        public WeaponOptions() { }

        public void SetOptionsToDefault()
        {
            SetWeaponListDefaultValues();
        }

        [XmlIgnore]
        public List<WeaponHash> PrimaryWeapons { get; private set; } = new List<WeaponHash>();

        [XmlIgnore]
        public List<WeaponHash> DriveByWeapons { get; } = new List<WeaponHash>()
        {
            WeaponHash.APPistol,
            WeaponHash.CombatPistol,
            WeaponHash.HeavyPistol,
            WeaponHash.MachinePistol,
            WeaponHash.MarksmanPistol,
            WeaponHash.Pistol,
            WeaponHash.PistolMk2,
            WeaponHash.Pistol50,
            WeaponHash.Revolver,
            WeaponHash.DoubleActionRevolver,
            WeaponHash.RevolverMk2,
            WeaponHash.SawnOffShotgun,
            WeaponHash.SNSPistol,
            WeaponHash.SNSPistolMk2,
            WeaponHash.VintagePistol,
            WeaponHash.MicroSMG
        };

        [XmlIgnore]
        public List<WeaponHash> MeleeWeapons { get; } = new List<WeaponHash>()
        {
            WeaponHash.Bat,
            WeaponHash.BattleAxe,
            WeaponHash.Bottle,
            WeaponHash.Crowbar,
            WeaponHash.Dagger,
            WeaponHash.GolfClub,
            WeaponHash.Hammer,
            WeaponHash.Hatchet,
            WeaponHash.Knife,
            WeaponHash.KnuckleDuster,
            WeaponHash.Machete,
            WeaponHash.Nightstick,
            WeaponHash.PoolCue,
            WeaponHash.SwitchBlade,
            WeaponHash.Wrench,
        };

        public List<BuyableWeapon> BuyableWeapons { get; private set; }

        public BuyableWeapon GetBuyableWeaponByHash(WeaponHash wepHash)
        {
            for (int i = 0; i < BuyableWeapons.Count; i++)
            {
                if (BuyableWeapons[i].WepHash == wepHash)
                {
                    return BuyableWeapons[i];
                }
            }

            return null;
        }

        /// <summary>
        /// gets a weapon from a list and check if it is in the buyables list.
        /// if it isn't, get another or get a random one from the buyables
        /// </summary>
        /// <param name="theWeaponList"></param>
        /// <returns></returns>
        public WeaponHash GetWeaponFromListIfBuyable(List<WeaponHash> theWeaponList)
        {
            for (int attempts = 0; attempts < 5; attempts++)
            {
                WeaponHash chosenWeapon = RandoMath.GetRandomElementFromList(theWeaponList);
                if (GetBuyableWeaponByHash(chosenWeapon) != null)
                {
                    return chosenWeapon;
                }
            }

            return RandoMath.GetRandomElementFromList(BuyableWeapons).WepHash;
        }

        public void SetupPrimaryWeapons(IDirtableSaveable modOptions)
        {
            if (modOptions == null) throw new ArgumentNullException(nameof(modOptions));

            if (PrimaryWeapons != null)
            {
                PrimaryWeapons.Clear();
            }
            else
            {
                PrimaryWeapons = new List<WeaponHash>();
            }

            if (BuyableWeapons == null)
            {
                SetWeaponListDefaultValues();
                modOptions.IsDirty = true;
            }
            //primary weapons are the ones that are not melee and cannot be used to drive-by (the bigger weapons, like rifles)
            for (int i = 0; i < BuyableWeapons.Count; i++)
            {
                if (!MeleeWeapons.Contains(BuyableWeapons[i].WepHash) &&
                    !DriveByWeapons.Contains(BuyableWeapons[i].WepHash))
                {
                    PrimaryWeapons.Add(BuyableWeapons[i].WepHash);
                }
            }
        }

        public void SetWeaponListDefaultValues()
        {
            BuyableWeapons = new List<BuyableWeapon>()
        {
            //--melee
			
            new BuyableWeapon(WeaponHash.Bat, 1000),
            new BuyableWeapon(WeaponHash.BattleAxe, 4500),
            new BuyableWeapon(WeaponHash.Bottle, 500),
            new BuyableWeapon(WeaponHash.Crowbar, 800),
            new BuyableWeapon(WeaponHash.Dagger, 4000),
            new BuyableWeapon(WeaponHash.GolfClub, 3000),
            new BuyableWeapon(WeaponHash.Hammer, 800),
            new BuyableWeapon(WeaponHash.Hatchet, 1100),
            new BuyableWeapon(WeaponHash.Knife, 1000),
            new BuyableWeapon(WeaponHash.KnuckleDuster, 650),
            new BuyableWeapon(WeaponHash.Machete, 1050),
            new BuyableWeapon(WeaponHash.Nightstick, 700),
            new BuyableWeapon(WeaponHash.PoolCue, 730),
            new BuyableWeapon(WeaponHash.SwitchBlade, 1100),
            new BuyableWeapon(WeaponHash.Wrench, 560),
			//--guns
            new BuyableWeapon(WeaponHash.AdvancedRifle, 200000),
            new BuyableWeapon(WeaponHash.APPistol, 60000),
            new BuyableWeapon(WeaponHash.AssaultRifle, 120000),
            new BuyableWeapon(WeaponHash.AssaultrifleMk2, 195000),
            new BuyableWeapon(WeaponHash.AssaultShotgun, 250000),
            new BuyableWeapon(WeaponHash.AssaultSMG, 190000),
            new BuyableWeapon(WeaponHash.BullpupRifle, 230000),
            new BuyableWeapon(WeaponHash.BullpupRifleMk2, 285000),
            new BuyableWeapon(WeaponHash.BullpupShotgun, 265000),
            new BuyableWeapon(WeaponHash.CarbineRifle, 150000),
            new BuyableWeapon(WeaponHash.CarbineRifleMk2, 210000),
            new BuyableWeapon(WeaponHash.CombatMG, 220000),
            new BuyableWeapon(WeaponHash.CombatMGMk2, 245000),
            new BuyableWeapon(WeaponHash.CombatPDW, 205000),
            new BuyableWeapon(WeaponHash.CombatPistol, 50000),
            new BuyableWeapon(WeaponHash.CompactGrenadeLauncher, 1000000),
            new BuyableWeapon(WeaponHash.CompactRifle, 175000),
            new BuyableWeapon(WeaponHash.DoubleActionRevolver, 120000),
            new BuyableWeapon(WeaponHash.DoubleBarrelShotgun, 210000),
            new BuyableWeapon(WeaponHash.Firework, 1000000),
            new BuyableWeapon(WeaponHash.FlareGun, 600000),
            new BuyableWeapon(WeaponHash.GrenadeLauncher, 950000),
            new BuyableWeapon(WeaponHash.Gusenberg, 200000),
            new BuyableWeapon(WeaponHash.HeavyPistol, 55000),
            new BuyableWeapon(WeaponHash.HeavyShotgun, 180000),
            new BuyableWeapon(WeaponHash.HeavySniper, 300000),
            new BuyableWeapon(WeaponHash.HeavySniperMk2, 380000),
            new BuyableWeapon(WeaponHash.HomingLauncher, 2100000),
            new BuyableWeapon(WeaponHash.MachinePistol, 65000),
            new BuyableWeapon(WeaponHash.MarksmanPistol, 50000),
            new BuyableWeapon(WeaponHash.MarksmanRifle, 250000),
            new BuyableWeapon(WeaponHash.MarksmanRifleMk2, 310000),
            new BuyableWeapon(WeaponHash.MG, 290000),
            new BuyableWeapon(WeaponHash.MicroSMG, 90000),
            new BuyableWeapon(WeaponHash.Minigun, 400000),
            new BuyableWeapon(WeaponHash.MiniSMG, 100000),
            new BuyableWeapon(WeaponHash.Musket, 70000),
            new BuyableWeapon(WeaponHash.Pistol, 30000),
            new BuyableWeapon(WeaponHash.Pistol50, 70000),
            new BuyableWeapon(WeaponHash.PistolMk2, 65000),
            new BuyableWeapon(WeaponHash.PumpShotgun, 100000),
            new BuyableWeapon(WeaponHash.PumpShotgunMk2, 135000),
            new BuyableWeapon(WeaponHash.Railgun, 5100000),
            new BuyableWeapon(WeaponHash.Revolver, 80000),
            new BuyableWeapon(WeaponHash.RevolverMk2, 100000),
            new BuyableWeapon(WeaponHash.RPG, 1200000),
            new BuyableWeapon(WeaponHash.SawnOffShotgun, 95000),
            new BuyableWeapon(WeaponHash.SMG, 115000),
            new BuyableWeapon(WeaponHash.SMGMk2, 155000),
            new BuyableWeapon(WeaponHash.SniperRifle, 230000),
            new BuyableWeapon(WeaponHash.SNSPistol, 27000),
            new BuyableWeapon(WeaponHash.SNSPistolMk2, 38000),
            new BuyableWeapon(WeaponHash.SpecialCarbine, 230000),
            new BuyableWeapon(WeaponHash.SpecialCarbineMk2, 290000),
            new BuyableWeapon(WeaponHash.StunGun, 45000),
            new BuyableWeapon(WeaponHash.SweeperShotgun, 230000),
            new BuyableWeapon(WeaponHash.UnholyHellbringer, 5100000),
            new BuyableWeapon(WeaponHash.UpNAtomizer, 4100000),
            new BuyableWeapon(WeaponHash.VintagePistol, 50000),
            new BuyableWeapon(WeaponHash.Widowmaker, 5100000),
        };
        }
    }
}

using GTA.Native;

namespace GTA.GangAndTurfMod
{
    public class BuyableWeapon
    {
        public int Price { get; private set; }

        public WeaponHash WepHash { get; private set; }

        public BuyableWeapon()
        {
            WepHash = WeaponHash.SNSPistol;
            Price = 500;
        }

        public BuyableWeapon(WeaponHash wepHash, int price)
        {
            WepHash = wepHash;
            Price = price;
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// contains zones' upgrade costs, rewards, max levels and consequences of leveling
    /// </summary>
    [Serializable]
    public class ZoneOptions : IModOptionGroup
    {

        public ZoneOptions() { }

        public void SetOptionsToDefault()
        {
            MsTimeBetweenTurfRewards = 180000;

            BaseRewardPerTurfOwned = 1200;
            MaxRewardPerTurfOwned = 15000;
            MaxTurfLevel = 10;

            RewardMultiplierPerZone = 0.1f;

            BaseCostToTakeTurf = 3000;
            RewardForTakingEnemyTurf = 5000;

            BaseCostToUpgradeGeneralGangTurfLevel = 1000000;
            BaseCostToUpgradeSingleTurfLevel = 2000;

            WantedFactorInMaxedGangTurf = 0.0f;
            MaxWantedLevelInMaxedGangTurf = 0;
        }

        public int BaseRewardPerTurfOwned { get; set; } = 1200;
        public int MaxRewardPerTurfOwned { get; set; } = 15000;
        public int MaxTurfLevel { get; set; } = 10;

        public int MsTimeBetweenTurfRewards { get; set; } = 180000;

        /// <summary>
        /// percentage sum, per zone owned, over the total reward received.
        /// for example, if the gang owns 2 zones and the multiplier is 0.2, the reward percentage will be 140%
        /// </summary>
        public float RewardMultiplierPerZone { get; set; } = 0.1f;

        public int BaseCostToTakeTurf { get; set; } = 3000;
        public int RewardForTakingEnemyTurf { get; set; } = 5000;


        public int BaseCostToUpgradeGeneralGangTurfLevel { get; set; } = 1000000;
        public int BaseCostToUpgradeSingleTurfLevel { get; set; } = 2000;


        public float WantedFactorInMaxedGangTurf { get; set; } = 0.0f;
        public int MaxWantedLevelInMaxedGangTurf { get; set; } = 0;
    }
}

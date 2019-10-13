using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// contains members' base and max statuses and the costs to upgrade them
    /// </summary>
    [Serializable]
    public class MemberUpgradeOptions : IModOptionGroup
    {

        public MemberUpgradeOptions() { }

        public void SetOptionsToDefault()
        {
            StartingGangMemberHealth = 20;
            MaxGangMemberHealth = 120;
            MaxGangMemberArmor = 100;
            MaxGangMemberAccuracy = 30;

            BaseCostToUpgradeArmor = 35000;
            BaseCostToUpgradeHealth = 20000;
            BaseCostToUpgradeAccuracy = 40000;

            NumUpgradesUntilMaxMemberAttribute = 10;

        }


        public int BaseCostToUpgradeArmor { get; set; } = 35000;
        public int BaseCostToUpgradeHealth { get; set; } = 20000;
        public int BaseCostToUpgradeAccuracy { get; set; } = 40000;

        public int StartingGangMemberHealth { get; set; } = 20;
        public int MaxGangMemberHealth { get; set; } = 120;
        public int MaxGangMemberArmor { get; set; } = 100;
        public int MaxGangMemberAccuracy { get; set; } = 30;

        public int NumUpgradesUntilMaxMemberAttribute { get; set; } = 10;

        /// <summary>
        /// gets how much the member accuracy increases with each upgrade (this depends on maxGangMemberAccuracy and numUpgradesUntilMaxMemberAttribute)
        /// </summary>
        /// <returns></returns>
        public int GetAccuracyUpgradeIncrement()
        {
            return RandoMath.Max(1, MaxGangMemberAccuracy / NumUpgradesUntilMaxMemberAttribute);
        }

        /// <summary>
        /// gets how much the member health increases with each upgrade (this depends on maxGangMemberHealth and numUpgradesUntilMaxMemberAttribute)
        /// </summary>
        /// <returns></returns>
        public int GetHealthUpgradeIncrement()
        {
            return RandoMath.Max(1, MaxGangMemberHealth / NumUpgradesUntilMaxMemberAttribute);
        }

        /// <summary>
        /// gets how much the member armor increases with each upgrade (this depends on maxGangMemberArmor and numUpgradesUntilMaxMemberAttribute)
        /// </summary>
        /// <returns></returns>
        public int GetArmorUpgradeIncrement()
        {
            return RandoMath.Max(1, MaxGangMemberArmor / NumUpgradesUntilMaxMemberAttribute);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// contains costs and cooldowns for backup calls
    /// </summary>
    [Serializable]
    public class BackupOptions : IModOptionGroup
    {

        public BackupOptions() { }

        public void SetOptionsToDefault()
        {
            CostToCallBackupCar = 900;
            CostToCallParachutingMember = 250;
            TicksCooldownBackupCar = 1000;
            TicksCooldownParachutingMember = 600;
        }


        public int CostToCallBackupCar { get; set; } = 900;
        public int CostToCallParachutingMember { get; set; } = 250;
        public int TicksCooldownBackupCar { get; set; } = 1000;
        public int TicksCooldownParachutingMember { get; set; } = 600;
    }
}

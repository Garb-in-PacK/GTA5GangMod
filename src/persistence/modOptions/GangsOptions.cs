using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// contains options related to all gangs, like the max number of coexisting gangs
    /// </summary>
    [Serializable]
    public class GangsOptions : IModOptionGroup
    {

        public GangsOptions() { }

        public void SetOptionsToDefault()
        {
            GangsStartWithPistols = true;
            GangsCanBeWipedOut = true;
            MaxCoexistingGangs = 7;
        }


        public bool GangsStartWithPistols { get; set; } = true;
        public bool GangsCanBeWipedOut { get; set; } = true;
        public int MaxCoexistingGangs { get; set; } = 7;
    }
}

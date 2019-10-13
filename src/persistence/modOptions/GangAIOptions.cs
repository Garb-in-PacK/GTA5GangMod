using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// contains update intervals and extra controllers/limiters for the AI Gangs
    /// </summary>
    [Serializable]
    public class GangAIOptions : IModOptionGroup
    {

        public GangAIOptions() { }

        public void SetOptionsToDefault()
        {
            TicksBetweenGangAIUpdates = 15000;
            MinMsTimeBetweenAttacksOnPlayerTurf = 600000;
            PreventAIExpansion = false;
            ExtraProfitForAIGangsFactor = 1.5f;
        }

        public int TicksBetweenGangAIUpdates { get; set; } = 15000;
        public int MinMsTimeBetweenAttacksOnPlayerTurf { get; set; } = 600000;

        public float ExtraProfitForAIGangsFactor { get; set; } = 1.5f;

        public bool PreventAIExpansion { get; set; }

    }
}

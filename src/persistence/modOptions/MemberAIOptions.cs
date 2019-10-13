using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// contains members' AI configurations, like AI ticks interval and driving styles
    /// </summary>
    [Serializable]
    public class MemberAIOptions : IModOptionGroup
    {

        public MemberAIOptions() { }

        public void SetOptionsToDefault()
        {
            GangMemberAggressiveness = AggressivenessMode.veryAggressive;

            TicksBetweenGangMemberAIUpdates = 100;

            WanderingDriverDrivingStyle = 1 + 2 + 8 + 16 + 32 + 128 + 256;
            DriverWithDestinationDrivingStyle = 4 + 8 + 16 + 32 + 512 + 262144;
        }

        public enum AggressivenessMode
        {
            veryAggressive = 0,
            aggressive = 1,
            defensive = 2
        }

        public int TicksBetweenGangMemberAIUpdates { get; set; } = 100;

        //special thanks to Eddlm for the driving style data! 
        //more info here: https://gtaforums.com/topic/822314-guide-driving-styles/
        public int WanderingDriverDrivingStyle { get; set; } = 1 + 2 + 8 + 32 + 128 + 256;
        public int DriverWithDestinationDrivingStyle { get; set; } = 2 + 4 + 8 + 32 + 512 + 262144;
        public AggressivenessMode GangMemberAggressiveness { get; private set; } = AggressivenessMode.veryAggressive;

        /// <summary>
        /// sets the new aggressiveness mode and makes gangs hostile to cops if set to veryAggressive
        /// </summary>
        /// <param name="newMode"></param>
        public void SetMemberAggressiveness(AggressivenessMode newMode)
        {
            GangMemberAggressiveness = newMode;
            GangManager.SetGangRelationsAccordingToAggrLevel(newMode);
            //makes everyone hate cops if set to very aggressive
            GangManager.SetCopRelations(newMode == AggressivenessMode.veryAggressive);
            MenuScript.Instance.AggOption.Index = (int)newMode;
        }

    }
}

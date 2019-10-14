using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// contains gangs' aggressiveness options and utilities
    /// </summary>
    [Serializable]
    public class RelationOptions : IModOptionGroup
    {

        public RelationOptions() { }

        public void SetOptionsToDefault()
        {
            GangMemberAggressiveness = AggressivenessMode.veryAggressive;
        }

        public enum AggressivenessMode
        {
            veryAggressive = 0,
            aggressive = 1,
            defensive = 2
        }
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

        public static Relationship AggrModeToRelationship(AggressivenessMode mode)
        {
            switch (mode)
            {
                case RelationOptions.AggressivenessMode.veryAggressive:
                    return Relationship.Hate;
                case RelationOptions.AggressivenessMode.aggressive:
                    return Relationship.Dislike;
                case RelationOptions.AggressivenessMode.defensive:
                    return Relationship.Neutral;
                default:
                    return Relationship.Hate;
            }
        }

    }
}

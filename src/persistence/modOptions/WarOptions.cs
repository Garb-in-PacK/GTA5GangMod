using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    [Serializable]
    public class WarOptions : IModOptionGroup
    {

        public WarOptions() { }

        public void SetOptionsToDefault()
        {
            EmptyZoneDuringWar = true;
            FreezeWantedLevelDuringWars = true;
            WarAgainstPlayerEnabled = true;
            MaxDistToWarBlipBeforePlayerLeavesWar = 300;
            TicksBeforeWarEndWithPlayerAway = 30000;
            PostWarBackupsAmount = 5;
            BaseNumKillsBeforeWarVictory = 25;
            ExtraKillsPerTurfValue = 15;
            KillsBetweenEnemySpawnReplacement = 25;
            TicksBetweenEnemySpawnReplacement = 3600;

            NumSpawnsReservedForCarsDuringWars = 1;
        }

        public int PostWarBackupsAmount { get; set; } = 5;
        public bool WarAgainstPlayerEnabled { get; set; }

        public int BaseNumKillsBeforeWarVictory { get; set; } = 25;
        public int ExtraKillsPerTurfValue { get; set; } = 15;
        public int KillsBetweenEnemySpawnReplacement { get; set; } = 25;
        public int TicksBetweenEnemySpawnReplacement { get; set; } = 3600;

        public bool EmptyZoneDuringWar { get; set; } = true;
        public bool FreezeWantedLevelDuringWars { get; set; } = true;

        public int MaxDistToWarBlipBeforePlayerLeavesWar { get; set; } = 300;
        public int TicksBeforeWarEndWithPlayerAway { get; set; } = 30000;

        public int NumSpawnsReservedForCarsDuringWars { get; set; } = 1;

    }
}

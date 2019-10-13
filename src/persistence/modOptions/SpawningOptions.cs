using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    [Serializable]
    public class SpawningOptions : IModOptionGroup
    {

        public SpawningOptions() { }

        public void SetOptionsToDefault()
        {
            MembersSpawnWithMeleeOnly = false;
            AmbientSpawningEnabled = true;

            SpawnedMembersBeforeAmbientGenStops = 20;
            MsBaseIntervalBetweenAmbientSpawns = 15000;
            SpawnedMemberLimit = 30; //max number of living gang members at any time

            MinDistanceMemberSpawnFromPlayer = 50;
            MaxDistanceMemberSpawnFromPlayer = 130;
            MinDistanceCarSpawnFromPlayer = 80;
            MaxDistanceCarSpawnFromPlayer = 190;

            ShowGangMemberBlips = true;

            ForceSpawnCars = false;

        }

        public bool AmbientSpawningEnabled { get; set; } = true;
        public int SpawnedMembersBeforeAmbientGenStops { get; set; } = 20;
        public int MsBaseIntervalBetweenAmbientSpawns { get; set; } = 15000;

        public int MinDistanceMemberSpawnFromPlayer { get; set; } = 50;
        public int MaxDistanceMemberSpawnFromPlayer { get; set; } = 130;
        public int MinDistanceCarSpawnFromPlayer { get; set; } = 80;
        public int MaxDistanceCarSpawnFromPlayer { get; set; } = 190;

        public bool MembersSpawnWithMeleeOnly { get; set; }

        /// <summary>
        /// member blips are only added (or not) when spawning
        /// </summary>
        public bool ShowGangMemberBlips { get; set; } = true;


        /// <summary>
        /// max number of living members coexisting at any time
        /// </summary>
        public int SpawnedMemberLimit { get; set; } = 30;

        public bool ForceSpawnCars { get; set; } = false;

        /// <summary>
        /// returns a random distance between the minimum and maximum distances that a member can spawn from the player
        /// </summary>
        /// <returns></returns>
        public int GetAcceptableMemberSpawnDistance(int paddingFromLimits = 0)
        {
            if (MaxDistanceMemberSpawnFromPlayer <= MinDistanceMemberSpawnFromPlayer)
            {
                MaxDistanceMemberSpawnFromPlayer = MinDistanceMemberSpawnFromPlayer + 2;
            }
            return RandoMath.CachedRandom.Next(MinDistanceMemberSpawnFromPlayer,
                RandoMath.Max(MinDistanceCarSpawnFromPlayer + paddingFromLimits, MaxDistanceMemberSpawnFromPlayer - paddingFromLimits));
        }

        /// <summary>
        /// returns a random distance between the minimum and maximum distances that a car can spawn from the player
        /// </summary>
        /// <returns></returns>
        public int GetAcceptableCarSpawnDistance()
        {
            if (MaxDistanceCarSpawnFromPlayer <= MinDistanceCarSpawnFromPlayer)
            {
                MaxDistanceCarSpawnFromPlayer = MinDistanceCarSpawnFromPlayer + 2;
            }
            return RandoMath.CachedRandom.Next(MinDistanceCarSpawnFromPlayer, MaxDistanceCarSpawnFromPlayer);
        }
    }
}

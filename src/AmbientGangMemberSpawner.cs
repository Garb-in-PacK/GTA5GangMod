using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Math;
using GTA.Native;

/// <summary>
/// this script takes care of spawning roaming gang members and vehicles in gang zones.
/// It also regulates police influence according to the settings in ModOptions
/// </summary>
namespace GTA.GangAndTurfMod
{
    public class AmbientGangMemberSpawner : Script
    {
        public SpawningOptions SpawningOptions { get; set; }
        public ZoneOptions ZoneOptions { get; set; }
        public int PostWarBackupsRemaining { get; set; } = 0;
        public bool Enabled { get; set; } = true;
        public static AmbientGangMemberSpawner Instance { get; private set; }

        private void OnTick(object sender, EventArgs e)
        {
            Wait(3000 + RandoMath.CachedRandom.Next(1000));
            Logger.Log("ambient spawner tick: begin", 5);

            TurfZone curTurfZone = ZoneManager.instance.GetCurrentTurfZone();
            if (curTurfZone != null)
            {
                // also reduce police influence
                if (Enabled)
                {
                    Game.WantedMultiplier = (1.0f / (curTurfZone.value + 1)) + ZoneOptions.WantedFactorInMaxedGangTurf;
                    Game.MaxWantedLevel = RandoMath.Max(CalculateMaxWantedLevelInTurf(curTurfZone.value), ZoneOptions.MaxWantedLevelInMaxedGangTurf);
                }

                if (Game.Player.WantedLevel > Game.MaxWantedLevel) Game.Player.WantedLevel--;

                if (PostWarBackupsRemaining > 0 && GangWarManager.instance.playerNearWarzone)
                {
                    Vector3 playerPos = MindControl.CurrentPlayerCharacter.Position,
                        safePlayerPos = MindControl.SafePositionNearPlayer;
                    if (SpawnManager.instance.SpawnParachutingMember(GangManager.PlayerGang,
                       playerPos + Vector3.WorldUp * 50, safePlayerPos) == null)
                    {
                        SpawnManager.instance.SpawnGangVehicle(GangManager.PlayerGang,
                        SpawnManager.instance.FindGoodSpawnPointForCar(safePlayerPos), safePlayerPos);
                    }
                    PostWarBackupsRemaining--;
                }

                //if spawning is enabled, lets try to spawn the current zone's corresponding gang members!
                if (SpawningOptions.AmbientSpawningEnabled && Enabled)
                {

                    Gang curGang = GangManager.GetGangByName(curTurfZone.ownerGangName);
                    if (GangWarManager.instance.isOccurring && GangWarManager.instance.enemyGang == curGang) return; //we want enemies of this gang to spawn only when close to the war

                    if (curTurfZone.ownerGangName != "none" && curGang != null) //only spawn if there really is a gang in control here
                    {
                        if (SpawnManager.instance.livingMembersCount < SpawningOptions.SpawnedMembersBeforeAmbientGenStops)
                        {
                            Vehicle playerVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                            if ((playerVehicle != null && playerVehicle.Speed < 30) || playerVehicle == null)
                            {
                                SpawnAmbientMember(curGang);
                            }
                            if (RandoMath.CachedRandom.Next(0, 5) < 3)
                            {
                                Wait(100 + RandoMath.CachedRandom.Next(300));
                                SpawnAmbientVehicle(curGang);
                            }

                            Wait(1 + RandoMath.CachedRandom.Next(RandoMath.Max(1, SpawningOptions.MsBaseIntervalBetweenAmbientSpawns / 2), SpawningOptions.MsBaseIntervalBetweenAmbientSpawns) / (curTurfZone.value + 1));
                        }
                    }
                    else
                    {
                        Game.WantedMultiplier = 1;
                        Game.MaxWantedLevel = 6;
                    }

                }
            }

            Logger.Log("ambient spawner tick: end", 5);
        }

        public SpawnedGangMember SpawnAmbientMember(Gang curGang)
        {
            Vector3 spawnPos = SpawnManager.instance.FindGoodSpawnPointForMember
                (MindControl.CurrentPlayerCharacter.Position);
            SpawnedGangMember newMember = SpawnManager.instance.SpawnGangMember(curGang, spawnPos);
            return newMember;
        }

        public void SpawnAmbientVehicle(Gang curGang)
        {
            Vector3 vehSpawnPoint = SpawnManager.instance.FindGoodSpawnPointForCar
                (MindControl.CurrentPlayerCharacter.Position);
            SpawnedDrivingGangMember spawnedVehicleAI = SpawnManager.instance.SpawnGangVehicle(curGang,
                                vehSpawnPoint, Vector3.Zero, true);
            if (spawnedVehicleAI != null)
            {
                SpawnManager.instance.TryPlaceVehicleOnStreet(spawnedVehicleAI.vehicleIAmDriving, vehSpawnPoint);
                Ped driver = spawnedVehicleAI.watchedPed;
                if (driver != null) //if, for some reason, we don't have a driver, do nothing
                {
                    spawnedVehicleAI.DoCruise();
                }
            }
        }

        public AmbientGangMemberSpawner()
        {
            this.Tick += OnTick;
            Instance = this;
        }

        public int CalculateMaxWantedLevelInTurf(int curTurfValue)
        {
            int maxTurfValue = ZoneOptions.MaxTurfLevel;
            float turfProgressPercent = (float)curTurfValue / maxTurfValue;
            return 6 - (int)(6 * turfProgressPercent);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using GTA;
using System.Windows.Forms;
using GTA.Native;
using System.Drawing;
using GTA.Math;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// this script controls most things related to spawning members and vehicles,
    /// and handling/getting info from already spawned ones
    /// </summary>
    public class SpawnManager
    {
        public SpawningOptions SpawningOptions { get; private set; }

        public WeaponOptions WeaponOptions { get; private set; }

        public MemberAIOptions MemberAIOptions { get; private set; }
        public List<SpawnedGangMember> MemberAIPool { get; private set; }
        public List<SpawnedDrivingGangMember> DriverAIPool { get; private set; }

        /// <summary>
        /// the number of currently alive members
        /// </summary>
        public int LivingMembersCount { get; set; } = 0;

        public delegate void SuccessfulMemberSpawnDelegate();

        #region setup

        public SpawnManager(SpawningOptions spawningOptions, WeaponOptions weaponOptions, MemberAIOptions memberAIOptions)
        {
            SpawningOptions = spawningOptions;
            WeaponOptions = weaponOptions;
            MemberAIOptions = memberAIOptions;
            MemberAIPool = new List<SpawnedGangMember>();
            DriverAIPool = new List<SpawnedDrivingGangMember>();
        }


        #endregion

        /// <summary>
        /// marks all living members as no longer needed and removes their blips, 
        /// as if everyone had died or were too far from the player
        /// </summary>
        public void RemoveAllMembers()
        {
            for (int i = 0; i < MemberAIPool.Count; i++)
            {
                MemberAIPool[i].Die();
            }

            for (int i = 0; i < DriverAIPool.Count; i++)
            {
                DriverAIPool[i].ClearAllRefs();
            }

        }

        #region gang general control stuff


        /// <summary>
        /// when the player asks to reset mod options, we must reset these update intervals because they
        /// may have changed
        /// </summary>
        public void ResetSpawnedsUpdateInterval()
        {
            for (int i = 0; i < MemberAIPool.Count; i++)
            {
                MemberAIPool[i].ResetUpdateInterval();
            }
        }

        #endregion

        #region Eddlm's spawnpos generator

        //from: https://gtaforums.com/topic/843561-pathfind-node-types
        //with some personal preference edits

        public enum Nodetype
        {
            Road,
            AnyRoad,
            Offroad,
            Water
        }

        /// <summary>
        /// gets the closest vehicle node of the desired type, 
        /// optionally returning the next pos on sidewalk instead of the node pos.
        /// All credit goes to Eddlm!
        /// </summary>
        /// <param name="desiredPos"></param>
        /// <param name="roadtype"></param>
        /// <param name="sidewalk"></param>
        /// <returns></returns>
        public static Vector3 GenerateSpawnPos(Vector3 desiredPos, Nodetype roadtype, bool sidewalk)
        {
            Vector3 finalpos;

            using (OutputArgument outArgA = new OutputArgument())
            {
                int nodeNumber = 1;
                int roadTypeAsInt = (int)roadtype;

                int nodeID = Function.Call<int>(
                    Hash.GET_NTH_CLOSEST_VEHICLE_NODE_ID, desiredPos.X, desiredPos.Y, desiredPos.Z,
                    nodeNumber, roadTypeAsInt, 300f, 300f);

                Function.Call(Hash.GET_VEHICLE_NODE_POSITION, nodeID, outArgA);
                finalpos = outArgA.GetResult<Vector3>();
            }

            if (sidewalk) finalpos = World.GetNextPositionOnSidewalk(finalpos);

            return finalpos;
        }

        #endregion

        #region getters

        public List<Ped> GetSpawnedPedsOfGang(Gang desiredGang)
        {
            if (desiredGang == null) throw new ArgumentNullException(nameof(desiredGang));

            List<Ped> returnedList = new List<Ped>();

            for (int i = 0; i < MemberAIPool.Count; i++)
            {
                if (MemberAIPool[i].watchedPed != null)
                {
                    if (MemberAIPool[i].watchedPed.RelationshipGroup == desiredGang.relationGroup)
                    {
                        returnedList.Add(MemberAIPool[i].watchedPed);
                    }
                }
            }

            return returnedList;
        }

        /// <summary>
        /// gets all currently active spawned members of the desired gang.
        /// The onlyGetIfInsideVehicle will only add members who are inside vehicles to the returned list
        /// </summary>
        /// <param name="desiredGang"></param>
        /// <param name="onlyGetIfInsideVehicle"></param>
        /// <returns></returns>
        public List<SpawnedGangMember> GetSpawnedMembersOfGang(Gang desiredGang, bool onlyGetIfInsideVehicle = false)
        {
            List<SpawnedGangMember> returnedList = new List<SpawnedGangMember>();

            for (int i = 0; i < MemberAIPool.Count; i++)
            {
                if (MemberAIPool[i] != null)
                {
                    if (MemberAIPool[i].myGang == desiredGang)
                    {
                        if (onlyGetIfInsideVehicle)
                        {
                            if (Function.Call<bool>(Hash.IS_PED_IN_ANY_VEHICLE, MemberAIPool[i].watchedPed, false))
                            {
                                returnedList.Add(MemberAIPool[i]);
                            }
                        }
                        else
                        {
                            returnedList.Add(MemberAIPool[i]);
                        }

                    }
                }

            }

            return returnedList;
        }

        /// <summary>
        /// gets all (alive) driver members of the desired gang.
        /// </summary>
        /// <param name="desiredGang"></param>
        /// <returns></returns>
        public List<SpawnedDrivingGangMember> GetSpawnedDriversOfGang(Gang desiredGang)
        {
            if (desiredGang == null) throw new ArgumentNullException(nameof(desiredGang));

            List<SpawnedDrivingGangMember> returnedList = new List<SpawnedDrivingGangMember>();

            for (int i = 0; i < DriverAIPool.Count; i++)
            {
                if (DriverAIPool[i].watchedPed != null)
                {
                    if (DriverAIPool[i].watchedPed.RelationshipGroup == desiredGang.relationGroup)
                    {
                        returnedList.Add(DriverAIPool[i]);
                    }
                }
            }

            return returnedList;
        }

        /// <summary>
        /// gets the SpawnedGangMember object that is handling the target ped's AI, optionally returning null instead if the ped is dead
        /// </summary>
        /// <param name="targetPed"></param>
        /// <param name="onlyGetIfIsAlive"></param>
        /// <returns></returns>
        public SpawnedGangMember GetTargetMemberAI(Ped targetPed, bool onlyGetIfIsAlive = false)
        {
            if (targetPed == null) return null;
            if (!targetPed.IsAlive && onlyGetIfIsAlive) return null;
            for (int i = 0; i < MemberAIPool.Count; i++)
            {
                if (MemberAIPool[i].watchedPed == targetPed)
                {
                    return MemberAIPool[i];
                }
            }

            return null;
        }

        public SpawnedDrivingGangMember GetTargetMemberDrivingAI(Ped targetMember)
        {
            if (targetMember == null) return null;
            for (int i = 0; i < DriverAIPool.Count; i++)
            {
                if (DriverAIPool[i].watchedPed == targetMember)
                {
                    return DriverAIPool[i];
                }
            }

            return null;
        }

        /// <summary>
        /// returns gang members who are not from the gang provided
        /// </summary>
        /// <param name="myGang"></param>
        /// <returns></returns>
        public List<Ped> GetMembersNotFromMyGang(Gang myGang, bool includePlayer = true)
        {
            if (myGang == null) throw new ArgumentNullException(nameof(myGang));

            List<Ped> returnedList = new List<Ped>();

            for (int i = 0; i < MemberAIPool.Count; i++)
            {
                if (MemberAIPool[i].watchedPed != null)
                {
                    if (MemberAIPool[i] != MindControl.CurrentlyControlledMember &&
                        MemberAIPool[i].watchedPed.RelationshipGroup != myGang.relationGroup)
                    {
                        returnedList.Add(MemberAIPool[i].watchedPed);
                    }
                }
            }

            if (includePlayer && !myGang.isPlayerOwned)
            {
                returnedList.Add(MindControl.CurrentPlayerCharacter);
            }

            return returnedList;
        }

        public static List<Ped> GetHostilePedsAround(Vector3 targetPos, Ped referencePed, float radius)
        {
            if (referencePed == null) throw new ArgumentNullException(nameof(referencePed));

            Logger.Log("GetHostilePedsAround: start", 3);
            Ped[] detectedPeds = World.GetNearbyPeds(targetPos, radius);

            List<Ped> hostilePeds = new List<Ped>();

            RelationshipGroup referencePedRelationGroup = referencePed.RelationshipGroup;

            foreach (Ped ped in detectedPeds)
            {
                if (referencePed.RelationshipGroup != ped.RelationshipGroup && ped.IsAlive)
                {
                    int pedRelation = (int)referencePedRelationGroup.GetRelationshipBetweenGroups(ped.RelationshipGroup);
                    //if the relationship between them is hate or they were neutral and our reference ped has been hit by this ped...
                    if (pedRelation == 5 ||
                        (pedRelation >= 3 && referencePed.HasBeenDamagedBy(ped)))
                    {
                        hostilePeds.Add(ped);
                    }
                }

            }
            Logger.Log("GetHostilePedsAround: end", 3);
            return hostilePeds;
        }

        #endregion

        #region spawner methods

        /// <summary>
        /// a good spawn point is one that is not too close and not too far from the player or referencePosition (according to the Mod Options)
        /// </summary>
        /// <returns></returns>
        public Vector3 FindGoodSpawnPointForMember(Vector3? referencePosition = null)
        {
            Vector3 referencePos = referencePosition ?? MindControl.SafePositionNearPlayer;

            return World.GetNextPositionOnSidewalk(referencePos + RandoMath.RandomDirection(true) *
                          SpawningOptions.GetAcceptableMemberSpawnDistance(10));
        }

        /// <summary>
        /// finds a spawn point that is close to the specified reference point and, optionally, far from the specified repulsor
        /// </summary>
        /// <returns></returns>
        public static Vector3 FindCustomSpawnPoint(Vector3 referencePoint, float averageDistanceFromReference, float minDistanceFromReference, int maxAttempts = 10, Vector3? repulsor = null, float minDistanceFromRepulsor = 0)
        {

            int attempts = 0;

            Vector3 chosenPos = World.GetNextPositionOnSidewalk(referencePoint + RandoMath.RandomDirection(true) *
                          averageDistanceFromReference);

            float distFromRef = World.GetDistance(referencePoint, chosenPos);
            while (((distFromRef > averageDistanceFromReference * 3 || (distFromRef < minDistanceFromReference)) ||
                (repulsor != null && World.GetDistance(repulsor.Value, chosenPos) < minDistanceFromRepulsor)) &&
                attempts <= maxAttempts)
            {
                chosenPos = World.GetNextPositionOnSidewalk(referencePoint + RandoMath.RandomDirection(true) *
                    averageDistanceFromReference);
                distFromRef = World.GetDistance(referencePoint, chosenPos);
                attempts++;
            }

            return chosenPos;
        }

        /// <summary>
        /// finds a spawn point that is close to the specified reference point and, optionally, far from the specified repulsor.
        /// this version uses "GetNextPositionOnStreet"
        /// </summary>
        /// <returns></returns>
        public static Vector3 FindCustomSpawnPointInStreet(Vector3 referencePoint, float averageDistanceFromReference, float minDistanceFromReference, int maxAttempts = 10, Vector3? repulsor = null, float minDistanceFromRepulsor = 0)
        {
            int attempts = 0;

            Vector3 getNextPosTarget = referencePoint + RandoMath.RandomDirection(true) *
                          averageDistanceFromReference;

            Vector3 chosenPos = WorldLocChecker.PlayerIsAwayFromRoads ? World.GetNextPositionOnSidewalk(getNextPosTarget) :
                World.GetNextPositionOnStreet(getNextPosTarget);

            float distFromRef = World.GetDistance(referencePoint, chosenPos);
            while (((distFromRef > averageDistanceFromReference * 5 || (distFromRef < minDistanceFromReference)) ||
                (repulsor != null && World.GetDistance(repulsor.Value, chosenPos) < minDistanceFromRepulsor)) &&
                attempts <= maxAttempts)
            {

                getNextPosTarget = referencePoint + RandoMath.RandomDirection(true) *
                          averageDistanceFromReference;
                chosenPos = WorldLocChecker.PlayerIsAwayFromRoads ? World.GetNextPositionOnSidewalk(getNextPosTarget) :
                    World.GetNextPositionOnStreet(getNextPosTarget);

                distFromRef = World.GetDistance(referencePoint, chosenPos);
                attempts++;
            }

            return chosenPos;
        }

        /// <summary>
        /// finds a nice spot neither too close or far from the reference pos, or the player safe pos if not provided.
        /// use the safe pos as parameter if you've already got it before calling this func!
        /// </summary>
        /// <param name="referencePos"></param>
        /// <returns></returns>
        public Vector3 FindGoodSpawnPointForCar(Vector3? referencePos = null)
        {
            Vector3 refPos = referencePos ?? MindControl.SafePositionNearPlayer;

            Vector3 getNextPosTarget = refPos + RandoMath.RandomDirection(true) *
                          SpawningOptions.GetAcceptableCarSpawnDistance();

            return WorldLocChecker.PlayerIsAwayFromRoads ? World.GetNextPositionOnSidewalk(getNextPosTarget) :
                    World.GetNextPositionOnStreet(getNextPosTarget);
        }

        /// <summary>
        /// makes one attempt to place the target vehicle on a street.
        /// if it fails, the vehicle is returned to its original position
        /// </summary>
        /// <param name="targetVehicle"></param>
        /// <param name="originalPos"></param>
        public void TryPlaceVehicleOnStreet(Vehicle targetVehicle, Vector3 originalPos)
        {
            if (targetVehicle == null) throw new ArgumentNullException(nameof(targetVehicle));

            targetVehicle.PlaceOnNextStreet();
            float distFromPlayer = targetVehicle.Position.DistanceTo2D(MindControl.CurrentPlayerCharacter.Position);

            if (distFromPlayer > SpawningOptions.MaxDistanceCarSpawnFromPlayer ||
                distFromPlayer < SpawningOptions.MinDistanceCarSpawnFromPlayer)
            {
                targetVehicle.Position = originalPos;
            }
        }

        public SpawnedGangMember SpawnGangMember(Gang ownerGang, Vector3 spawnPos, SuccessfulMemberSpawnDelegate onSuccessfulMemberSpawn = null)
        {
            if (ownerGang == null) throw new ArgumentNullException(nameof(ownerGang));

            if (LivingMembersCount >= SpawningOptions.SpawnedMemberLimit || spawnPos == Vector3.Zero || ownerGang.memberVariations == null)
            {
                //don't start spawning, we're on the limit already or we failed to find a good spawn point or we haven't started up our data properly yet
                return null;
            }
            if (ownerGang.memberVariations.Count > 0)
            {
                Logger.Log("spawn member: begin", 4);
                PotentialGangMember chosenMember =
                    RandoMath.GetRandomElementFromList(ownerGang.memberVariations);
                Ped newPed = World.CreatePed(chosenMember.modelHash, spawnPos);
                if (newPed != null)
                {
                    chosenMember.SetPedAppearance(newPed);

                    newPed.Accuracy = ownerGang.memberAccuracyLevel;
                    newPed.MaxHealth = ownerGang.memberHealth;
                    newPed.Health = ownerGang.memberHealth;
                    newPed.Armor = ownerGang.memberArmor;

                    newPed.Money = RandoMath.CachedRandom.Next(60);

                    //set the blip, if enabled
                    if (SpawningOptions.ShowGangMemberBlips)
                    {
                        Blip pedBlip = newPed.AddBlip();
                        pedBlip.IsShortRange = true;
                        pedBlip.Scale = 0.65f;
                        Function.Call(Hash.SET_BLIP_COLOUR, pedBlip, ownerGang.blipColor);

                        //set blip name - got to use native, the c# blip.name returns error ingame
                        //Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                        //Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, ownerGang.name + " member");
                        //Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, newPed.CurrentBlip);
                        pedBlip.Name = ownerGang.name + " member";
                    }


                    bool hasDriveByGun = false; //used for when the member has to decide between staying inside a vehicle or not

                    //give a weapon
                    if (ownerGang.gangWeaponHashes.Count > 0)
                    {
                        //get one weap from each type... if possible AND we're not forcing melee only
                        newPed.Weapons.Give(ownerGang.GetListedGunFromOwnedGuns(WeaponOptions.MeleeWeapons), 1000, false, true);
                        if (!SpawningOptions.MembersSpawnWithMeleeOnly)
                        {
                            WeaponHash driveByGun = ownerGang.GetListedGunFromOwnedGuns(WeaponOptions.DriveByWeapons);
                            hasDriveByGun = driveByGun != WeaponHash.Unarmed;
                            newPed.Weapons.Give(driveByGun, 1000, false, true);
                            newPed.Weapons.Give(ownerGang.GetListedGunFromOwnedGuns(WeaponOptions.PrimaryWeapons), 1000, false, true);

                            //and one extra
                            newPed.Weapons.Give(RandoMath.GetRandomElementFromList(ownerGang.gangWeaponHashes), 1000, false, true);
                        }
                    }

                    //set the relationship group
                    newPed.RelationshipGroup = ownerGang.relationGroup;

                    newPed.NeverLeavesGroup = true;

                    //newPed.BlockPermanentEvents = true;
                    //newPed.StaysInVehicleWhenJacked = true;

                    Function.Call(Hash.SET_CAN_ATTACK_FRIENDLY, newPed, false, false); //cannot attack friendlies
                    Function.Call(Hash.SET_PED_COMBAT_ABILITY, newPed, 100); //average combat ability
                                                                             //Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, newPed, 0, 0); //clears the flee attributes?

                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, newPed, 46, true); // alwaysFight = true and canFightArmedWhenNotArmed. which one is which is unknown
                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, newPed, 5, true);
                    Function.Call(Hash.SET_PED_COMBAT_RANGE, newPed, 2); //combatRange = far

                    newPed.CanSwitchWeapons = true;
                    newPed.CanWrithe = false; //no early dying

                    //enlist this new gang member in the spawned list!
                    SpawnedGangMember newMemberAI = null;

                    bool couldEnlistWithoutAdding = false;
                    for (int i = 0; i < MemberAIPool.Count; i++)
                    {
                        if (MemberAIPool[i].watchedPed == null)
                        {
                            MemberAIPool[i].AttachData(newPed, ownerGang, hasDriveByGun);
                            newMemberAI = MemberAIPool[i];
                            couldEnlistWithoutAdding = true;
                            break;
                        }
                    }
                    if (!couldEnlistWithoutAdding)
                    {
                        if (MemberAIPool.Count < SpawningOptions.SpawnedMemberLimit)
                        {
                            newMemberAI = new SpawnedGangMember(newPed, ownerGang, hasDriveByGun);
                            MemberAIPool.Add(newMemberAI);
                        }
                    }

                    LivingMembersCount++;
                    onSuccessfulMemberSpawn?.Invoke();
                    Logger.Log("spawn member: end (success). livingMembers list size = " + MemberAIPool.Count, 4);
                    return newMemberAI;
                }
                else
                {
                    Logger.Log("spawn member: end with fail (world createped returned null)", 4);
                    return null;
                }
            }
            return null;
        }

        public SpawnedDrivingGangMember SpawnGangVehicle(Gang ownerGang, Vector3 spawnPos, Vector3 destPos, bool playerIsDest = false, bool isDeliveringCar = false, SuccessfulMemberSpawnDelegate onSuccessfulPassengerSpawn = null)
        {
            if (ownerGang == null) throw new ArgumentNullException(nameof(ownerGang));

            if (LivingMembersCount >= SpawningOptions.SpawnedMemberLimit || spawnPos == Vector3.Zero || ownerGang.carVariations == null)
            {
                //don't start spawning, we're on the limit already or we failed to find a good spawn point or we haven't started up our data properly yet
                return null;
            }

            if (ownerGang.carVariations.Count > 0)
            {
                Logger.Log("spawn car: start", 4);
                Vehicle newVehicle = World.CreateVehicle(RandoMath.GetRandomElementFromList(ownerGang.carVariations).modelHash, spawnPos);
                if (newVehicle != null)
                {
                    newVehicle.Mods.PrimaryColor = ownerGang.vehicleColor;


                    SpawnedGangMember driver = SpawnGangMember(ownerGang, spawnPos, onSuccessfulMemberSpawn: onSuccessfulPassengerSpawn);

                    if (driver != null)
                    {
                        driver.curStatus = SpawnedGangMember.MemberStatus.inVehicle;
                        driver.watchedPed.SetIntoVehicle(newVehicle, VehicleSeat.Driver);

                        int passengerCount = newVehicle.PassengerCapacity;
                        if (destPos == Vector3.Zero && passengerCount > 4) passengerCount = 4; //limit ambient passengers in order to have less impact in ambient spawning

                        for (int i = 0; i < passengerCount; i++)
                        {
                            SpawnedGangMember passenger = SpawnGangMember(ownerGang, spawnPos, onSuccessfulMemberSpawn: onSuccessfulPassengerSpawn);
                            if (passenger != null)
                            {
                                passenger.curStatus = SpawnedGangMember.MemberStatus.inVehicle;
                                passenger.watchedPed.SetIntoVehicle(newVehicle, VehicleSeat.Any);
                            }
                        }

                        SpawnedDrivingGangMember driverAI = EnlistDrivingMember(driver.watchedPed, newVehicle, destPos, ownerGang.isPlayerOwned, playerIsDest, isDeliveringCar);

                        if (SpawningOptions.ShowGangMemberBlips)
                        {
                            Blip vehBlip = newVehicle.AddBlip();
                            vehBlip.IsShortRange = true;

                            Function.Call(Hash.SET_BLIP_COLOUR, vehBlip, ownerGang.blipColor);
                        }

                        Logger.Log("spawn car: end (success)", 4);
                        return driverAI;
                    }
                    else
                    {
                        newVehicle.Delete();
                        Logger.Log("spawn car: end (fail: couldnt spawn driver)", 4);
                        return null;
                    }
                }

                Logger.Log("spawn car: end (fail: car creation failed)", 4);
            }

            return null;
        }

        public Ped SpawnParachutingMember(Gang ownerGang, Vector3 spawnPos, Vector3 destPos)
        {
            SpawnedGangMember spawnedPara = SpawnGangMember(ownerGang, spawnPos);
            if (spawnedPara != null)
            {
                spawnedPara.watchedPed.BlockPermanentEvents = true;
                spawnedPara.watchedPed.Task.ParachuteTo(destPos);
                return spawnedPara.watchedPed;
            }

            return null;
        }

        private SpawnedDrivingGangMember EnlistDrivingMember(Ped pedToEnlist, Vehicle vehicleDriven, Vector3 destPos, bool friendlyToPlayer, bool playerIsDest = false, bool deliveringCar = false)
        {
            SpawnedDrivingGangMember newDriverAI = null;

            bool couldEnlistWithoutAdding = false;
            for (int i = 0; i < DriverAIPool.Count; i++)
            {
                if (DriverAIPool[i].watchedPed == null)
                {
                    newDriverAI = DriverAIPool[i];
                    DriverAIPool[i].AttachData(pedToEnlist, vehicleDriven, destPos, friendlyToPlayer, playerIsDest, deliveringCar);
                    couldEnlistWithoutAdding = true;
                    break;
                }
            }
            if (!couldEnlistWithoutAdding)
            {
                newDriverAI = new SpawnedDrivingGangMember(pedToEnlist, vehicleDriven, SpawningOptions, MemberAIOptions, destPos, friendlyToPlayer, playerIsDest, deliveringCar);
                DriverAIPool.Add(newDriverAI);
            }

            return newDriverAI;
        }
        #endregion
    }

}

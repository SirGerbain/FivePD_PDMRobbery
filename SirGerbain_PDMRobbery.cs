using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using FivePD.API.Utils;
using System.Runtime.ConstrainedExecution;

namespace SirGerbain_PDMRobbery
{
    [CalloutProperties("PDM Robbery", "sirGerbain", "1.0.0")]
    public class SirGerbain_PDMRobbery : FivePD.API.Callout
    {

        Ped robber, swat, pilot;
        List<VehicleHash> carList = new List<VehicleHash>();
        List<PedHash> robberList = new List<PedHash>(); 
        Vector3 robberyLocation;
        Random random = new Random();
        Vehicle robbedVehicle, helicopter;
        private float tickTimer = 0f;
        private float tickInterval = 1f;
        bool air1Dispatched = false;
        bool air1HasSwat = false;

        public SirGerbain_PDMRobbery()
        {

            robberyLocation = new Vector3(-29.21f,-1086.15f,26.12f); //339.7

            robberList.Add(PedHash.SalvaGoon01GMY);
            robberList.Add(PedHash.SalvaGoon02GMY);
            robberList.Add(PedHash.SalvaBoss01GMY);

            carList.Add(VehicleHash.Adder);

            InitInfo(robberyLocation);
            ShortName = "PDM Robbery in progress";
            CalloutDescription = "PDM Robbery";
            ResponseCode = 3;
            StartDistance = 200f;

        }

        public async override Task OnAccept()
        {
            InitBlip();
            UpdateData();

            PlayerData playerData = Utilities.GetPlayerData();
            string displayName = playerData.DisplayName;
            Notify("~r~[PDM 911] ~y~Officer ~b~" + displayName + ",~y~ someone is tampering with a vehicle at PDM!");

        }

        public async Task OnTick()
        {
            tickTimer += Game.LastFrameTime;
            if (tickTimer >= tickInterval)
            {

                robber.AlwaysKeepTask = true;
                robber.BlockPermanentEvents = true;
                API.SetDriveTaskMaxCruiseSpeed(robber.GetHashCode(), 250f);
                API.SetDriveTaskDrivingStyle(robber.GetHashCode(), 524852);
                robber.Task.FleeFrom(Game.PlayerPed);

                float distance = Game.PlayerPed.Position.DistanceTo(robber.Position);
                if (distance < 180f && random.Next(0, 100) < 10)
                {
                    robber.Task.VehicleShootAtPed(Game.PlayerPed);
                    await BaseScript.Delay(random.Next(1000, 5000));
                    robber.Task.FleeFrom(Game.PlayerPed);
                }

                if (!air1Dispatched && random.Next(0, 100) < 50)
                {
                    air1Dispatched = true;
                    Notify("We called in Air-1 for you.");

                    helicopter = await SpawnVehicle(VehicleHash.Polmav, new Vector3(robbedVehicle.Position.X, robbedVehicle.Position.Y, 200));
                    pilot = await SpawnPed(PedHash.Pilot02SMM, new Vector3(robbedVehicle.Position.X, robbedVehicle.Position.Y, 200));
                    pilot.SetIntoVehicle(helicopter, VehicleSeat.Driver);
                    pilot.AlwaysKeepTask = true;
                    pilot.BlockPermanentEvents = true;
                    pilot.Task.ChaseWithHelicopter(robbedVehicle, new Vector3(35f, 35f, 35f));

                    if (random.Next(0, 100) < 25)
                    {
                        Notify("Air-1 has swat on board.");
                        swat = await SpawnPed(PedHash.Swat01SMY, new Vector3(robbedVehicle.Position.X, robbedVehicle.Position.Y, 200));
                        swat.SetIntoVehicle(helicopter, VehicleSeat.LeftRear);
                        swat.AlwaysKeepTask = true;
                        swat.BlockPermanentEvents = true;
                        swat.Weapons.Give(WeaponHash.AssaultSMG, 250, true, true);
                        air1HasSwat = true;
                    }

                }

                if (air1HasSwat && air1Dispatched)
                {
                    swat.Task.VehicleShootAtPed(robber);
                    await BaseScript.Delay(random.Next(1000, 5000));
                    swat.Task.ClearAll();
                }

                if (air1Dispatched && (helicopter.Position.DistanceTo(robber.Position)<300f))
                {
                    Vector3 pedPosition = robber.Position;
                    float pedHeading = robber.Heading;
                    string dispatchPosition = "";
                    string streetName = World.GetStreetName(pedPosition);
                    if (pedHeading >= 45 && pedHeading < 135)
                    {
                        dispatchPosition += "West on " + streetName;
                    }
                    else if (pedHeading >= 135 && pedHeading < 225)
                    {
                        dispatchPosition += "South on " + streetName;
                    }
                    else if (pedHeading >= 225 && pedHeading < 315)
                    {
                        dispatchPosition += "East on " + streetName;
                    }
                    else
                    {
                        dispatchPosition += "North on " + streetName;
                    }

                    DrawSubtitle("[~w~AIR-~b~1~w~] ~y~Suspect is going " + dispatchPosition, 3500);
                }


                tickTimer = 0f;
            }
        }

        public async override void OnStart(Ped closest)
        {
            base.OnStart(closest);

            await setupCallout();

            Tick += OnTick;
        }

        public async Task setupCallout()
        {
            robbedVehicle = await SpawnVehicle(carList[random.Next(carList.Count)], robberyLocation);
            robbedVehicle.Heading = 300.91f;
            robbedVehicle.LockStatus = VehicleLockStatus.Unlocked;
            robbedVehicle.Mods.LicensePlate = "MTHRBTCH";
            robbedVehicle.AttachBlip();
            robbedVehicle.EnginePowerMultiplier = 2;

            VehicleData vehicleData = new VehicleData();
            vehicleData.Registration = false;
            Utilities.SetVehicleData(robbedVehicle.NetworkId, vehicleData);
            Utilities.ExcludeVehicleFromTrafficStop(robbedVehicle.NetworkId, true);

            robber = await SpawnPed(robberList[random.Next(0, robberList.Count)], robberyLocation);
            robber.AlwaysKeepTask = true;
            robber.BlockPermanentEvents = true;
            robber.Weapons.Give(WeaponHash.Pistol, 250, true, true);

            robber.SetIntoVehicle(robbedVehicle, VehicleSeat.Driver);

            PedData data = new PedData();
            List<Item> items = new List<Item>();
            data.BloodAlcoholLevel = 0.25;
            Item DrugBags = new Item
            {
                Name = "Drug Bags",
                IsIllegal = true
            };
            items.Add(DrugBags);
            Item Pistol = new Item
            {
                Name = "Pistol",
                IsIllegal = true
            };
            items.Add(Pistol);
            Item Dildo = new Item
            {
                Name = "Dirty used dildo",
                IsIllegal = false
            };
            items.Add(Dildo);
            data.Items = items;
            Utilities.SetPedData(robber.NetworkId, data);

        }
        private void Notify(string message)
        {
            ShowNetworkedNotification(message, "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "AIR-1", 15f);
        }
        private void DrawSubtitle(string message, int duration)
        {
            API.BeginTextCommandPrint("STRING");
            API.AddTextComponentSubstringPlayerName(message);
            API.EndTextCommandPrint(duration, false);
        }

    }
}
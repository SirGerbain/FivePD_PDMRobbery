using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using System.Security.Claims;
using System.Xml.Linq;
using System.Security.Cryptography.X509Certificates;
using FivePD.API.Utils;
using System.Data.SqlTypes;
using System.Security.AccessControl;

namespace SirGerbain_PDMRobbery
{
    [CalloutProperties("PDM Robbery", "sirGerbain", "1.0.0")]
    public class SirGerbain_PDMRobbery : FivePD.API.Callout
    {

        Ped robber, swat;
        List<VehicleHash> carList = new List<VehicleHash>();
        List<PedHash> robberList = new List<PedHash>(); 
        Vector3 robberyLocation;
        Random random = new Random();
        Vehicle robbedVehicle;
        bool arrivalOnScene = false;
        bool chaseStarted = false;
        bool air1Dispatched = false;
        bool air1HasSwat = false;

        public SirGerbain_PDMRobbery()
        {

            robberyLocation = new Vector3(-32.04f,-1092.35f,25.85f); //339.7

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
            Notify("~r~[PDM Robbery] ~y~Officer ~b~" + displayName + ",~y~ someone is tampering with a vehicle at PDM!");

        }

        public async override void OnStart(Ped closest)
        {
            setupCallout();
            base.OnStart(closest);

            while (!arrivalOnScene)
            {
                await BaseScript.Delay(500);
                float distance = Game.PlayerPed.Position.DistanceToSquared(robberyLocation);
                if (distance < 200f)
                {
                    robber.Task.CruiseWithVehicle(robbedVehicle, 150f, 786988);

                    arrivalOnScene = true;
                    break;
                }
            }

            while (!chaseStarted && arrivalOnScene)
            {
                await BaseScript.Delay(1000);
                float distance = Game.PlayerPed.Position.DistanceToSquared(robber.Position);
                if (distance < 250f)
                {
                    robber.Task.ClearAll();
                    robber.Task.FleeFrom(closest);
                    if (random.Next(0, 100) < 20)
                    {
                        robber.Task.VehicleShootAtPed(closest);
                        await BaseScript.Delay(random.Next(1000, 5000));
                        robber.Task.ClearAll();
                        robber.Task.CruiseWithVehicle(robbedVehicle, 150f, 786988);
                    }
                }
                else
                {
                    robber.Task.ClearAll();
                    robber.Task.CruiseWithVehicle(robbedVehicle, 150f, 786988);
                }

                if(!air1Dispatched && (random.Next(0, 100) < 15))
                {
                    air1Dispatched = true;
                    Notify("We called in Air-1 for you.");

                    Vehicle helicopter = await SpawnVehicle(VehicleHash.Polmav, new Vector3(robbedVehicle.Position.X, robbedVehicle.Position.Y, 200));
                    helicopter.CurrentRPM = 5000;
                    Ped pilot = await SpawnPed(PedHash.Prisguard01SMM, new Vector3(robbedVehicle.Position.X, robbedVehicle.Position.Y, 200));
                    pilot.SetIntoVehicle(helicopter, VehicleSeat.Driver);
                    pilot.AlwaysKeepTask = true;
                    pilot.BlockPermanentEvents = true;
                    pilot.Task.ChaseWithHelicopter(robbedVehicle, new Vector3(35f, 35f, 35f));

                    if(random.Next(0, 100) < 25)
                    {
                        swat = await SpawnPed(PedHash.Swat01SMY, new Vector3(robbedVehicle.Position.X, robbedVehicle.Position.Y, 200));
                        swat.SetIntoVehicle(helicopter, VehicleSeat.LeftRear);
                        swat.AlwaysKeepTask = true;
                        swat.BlockPermanentEvents = true;
                        swat.Weapons.Give(WeaponHash.AssaultSMG, 250, true, true);
                        air1HasSwat = true;
                    }

                }

                if (air1HasSwat)
                {
                    swat.Task.VehicleShootAtPed(robber);
                    await BaseScript.Delay(random.Next(1000, 5000));
                    swat.Task.ClearAll();
                }

                if (robbedVehicle.IsEngineOnFire)
                {
                    robber.Task.ClearAll();
                    robber.Task.FightAgainst(closest);

                    chaseStarted = true;
                    break;
                }
            }

        }

        public async void setupCallout()
        {
            robbedVehicle = await SpawnVehicle(carList[random.Next(carList.Count)], robberyLocation);
            robbedVehicle.Heading = 339.7f;
            robbedVehicle.LockStatus = VehicleLockStatus.Unlocked;
            robbedVehicle.Mods.LicensePlate = "MTHRBTCH";
            robbedVehicle.AttachBlip();
            robbedVehicle.EnginePowerMultiplier = 2;

            robber = await SpawnPed(robberList[random.Next(0, robberList.Count)], robberyLocation);
            robber.AlwaysKeepTask = true;
            robber.BlockPermanentEvents = true;
            robber.Weapons.Give(WeaponHash.Pistol, 250, true, true);

            robber.SetIntoVehicle(robbedVehicle, VehicleSeat.Driver);
            robber.Task.CruiseWithVehicle(robbedVehicle, 150f, 786988);

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
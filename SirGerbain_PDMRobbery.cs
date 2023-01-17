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

        Ped robber;
        List<VehicleHash> carList = new List<VehicleHash>();
        List<PedHash> robberList = new List<PedHash>(); 
        Vector3 robberyLocation;
        Random random = new Random();
        Vehicle robbedVehicle;
        bool arrivalOnScene = false;

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
            StartDistance = 150f;

        }

        public async override Task OnAccept()
        {
            InitBlip();
            UpdateData();

            PlayerData playerData = Utilities.GetPlayerData();
            string displayName = playerData.DisplayName;
            Notify("~r~[PDM Robbery] ~y~Officer ~b~" + displayName + ",~y~ someone is tampering with a vehicle in the PDM showroom!");

        }

        public async override void OnStart(Ped closest)
        {
            setupCallout();
            base.OnStart(closest);

            while (!arrivalOnScene)
            {
                await BaseScript.Delay(1000);
                float distance = Game.PlayerPed.Position.DistanceToSquared(robberyLocation);
                if (distance > 120f)
                {
                    robber.Task.FleeFrom(closest);

                    arrivalOnScene = true;
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
            robbedVehicle.EngineTorqueMultiplier = 2;

            robber = await SpawnPed(robberList[random.Next(0, robberList.Count)], robberyLocation);
            robber.AlwaysKeepTask = true;
            robber.BlockPermanentEvents = true;
            robber.Weapons.Give(WeaponHash.Pistol, 250, true, true);

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
            ShowNetworkedNotification(message, "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "Prisoner Transport", 15f);
        }
        private void DrawSubtitle(string message, int duration)
        {
            API.BeginTextCommandPrint("STRING");
            API.AddTextComponentSubstringPlayerName(message);
            API.EndTextCommandPrint(duration, false);
        }

    }
}
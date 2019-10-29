using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace EquipmentModelTutorial
{
    class Program
    {
        public static ABB.Vtrin.Drivers.cDriverSkeleton driver;

        // NOTE: Never push your credentials into a repository.
        // This will do because of the demonstration purposes.
        private static readonly string DATA_SOURCE = "wss://localhost/history";
        private static readonly string DB_USERNAME = "username";
        private static readonly string DB_PASSWORD = "password";

        static void Main(string[] args)
        {
            ABB.Vtrin.cDataLoader dataloader = null;

            try
            {
                // Try to connect to the database
                dataloader = new ABB.Vtrin.cDataLoader();
                ConnectOrThrow(
                    dataloader: dataloader,
                    data_source: DATA_SOURCE,
                    db_username: DB_USERNAME,
                    db_password: DB_PASSWORD);

                // Create or update equipment type and properties
                CreateOrUpdateEquipmentTypes();

                // Create or update equipment instances
                CreateOrUpdateEquipmentInstances();
            }

            // Case: Something went wrong
            // > Log the error
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            finally
            {
                // Dispose dataloader if necessary
                if (dataloader != null)
                    dataloader.Dispose();
            }
        }

        public static void CreateOrUpdateEquipmentTypes()
        {

            // CREATE EQUIPMENT TYPES
            // ======================

            // Abstract base types
            // ===================

            ABB.Vtrin.Interfaces.IEquipment baseEquipmentType = CreateOrUpdateEquipmentType(
                equipmentTypeName: "Device",
                isAbstract: true);

            ABB.Vtrin.Interfaces.IEquipment mechanicalEquipmentType = CreateOrUpdateEquipmentType(
                equipmentTypeName: "Mechanical device",
                baseEquipmentType: baseEquipmentType,
                isAbstract: true);

            ABB.Vtrin.Interfaces.IEquipment electricalEquipmentType = CreateOrUpdateEquipmentType(
                equipmentTypeName: "Electrical device",
                baseEquipmentType: baseEquipmentType,
                isAbstract: true);

            // Equipment types
            // ===============

            ABB.Vtrin.Interfaces.IEquipment tankType = CreateOrUpdateEquipmentType(
                equipmentTypeName: "Tank",
                baseEquipmentType: mechanicalEquipmentType);

            ABB.Vtrin.Interfaces.IEquipment pipeType = CreateOrUpdateEquipmentType(
                equipmentTypeName: "Pipe",
                baseEquipmentType: mechanicalEquipmentType);

            ABB.Vtrin.Interfaces.IEquipment pumpType = CreateOrUpdateEquipmentType(
                equipmentTypeName: "Pump",
                baseEquipmentType: electricalEquipmentType);


            // EQUIPMENT TYPE PROPERTIES
            // =========================

            // Common properties
            // =================

            CreateOrUpdateEquipmentProperty(
                propertyName: "Manufacturer",
                propertyType: ABB.Vtrin.cTypeCode.String,
                propertyUnit: "",
                propertyDescription: "The manufacturer of the device",
                isHistorized: false,
                equipmentType: baseEquipmentType);

            // Tank properties
            // ===============

            CreateOrUpdateEquipmentProperty(
                propertyName: "Level",
                propertyType: ABB.Vtrin.cTypeCode.Double,
                propertyUnit: "m",
                propertyDescription: "The water level inside the tank",
                isHistorized: true,
                equipmentType: tankType);

            CreateOrUpdateEquipmentProperty(
                propertyName: "Volume",
                propertyType: ABB.Vtrin.cTypeCode.Double,
                propertyUnit: "m3",
                propertyDescription: "The volume of the water tank",
                isHistorized: false,
                equipmentType: tankType);

            // Pipe properties
            // ===============

            CreateOrUpdateEquipmentProperty(
                propertyName: "Diameter",
                propertyType: ABB.Vtrin.cTypeCode.Double,
                propertyUnit: "cm",
                propertyDescription: "The diameter of the pipe",
                isHistorized: false,
                equipmentType: pipeType);

            CreateOrUpdateEquipmentProperty(
                propertyName: "Flow",
                propertyType: ABB.Vtrin.cTypeCode.Double,
                propertyUnit: "l/min",
                propertyDescription: "The current water flow through the pipe",
                isHistorized: true,
                equipmentType: pipeType);

            // Pump properties
            // ===============

            CreateOrUpdateEquipmentProperty(
                propertyName: "Current power",
                propertyType: ABB.Vtrin.cTypeCode.Double,
                propertyUnit: "W",
                propertyDescription: "The current power of the pump",
                isHistorized: true,
                equipmentType: pumpType);

            CreateOrUpdateEquipmentProperty(
                propertyName: "Nominal power",
                propertyType: ABB.Vtrin.cTypeCode.Double,
                propertyUnit: "W",
                propertyDescription: "The nominal power of the pump",
                isHistorized: false,
                equipmentType: pumpType);

            CreateOrUpdateEquipmentProperty(
                propertyName: "Source tank",
                propertyType: ABB.Vtrin.cTypeCode.GUID,
                propertyUnit: null,
                propertyDescription: "The tank that pump is pumping water from",
                isHistorized: false,
                equipmentType: pumpType);

            CreateOrUpdateEquipmentProperty(
                propertyName: "Target tank",
                propertyType: ABB.Vtrin.cTypeCode.GUID,
                propertyUnit: null,
                propertyDescription: "The tank that pump is pumping water into",
                isHistorized: false,
                equipmentType: pumpType);
        }

        public static ABB.Vtrin.Interfaces.IEquipment CreateOrUpdateEquipmentType(
            string equipmentTypeName,
            bool isAbstract = false,
            ABB.Vtrin.Interfaces.IEquipment baseEquipmentType = null)
        {
            // Try to find existing equipment type with the given name
            ABB.Vtrin.Interfaces.IEquipment equipmentType =
                (ABB.Vtrin.Interfaces.IEquipment)driver.Classes["Equipment"].Instances[equipmentTypeName]?.BeginUpdate();

            // Case: No existing equipment type found
            // > Create a new equipment type
            if (equipmentType == null)
                equipmentType = (ABB.Vtrin.Interfaces.IEquipment)driver.Classes["Equipment"].Instances.Add();

            // Update attributes and commit changes
            equipmentType.Name = equipmentTypeName;
            equipmentType.Base = baseEquipmentType;
            equipmentType.Abstract = isAbstract;
            equipmentType.CommitChanges();

            return equipmentType;
        }

        private static ABB.Vtrin.Interfaces.IPropertyDefinition CreateOrUpdateEquipmentProperty(
            string propertyName,
            ABB.Vtrin.cTypeCode propertyType,
            string propertyUnit,
            bool isHistorized,
            ABB.Vtrin.Interfaces.IEquipment equipmentType,
            string propertyDescription = null)
        {
            ABB.Vtrin.Interfaces.IPropertyDefinition property = null;

            try
            {
                // Try to find existing equipment type property with the given name
                property = (ABB.Vtrin.Interfaces.IPropertyDefinition)driver.Classes["EquipmentPropertyInfo"].Instances
                    .GetInstanceSet("DisplayName=?", propertyName)
                    .First()
                    .BeginUpdate();
            }

            // Case: No existing property found
            // > Create a new property
            catch (System.InvalidOperationException)
            {
                property = (ABB.Vtrin.Interfaces.IPropertyDefinition)driver.Classes["EquipmentPropertyInfo"].Instances
                    .Add();
            }

            finally
            {
                // Set property info
                property.DisplayName = propertyName;
                property.Type = (int)propertyType;
                property.Unit = propertyUnit;
                property.Description = propertyDescription;
                property.Historized = isHistorized;
                property.Equipment = equipmentType;

                // Save or update property
                property.CommitChanges();

            }
            
            return property;
        }

        private static void CreateOrUpdateEquipmentInstances()
        {
            // Define tank instances
            // =====================

            ABB.Vtrin.Interfaces.IEquipmentInstance sourceTank = GetOrCreateEquipmentInstance(
                instanceName: "Example site.Water transfer system.Tank area.Source tank",
                equipmentType: driver.Classes["Path_Tank"]);

            ABB.Vtrin.Interfaces.IEquipmentInstance targetTank = GetOrCreateEquipmentInstance(
                instanceName: "Example site.Water transfer system.Tank area.Target tank",
                equipmentType: driver.Classes["Path_Tank"]);


            // Define pipe instances
            // =====================

            ABB.Vtrin.Interfaces.IEquipmentInstance mainPipe = GetOrCreateEquipmentInstance(
                instanceName: "Example site.Water transfer system.Pipe",
                equipmentType: driver.Classes["Path_Pipe"]);

            ABB.Vtrin.Interfaces.IEquipmentInstance flowbackPipe = GetOrCreateEquipmentInstance(
                instanceName: "Example site.Water transfer system.Flowback pipe",
                equipmentType: driver.Classes["Path_Pipe"]);

            // Define pump instance
            // ====================

            ABB.Vtrin.Interfaces.IEquipmentInstance pump = GetOrCreateEquipmentInstance(
                instanceName: "Example site.Water transfer system.Pump section.Pump",
                equipmentType: driver.Classes["Path_Pump"]);


            // Defining instance properties
            // ============================

            pump = (ABB.Vtrin.Interfaces.IEquipmentInstance)pump.BeginUpdate();
            pump["Source tank"] = sourceTank.Id;
            pump["Target tank"] = targetTank.Id;
            pump["Nominal power"] = 1000;
            pump["Manufacturer"] = "Pumps & Pipes Inc.";
            pump.CommitChanges();

            targetTank = (ABB.Vtrin.Interfaces.IEquipmentInstance)targetTank.BeginUpdate();
            targetTank["Volume"] = 1000;
            targetTank["Manufacturer"] = "Tank Company";
            targetTank.CommitChanges();

            sourceTank = (ABB.Vtrin.Interfaces.IEquipmentInstance)sourceTank.BeginUpdate();
            sourceTank["Volume"] = 1000;
            sourceTank["Manufacturer"] = "Tank Company";
            sourceTank.CommitChanges();

            mainPipe = (ABB.Vtrin.Interfaces.IEquipmentInstance)mainPipe.BeginUpdate();
            mainPipe["Diameter"] = 20;
            mainPipe["Manufacturer"] = "Pumps & Pipes Inc.";
            mainPipe.CommitChanges();

            flowbackPipe = (ABB.Vtrin.Interfaces.IEquipmentInstance)flowbackPipe.BeginUpdate();
            flowbackPipe["Diameter"] = 10;
            flowbackPipe["Manufacturer"] = "Pumps & Pipes Inc.";
            flowbackPipe.CommitChanges();
        }

        private static ABB.Vtrin.Interfaces.IEquipmentInstance GetOrCreateEquipmentInstance(
            string instanceName,
            ABB.Vtrin.cDbClass equipmentType)
        {

            // Try to find existing instance by the given name
            ABB.Vtrin.Interfaces.IEquipmentInstance instance =
                (ABB.Vtrin.Interfaces.IEquipmentInstance)equipmentType.Instances
                    .GetInstanceByName(instanceName);

            // Case: No existing instance found
            // > Create a new one and set info
            if (instance == null)
            {
                instance = (ABB.Vtrin.Interfaces.IEquipmentInstance)equipmentType.Instances.Add();
                instance.Name = instanceName;
                instance.CommitChanges();
            }

            return instance;
        }

        private static void ConnectOrThrow(
            ABB.Vtrin.cDataLoader dataloader,
            string data_source,
            string db_username,
            string db_password)
        {
            // Set up a memory stream to catch exceptions
            using (MemoryStream memoryStream = new MemoryStream())
            {
                TraceListener listener = new TextWriterTraceListener(memoryStream, "connectlistener");
                Trace.Listeners.Add(listener);

                // Convert password to a secure string
                SecureString db_password_secure = new SecureString();
                db_password.ToList().ForEach(c => db_password_secure.AppendChar(c));

                // Initialize the database driver
                driver = dataloader.Connect(
                    data_source,
                    db_username,
                    db_password_secure,
                    ABB.Vtrin.cDataLoader.cConnectOptions.AcceptNewServerKeys
                    | ABB.Vtrin.cDataLoader.cConnectOptions.AcceptServerKeyChanges,
                    out _);

                // Unbind the connect listener
                Trace.Listeners.Remove("connectlistener");

                // Case: driver is null, something went wrong
                // > throw an error
                if (driver == null)
                {
                    // Read stack trace from the memorystream buffer
                    string msg = Encoding.UTF8.GetString(memoryStream.GetBuffer());
                    throw new ApplicationException(msg);
                }
            }
        }
    }
}

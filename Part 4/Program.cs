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

                // Create or equipment type and properties
                CreateOrUpdateEquipmentTypes();
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

            // Actual equipment
            // ================

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
                relatedEquipmentType: baseEquipmentType);

            // Tank properties
            // ===============

            CreateOrUpdateEquipmentProperty(
                propertyName: "Level",
                propertyType: ABB.Vtrin.cTypeCode.Double,
                propertyUnit: "m",
                propertyDescription: "The water level inside the tank",
                isHistorized: true,
                relatedEquipmentType: tankType);

            CreateOrUpdateEquipmentProperty(
                propertyName: "Volume",
                propertyType: ABB.Vtrin.cTypeCode.Double,
                propertyUnit: "m3",
                propertyDescription: "The volume of the water tank",
                isHistorized: false,
                relatedEquipmentType: tankType);

            // Pipe properties
            // ===============

            CreateOrUpdateEquipmentProperty(
                propertyName: "Diameter",
                propertyType: ABB.Vtrin.cTypeCode.Double,
                propertyUnit: "cm",
                propertyDescription: "The diameter of the pipe",
                isHistorized: false,
                relatedEquipmentType: pipeType);

            CreateOrUpdateEquipmentProperty(
                propertyName: "Flow",
                propertyType: ABB.Vtrin.cTypeCode.Double,
                propertyUnit: "l/min",
                propertyDescription: "The current water flow through the pipe",
                isHistorized: true,
                relatedEquipmentType: pipeType);

            // Pump properties
            // ===============

            CreateOrUpdateEquipmentProperty(
                propertyName: "Power",
                propertyType: ABB.Vtrin.cTypeCode.Double,
                propertyUnit: "W",
                propertyDescription: "The current power of the pump",
                isHistorized: true,
                relatedEquipmentType: pumpType);

            CreateOrUpdateEquipmentProperty(
                propertyName: "Nominal power",
                propertyType: ABB.Vtrin.cTypeCode.Double,
                propertyUnit: "W",
                propertyDescription: "The nominal power of the pump",
                isHistorized: false,
                relatedEquipmentType: pumpType);
        }

        public static ABB.Vtrin.Interfaces.IEquipment CreateOrUpdateEquipmentType(
            string equipmentTypeName,
            bool isAbstract = false,
            ABB.Vtrin.Interfaces.IEquipment baseEquipmentType = null)
        {
            // Try to find existing equipment type with that name
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

        private static void CreateOrUpdateEquipmentProperty(
            string propertyName,
            ABB.Vtrin.cTypeCode propertyType,
            string propertyUnit,
            bool isHistorized,
            ABB.Vtrin.Interfaces.IEquipment relatedEquipmentType,
            string propertyDescription = null)
        {
            ABB.Vtrin.Interfaces.IPropertyDefinition property = null;

            try
            {
                // Try to find existing equipment type property with that name
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
                property.Equipment = relatedEquipmentType;

                // Save or update property
                property.CommitChanges();
            }
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

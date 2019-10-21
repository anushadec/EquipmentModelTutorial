
using System;
using System.Linq;
using System.Security;

namespace ABB.Vtrin.DemoExample
{
    class DemoExample
    {
        public static ABB.Vtrin.Drivers.cDriverSkeleton driver;
        
        // NOTE: Never push credentials into repository. This repository
        // is only for demonstration purposes. Not production ready code.
        private static string DATA_SOURCE = "xxxxxxxx";
        private static string DB_USERNAME = "xxxxxxxx";
        private static string DB_PASSWORD = "xxxxxxxx";

        static void Main(string[] args)
        {
            ABB.Vtrin.cDataLoader dataloader = null;

            try
            {
                // Convert password to secure string
                SecureString db_password = new SecureString();
                DB_PASSWORD.ToList()
                           .ForEach(c => db_password.AppendChar(c));

                // Connect database
                dataloader = new ABB.Vtrin.cDataLoader();
                driver = ConnectOrThrow(dataloader, DATA_SOURCE, DB_USERNAME, db_password);

                // Create equipment if not already created
                CreateOrUpdateEquipment();
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
            finally
            {
                if (dataloader != null && driver != null)
                    dataloader.Dispose();

                System.Console.WriteLine("Done.");
            }
        }

        private static void CreateOrUpdateEquipment()
        {
            // Create base equipment
            ABB.Vtrin.Interfaces.IEquipment baseEquipmentType = GetOrCreateEquipment("Device", isAbstract: true);
            ABB.Vtrin.Interfaces.IEquipment mechanicalEquipmentType = GetOrCreateEquipment("Mechanical device", baseEquipment: baseEquipmentType, isAbstract: true);
            ABB.Vtrin.Interfaces.IEquipment electricalEquipmentType = GetOrCreateEquipment("Electrical device", baseEquipment: baseEquipmentType, isAbstract: true);

            // Create actual equipment
            ABB.Vtrin.Interfaces.IEquipment tankType = GetOrCreateEquipment("Tank", baseEquipment: mechanicalEquipmentType);
            ABB.Vtrin.Interfaces.IEquipment pipeType = GetOrCreateEquipment("Pipe", baseEquipment: mechanicalEquipmentType);
            ABB.Vtrin.Interfaces.IEquipment pumpType = GetOrCreateEquipment("Pump", baseEquipment: electricalEquipmentType);

            // Tank properties
            // ===============

            UpdateOrCreateEquipmentProperty(
                propertyName: "Level",
                propertyType: Vtrin.cTypeCode.Double,
                propertyUnit: "m",
                propertyDescription: "The water level inside the tank",
                isHistorized: true,
                relatedEquipmentType: tankType);

            UpdateOrCreateEquipmentProperty(
                propertyName: "Volume",
                propertyType: Vtrin.cTypeCode.Double,
                propertyUnit: "m3",
                propertyDescription: "The volume of the water tank",
                isHistorized: false,
                relatedEquipmentType: tankType);

            // Pipe properties
            // ===============

            UpdateOrCreateEquipmentProperty(
                propertyName: "Diameter",
                propertyType: Vtrin.cTypeCode.Double,
                propertyUnit: "cm",
                propertyDescription: "The diameter of the pipe",
                isHistorized: false,
                relatedEquipmentType: pipeType);

            UpdateOrCreateEquipmentProperty(
                propertyName: "Flow",
                propertyType: Vtrin.cTypeCode.Double,
                propertyUnit: "l/min",
                propertyDescription: "The current water flow through the pipe",
                isHistorized: true,
                relatedEquipmentType: pipeType);

            // Pump properties
            // ===============

            UpdateOrCreateEquipmentProperty(
                propertyName: "Power",
                propertyType: Vtrin.cTypeCode.Double,
                propertyUnit: "W",
                propertyDescription: "The current power of the pump",
                isHistorized: true,
                relatedEquipmentType: pumpType);

            UpdateOrCreateEquipmentProperty(
                propertyName: "Manufacturer",
                propertyType: Vtrin.cTypeCode.String,
                propertyUnit: null,
                propertyDescription: "The manufacturer of the pump",
                isHistorized: false,
                relatedEquipmentType: pumpType);
        }

        private static ABB.Vtrin.Interfaces.IEquipment GetOrCreateEquipment(
            String equipmentName,
            Boolean isAbstract = false,
            ABB.Vtrin.Interfaces.IEquipment baseEquipment = null)
        {
            ABB.Vtrin.Interfaces.IEquipment equipment = (ABB.Vtrin.Interfaces.IEquipment)driver.Classes["Equipment"].Instances[equipmentName];
            if (equipment == null)
            {
                equipment = (ABB.Vtrin.Interfaces.IEquipment)driver.Classes["Equipment"].Instances.Add();
                equipment.Name = equipmentName;
                equipment.Base = baseEquipment;
                equipment.Abstract = isAbstract;

                equipment.CommitChanges();
            }

            return equipment;
        }

        private static void UpdateOrCreateEquipmentProperty(
            String propertyName,
            ABB.Vtrin.cTypeCode propertyType,
            String propertyUnit,
            Boolean isHistorized,
            ABB.Vtrin.Interfaces.IEquipment relatedEquipmentType,
            String propertyDescription = null,
            String targetReference = null)
        {
            ABB.Vtrin.Interfaces.IPropertyDefinition property;

            try
            {
                // Try to find existing property and begin to update
                property = (ABB.Vtrin.Interfaces.IPropertyDefinition)driver.Classes["EquipmentPropertyInfo"].Instances
                    .GetInstanceSet("DisplayName=?", propertyName)
                    .First()
                    .BeginUpdate();
            }
            catch (System.InvalidOperationException)
            {
                // Case: Property not found, create a new property
                property = (ABB.Vtrin.Interfaces.IPropertyDefinition)driver.Classes["EquipmentPropertyInfo"].Instances
                    .Add();
            }

            // Fill some information automically targetReference was provided.
            if (targetReference != null)
            {
                if (targetReference.StartsWith("Class:") && targetReference.Contains("Variable"))
                {
                    propertyType = Vtrin.cTypeCode.UInt32;
                    isHistorized = false;
                }
                else if (targetReference.StartsWith("Class:") && targetReference.Contains("Path_"))
                {
                    propertyType = Vtrin.cTypeCode.GUID;
                    isHistorized = false;
                }
                else if (targetReference.StartsWith("Enumeration:"))
                {
                    propertyType = Vtrin.cTypeCode.String;
                    isHistorized = false;
                }
            }

            // Set info for the property
            property.DisplayName = propertyName;
            property.Type = (int)propertyType;
            property.Unit = propertyUnit;
            property.Description = propertyDescription;
            property.Historized = isHistorized;
            property.Equipment = relatedEquipmentType;

            // Try to commit changes
            property.CommitChanges();
        }

        private static ABB.Vtrin.Drivers.cDriverSkeleton ConnectOrThrow(ABB.Vtrin.cDataLoader dataloader, string data_source, string user, System.Security.SecureString pw)
        {
            System.IO.MemoryStream memorystream = new System.IO.MemoryStream();
            System.Diagnostics.TraceListener listener = new System.Diagnostics.TextWriterTraceListener(memorystream, "connectlistener");
            System.Diagnostics.Trace.Listeners.Add(listener);
            ABB.Vtrin.Drivers.cDriverSkeleton driver = dataloader.Connect(data_source, user, pw, ABB.Vtrin.cDataLoader.cConnectOptions.AcceptNewServerKeys | ABB.Vtrin.cDataLoader.cConnectOptions.AcceptServerKeyChanges, out _);
            System.Diagnostics.Trace.Listeners.Remove("connectlistener");
            System.String s = System.String.Empty;

            if (memorystream.Length > 0)
                s = System.Text.Encoding.UTF8.GetString(memorystream.GetBuffer());

            memorystream.Dispose();
            if (driver == null)
                throw new System.ApplicationException(s);

            return driver;
        }
    }
}

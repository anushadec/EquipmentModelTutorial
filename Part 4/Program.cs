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
            var dataloader = new ABB.Vtrin.cDataLoader();

            try
            {
                // Try to connect to the database
                ConnectOrThrow(
                    dataloader: dataloader,
                    data_source: DATA_SOURCE,
                    db_username: DB_USERNAME,
                    db_password: DB_PASSWORD);

                // Create or update equipment type and properties
                CreateOrUpdateEquipmentTypes();
            }

            // Case: Something went wrong
            // > Log the error
            catch (System.Exception e)
            {
                System.Console.WriteLine(e.ToString());
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

            var baseEquipmentType = CreateOrUpdateEquipmentType(
                equipmentTypeName: "Device",
                isAbstract: true);

            var mechanicalEquipmentType = CreateOrUpdateEquipmentType(
                equipmentTypeName: "Mechanical device",
                baseEquipmentType: baseEquipmentType,
                isAbstract: true);

            var electricalEquipmentType = CreateOrUpdateEquipmentType(
                equipmentTypeName: "Electrical device",
                baseEquipmentType: baseEquipmentType,
                isAbstract: true);

            // Equipment types
            // ===============

            var tankType = CreateOrUpdateEquipmentType(
                equipmentTypeName: "Tank",
                baseEquipmentType: mechanicalEquipmentType);

            var pipeType = CreateOrUpdateEquipmentType(
                equipmentTypeName: "Pipe",
                baseEquipmentType: mechanicalEquipmentType);

            var pumpType = CreateOrUpdateEquipmentType(
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
                propertyUnit: "mm",
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
                propertyName: "Power",
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
                equipmentType: pumpType,
                referenceTarget: "Class:" + tankType.ClassName);

            CreateOrUpdateEquipmentProperty(
                propertyName: "Target tank",
                propertyType: ABB.Vtrin.cTypeCode.GUID,
                propertyUnit: null,
                propertyDescription: "The tank that pump is pumping water into",
                isHistorized: false,
                equipmentType: pumpType,
                referenceTarget: "Class:" + tankType.ClassName);

            CreateOrUpdateEquipmentProperty(
                propertyName: "Operational state",
                propertyType: ABB.Vtrin.cTypeCode.String,
                propertyUnit: null,
                propertyDescription: "Tells whether the pump is running or not",
                isHistorized: true,
                equipmentType: pumpType,
                referenceTarget: "Enumeration:Binary Text(1)");

            CreateOrUpdateEquipmentProperty(
               propertyName: "Power state",
               propertyType: ABB.Vtrin.cTypeCode.String,
               propertyUnit: null,
               propertyDescription: "Tells whether the pump is powered or not",
               isHistorized: true,
               equipmentType: pumpType,
               referenceTarget: "Enumeration:Binary Text(6)");
        }

        public static ABB.Vtrin.Interfaces.IEquipment CreateOrUpdateEquipmentType(
            string equipmentTypeName,
            bool isAbstract = false,
            ABB.Vtrin.Interfaces.IEquipment baseEquipmentType = null)
        {
            var equipmentCache = driver.Classes["Equipment"].Instances;

            // Try to find existing equipment type with the given name
            var equipmentType =
                (ABB.Vtrin.Interfaces.IEquipment)equipmentCache[equipmentTypeName]?.BeginUpdate();

            // Case: No existing equipment type found
            // > Create a new equipment type
            if (equipmentType == null)
                equipmentType = (ABB.Vtrin.Interfaces.IEquipment)equipmentCache.Add();

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
            ABB.Vtrin.Interfaces.IPropertyDefinition property;
            var propertyInfoCache = driver.Classes["EquipmentPropertyInfo"].Instances;

            // Query existing property infos using property name and equipment type
            var properties = propertyInfoCache.GetInstanceSet("Equipment=? AND DisplayName=?", equipmentType, propertyName);
            
            // Case: No existing property found
            // > Create a new property
            if (properties.Length == 0)
                property = (ABB.Vtrin.Interfaces.IPropertyDefinition)propertyInfoCache.Add();

            // Case: Existing property found
            // > Select that and begin to update
            else
                property = (ABB.Vtrin.Interfaces.IPropertyDefinition)properties[0].BeginUpdate();

            // Update property info
            property.DisplayName = propertyName;
            property.Type = (int)propertyType;
            property.Unit = propertyUnit;
            property.Description = propertyDescription;
            property.Historized = isHistorized;
            property.Equipment = equipmentType;

            // Save or update property
            property.CommitChanges();

            return property;
        }
        
        private static void ConnectOrThrow(
            ABB.Vtrin.cDataLoader dataloader,
            string data_source,
            string db_username,
            string db_password)
        {
            // Set up a memory stream to catch exceptions
            using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream())
            {
                var listener = new System.Diagnostics.TextWriterTraceListener(memoryStream, "connectlistener");
                System.Diagnostics.Trace.Listeners.Add(listener);

                // Set connection options
                dataloader.ConnectOptions =
                    ABB.Vtrin.cDataLoader.cConnectOptions.AcceptNewServerKeys
                    | ABB.Vtrin.cDataLoader.cConnectOptions.AcceptServerKeyChanges;

                // Initialize the database driver
                driver = dataloader.Connect(
                    data_source,
                    db_username,
                    db_password,
                    false);

                // Unbind the connect listener
                System.Diagnostics.Trace.Listeners.Remove("connectlistener");

                // Case: driver is null, something went wrong
                // > throw an error
                if (driver == null)
                {
                    // Read stack trace from the memorystream buffer
                    string msg = System.Text.Encoding.UTF8.GetString(memoryStream.GetBuffer());
                    throw new System.ApplicationException(msg);
                }
            }
        }
    }
}

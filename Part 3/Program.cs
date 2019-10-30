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

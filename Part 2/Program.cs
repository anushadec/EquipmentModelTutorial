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

                System.Console.WriteLine("Connection successful!");
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

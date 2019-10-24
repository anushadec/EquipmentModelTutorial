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

                Console.WriteLine("Connection successful!");
            }

            // Case: Something went wrong
            // > Log the error
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            finally {
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

using System;
using System.Threading.Tasks;
using TraceabilityDriverService.Services.Interfaces;
using TraceabilityEngine.Clients;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Models.Driver;
using TraceabilityEngine.Models.Identifiers;

namespace TraceabilityDriverDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                string directoryServiceURL = "http://localhost:8000";
                string solutionProviderURL01 = "http://localhost:8001";
                string solutionProviderURL02 = "http://localhost:8002";
                string traceDriveURL01 = "http://localhost:8003";
                string traceDriveURL02 = "http://localhost:8004";

                // clear the databases
                await Utility.ClearDatabases();

                // launch the directory service
                Utility.StartDirectoryService(directoryServiceURL);

                // launch the trace driver #1
                ITDConfiguration TD_config01 = await Utility.GetConfiguration(traceDriveURL01, directoryServiceURL, solutionProviderURL01, Utility.TraceabilityDriverDB01);
                Utility.StartTraceabilityDriverService(TD_config01);

                // create and register account #1 to trace driver #1 
                ITEDriverAccount account01 = null;
                ITEDriverTradingPartner tp01 = null;
                using (ITEInternalClient client = TEClientFactory.InternalClient(traceDriveURL01, TD_config01.APIKey))
                {
                    account01 = await CreateAccount(client, 1);
                }

                // launch trace driver #2
                ITDConfiguration TD_config02 = await Utility.GetConfiguration(traceDriveURL02, directoryServiceURL, solutionProviderURL02, Utility.TraceabilityDriverDB02);
                TD_config02.MapperClassName = "TestDriver.JsonTestDriver";
                Utility.StartTraceabilityDriverService(TD_config02);

                // create and register account #2 to trace driver #2
                ITEDriverAccount account02 = null;
                ITEDriverTradingPartner tp02 = null;
                using (ITEInternalClient client = TEClientFactory.InternalClient(traceDriveURL02, TD_config02.APIKey))
                {
                    account02 = await CreateAccount(client, 2);
                    tp02 = await AddTradingPartner(client, account02, account01.PGLN);
                }
                using (ITEInternalClient client = TEClientFactory.InternalClient(traceDriveURL01, TD_config01.APIKey))
                {
                    tp01 = await AddTradingPartner(client, account01, account02.PGLN);
                }

                // launch test solution provider #1
                TestSolutionProvider.Program.Start(solutionProviderURL01, TD_config01.MapperDLLPath, TD_config01.MapperClassName, "xml", account01.ID, tp01.ID, traceDriveURL01, TD_config01.APIKey);

                // launch test solution provider #2
                TestSolutionProvider.Program.Start(solutionProviderURL02, TD_config02.MapperDLLPath, TD_config02.MapperClassName, "json", account02.ID, account02.ID, traceDriveURL02, TD_config02.APIKey);

                //System.Threading.Thread.Sleep(3000);
                //System.Diagnostics.Process.Start($"microsoft-edge:{solutionProviderURL01}");

                //System.Threading.Thread.Sleep(3000);
                //System.Diagnostics.Process.Start("msedge.exe", solutionProviderURL02);

                Console.WriteLine("DEMO IS READY TO GO!");
            });

            Console.WriteLine("Press any key to exit....");
            Console.ReadKey();
        }

        private static async Task<ITEDriverAccount> CreateAccount(ITEInternalClient client, int number)
        {
            // create the account
            ITEDriverAccount account = new TEDriverAccount();
            account.Name = $"Test Account #{number}";
            account.PGLN = IdentifierFactory.ParsePGLN($"urn:epc:id:sgln:08600031303.0.{number}");
            account = await client.SaveAccountAsync(account);
            return account;
        }

        private static async Task<ITEDriverTradingPartner> AddTradingPartner(ITEInternalClient client, ITEDriverAccount account, IPGLN pgln)
        {
            // add second account as a trading partner to the first account
            ITEDriverTradingPartner tp = await client.AddTradingPartnerAsync(account.ID, pgln);
            return tp;
        }
    }
}

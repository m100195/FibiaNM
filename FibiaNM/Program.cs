using EtrayAPIWrapper;
using Newtonsoft.Json;
using FibiaNM.Logging;



namespace FibiaNM
{
    class program
    {

        static Credentials? Credentials;

        static void Main(string[] args)
        {

            InitializeCredentials();

            EtrayAPI eTrayClient = new EtrayAPI(
                Credentials.EtrayUser,
                Credentials.EtrayPass,
                EtrayAPI.Environment.Private,
                usePinger: true,
                Credentials.EtrayAPIKey
            );

            EtrayHandler etrayHandler = new EtrayHandler(eTrayClient);
            NvasHandler nvasHandler = new NvasHandler(Credentials);

            nvasHandler.NVASLaunchAndLogin();
            nvasHandler.NVASPCICS();

            var pendingCases = etrayHandler.GetPendingCases();

            foreach (var caseID in pendingCases)
            {
                try
                {
                    EtrayData etrayData = etrayHandler.GetCase(caseID);

                    if (etrayData == null)
                    {
                        continue;
                    }

                    string kontNr = nvasHandler.FibiaNMCU(etrayData.OrderNumber, etrayData.ReasonCode);

                    etrayHandler.NextNM(kontNr, caseID);

                }
                catch (Exception ex)
                {

                    DbEntry.AddLog($"CaseID: {caseID} \n " +
                                   $"InnerException: {ex.InnerException} \n " +
                                   $"StackTrace: {ex.StackTrace} \n" +
                                   $"Message: {ex.Message} \n" +
                                   $"Source: {ex.Source}");

                    etrayHandler.SendToManualHandling(caseID);
                }
            }
            nvasHandler.NVASClose();
        }

        private static void InitializeCredentials()
        {
            var json = System.IO.File.ReadAllText("Credentials.json");
            Credentials = JsonConvert.DeserializeObject<Credentials>(json);
        }
    }
}
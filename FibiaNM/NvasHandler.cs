using NVASConnection;
using NVASConnection.CU_Subpages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace FibiaNM
{
    internal class NvasHandler
    {
        dynamic NvasSession;
        Credentials Credentials;

        public NvasHandler(Credentials credentials)
        {
            Credentials = credentials;
        }

        private void AwaitNvasReady(dynamic nvasSession)
        {
            while (true)
            {
                Process[] processes = Process.GetProcessesByName("R8win");

                if (processes.Length <= 0)
                    continue;

                string brugerField = NVASFunctions.ReadFieldText(nvasSession, 21, 20, 9);
                if (brugerField != null && brugerField.Contains("Bruger") == false)
                    continue;

                return;
            }
        }

        public void NVASLaunchAndLogin()
        {
            NvasSession = NVASFunctions.LaunchNVAS("FibiaNM");
            AwaitNvasReady(NvasSession);
            NVASFunctions.Login(NvasSession, Credentials.NvasUser, Credentials.NvasPass);
        }

        public void NVASPCICS()
        {
            NVASFunctions.Wait(NvasSession);
            NVASFunctions.HovedMenu(NvasSession, "PCICS");
            NVASFunctions.Wait(NvasSession);
        }

        public void NVASClose()
        {
            NVASFunctions.CloseNvas(NvasSession);
        }

        public void NVASException()
        {
            if (NVASFunctions.TerminalLocked(NvasSession) == true)
            {
                NVASFunctions.SendKeys(NvasSession, Keys.ResetKey);
                NVASFunctions.Wait(NvasSession);
            }

            if (NVASFunctions.GetScreenName(NvasSession) != "MENU")
            {

                if (NVASFunctions.FindText(NvasSession, "OPGI").Item3 == true)
                {
                    NVASFunctions.Wait(NvasSession);
                    NVASFunctions.OpgivOrdre(NvasSession);
                    NVASFunctions.Wait(NvasSession);
                }

                NVASFunctions.Wait(NvasSession);
                NVASFunctions.GoToScreen(NvasSession, "MENU");
                NVASFunctions.Wait(NvasSession);
            }

            if (NVASFunctions.GetScreenName(NvasSession) != "MENU")
            {
                NVASClose();
                NVASLaunchAndLogin();
                NVASPCICS();
            }
        }

        public string FibiaNMCU(string orderNumber, string reasonCode)
        {

            string dateOTID = "NM" + DateTime.Now.ToString("MMyy");
            string kontNr = "";
            
            int flowCUCounter = 0;
            
            OsluData infoOSLU;

            if (reasonCode == "A01" || reasonCode == "A11" || reasonCode == "A19" || reasonCode == "A20" || reasonCode == "A24" || reasonCode == "A25" || reasonCode == "A26" || reasonCode == "A28" || reasonCode == "A29" || reasonCode == "A30" || reasonCode == "A34" || reasonCode == "A35" || reasonCode == "A38" || reasonCode == "A39" || reasonCode == "A42" || reasonCode == "A48" || reasonCode == "TF01" || reasonCode == "TF02" || reasonCode == "TS03" || reasonCode == "TS04")
            {
                //"Infrastruktur Ej Som Forventet"
                reasonCode = "Infrastruktur Ej Som Forventet";
            }
            else if (reasonCode == "A02" || reasonCode == "A03" || reasonCode == "A07" || reasonCode == "A08" || reasonCode == "A33" || reasonCode == "A49")
            {
                //"Kundesvigt"
                reasonCode = "Kundesvigt";
            }
            else if (reasonCode == "A09" || reasonCode == "A12" || reasonCode == "A15" || reasonCode == "A16" || reasonCode == "A27" || reasonCode == "A36" || reasonCode == "A37" || reasonCode == "A53")
            {
                //"Afventer Tillaldelse"
                reasonCode = "Afventer Tillaldelse";
            }
            else if (reasonCode == "V01" || reasonCode == "V02" || reasonCode == "V03")
            {
                //"Forsinket Grundet Vejrlig Mm."
                reasonCode = "Forsinket Grundet Vejrlig Mm.";
            }
            else
            {
                throw new Exception($"Can't find a match on reason code: {reasonCode}");
            }

            if (NVASFunctions.CheckScreen(NvasSession, "FORD") == false)
            {
                NVASFunctions.Wait(NvasSession);
                NVASFunctions.GoToScreen(NvasSession, "FORD");
                NVASFunctions.Wait(NvasSession);
            }

            NVASFunctions.Write(NvasSession, orderNumber, 3, 32, update: true);
            NVASFunctions.Wait(NvasSession);
            FORD.Write(NvasSession, "O");
            NVASFunctions.Wait(NvasSession);

            while (NVASFunctions.GetScreenName(NvasSession) != "FORD")
            {
                if (flowCUCounter > 20)
                {
                    NVASException();
                    throw new Exception("Stuck on CU-Flow");
                }

                flowCUCounter++;

                NVASFunctions.Wait(NvasSession);
                switch (NVASFunctions.GetScreenName(NvasSession))
                {
                    case "OTID":
                        NVASFunctions.Wait(NvasSession);
                        OTID.Write(NvasSession, dateOTID, bookbem: reasonCode, update: true);
                        NVASFunctions.Wait(NvasSession);
                        NVASFunctions.SendKeys(NvasSession, Keys.Enter);
                        continue;

                    case "OSLU":
                        NVASFunctions.Wait(NvasSession);
                        infoOSLU = OSLU.Read(NvasSession);
                        NVASFunctions.Wait(NvasSession);
                        kontNr = infoOSLU.KontNrNavn.Substring(0, 8);
                        NVASFunctions.SendKeys(NvasSession, Keys.Enter);
                        continue;

                    default:
                        NVASFunctions.Wait(NvasSession);
                        NVASFunctions.SendKeys(NvasSession, Keys.Enter);
                        continue;
                }
            }
        return kontNr;
        }
    }
}

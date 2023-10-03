using EtrayAPIWrapper;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace FibiaNM
{
	//Etrayhandler behandler alt eTray logik 
    internal class EtrayHandler
    {
        //Strings der bruges til at bestemme hvilke bakker i eTray der arbejdes med
        const string TrayName = "DEV - Fibia events";
        const string TrayType = "DEV - Fibia events";


        //EtrayAPIWrapper Nuget opretter forbindelse til eTray API'et  
        EtrayAPI? EtrayClient;

        public EtrayHandler(EtrayAPI eTrayClient)
        {
            EtrayClient = eTrayClient;
        }

		//GetPendingCases henter alle cases fra bakken (TrayName) i eTray som en liste i en liste
        public List<string> GetPendingCases()
        {

            EtrayAPI.GetMultipleCasesSearchData searchData = new EtrayAPI.GetMultipleCasesSearchData
            {
                TrayName = TrayName,
                IncludeDocProperties = true
            };

            var casesActive = EtrayClient.GetCasesExceptPostponed(TrayName);

            if (casesActive == null)
			{
				return null;
			}

            return casesActive;
        }  
        public EtrayData GetCase(string docID)
        {

            string orderNumber = "";
            string reasonCode = "";

            bool workableCase = false;


            EtrayAPI.GetCaseSearchData searchData = new EtrayAPI.GetCaseSearchData
            {
                DocId = docID, 
                IncludeDocProperties = true,
                //IncludeArchived = true,
                IncludeNote = true,
                IncludeDocFiles = true
            };
            
            var caseData = EtrayClient.GetCase(searchData);

            if (caseData.Body.DocumentSearchResponse.DocumentSearchResult.DocumentData == null)
            {
                return null;
                //throw new Exception("No documentData in etray was available");
            }

            if (caseData.Body.DocumentSearchResponse.DocumentSearchResult.DocumentData.DocFiles.ToString() == "System.Object")
            {
                return null;
                //throw new Exception("No docFiles in etray was available");
            }

            XmlNode[]? docFiles = (XmlNode[])caseData.Body.DocumentSearchResponse.DocumentSearchResult.DocumentData.DocFiles;

            if (caseData.Body.DocumentSearchResponse.DocumentSearchResult.DocumentData.Document.Note.ToString() != "System.Object")
            {

                return null;

                //XmlNode[]? note = (XmlNode[])caseData.Body.DocumentSearchResponse.DocumentSearchResult.DocumentData.Document.Note;

                //if (!Regex.IsMatch(note[0].InnerText, "[a-zA-Z]\\d{2}"))
                //{
                //    return null;
                //}
                
            }

            foreach (var item in docFiles)
            {
                if (item.InnerText.ToLower().Contains("fibia : order on hold notification - order"))
                {
                    workableCase = true;
                    continue;
                }
            }

            if (!workableCase)
            {
                return null;
            }

            //XmlNode[]? notes = (XmlNode[])caseData.Body.DocumentSearchResponse.DocumentSearchResult.DocumentData.Document.Notes;

            //foreach (var item in notes)
            //{
            //    if (item.InnerText.ToLower().Contains("nm ej muligt, skal håndteres manuelt"))
            //    {
            //        workableCase = false;
            //        continue;
            //    }
            //}

            if (!workableCase) 
            {
                return null;
            }

            foreach (var item in caseData.Body.DocumentSearchResponse.DocumentSearchResult.DocumentData.DocPropertiesMulti)
            {
                if (item.Name == "Reason_code")
                    reasonCode = item.Values.Value;

                if (item.Name == "Ordernr")
                    orderNumber = item.Values.Value;
            }

            if (reasonCode.Length != 3)
            {
                throw new Exception($"ReasonCode error detected! ReasonCode: {reasonCode}");
            }

            if (orderNumber.Length != 9)
            {
                throw new Exception($"OrderNumber error detected! OrderNumber: {orderNumber}");
            }

            EtrayData neededCaseData = new EtrayData
            {
                OrderNumber = orderNumber,
                ReasonCode = reasonCode
            };
            
            return neededCaseData;
        }

        public void SendToManualHandling(string docID)
        {
            EtrayClient.AddNoteToCase("NM ej muligt, skal håndteres manuelt", docID);
        }

        public void NextNM(string kontNr, string docID)
        {
            EtrayClient.WriteToSUB(kontNr, docID);
            EtrayClient.ChangeLifecycleAndTask("Ordre sat på NM", "Ordre sat på NM", docID);
        }
	}
}

using Azure.Storage;
using Azure.Storage.Files.DataLake;
using Newtonsoft.Json;
using System;
using System.IO;

namespace TrailBlazers.Qualtrics.SurveyResponseExportToSql
{
    internal class SurveyResponseJson_Service
    {
        //This method retrieves the survey response JSON file from Data Lake storage based on the survey id and export id
        public dynamic LoadJson(string surveyId, string exportId, string accountName, string dataLakeStorageKey)
        {
            try
            {
                //For local development
                //"C:\\Users\\BrandonDuncan\\source\\repos\\TrailBlazers.Qualtrics.SurveyResponseExportToSql\\TrailBlazers.Qualtrics.SurveyResponseExportToSql\\development\\SurveyResponseExport.json"))

                // Get Data Lake survey response JSON file based on survey id and export id passed in
                var fileClient = GetDataLakeFileClient(accountName, dataLakeStorageKey, surveyId, exportId);
                using (StreamReader r = new StreamReader(fileClient.OpenRead()))
                {
                    string json = r.ReadToEnd();
                    dynamic array = JsonConvert.DeserializeObject(json);
                    return array;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error " + ex.Message);
                return null;
            }
        }

        public static DataLakeFileClient GetDataLakeFileClient(string accountName, string accountKey, string surveyId, string exportId)
        {
            StorageSharedKeyCredential sharedKeyCredential = new StorageSharedKeyCredential(accountName, accountKey);
            return new DataLakeFileClient(new Uri("https://std365appendonlyprod001.blob.core.windows.net/qualtrics-survey-response-export/unzipped/" + surveyId + "_" + exportId + "_surveyresponses.json"), sharedKeyCredential);
        }

    }
}

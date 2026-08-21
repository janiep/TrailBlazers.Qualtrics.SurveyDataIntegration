using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TrailBlazers.Qualtrics.SurveyDataIntegration.Models;
using System.Linq;

namespace TrailBlazers.Qualtrics.SurveyDataIntegration
{
    public static class SurveyResponseMetadataExportIntegration
    {
        private static string _storageAccount = Environment.GetEnvironmentVariable("StorageAccount");
        private static string _dataLakeStorageKey = Environment.GetEnvironmentVariable("DataLakeStorageKey");
        private static List<string> _ignoreFieldList = new List<string>{ "responseid", "surveyid", "contactlanguage", "duration", "startdate", "enddate", "finished", "userlanguage", "_recordId","lastseenquestions" };

        [FunctionName("SurveyResponseMetadataExportIntegration_SurveyResponse")]
        public static async Task<IActionResult> RunSurveyResponseMetadataIntegration(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "SurveyResponseMetadataExportIntegration_SurveyResponse/{surveyId}/{exportId}")] HttpRequest req, string surveyId, string exportId,
            ILogger log)
        {
            //Get JSON from survey export endpoint
            var surveyResponseService = new SurveyResponseJson_Service();
            var surveyResponseJson = surveyResponseService.LoadJson(surveyId, exportId, _storageAccount, _dataLakeStorageKey);
            
            List<SurveyResponseMetadataModel> surveyResponseMetadata = new List<SurveyResponseMetadataModel>();
            if(surveyResponseJson == null)
            {
                return new BadRequestObjectResult("Unable to retrieve JSON from DataLake for Survey ID " + surveyId + ", Export Id: " + exportId); 
            }
            try
            {
                //Loop over individual responses
                log.LogInformation("line 41");
                foreach (var doc in surveyResponseJson)
                {
                    foreach (var responseArr in doc)
                    {
                        foreach (var response in responseArr)
                        {
                            //"responseId": "R_3JEkxSzOJ4mF94O", - primary key
                            //"values": { } - Contains all question responses with integer value for choices
                            //"labels": { } - Contains all choice questions responses with label
                            var newResponse = new SurveyResponseMetadataModel();
                            var QID = "QID";
                            foreach (var answer in response.values)
                            {
                                newResponse = new SurveyResponseMetadataModel();
                                newResponse.ResponseId = response.responseId;
                                if (answer.Name.Contains(QID))
                                {
                                    continue;
                                }
                                if (!_ignoreFieldList.Any(ignore => string.Equals(ignore, answer.Name, StringComparison.OrdinalIgnoreCase)) && !answer.Name.Contains("language"))
                                {
                                    var answerLower = answer.Name.ToLower();
                                    switch (answerLower)
                                    {
                                        case "eml":
                                        case "contact email":
                                        case "email":
                                        case "em":
                                            newResponse.FieldName = "email";
                                            newResponse.Value = answer.Value;
                                            surveyResponseMetadata.Add(newResponse);
                                            break;

                                        case "cid":
                                        case "contactid":
                                            newResponse.FieldName = "contactid";
                                            newResponse.Value = answer.Value;
                                            surveyResponseMetadata.Add(newResponse);
                                            break;
                                        default:
                                            newResponse.FieldName = answer.Name;
                                            newResponse.Value = answer.Value;
                                            surveyResponseMetadata.Add(newResponse);
                                            break;
                                    }
                                    
                                }

                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogCritical(ex.StackTrace);
                return new BadRequestObjectResult(ex.Message);
            }
            

            return new OkObjectResult(surveyResponseMetadata);
        }
    }

}

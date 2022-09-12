using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Collections;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using TrailBlazers.Qualtrics.SurveyDataIntegration.Models;
using Azure;
using System.Net;
using System.Linq;

namespace TrailBlazers.Qualtrics.SurveyDataIntegration
{
    public static class SurveyResponseExportIntegration
    {
        private static string _dataLakeStorageKey = Environment.GetEnvironmentVariable("DataLakeStorageKey");

        [FunctionName("SurveyResponseExportIntegration_SurveyResponse")]
        public static async Task<IActionResult> RunSurveyResponseIntegration(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "SurveyResponseExportIntegration_SurveyResponse/{surveyId}/{exportId}")] HttpRequest req, string surveyId, string exportId,
            ILogger log)
        {
            //Get JSON from survey export endpoint
            var surveyResponseService = new SurveyResponseJson_Service();
            var surveyResponseJson = surveyResponseService.LoadJson(surveyId, exportId, "std365appendonlyprod001", _dataLakeStorageKey);

            List<SurveyResponseModel> surveyResponses = new List<SurveyResponseModel>();

            if(surveyResponseJson == null)
            {
                return new BadRequestObjectResult("Unable to retrieve JSON from DataLake for Survey ID " + surveyId + ", Export Id: " + exportId);
            }
            try
            {
                //Loop over individual responses
                foreach (var doc in surveyResponseJson)
                {
                    foreach (var responseArr in doc)
                    {
                        foreach (var response in responseArr)
                        {
                            //"responseId": "R_3JEkxSzOJ4mF94O", - primary key
                            //"values": { } - Contains all question responses with integer value for choices
                            //"labels": { } - Contains all choice questions responses with label
                            var newResponse = new SurveyResponseModel();
                            newResponse.SurveyId = surveyId;
                            newResponse.ResponseId = response.responseId;
                            foreach (var answer in response.values)
                            {
                                switch (answer.Name)
                                {
                                    case "startDate":
                                        newResponse.StartDate = answer.Value;
                                        break;
                                    case "endDate":
                                        newResponse.EndDate = answer.Value;
                                        break;
                                    case "duration":
                                        newResponse.Duration = answer.Value;
                                        break;
                                    case "ContactID":
                                        newResponse.ContactId = answer.Value;
                                        break;
                                    case "Email":
                                        newResponse.ContactEmail = answer.Value;
                                        break;
                                    case "userLanguage":
                                        newResponse.ContactLanguage = answer.Value;
                                        break;
                                    case "Section":
                                        newResponse.Section = answer.Value;
                                        break;
                                    case "EventCode":
                                        newResponse.EventCode = answer.Value;
                                        break;
                                    case "EventDate":
                                        newResponse.EventDate = answer.Value;
                                        break;
                                    case "ZipCode":
                                        newResponse.ZipCode = answer.Value;
                                        break;
                                    case "EventName":
                                        newResponse.EventName = answer.Value;
                                        break;
                                    default:
                                        break;
                                }

                            }

                            surveyResponses.Add(newResponse);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogCritical(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
            

            return new OkObjectResult(surveyResponses);
        }

        [FunctionName("SurveyResponseExportIntegration_QuestionResponse")]
        public static async Task<IActionResult> RunQuestionResponseIntegration(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = "SurveyResponseExportIntegration_QuestionResponse/{surveyId}/{exportId}")] HttpRequest req, string surveyId, string exportId,
           ILogger log)
        {
            //Get JSON from survey export endpoint
            var surveyResponseService = new SurveyResponseJson_Service();
            var surveyResponseJson = surveyResponseService.LoadJson(surveyId, exportId, "std365appendonlyprod001", _dataLakeStorageKey);

            List<QuestionResponseModel> surveyQuestionResponses = new List<QuestionResponseModel>();

            try
            {
                //Loop over individual responses
                foreach (var doc in surveyResponseJson)
                {
                    foreach (var responseArr in doc)
                    {
                        foreach (var response in responseArr)
                        {
                            //Ex. "responseId": "R_3JEkxSzOJ4mF94O", - primary key
                            var responseId = response.responseId;
                            foreach (var answer in response.values)
                            {
                                var newQuestionResponse = new QuestionResponseModel();
                                newQuestionResponse.ResponseId = responseId;
                                newQuestionResponse.SurveyId = surveyId;
                                switch (answer.Name)
                                {
                                    
                                    //Ex. QID49_1
                                    case var questionId when new Regex(@"^QID\d+_\d+$").IsMatch(questionId):
                                        newQuestionResponse.QuestionId = questionId;
                                        //Get label value from labels object
                                        var answerLabel = response.labels.Value<string>(questionId) ?? "";
                                        if (answerLabel != "")
                                        {
                                            newQuestionResponse.QuestionResponse = answerLabel;
                                        }
                                        else
                                        {   //If not found in labels, use answer from values object
                                            newQuestionResponse.QuestionResponse = (string)answer.Value;
                                        }
                                        newQuestionResponse.Id = newQuestionResponse.ResponseId + "_" + newQuestionResponse.QuestionId; //Set primary key (responseid_questionid)
                                        surveyQuestionResponses.Add(newQuestionResponse);
                                        break;
                                    //Ex. QID50_TEXT
                                    case var questionId when new Regex(@"^(QID\d+)_TEXT$").IsMatch(questionId):
                                        newQuestionResponse.QuestionId = questionId.Replace("_TEXT", "");
                                        newQuestionResponse.QuestionResponse = answer.Value.ToString();
                                        newQuestionResponse.Id = newQuestionResponse.ResponseId + "_" + newQuestionResponse.QuestionId; //Set primary key (responseid_questionid)
                                        surveyQuestionResponses.Add(newQuestionResponse);
                                        break;
                                    default:
                                        break;
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogCritical(ex.ToString());
                return new BadRequestObjectResult(ex.Message);
            }


            return new OkObjectResult(surveyQuestionResponses);
        }

        [FunctionName("SurveyQuestionIntegration")]
        public static async Task<IActionResult> RunSurveyQuestionIntegration(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "SurveyQuestionIntegration/{surveyId}")] HttpRequest req, string surveyId,
            ILogger log)
        {
            try
            {
                //Get requestBody body
                var body = new StreamReader(req.Body);
                body.BaseStream.Seek(0, SeekOrigin.Begin);
                JObject surveyQuestionJson = JObject.Parse(body.ReadToEnd());

                List<SurveyQuestionModel> surveyQuestions = new List<SurveyQuestionModel>();

                //Loop over individual responses
                foreach (var question in surveyQuestionJson["result"]["elements"])
                {
                    var newQuestion = new SurveyQuestionModel();
                    newQuestion.SurveyId = surveyId; //Ex. SV_afMzammycfmSPXw
                    newQuestion.QuestionId = (string)question["QuestionID"]; //Ex. QID9
                    newQuestion.QuestionText = (string)question["QuestionText"];
                    newQuestion.QuestionDescription = (string)question["QuestionDescription"];
                    newQuestion.QuestionType = (string)question["QuestionType"];
                    newQuestion.Selector = (string)question["Selector"];
                    newQuestion.SubSelector = (string)question["SubSelector"];
                    newQuestion.Id = surveyId + "_" + newQuestion.QuestionId;

                    //Avoid creating records for unfinished questions
                    if (newQuestion.QuestionDescription == "Click to write the question text")
                    {
                        continue; //Skip to next question
                    }

                    //Handle matrix question
                    if ((string)question["QuestionType"] == "Matrix")
                    {
                        foreach (var choice in question["Choices"])
                        {
                            //Ex.
                            //"Choices": {
                            //    "1": {
                            //          "Display": "Taste"
                            //            },
                            //    "2": {
                            //          "Display": "Variety"
                            //    },
                            //    "3": {
                            //          "Display": "Value"
                            //    },
                            //    "4": {
                            //          "Display": "Service Staff"
                            //    },
                            //    "5": {
                            //          "Display": "Wait Time"
                            //    },
                            //    "7": {
                            //          "Display": "Cashless Payment"
                            //    }
                            //This will become 6 separate question records
                            //QID9_1, QID_2, QID_3, etc.

                            Console.WriteLine("{0}\n", choice);
                            var newChoiceQuestion = new SurveyQuestionModel();

                            newChoiceQuestion.SurveyId = surveyId;
                            dynamic choiceObj = choice; //Convert to dynamic obj first to access Name
                            newChoiceQuestion.QuestionId = newQuestion.QuestionId + "_" + choiceObj.Name;
                            newChoiceQuestion.QuestionText = newQuestion.QuestionText;
                            newChoiceQuestion.QuestionDescription = newQuestion.QuestionDescription;
                            newChoiceQuestion.QuestionType = newQuestion.QuestionType;
                            newChoiceQuestion.Selector = newQuestion.Selector;
                            newChoiceQuestion.SubSelector = newQuestion.SubSelector;
                            newChoiceQuestion.Id = surveyId + "_" + newChoiceQuestion.QuestionId;

                            //Get matrix display label
                            var children = choice.Children().ToList();
                            foreach (var token in children)
                            {
                                Console.WriteLine(token.ToString());
                                var test = token.SelectToken("Display") ?? null;
                                if(test != null)
                                {
                                    newChoiceQuestion.MatrixOption = test.Value<string>();
                                    break; //Break loop since we found the display matrix value
                                }
                            }
                            
                            surveyQuestions.Add(newChoiceQuestion);
                        }
                    } else
                    {
                        surveyQuestions.Add(newQuestion);
                    }
                }
                return new OkObjectResult(surveyQuestions);
            }
            catch (Exception ex)
            {
                log.LogCritical(ex.ToString());
                return new BadRequestObjectResult(ex.Message);
            }

        }

    }

}

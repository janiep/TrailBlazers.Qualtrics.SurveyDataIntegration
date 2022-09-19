using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrailBlazers.Qualtrics.SurveyDataIntegration.Models
{
    public class SurveyResponseModel
    {
        public string ResponseId { get; set; }
        public string SurveyId { get; set; }
        public string ContactId { get; set; }
        public string ContactEmail { get; set; }
        public string ContactLanguage { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Duration { get; set; }
        public string Section { get; set; }
        public string EventCode { get; set; }
        public string EventDate { get; set; }
        public string ZipCode { get; set; }
        public string EventName { get; set; }
        public string Category { get; set; }
        public string Finished { get; set; }
    }
}

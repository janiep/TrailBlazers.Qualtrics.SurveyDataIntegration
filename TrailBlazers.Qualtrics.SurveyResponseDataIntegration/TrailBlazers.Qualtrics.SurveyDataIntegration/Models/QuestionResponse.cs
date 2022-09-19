using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrailBlazers.Qualtrics.SurveyDataIntegration.Models
{
    public class QuestionResponseModel
    {
        public string Id { get; set; } //Primary key - ResponseId + Question Id
        public string ResponseId { get; set; } //Ties back to SurveyResponse record
        public string SurveyId { get; set; }
        public string QuestionId { get; set; }
        public string QuestionResponse { get; set; }
        public string QuestionResponseNumeric { get; set; }
    }
}

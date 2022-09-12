using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrailBlazers.Qualtrics.SurveyDataIntegration.Models
{
    public class SurveyQuestionModel
    {
        public string Id { get; set; }
        public string QuestionId { get; set; }
        public string SurveyId { get; set; }
        public string QuestionDescription { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public string Selector { get; set; }
        public string SubSelector { get; set; }
        public string MatrixOption { get; set; }
    }
}

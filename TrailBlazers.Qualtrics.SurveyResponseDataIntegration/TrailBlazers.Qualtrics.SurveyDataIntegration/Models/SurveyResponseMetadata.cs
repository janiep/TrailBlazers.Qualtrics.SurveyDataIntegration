using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrailBlazers.Qualtrics.SurveyDataIntegration.Models
{
    public class SurveyResponseMetadataModel
    {
        public string ResponseId { get; set; }
        public string FieldName { get; set; }
        public string Value { get; set; }
    }
}

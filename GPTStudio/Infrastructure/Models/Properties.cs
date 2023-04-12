using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GPTStudio.Infrastructure.Models
{
    internal sealed class Properties
    {
        public string OpenAIAPIKey { get; set; }
        public string AzureAPIKey { get; set; }
        public string AzureSpeechRegion { get; set; }
    }
}

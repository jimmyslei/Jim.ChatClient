using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JIm.ChatClient.Core.Models
{
    public class AIModel
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string ApiEndpoint { get; set; }
        public string ApiKey { get; set; }
    }
}
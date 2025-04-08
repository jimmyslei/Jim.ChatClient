using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JIm.ChatClient.Core.Entity
{
    [Table(Name = "ai_model")]
    public class AIModelEntity
    {
        [Column(Name = "Id",IsIdentity = true, IsPrimary = true)]
        public long Id { get; set; }
        [Column(Name = "Type")]
        public string Type { get; set; }
        [Column(Name = "ModelId")]
        public string ModelId { get; set; }
        [Column(Name = "DisplayName")]
        public string DisplayName { get; set; }
        [Column(Name = "ApiEndpoint")]
        public string ApiEndpoint { get; set; }
        [Column(Name = "ApiKey")]
        public string ApiKey { get; set; }
        [Column(Name = "IsDefault")]
        public bool IsDefault { get; set; }
        [Column(Name = "CreateTime",DbType = "datetime")]
        public DateTime CreateTime { get; set; }
        [Column(Name = "UpdateTime", DbType = "datetime")]
        public DateTime UpdateTime { get; set; }
    }
}
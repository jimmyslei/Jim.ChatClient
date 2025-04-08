using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JIm.ChatClient.Core.Entity
{
    [Table(Name = "chat_message")]
    public class ChatMessageEntity
    {
        [Column(Name = "Id",IsIdentity = true, IsPrimary = true)]
        public long Id { get; set; }
        [Column(Name = "Role")]
        public string Role { get; set; }
        
        [Column(Name = "Content",StringLength = -1)]
        public string Content { get; set; }
        [Column(Name = "ModelId")]
        public string ModelId { get; set; }
        [Column(Name = "CreateTime")]
        public DateTime CreateTime { get; set; }
        [Column(Name = "SessionId")]
        public string SessionId { get; set; }
    
    }
}
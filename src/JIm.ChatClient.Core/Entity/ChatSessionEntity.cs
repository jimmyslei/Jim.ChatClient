using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JIm.ChatClient.Core.Entity
{
    [Table(Name = "chat_session")]
    public class ChatSessionEntity
    {
        [Column(Name = "Id", IsIdentity = true, IsPrimary = true)]
        public long Id { get; set; }
        [Column(Name = "Title")]
        public string Title { get; set; }

        [Column(Name = "CreateTime")]
        public DateTime CreateTime { get; set; }

        [Column(Name = "ModifyTime")]
        public DateTime ModifyTime { get; set; }

    }
}
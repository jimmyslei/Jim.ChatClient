using FreeSql;
using chatClient.Models;
using System;
using System.Threading.Tasks;
using chatClient.Entity;
using System.Collections.Generic;

namespace chatClient.Services
{
    public class DatabaseService
    {
        private readonly IFreeSql _freeSql;

        public DatabaseService()
        {
            _freeSql = new FreeSqlBuilder()
                .UseConnectionString(DataType.Sqlite, @"Data Source=chatClient.db")
                .UseAutoSyncStructure(true)
                .UseMonitorCommand(cmd => Console.Write(cmd.CommandText))
                .Build();
            _freeSql.CodeFirst.IsAutoSyncStructure = true;
        }

        // ��ȡ��ʷ�����¼
        public async Task<List<ChatSessionEntity>> GetHistorySessionAsync(int count = 100)
        {
            return await _freeSql.Select<ChatSessionEntity>()
                .OrderByDescending(x => x.ModifyTime)
                .Take(count)
                .ToListAsync();
        }
        // ��������¼
        public async Task<long> SaveMessageAsync(ChatSessionEntity session)
        {
            return await _freeSql.Insert(session).ExecuteIdentityAsync();
        }

        // �����Ϣ
        public async Task SaveMessageAsync(ChatMessageEntity message)
        {
            await _freeSql.Insert(message).ExecuteAffrowsAsync();
        }

        // ��ȡ��ʷ��Ϣ
        public async Task<List<ChatMessageEntity>> GetHistoryMessagesAsync(string sessionId, int count = 100)
        {
            return await _freeSql.Select<ChatMessageEntity>().Where(x => x.SessionId == sessionId)
                .OrderByDescending(x => x.CreateTime)
                .Take(count)
                .ToListAsync();
        }

        // ��ȡ��ʷ��Ϣ
        public async Task<List<ChatMessageEntity>> GetMessageListAsync(string sessionId)
        {
            return await _freeSql.Select<ChatMessageEntity>().Where(x => x.SessionId == sessionId).OrderByDescending(x => x.Id).Take(10).ToListAsync();
        }

        // �޸ĻỰʱ��
        public async Task UpdateModifyTimeAsync(string sessionId)
        {
            _ = await _freeSql.Update<ChatSessionEntity>().Set(x => x.ModifyTime, DateTime.Now).Where(x => x.Id == long.Parse(sessionId)).ExecuteAffrowsAsync();
        }
        // ��ȡû�лỰ����Ϣ
        public async Task<List<ChatMessageEntity>> GetNoSessionMessageListAsync()
        {
            return await _freeSql.Select<ChatMessageEntity>().Where(x => x.SessionId == null || x.SessionId == "").ToListAsync();
        }
        // �޸���Ϣ�ỰID
        public async Task UpdateMessageSessionIdAsync(string chatSessionId)
        {
            _ = await _freeSql.Update<ChatMessageEntity>().Set(m => m.SessionId, chatSessionId).Where(m => m.SessionId == null || m.SessionId == "").ExecuteAffrowsAsync();
        }

    }
}
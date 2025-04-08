using FreeSql;
using JIm.ChatClient.Core.Models;
using System;
using System.Threading.Tasks;
using JIm.ChatClient.Core.Entity;
using System.Collections.Generic;

namespace JIm.ChatClient.Core.Services
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

        #region ����Aiģ��������Ϣ

        // ��ȡ����ģ������
        public async Task<List<AIModelEntity>> GetAllModelsAsync()
        {
            return await _freeSql.Queryable<AIModelEntity>()
                .OrderByDescending(m => m.IsDefault)
                .OrderBy(m => m.CreateTime)
                .ToListAsync();
        }

        // ���ģ������
        public async Task<AIModelEntity> AddModelAsync(AIModelEntity model)
        {
            //model.Id = Guid.NewGuid();
            model.CreateTime = DateTime.Now;
            model.UpdateTime = DateTime.Now;

            // �������ΪĬ�ϣ�������ģ������Ϊ��Ĭ��
            if (model.IsDefault)
            {
                await _freeSql.Update<AIModelEntity>()
                    .Set(m => new AIModelEntity { IsDefault = false })
                    .Where(m => m.IsDefault)
                    .ExecuteAffrowsAsync();
            }

            await _freeSql.Insert(model).ExecuteAffrowsAsync();
            return model;
        }

        // ����ģ������
        public async Task<bool> UpdateModelAsync(AIModelEntity model)
        {
            model.UpdateTime = DateTime.Now;

            // �������ΪĬ�ϣ�������ģ������Ϊ��Ĭ��
            if (model.IsDefault)
            {
                await _freeSql.Update<AIModelEntity>()
                    .Set(m => new AIModelEntity { IsDefault = false })
                    .Where(m => m.Id != model.Id && m.IsDefault)
                    .ExecuteAffrowsAsync();
            }

            return await _freeSql.Update<AIModelEntity>().Set(m => model)
                .Where(m=>m.Id == model.Id)
                .ExecuteAffrowsAsync() > 0;
        }

        // ɾ��ģ������
        public async Task<bool> DeleteModelAsync(long id)
        {
            // ����Ƿ�ΪĬ��ģ�ͣ������������ɾ��
            var model = await _freeSql.Queryable<AIModelEntity>().Where(m => m.Id == id).FirstAsync();
            if (model != null && model.IsDefault)
            {
                return false; // ������ɾ��Ĭ��ģ��
            }

            return await _freeSql.Delete<AIModelEntity>().Where(m => m.Id == id).ExecuteAffrowsAsync() > 0;
        }

        // ����Ĭ��ģ��
        public async Task<bool> SetDefaultModelAsync(long id)
        {
            // ������ģ������Ϊ��Ĭ��
            await _freeSql.Update<AIModelEntity>()
                .Set(m => new AIModelEntity { IsDefault = false })
                .Where(m => m.IsDefault)
                .ExecuteAffrowsAsync();

            // ��ָ��ģ������ΪĬ��
            return await _freeSql.Update<AIModelEntity>()
                .Set(m => new AIModelEntity { IsDefault = true })
                .Where(m => m.Id == id)
                .ExecuteAffrowsAsync() > 0;
        }

        // ��ע����е�ģ��Ǩ�Ƶ����ݿ⣨һ���Բ�����
        public async Task MigrateModelsFromRegistryAsync()
        {
            var registryModels = Config.RegistryModelConfig.GetModels();
            if (registryModels.Count == 0) return;

            // ������ݿ����Ƿ�����ģ������
            var existingModels = await _freeSql.Queryable<AIModelEntity>().CountAsync();
            if (existingModels > 0) return; // ����ģ�����ã�����Ǩ��

            var models = new List<AIModelEntity>();
            foreach (var model in registryModels)
            {
                models.Add(new AIModelEntity
                {
                    Type = model.Type,
                    ModelId = model.Id,
                    DisplayName = model.DisplayName,
                    ApiEndpoint = model.ApiEndpoint,
                    ApiKey = model.ApiKey,
                    IsDefault = model == registryModels[0] // ��һ��ģ����ΪĬ��
                });
            }

            _freeSql.Insert(models).ExecuteAffrows(); // �������루����£�ģ�����ã�һ���Բ�����
        }

        #endregion


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
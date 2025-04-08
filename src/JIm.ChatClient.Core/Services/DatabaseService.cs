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

        #region 操作Ai模型配置信息

        // 获取所有模型配置
        public async Task<List<AIModelEntity>> GetAllModelsAsync()
        {
            return await _freeSql.Queryable<AIModelEntity>()
                .OrderByDescending(m => m.IsDefault)
                .OrderBy(m => m.CreateTime)
                .ToListAsync();
        }

        // 添加模型配置
        public async Task<AIModelEntity> AddModelAsync(AIModelEntity model)
        {
            //model.Id = Guid.NewGuid();
            model.CreateTime = DateTime.Now;
            model.UpdateTime = DateTime.Now;

            // 如果设置为默认，则将其他模型设置为非默认
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

        // 更新模型配置
        public async Task<bool> UpdateModelAsync(AIModelEntity model)
        {
            model.UpdateTime = DateTime.Now;

            // 如果设置为默认，则将其他模型设置为非默认
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

        // 删除模型配置
        public async Task<bool> DeleteModelAsync(long id)
        {
            // 检查是否为默认模型，如果是则不允许删除
            var model = await _freeSql.Queryable<AIModelEntity>().Where(m => m.Id == id).FirstAsync();
            if (model != null && model.IsDefault)
            {
                return false; // 不允许删除默认模型
            }

            return await _freeSql.Delete<AIModelEntity>().Where(m => m.Id == id).ExecuteAffrowsAsync() > 0;
        }

        // 设置默认模型
        public async Task<bool> SetDefaultModelAsync(long id)
        {
            // 将所有模型设置为非默认
            await _freeSql.Update<AIModelEntity>()
                .Set(m => new AIModelEntity { IsDefault = false })
                .Where(m => m.IsDefault)
                .ExecuteAffrowsAsync();

            // 将指定模型设置为默认
            return await _freeSql.Update<AIModelEntity>()
                .Set(m => new AIModelEntity { IsDefault = true })
                .Where(m => m.Id == id)
                .ExecuteAffrowsAsync() > 0;
        }

        // 将注册表中的模型迁移到数据库（一次性操作）
        public async Task MigrateModelsFromRegistryAsync()
        {
            var registryModels = Config.RegistryModelConfig.GetModels();
            if (registryModels.Count == 0) return;

            // 检查数据库中是否已有模型配置
            var existingModels = await _freeSql.Queryable<AIModelEntity>().CountAsync();
            if (existingModels > 0) return; // 已有模型配置，不再迁移

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
                    IsDefault = model == registryModels[0] // 第一个模型设为默认
                });
            }

            _freeSql.Insert(models).ExecuteAffrows(); // 批量插入（或更新）模型配置（一次性操作）
        }

        #endregion


        // 获取历史聊天记录
        public async Task<List<ChatSessionEntity>> GetHistorySessionAsync(int count = 100)
        {
            return await _freeSql.Select<ChatSessionEntity>()
                .OrderByDescending(x => x.ModifyTime)
                .Take(count)
                .ToListAsync();
        }
        // 添加聊天记录
        public async Task<long> SaveMessageAsync(ChatSessionEntity session)
        {
            return await _freeSql.Insert(session).ExecuteIdentityAsync();
        }

        // 添加消息
        public async Task SaveMessageAsync(ChatMessageEntity message)
        {
            await _freeSql.Insert(message).ExecuteAffrowsAsync();
        }

        // 获取历史消息
        public async Task<List<ChatMessageEntity>> GetHistoryMessagesAsync(string sessionId, int count = 100)
        {
            return await _freeSql.Select<ChatMessageEntity>().Where(x => x.SessionId == sessionId)
                .OrderByDescending(x => x.CreateTime)
                .Take(count)
                .ToListAsync();
        }

        // 获取历史消息
        public async Task<List<ChatMessageEntity>> GetMessageListAsync(string sessionId)
        {
            return await _freeSql.Select<ChatMessageEntity>().Where(x => x.SessionId == sessionId).OrderByDescending(x => x.Id).Take(10).ToListAsync();
        }

        // 修改会话时间
        public async Task UpdateModifyTimeAsync(string sessionId)
        {
            _ = await _freeSql.Update<ChatSessionEntity>().Set(x => x.ModifyTime, DateTime.Now).Where(x => x.Id == long.Parse(sessionId)).ExecuteAffrowsAsync();
        }
        // 获取没有会话的消息
        public async Task<List<ChatMessageEntity>> GetNoSessionMessageListAsync()
        {
            return await _freeSql.Select<ChatMessageEntity>().Where(x => x.SessionId == null || x.SessionId == "").ToListAsync();
        }
        // 修改消息会话ID
        public async Task UpdateMessageSessionIdAsync(string chatSessionId)
        {
            _ = await _freeSql.Update<ChatMessageEntity>().Set(m => m.SessionId, chatSessionId).Where(m => m.SessionId == null || m.SessionId == "").ExecuteAffrowsAsync();
        }

    }
}
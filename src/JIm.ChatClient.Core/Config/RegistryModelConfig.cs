using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JIm.ChatClient.Core.Models;
using Microsoft.Win32;
using System.Text.Json;
using Newtonsoft.Json;

namespace JIm.ChatClient.Core.Config
{
    /// <summary>
    /// 读取注册表模型配置
    /// </summary>
    public class RegistryModelConfig
    {
        private const string RegistryPath = @"SOFTWARE\JimChat";
        private const string ModelsKey = "AIModels";
        
        public static ObservableCollection<AIModel> GetModels()
        {
            var models = new ObservableCollection<AIModel>();
            
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        var modelsJson = key.GetValue(ModelsKey) as string;
                        if (!string.IsNullOrEmpty(modelsJson))
                        {
                            var modelsList = JsonConvert.DeserializeObject<List<AIModel>>(modelsJson); //JsonSerializer.Deserialize<List<AIModel>>(modelsJson);
                            if (modelsList != null)
                            {
                                foreach (var model in modelsList)
                                {
                                    models.Add(model);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"从注册表读取模型配置失败: {ex.Message}");
            }
            
            // 如果没有从注册表读取到任何模型，添加默认的本地模型
            if (models.Count == 0)
            {
                models.Add(new AIModel
                {
                    Type = "local",
                    Id = "deepseek-r1:7b",
                    DisplayName = "本地 deepseek-r1 7b (默认)",
                    ApiEndpoint = "http://127.0.0.1:11434/api/chat",
                    ApiKey = ""
                });
            }
            
            return models;
        }
        
        public static void SaveModels(IEnumerable<AIModel> models)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        var modelsJson = JsonConvert.SerializeObject(models); //JsonSerializer.Serialize(models);
                        key.SetValue(ModelsKey, modelsJson);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存模型配置到注册表失败: {ex.Message}");
            }
        }
    }
} 
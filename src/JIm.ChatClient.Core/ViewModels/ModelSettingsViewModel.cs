using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JIm.ChatClient.Core.Entity;
using JIm.ChatClient.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace JIm.ChatClient.Core.ViewModels
{
    public partial class ModelSettingsViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;

        [ObservableProperty]
        private ObservableCollection<AIModelEntity> _models = new();

        [ObservableProperty]
        private AIModelEntity _selectedModel;

        [ObservableProperty]
        private AIModelEntity _editingModel;

        [ObservableProperty]
        private bool _isEditDialogOpen;

        [ObservableProperty]
        private bool _isNewModel;

        public ModelSettingsViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            LoadModelsAsync().ConfigureAwait(false);
        }

        private async Task LoadModelsAsync()
        {
            var models = await _databaseService.GetAllModelsAsync();
            Models.Clear();
            foreach (var model in models)
            {
                Models.Add(model);
            }
        }

        [RelayCommand]
        private void AddNewModel()
        {
            EditingModel = new AIModelEntity
            {
                Type = "local",
                ModelId = "",
                DisplayName = "",
                ApiEndpoint = "http://127.0.0.1:11434/api/chat",
                ApiKey = "",
                IsDefault = false
            };
            IsNewModel = true;
            IsEditDialogOpen = true;
        }

        [RelayCommand]
        private void EditModel(AIModelEntity model)
        {
            if (model == null) return;
            
            // 创建一个副本进行编辑，避免直接修改原对象
            EditingModel = new AIModelEntity
            {
                Id = model.Id,
                Type = model.Type,
                ModelId = model.ModelId,
                DisplayName = model.DisplayName,
                ApiEndpoint = model.ApiEndpoint,
                ApiKey = model.ApiKey,
                IsDefault = model.IsDefault,
                CreateTime = model.CreateTime,
                UpdateTime = model.UpdateTime
            };
            IsNewModel = false;
            IsEditDialogOpen = true;
        }

        [RelayCommand]
        private async Task SaveModelAsync()
        {
            if (EditingModel == null) return;
            
            if (string.IsNullOrWhiteSpace(EditingModel.ModelId) ||
                string.IsNullOrWhiteSpace(EditingModel.DisplayName) ||
                string.IsNullOrWhiteSpace(EditingModel.ApiEndpoint))
            {
                // 显示错误消息
                return;
            }
            
            if (IsNewModel)
            {
                await _databaseService.AddModelAsync(EditingModel);
            }
            else
            {
                await _databaseService.UpdateModelAsync(EditingModel);
            }
            
            IsEditDialogOpen = false;
            await LoadModelsAsync();
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditDialogOpen = false;
        }

        [RelayCommand]
        private async Task DeleteModelAsync(AIModelEntity model)
        {
            if (model == null) return;
            
            // 确认删除
            // 这里可以添加确认对话框
            
            if (await _databaseService.DeleteModelAsync(model.Id))
            {
                await LoadModelsAsync();
            }
            else
            {
                // 显示错误消息：无法删除默认模型
            }
        }

        [RelayCommand]
        private async Task SetAsDefaultAsync(AIModelEntity model)
        {
            if (model == null || model.IsDefault) return;
            
            if (await _databaseService.SetDefaultModelAsync(model.Id))
            {
                await LoadModelsAsync();
            }
        }
    }
}
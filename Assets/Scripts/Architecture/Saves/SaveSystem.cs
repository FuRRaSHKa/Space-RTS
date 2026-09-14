using System.Collections.Generic;
using System;
using HalloGames.Architecture.Saves.Data;
using UnityEngine.Assertions;
using Cysharp.Threading.Tasks;
using HalloGames.Architecture.User;
using UnityEngine;
using HalloGames.Architecture.Events;
using HalloGames.Architecture.Services;

namespace HalloGames.Architecture.Saves
{
    public class SaveSystem : IDisposable
    {
        private const string DEFAULT_SAVE_VERSION = "1.0.0";

        private readonly SaveStorage _saveStorage;
        private readonly SaveRegistry _saveRegistry;
        private readonly IUserIdProvider _userIdProvider;
        private readonly IServiceRegistry _serviceRegistry;

        private GameSavePair _savePair;
        private Dictionary<Type, BaseSave> _saves;

        private IDisposable _saveSubscription;

        internal SaveSystem(SaveStorage saveStorage, SaveRegistry saveRegistry, IUserIdProvider userIdProvider, IServiceRegistry serviceRegistry)
        {
            _saveStorage = saveStorage;
            _saveRegistry = saveRegistry;
            _userIdProvider = userIdProvider;
            _serviceRegistry = serviceRegistry;

            Assert.IsNotNull(_saveStorage);
            Assert.IsNotNull(_saveRegistry);
            Assert.IsNotNull(_userIdProvider);
            Assert.IsNotNull(_serviceRegistry);
        }

        public async UniTask Init()
        {
            await LoadData();
            InitSaveData();
            Start();
        }

        private void Start()
        {
            foreach (var save in _saves.Values)
            {
                save.Start();
            }

            _saveSubscription = EventsManager.Subscribe<SaveRequestedEvent>(OnSaveRequested);
        }

        private void OnSaveRequested(SaveRequestedEvent saveRequestedEvent)
        {
            Save().Forget();
        }

        private async UniTask LoadData()
        {
            var data = await _saveStorage.Load(_userIdProvider.UserId);
            if (data == null)
                data = CreateDefaultSavePair();

            _savePair = data;
        }


        private async UniTask Save()
        {
            await _saveStorage.Save(_userIdProvider.UserId, _savePair.GameSaveContainer, _savePair.GameSaveData);
        }

        private GameSavePair CreateDefaultSavePair()
        {
            var container = new GameSaveContainer()
            {
                Version = DEFAULT_SAVE_VERSION,
                ApplicationVersion = Application.version
            };

            var saveData = new GameSaveData();

            var pair = new GameSavePair(container, saveData);
            return pair;
        }

        private void InitSaveData()
        {
            _saves = new Dictionary<Type, BaseSave>();

            var saves = _saveRegistry.Saves.Values;
            foreach (var save in saves)
            {
                AddSave(save);
            }

        }

        private void AddSave(BaseSave save)
        {
            if (_savePair.GameSaveData.Datas.TryGetValue(save.SaveName, out var data))
                save.ApplyData(data);
            else
            {
                data = save.CreateAndApplyDefaultSave();
                _savePair.GameSaveData.Datas.Add(save.SaveName, data);
            }

            var type = save.SaveType;
            _saves.Add(type, save);
            _serviceRegistry.AddService(save.GetType(), save);
        }

        public void Dispose()
        {
            foreach(var save in _saves.Values)
            {
                _serviceRegistry.RemoveService(save.GetType());
            }

            _saveSubscription.Dispose();    
        }
    }
}

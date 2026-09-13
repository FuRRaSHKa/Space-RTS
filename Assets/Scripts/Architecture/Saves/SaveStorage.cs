using Cysharp.Threading.Tasks;
using HalloGames.Architecture.Saves.Data;
using HalloGames.Architecture.Saves.ReadWrite;
using HalloGames.Architecture.Saves.Serialize;
using System;

namespace HalloGames.Architecture.Saves
{
    internal class SaveStorage
    {
        private readonly IReadWriteProvider _localProvider;
        private readonly ISaveSerializer _saveSerializer;

        internal SaveStorage(IReadWriteProvider localProvider, ISaveSerializer saveSerializer)
        {
            _localProvider = localProvider;
            _saveSerializer = saveSerializer;
        }

        internal async UniTask<GameSavePair> Load(string userId)
        {
            var pair = await LoadSavePair(() => _localProvider.ReadMainSave(userId));
            if (pair == null)
                pair = await LoadSavePair(() => _localProvider.ReadBackupSave(userId));

            return pair;
        }

        private async UniTask<GameSavePair> LoadSavePair(Func<UniTask<byte[]>> ReadFunc)
        {
            var container = await LoadSaveContainer(ReadFunc);
            if (container == null)
                return null;

            try
            {
                var data = _saveSerializer.Deserialize(container.SaveData);
                var saveData = data as GameSaveData;
                if (saveData == null)
                    return null;

                return new GameSavePair(container, saveData);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                return null;
            }
        }

        private async UniTask<GameSaveContainer> LoadSaveContainer(Func<UniTask<byte[]>> readFunc)
        {
            try
            {
                byte[] file = await readFunc();
                if (file == null || file.Length == 0)
                    return null;

                var container = _saveSerializer.Deserialize(file) as GameSaveContainer;
                if (container != null && SaveValidator.IsSaveValid(container))
                    return container;

                return null;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                return null;
            }
        }

        internal async UniTask Save(string userId, GameSaveContainer gameSaveData, GameSaveData newData)
        {
            var bytes = _saveSerializer.Serialize(newData);
            gameSaveData.SaveData = bytes;
            SaveValidator.ValidateData(gameSaveData);

            bytes = _saveSerializer.Serialize(gameSaveData);
            await _localProvider.WriteSave(userId, bytes);
        }
    }
}

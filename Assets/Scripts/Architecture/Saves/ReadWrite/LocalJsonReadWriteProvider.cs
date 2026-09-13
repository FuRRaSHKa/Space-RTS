using Cysharp.Threading.Tasks;
using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace HalloGames.Architecture.Saves.ReadWrite
{
    public class LocalJsonReadWriteProvider : IReadWriteProvider
    {
        private const string SAVE_DATA_NAME = "Save.json";
        private const string TEMP_DATA_NAME = "Save.json.tmp";
        private const string BACKUP_DATA_NAME = "Save.json.bak";

        public UniTask Delete(string userId, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        private async UniTask<byte[]> TryLoadAsync(string path, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                return await File.ReadAllBytesAsync(path, cancellationToken);
            }
            catch (Exception ex) 
            {
                Debug.LogException(ex);
                return null;
            }
        }

        public async UniTask WriteSave(string userId, byte[] bytes, CancellationToken cancellationToken = default)
        {
            var dataPath = Application.persistentDataPath;
            var saveDirectory = Path.Combine(dataPath, userId);

            var mainPath = Path.Combine(saveDirectory, SAVE_DATA_NAME);
            var tempPath = Path.Combine(saveDirectory, TEMP_DATA_NAME);
            var backupPath = Path.Combine(saveDirectory, BACKUP_DATA_NAME);

            Directory.CreateDirectory(saveDirectory);

            await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);

            if (File.Exists(mainPath))
                File.Replace(tempPath, mainPath, backupPath);
            else
                File.Move(tempPath, mainPath);
        }

        public async UniTask<byte[]> ReadMainSave(string userId, CancellationToken cancellationToken = default)
        {
            var dataPath = Application.persistentDataPath;
            var saveDirectory = Path.Combine(dataPath, userId);

            var mainPath = Path.Combine(saveDirectory, SAVE_DATA_NAME);

            return await TryLoadAsync(mainPath, cancellationToken);
        }

        public async UniTask<byte[]> ReadBackupSave(string userId, CancellationToken cancellationToken = default)
        {
            var dataPath = Application.persistentDataPath;
            var saveDirectory = Path.Combine(dataPath, userId);
            var backupPath = Path.Combine(saveDirectory, BACKUP_DATA_NAME);

            return await TryLoadAsync(backupPath, cancellationToken);
        }
    }
}
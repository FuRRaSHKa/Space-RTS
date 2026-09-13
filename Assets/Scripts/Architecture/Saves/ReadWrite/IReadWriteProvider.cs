using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace HalloGames.Architecture.Saves.ReadWrite
{
    internal interface IReadWriteProvider
    {
        UniTask WriteSave(string userId, byte[] bytes, CancellationToken cancellationToken = default);
        UniTask<byte[]> ReadMainSave(string userId, CancellationToken cancellationToken = default);
        UniTask<byte[]> ReadBackupSave(string userId, CancellationToken cancellationToken = default);
        UniTask Delete(string userId, CancellationToken cancellationToken = default);
    }
}
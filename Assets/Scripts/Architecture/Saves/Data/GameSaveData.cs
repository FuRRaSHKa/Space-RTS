using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.Architecture.Saves.Data
{
    [System.Serializable]
    public class GameSaveContainer
    {
        public string Version;
        public string ApplicationVersion;
        public string Checksum;

        public byte[] SaveData;
    }

    public class GameSavePair
    {
        public GameSaveContainer GameSaveContainer { get; }
        public GameSaveData GameSaveData { get; }

        public GameSavePair(GameSaveContainer gameSaveContainer, GameSaveData gameSaveData)
        {
            GameSaveContainer = gameSaveContainer;
            GameSaveData = gameSaveData;
        }
    }

    public class GameSaveData : SaveData
    {
        public Dictionary<string, object> _datas;
    }
}
using HalloGames.Architecture.Saves.Data;
using System;
using System.Security.Cryptography;
using Unity.VisualScripting;

namespace HalloGames.Architecture.Saves
{
    public class SaveValidator
    {
        public static bool IsSaveValid(GameSaveContainer gameSaveContainer)
        {
            if(gameSaveContainer == null) 
                return false;

            var sha = SHA256.Create();
            var hash = sha.ComputeHash(gameSaveContainer.SaveData);
            string hashString = BitConverter.ToString(hash).Replace("-", "");

            return string.Equals(gameSaveContainer.Checksum, hashString);
        }
        
        public static void ValidateData(GameSaveContainer gameSaveContainer) 
        {
            if (gameSaveContainer == null)
                throw new Exception("Try to validate null container");

            var sha = SHA256.Create();
            var hash = sha.ComputeHash(gameSaveContainer.SaveData);
            string hashString = BitConverter.ToString(hash).Replace("-", "");
            gameSaveContainer.Checksum = hashString;
        }
    }
}
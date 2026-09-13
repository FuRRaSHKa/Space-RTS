using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.Architecture.Saves.Serialize
{
    public interface ISaveSerializer
    {
        public byte[] Serialize(object @object);
        public object Deserialize(byte[] bytes);
    }
}
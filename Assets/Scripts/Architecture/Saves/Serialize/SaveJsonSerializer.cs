using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace HalloGames.Architecture.Saves.Serialize
{
    public class SaveJsonSerializer : ISaveSerializer
    {
        public byte[] Serialize(object @object)
        {
            var json = JsonConvert.SerializeObject(@object);
            return Encoding.UTF8.GetBytes(json);
        }

        public object Deserialize(byte[] bytes)
        {
            var json = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<object>(json);
        }
    }
}

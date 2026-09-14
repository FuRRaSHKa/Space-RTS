using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.Architecture.User 
{
    public interface IUserIdProvider
    {
        public string UserId { get; }
    }
}


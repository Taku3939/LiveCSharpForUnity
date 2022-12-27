using System;
using UnityEngine;

namespace LiveClient
{
    public class User
    {
        public ulong Id { get; }
        public GameObject Go { get; }

        public User(ulong id, GameObject go)
        {
            Id = id;
            Go = go;
        }
    }
}
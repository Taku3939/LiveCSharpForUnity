using System;
using UnityEngine;

namespace LiveClient
{
    public class User
    {
        public Guid Id { get; }
        public GameObject Go { get; }

        public User(Guid id, GameObject go)
        {
            Id = id;
            Go = go;
        }
    }
}
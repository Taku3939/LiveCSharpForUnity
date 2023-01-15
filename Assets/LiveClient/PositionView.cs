using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using LiveCoreLibrary.Commands;
using LiveCoreLibrary.View;
using UnityEngine;

namespace LiveClient.View
{
    public class PositionView : UdpView<PositionPacket>
    {
        public Transform character;
        public Transform target;
        [SerializeField] private ulong id;
        [SerializeField] private Entry _entry;

        private void Start()
        {
            // id = (ulong)new System.Random().Next();
            id = _entry.userId;
        }

        public async void Update()
        {
            var udpCommand = new PositionPacket(
                id,
                character.position.x,
                character.position.y,
                character.position.z,
                character.rotation.x,
                character.rotation.y,
                character.rotation.z,
                character.rotation.w
            );
            SendUdp(udpCommand);
            await Task.Delay(33);
        }

        protected override void OnPacketReceived(PositionPacket command)
        {
            Debug.Log("received Id : " + command.Id);

            var t = this.target;
            t.position = new Vector3(command.X, command.Y, command.Z);
            t.rotation = new Quaternion(command.Qx, command.Qy, command.Qz, command.Qw);
        
        }
    }
}
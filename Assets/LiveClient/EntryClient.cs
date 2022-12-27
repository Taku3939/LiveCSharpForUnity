using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiveCoreLibrary;
using LiveCoreLibrary.Client;
using LiveCoreLibrary.Commands;
using LiveCoreLibrary.Messages;
using LiveCoreLibrary.Utility;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace LiveClient
{
    [RequireComponent(typeof(MonoLiveNetwork))]
    public class EntryClient : MonoBehaviour
    {
     
        public string roomName = "Test";

        [SerializeField] private Transform self;
     
        // private ConcurrentDictionary<ulong, User> _userHolder = new ConcurrentDictionary<ulong, User>();
        private User selfUser;

        [SerializeField] private string tcpIpField = "127.0.0.1";
        [SerializeField] private int tcpPortField = 25565;
        [SerializeField] private string udpIpField = "127.0.0.1";
        [SerializeField] private int udpPortField = 25561;
        public string chatInputField;

        private MonoLiveNetwork _liveNetwork;
        private void Start()
        {
            _liveNetwork = this.GetComponent<MonoLiveNetwork>();
            // IDの生成
            var userId = (ulong)new Random().Next();
            selfUser = new User(userId, self.gameObject);
            
            // コアシステムにイベントの追加
            _liveNetwork.OnMessageReceivedUdp += OnMessageReceivedUdp;
            _liveNetwork.OnMessageReceivedTcp += OnMessageReceivedTcp;
            _liveNetwork.OnConnected += OnConnected;
            _liveNetwork.OnJoin += OnJoin;
            _liveNetwork.OnLeave += OnLeave;

        }

        public async void Connect()
        {
            if (!_liveNetwork.IsConnected)
            {
                var task = await _liveNetwork.ConnectTcp(tcpIpField, tcpPortField);
                Debug.Log(task);
            }
        }


        public async void OnJoin(ulong id)
        {
            Debug.Log("join is " + id);
            await _liveNetwork.HolePunching();
        }

        public void OnLeave(ulong id)
        {
            // _userHolder.TryRemove(id, out var data);
            Debug.Log("leave is " + id);
        }

        public void OnConnected()
        {
            _liveNetwork.Join(selfUser.Id, roomName);
            _liveNetwork.ConnectUdp(udpIpField, udpPortField);
        }

        public void OnMessageReceivedTcp(ReceiveData message)
        {
            var command = message.TcpCommand;
            switch (command)
            {
                case ChatPacket x:
                    //chatField += x.Message + "\n";
                    break;
            }
        }

        public void OnMessageReceivedUdp(IUdpCommand command)
        {
            switch (command)
            {
                case PositionPacket x:
                    //
                    // Debug.Log("Id : " + x.Id);
                    // if (_userHolder.TryGetValue(x.Id, out var user) && user.Go)
                    // {
                    //     var t = user.Go.transform;
                    //     t.position = new Vector3(x.X, x.Y, x.Z);
                    //     t.rotation = new Quaternion(x.Qx, x.Qy, x.Qz, x.Qw);
                    // }

                    break;
            }
        }

        private async void Update()
        {
            if (_liveNetwork.IsConnected)
            {
                //SendChat("uouo");
                // Send Position
                IUdpCommand command = new PositionPacket(
                    selfUser.Id,
                    self.position.x,
                    self.position.y,
                    self.position.z,
                    self.rotation.x,
                    self.rotation.y,
                    self.rotation.z,
                    self.rotation.w);

                await _liveNetwork.SendClients(command);

                await Task.Delay(33);
            }
        }
        
        public void SendChat(string message)
        {
            ITcpCommand chat = new ChatPacket(selfUser.Id, message);
            _liveNetwork.Send(chat);
        }

        public void OnApplicationQuit()
        {
            LiveNetwork.Instance.Close();
        }
    }
}
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
    class Entry : MonoBehaviour
    {
        [SerializeField] private Button connectButton;
        public string roomName = "Test";

        [SerializeField] private Transform self;
        [SerializeField] private GameObject prefab;
        private ConcurrentDictionary<ulong, User> _userHolder = new ConcurrentDictionary<ulong, User>();
        private User selfUser;

        [SerializeField] private InputField tcpIpField;
        [SerializeField] private InputField tcpPortField;
        [SerializeField] private InputField udpIpField;
        [SerializeField] private InputField udpPortField;
        [SerializeField] private Text chatField;
        [SerializeField] private InputField chatInputField;
        [SerializeField] private Button chatButton;


        public ulong userId;
        private void Start()
        {
            // IDの生成
            userId = (ulong)new Random().Next();
            selfUser = new User(userId, self.gameObject);
            
            // コアシステムにイベントの追加
            LiveNetwork.Instance.OnMessageReceivedUdp += OnMessageReceivedUdp;
            LiveNetwork.Instance.OnMessageReceivedTcp += OnMessageReceivedTcp;
            LiveNetwork.Instance.OnConnected += OnConnected;
            LiveNetwork.Instance.OnJoin += OnJoin;
            LiveNetwork.Instance.OnLeave += OnLeave;
            
            //ボタンにイベント追加
            connectButton.onClick.AddListener(Connect);
            chatButton.onClick.AddListener(() => SendChat(chatInputField.text));
            
        }

        public async void Connect()
        {
            if (!LiveNetwork.Instance.IsConnected)
            {
                var task = await LiveNetwork.Instance.ConnectTcp(tcpIpField.text, int.Parse(tcpPortField.text));
                Debug.Log(task);
            }
        }


        public async void OnJoin(ulong id)
        {
            Debug.Log("join is " + id);
            await LiveNetwork.Instance.HolePunching();
        }

        public void OnLeave(ulong id)
        {
            _userHolder.TryRemove(id, out var data);
            Debug.Log("leave is " + id);
        }

        public void OnConnected()
        {
            LiveNetwork.Instance.Join(selfUser.Id, roomName);
            LiveNetwork.Instance.ConnectUdp(udpIpField.text, int.Parse(udpPortField.text));
        }

        public void OnMessageReceivedTcp(ReceiveData message)
        {
            var command = message.TcpCommand;
            switch (command)
            {
                case ChatPacket x:
                    chatField.text += x.Message + "\n";
                    break;

                case EndPointPacketHolder x:
                    // 削除
                    var rmUser = GetRmUser(x);
                    foreach (var id in rmUser)
                        if (_userHolder.TryRemove(id, out var user))
                        {
                            Destroy(user.Go);
                        }


                    foreach (var packet in x.EndPointPackets)
                    {
                        Debug.Log("uo : " + packet.Id);
                        var isContain = _userHolder.ContainsKey(packet.Id);
                        if (isContain) continue;

                        GameObject newGo = Instantiate(prefab, this.transform);
                        _userHolder.TryAdd(packet.Id, new User(packet.Id, newGo));
                    }

                    break;
            }
        }

        public void OnMessageReceivedUdp(IUdpCommand command)
        {
            switch (command)
            {
                case PositionPacket x:

                    Debug.Log("Id : " + x.Id);
                    if (_userHolder.TryGetValue(x.Id, out var user) && user.Go)
                    {
                        var t = user.Go.transform;
                        t.position = new Vector3(x.X, x.Y, x.Z);
                        t.rotation = new Quaternion(x.Qx, x.Qy, x.Qz, x.Qw);
                    }

                    break;
            }
        }

        public List<ulong> GetRmUser(EndPointPacketHolder endPointPacketHolder)
        {
            //
            List<ulong> ids = new List<ulong>();
            foreach (var userHolderKey in _userHolder.Keys)
            {
                bool isContain = false;
                foreach (var packet in endPointPacketHolder.EndPointPackets)
                {
                    if (packet.Id == userHolderKey)
                    {
                        isContain = true;
                    }
                }

                //現在登録されているユーザが存在しない場合
                if (!isContain) ids.Add(userHolderKey);
            }

            return ids;
        }


        private async void Update()
        {
            if (LiveNetwork.Instance.IsConnected)
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

                await LiveNetwork.Instance.SendClients(command);

                await Task.Delay(33);

                // await LiveNetwork.Instance.HolePunching();
            }
        }

        // public static async void ReConnect()
        // {
        //     LiveNetwork.Instance.Leave();
        //     await Task.Delay(2000);
        //     LiveNetwork.Instance.Close();
        //     await Task.Delay(2000);
        //     await LiveNetwork.Instance.ConnectTcp(tcpHost, tcpPort);
        //     LiveNetwork.Instance.Join(userId, roomName);
        //     LiveNetwork.Instance.ConnectUdp(udpHost, udpPort);
        // }

        private void SendChat(string message)
        {
            ITcpCommand chat = new ChatPacket(selfUser.Id, message);
            LiveNetwork.Instance.Send(chat);
        }

        public void OnApplicationQuit()
        {
            LiveNetwork.Instance.Close();
        }
    }
}
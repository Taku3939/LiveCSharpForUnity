using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiveCoreLibrary;
using LiveCoreLibrary.Commands;
using LiveCoreLibrary.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace LiveClient
{
    class Entry : MonoBehaviour
    {
        [SerializeField] private Button connectButton;
        public string roomName = "Test";

        [SerializeField] private Transform self;
        [SerializeField] private GameObject prefab;
        private ConcurrentDictionary<Guid, User> _userHolder = new ConcurrentDictionary<Guid, User>();
        private User selfUser;

        [SerializeField] private InputField tcpIpField;
        [SerializeField] private InputField tcpPortField;
        [SerializeField] private InputField udpIpField;
        [SerializeField] private InputField udpPortField;
        [SerializeField] private Text chatField;
        [SerializeField] private InputField chatInputField;
        [SerializeField] private Button chatButton;

        private void Start()
        {
            var userId = Guid.NewGuid();
            selfUser = new User(userId, self.gameObject);
            LiveNetwork.Instance.OnMessageReceivedUdp += OnMessageReceivedUdp;
            LiveNetwork.Instance.OnMessageReceivedTcp += OnMessageReceivedTcp;
            LiveNetwork.Instance.OnConnected += OnConnected;
            LiveNetwork.Instance.OnJoin += OnJoin;
            LiveNetwork.Instance.OnLeave += (id) => { _userHolder.TryRemove(id, out var data); };
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


        public async void OnJoin(Guid id)
        {
            Debug.Log("join is " + id);
            await LiveNetwork.Instance.HolePunching();
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
                        Debug.Log("uo : " + packet.Guid);
                        var isContain = _userHolder.ContainsKey(packet.Guid);
                        if (isContain) continue;

                        GameObject newGo = Instantiate(prefab, this.transform);
                        _userHolder.TryAdd(packet.Guid, new User(packet.Guid, newGo));
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

        public List<Guid> GetRmUser(EndPointPacketHolder endPointPacketHolder)
        {
            //
            List<Guid> ids = new List<Guid>();
            foreach (var userHolderKey in _userHolder.Keys)
            {
                bool isContain = false;
                foreach (var packet in endPointPacketHolder.EndPointPackets)
                {
                    if (packet.Guid == userHolderKey)
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

        // public async void ReConnect()
        // {
        //     LiveNetwork.Instance.Close();
        //     await LiveNetwork.Instance.ConnectTcp(tcpHost, tcpPort);
        //     LiveNetwork.Instance.Join(selfUser.Id, roomName);
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
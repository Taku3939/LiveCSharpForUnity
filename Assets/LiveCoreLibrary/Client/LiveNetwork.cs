﻿using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LiveCoreLibrary.Commands;
using LiveCoreLibrary.Messages;
using UnityEngine;

namespace LiveCoreLibrary.Client
{
    public class LiveNetwork
    {
        public bool IsConnected => _tcp is { IsConnected: true };

        private static LiveNetwork _instance;
        public static LiveNetwork Instance => _instance ??= new LiveNetwork();

        private Tcp _tcp;
        private Udp _udp;
        public EndPointPacketHolder P2PClients { get; private set; }
        public event Action<ReceiveData> OnMessageReceivedTcp;
        public event Action<IUdpCommand> OnMessageReceivedUdp;
        public event Action OnConnected;
        public event Action<ulong> OnJoin;
        public event Action<ulong> OnLeave;
        public event Action<EndPointPacketHolder> OnReceiveUsers;
        public event Action OnClose;
        public ulong UserId { get; private set; }
        private SynchronizationContext _context;

        public LiveNetwork()
        {
            _context = SynchronizationContext.Current;
        }

        /// <summary>
        /// Udpホールパンチングにより、Portの開放を行う
        /// </summary>
        public async Task HolePunching()
        {
            var hostName = Dns.GetHostName();
            var addresses = await Dns.GetHostAddressesAsync(hostName);   
            
            var result = addresses
                .Where(y => y.AddressFamily.Equals(AddressFamily.InterNetwork))
                .Select(y => y.ToString())
                .ToArray();
            IUdpCommand endPointPacket = new HolePunchingPacket(this.UserId, result);
            if (_udp != null) await _udp.SendServer(endPointPacket);
        }

        /// <summary>
        /// Udp送信
        /// </summary>
        /// <param name="udpCommand"></param>
        public async Task SendClients(IUdpCommand udpCommand)
        {
            if (_udp != null) await _udp.SendClients(udpCommand, P2PClients);
        }

        /// <summary>
        /// Tcp送信
        /// </summary>
        /// <param name="tcpCommand"></param>
        public void Send(ITcpCommand tcpCommand)
        {
            _tcp.SendAsync(tcpCommand);
        }

        /// <summary>
        /// ルームに入る
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roomName"></param>
        public void Join(ulong userId, string roomName)
        {
            this.UserId = userId;
            ITcpCommand join = new Join(userId, roomName, "");
            _tcp.SendAsync(join);
        }

        /// <summary>
        /// サーバー側未実装
        /// </summary>
        public void Leave()
        {
            ITcpCommand leave = new Leave(UserId);
            _tcp.SendAsync(leave);
            P2PClients = null;
            UserId = 0;
        }

        /// <summary>
        /// サーバに接続する
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public async Task<bool> ConnectTcp(string host, int port)
        {
            // Tcp
            _tcp = new Tcp();

            //イベント登録
            _tcp.OnMessageReceived += OnMessageReceived;
            _tcp.OnConnected += OnConnect;
            _tcp.OnDisconnected += OnDisconnected;
            _tcp.OnClose += OnClosed;
            // 接続するまで待機

            return await _tcp.ConnectAsync(host, port);
        }

        private void OnClosed()
        {
            _context.Post((e) => OnClose?.Invoke(), null);
        }


        /// <summary>
        /// サーバーにUdp接続を行う
        /// ConnectTcpとJoinメソッドによって,ルームへ入る必要がある
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public async void ConnectUdp(string host, int port)
        {
            // Udp
            var addresses = await Dns.GetHostAddressesAsync(host);
            var address = addresses[0];
            _udp = new Udp(UserId, new IPEndPoint(address, port));
            _udp.ReceiveLoop(10);
            _udp.Process(10);

            _udp.OnMessageReceived += OnMessageReceivedOfUdp;
            await HolePunching();
        }

        /// <summary>
        /// Close
        /// </summary>
        public void Close()
        {
            if (_udp != null) _udp.Close();
            if (_tcp != null) _tcp.Close();
            
            P2PClients = null;
            UserId = 0;
#if DEBUG
            Console.WriteLine("終了します.");
#endif
        }

        private void OnMessageReceived(ReceiveData receiveData)
        {
            switch (receiveData.TcpCommand)
            {
                case JoinResult x:
                    _context.Post(e => OnJoin?.Invoke(x.UserId), null);
                    break;
                case LeaveResult x:
                    _context.Post(e => OnLeave?.Invoke(x.UserId), null);
                    break;
                case EndPointPacketHolder x:
                    P2PClients = x;
                    _context.Post(e => OnReceiveUsers?.Invoke(x), null);
                    break;
                
                case EmotePacket x:
                    Debug.Log(x.Key + x.UserId.ToString());
                    break;
            }

            Debug.Log("受信した");
            _context.Post((e) => OnMessageReceivedTcp?.Invoke(receiveData), null);
            // OnMessageReceivedTcp?.Invoke(receiveData);
        }


        private void OnMessageReceivedOfUdp(IUdpCommand command)
        {
            _context.Post((e) => OnMessageReceivedUdp?.Invoke(command), null);
        }

        private void OnConnect(IPEndPoint ipEndPoint)
        {
            //Log
            Console.WriteLine($"[CLIENT]{ipEndPoint.Address}:[{ipEndPoint.Port.ToString()}] tcp connect");

            //受信開始
            _tcp.ReceiveStart(100);
            _context.Post((e) => OnConnected?.Invoke(), null);
        }

        private void OnDisconnected()
        {
            if (_tcp != null && _udp != null)
            {
                //_tcp.Close(); // 一応
                _udp.Close();

                Console.WriteLine("一応Close");
            }

            Console.WriteLine($"[CLIENT]disconnect");
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LiveCoreLibrary.Commands;
using MessagePack;
using UnityEngine;

namespace LiveCoreLibrary.Client
{
    public class Udp
    {
        readonly UdpClient _udp;
        private readonly ConcurrentQueue<byte[]> _bufferPool;
        private CancellationTokenSource _cts;
        private IPEndPoint _endPoint;
        public event Action<IUdpCommand> OnMessageReceived;
        private ulong UserId;

        public Udp(ulong userId, IPEndPoint endPoint)
        {
            UserId = userId;
            _cts = new CancellationTokenSource();
            _udp = new UdpClient();

            _endPoint = endPoint;
            // //適当なデータを送信
            // これいらんかも
            string hostname = Dns.GetHostName();
            Dns.GetHostAddressesAsync(hostname).ContinueWith(x =>
            {
                var addresses = 
                    x.Result.
                    Where(y => y.AddressFamily.Equals(AddressFamily.InterNetwork))
                    .Select(y => y.ToString())
                    .ToArray();
                IUdpCommand ping = new HolePunchingPacket(userId, addresses);
                var pingBuf = MessagePackSerializer.Serialize(ping);
                _udp.Client.SendTo(pingBuf, endPoint);
            });
  

            _bufferPool = new ConcurrentQueue<byte[]>();
        }

        public void ReceiveLoop(int interval)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (_cts.IsCancellationRequested) return;

                        while (_udp.Available > 0)
                        {
                            UdpReceiveResult res = await _udp.ReceiveAsync();
                            _bufferPool.Enqueue(res.Buffer);
                        }
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }

                    await Task.Delay(interval);
                }
            }, _cts.Token);
        }

        public void Process(int interval)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (_cts.IsCancellationRequested) return;

                        // サーバーからのp2pリストの更新
                        while (_bufferPool.Count > 0)
                        {
                            if (_bufferPool.TryDequeue(out var buffer))
                            {
                                var command = MessagePackSerializer.Deserialize<IUdpCommand>(buffer);
                                OnMessageReceived?.Invoke(command);
                            }
                        }

                        await Task.Delay(interval);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }, _cts.Token);
        }


   
        public async Task SendClients(IUdpCommand udpCommand, EndPointPacketHolder p2PClients, bool isSelf = false)
        {
            try
            {
                // メッセージのシリアライズ
                var data = MessagePackSerializer.Serialize(udpCommand);
                // 送信先が存在しない場合
                if (p2PClients == null) return;
                if(!p2PClients.GetPacketById(this.UserId, out var selfPacket)) return;
                string str = "";  
                var array = p2PClients.EndPointPackets.Select(x => $"[{x.Address} : {x.Port} : {x.Id}]");
                foreach (var a in array)
                {
                    str += a;
                }
                
                Debug.Log(str);
                
                foreach (var udpEndPoint in p2PClients.EndPointPackets)
                {
                    // あってるかは知らん
                    if (!Utility.Util.IsConnected(_udp.Client))
                        return;

                    // アドレスが自分のグローバルIPだった場合ローカルアドレスにする
                    string address = udpEndPoint.Address;
                    int port = udpEndPoint.Port;

                    
                    //自分に送信しない場合
                    if (!isSelf && port == selfPacket.Port) continue;

                    if (selfPacket.Address == udpEndPoint.Address)
                        foreach (var natAddress in udpEndPoint.NatAddresses)
                        {
                            //Debug.Log($"Send to [{natAddress} : {port}]");
                            await _udp.SendAsync(data, data.Length, natAddress, port);
                        }
                    else
                    {
                        //送信
                        //Debug.Log($"Send to [{address} : {port}]");
                        await _udp.SendAsync(data, data.Length, address, udpEndPoint.Port);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public IPEndPoint GetEndPoint()
        {
            return _udp.Client.LocalEndPoint as IPEndPoint;
        }

        public async Task SendServer(IUdpCommand c)
        {
            try
            {
                var data = MessagePackSerializer.Serialize(c);
                await _udp.SendAsync(data, data.Length, _endPoint);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        public void Close()
        {
            _cts.Cancel();
            _udp.Close();
        }
    }
}
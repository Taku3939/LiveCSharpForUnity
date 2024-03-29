using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LiveCoreLibrary.Commands;
using LiveCoreLibrary.Messages;
using UnityEngine;

namespace LiveCoreLibrary.Client
{
    public class Tcp
    {
        public bool IsDisposed => this.client == null;
        public bool IsConnected => this.client.Connected && this.client != null;
        private TcpClient client;
        private CancellationTokenSource cts;
        private readonly SynchronizationContext _context;

        public event Action<ReceiveData> OnMessageReceived;

        public event Action<IPEndPoint> OnConnected;
        public event Action OnDisconnected;
        public event Action OnClose;

        /// <summary>
        /// This Constructor must call by main thread
        /// </summary>
        public Tcp()
        {
            _context = SynchronizationContext.Current;
            client = new TcpClient();
        }

        /// <summary>
        /// 非同期接続
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public async Task<bool> ConnectAsync(string host, int port)
        {
            if (client == null || IsConnected)
            {
                Close();
                await Task.Delay(100);
                client = new TcpClient();
            }

            try
            {
                var addresses = await Dns.GetHostAddressesAsync(host);
                var address = addresses[0];
                await client.ConnectAsync(address, port);
                cts = new CancellationTokenSource();
                OnConnected?.Invoke(new IPEndPoint(address, port));
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return false;
            }
        }

        /// <summary>
        /// 非同期接続
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public async Task<bool> ConnectAsync(IPAddress host, int port) => await ConnectAsync(host.ToString(), port);

        /// <summary>
        /// 非同期送信
        /// </summary>
        /// <param name="cmd">MessagePack Object</param>
        public void SendAsync(ITcpCommand cmd)
        {
            var data = MessageParser.Encode(cmd);

            if (!client.Connected)
            {
                Console.WriteLine("ClientがCloseしています");
                return;
            }

            SendAsync(data);
        }

        /// <summary>
        /// 非同期送信
        /// </summary>
        /// <param name="serialize">byte array</param>
        private void SendAsync(byte[] serialize)
        {
            if (!client.Connected)
            {
                Console.WriteLine("ClientがCloseしています");
                return;
            }

            var sArgs = new SocketAsyncEventArgs();
            sArgs.SetBuffer(serialize, 0, serialize.Length);
            // tokenって何だろうわからん後で考えとけ
            //sArgs.UserToken = data;
            client.Client.SendAsync(sArgs);
        }

        /// <summary>
        /// 受信開始
        /// </summary>
        public void ReceiveStart(int interval)
        {
            //受信関数キャンセル用のトークンの作成

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        // 受信用関数の停止
                        if (cts.IsCancellationRequested) return;

                        // クライアントがCloseしていた場合
                        if (!client.Connected)
                        {
                            //再接続できるようにしたいね
                            Close();
                            return;
                        }
                        // ネットワークストリームの取得
                        NetworkStream nStream = client.GetStream();

                        if (!nStream.CanRead)
                        {
                            Console.WriteLine(
                                $"WARNING >> {client.Client.RemoteEndPoint} : Can not read this NetworkStream");
                            continue;
                        }

                        // メモリストリーム上に受信したデータの書き込み
                        // ※ 受信データに制限を設けていない
                        using MemoryStream mStream = new MemoryStream();

                        while (nStream.DataAvailable)
                        {
                            byte[] buffer = new byte[256];
                            int i;
                            while ((i = await nStream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
                            {
                                // ストリームに書き込み
                                await mStream.WriteAsync(buffer, 0, i, cts.Token);

                                // Null文字を受信時終了
                                if ((char)buffer[i - 1] == '\0') break;
                            }

                            // Null文字を除いた受信データの取り出し
                            byte[] dist = mStream.ToArray();

                            //　イベント関数のコール
                            if (client.Connected)
                                //if (client.Connected && MessageParser.CheckProtocol(dist))
                            {
                                var command = MessageParser.Decode(dist);
                                OnMessageReceived?.Invoke(new ReceiveData(command, client));

                                switch (command)
                                {
                                    case JoinResult x:
                                        Console.WriteLine(x.UserId);
                                        break;
                                }
                            }
                        }

                        await Task.Delay(interval);
                    }
                    catch (IOException e)
                    {
                        // リモートホストからの切断
                        Console.WriteLine(e);
                        Close();
                        return;
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("ReceiveLoop : Task Canceled");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }, cts.Token);
        }

        /// <summary>
        /// 受信停止
        /// </summary>
        public void ReceiveStop() => cts?.Cancel();


        /// <summary>
        /// 接続状態監視用ループ
        /// 切断時リストからクライアントを削除します
        /// </summary>
        /// <param name="interval"></param>
        public void HealthCheck(int interval)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (cts.IsCancellationRequested) return;
                        if (!client.Connected)
                        {
                            Close();
                        }

                        await Task.Delay(interval, cts.Token);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }, cts.Token);
        }

        /// <summary>
        /// コネクションのクローズ
        /// </summary>
        public void Close()
        {
            //とりあえずここに置いとく
            OnDisconnected?.Invoke();
            ReceiveStop();
            
            client?.Close();
            OnClose?.Invoke();
        }


        /// <summary>
        /// 接続確認用関数
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        private static bool CheckConnected(Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}
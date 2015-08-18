using System;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Baku.KinectForPepper
{
    /// <summary>TCP/IP通信を用いて角度値を送信するコネクタを表します。</summary>
    public class AngleDataSender : IDisposable
    {
        /// <summary>送信すべき角度値の要素数です。</summary>
        public const int JointNumber = RobotJointAngles.JointNumberToUse;
        /// <summary>送信データが角度値であることを接続先に知らせるためのヘッダー文字列を表します。</summary>
        public const string SendAngleDataHeader = "setj";
        /// <summary>接続先からの応答に対して予測される最大の文字列長です。</summary>
        public const int ReceiveDataBufferSize = 1024;

        /// <summary>通信のうちテキストの表現に用いるエンコードです。</summary>
        private static readonly Encoding ConnectionEncoding = Encoding.ASCII;

        private bool _isConnected;
        /// <summary>TcpClientが接続中かどうかを表示します。</summary>
        public bool IsConnected
        {
            get { return _isConnected; }
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    IsConnectedChanged?.Invoke(this, new IsConnectedChangedEventArgs(IsConnected));
                }
            }
        }

        /// <summary>接続先との接続を試みます。</summary>
        public void Connect(string ip, int port)
        {
            if(!IsConnected)
            {
                try
                {
                    _client = new TcpClient();
                    _endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                    _client.Connect(_endPoint);
                    IsConnected = true;
                }
                catch(FormatException ex)
                {
                    Dispose();
                    FailedToConnect?.Invoke(this, new ExceptionMessageEventArgs(ex.Message));
                }
                catch (SocketException ex)
                {
                    Dispose();
                    FailedToConnect?.Invoke(this, new ExceptionMessageEventArgs(ex.Message));
                }
            }
        }

        /// <summary>接続を遮断します。</summary>
        public void Close()
        {
            if(_client != null)
            {
                if (_client.Connected) _client.Close();
                _client = null;
            }
            IsConnected = false;
        }

        /// <summary>接続を遮断します。</summary>
        public void Dispose()
        {
            Close();
        }

        /// <summary>角度値を接続先へ送信します。接続先が無い場合は何もしません。</summary>
        /// <param name="angles">送信する角度値の一覧</param>
        public void SendAngleData(float[] angles)
        {
            if (!IsConnected) return;
            if (angles.Length != JointNumber)
            {
                throw new InvalidOperationException(
                    $"'angles.Length': {JointNumber} expected, but actual value was {angles.Length}"
                    );
            }

            try
            {
                var sendBuffer = new byte[angles.Length * 4 + 4];
                //代入操作を表すヘッダを設定
                byte[] setjHeader = ConnectionEncoding.GetBytes(SendAngleDataHeader);
                Array.Copy(setjHeader, 0, sendBuffer, 0, setjHeader.Length);

                //角度値を順に代入
                for (int i = 0; i < angles.Length; i++)
                {
                    byte[] f = BitConverter.GetBytes(angles[i]);
                    Array.Copy(f, 0, sendBuffer, i * 4 + 4, 4);
                }

                var stream = _client.GetStream();
                stream.Write(sendBuffer, 0, sendBuffer.Length);

                var receiveBuffer = new byte[ReceiveDataBufferSize];
                int resLen = stream.Read(receiveBuffer, 0, receiveBuffer.Length);

                string response = ConnectionEncoding.GetString(receiveBuffer, 0, resLen);

                DataSendCompleted?.Invoke(this, new DataSendCompletedEventArgs(response));
            }
            catch(IOException ex)
            {
                //サーバ側が急に落ちたケースを想定
                Dispose();
                ConnectionDisabled?.Invoke(this, new ExceptionMessageEventArgs(ex.Message));
            }
        }

        public string Address => (_endPoint != null && IsConnected) ? _endPoint.Address.ToString() : String.Empty;
        public int Port => (_endPoint != null && IsConnected) ? _endPoint.Port : -1;

        public event EventHandler<IsConnectedChangedEventArgs> IsConnectedChanged;
        public event EventHandler<DataSendCompletedEventArgs> DataSendCompleted;

        /// <summary>接続操作に失敗すると発火します。</summary>
        public event EventHandler<ExceptionMessageEventArgs> FailedToConnect;
        /// <summary>接続中にサーバ側から切断されると発火します。</summary>
        public event EventHandler<ExceptionMessageEventArgs> ConnectionDisabled;

        private TcpClient _client;
        private IPEndPoint _endPoint;

    }

    public class IsConnectedChangedEventArgs : EventArgs
    {
        public IsConnectedChangedEventArgs(bool isConnected)
        {
            IsConnected = isConnected;
        }
        public bool IsConnected { get; }
    }

    public class DataSendCompletedEventArgs : EventArgs
    {
        public DataSendCompletedEventArgs(string response)
        {
            Response = response;
        }
        public string Response { get; }
    }

    public class ExceptionMessageEventArgs : EventArgs
    {
        public ExceptionMessageEventArgs(string message)
        {
            Message = message;
        }
        public string Message { get; }
    }
}

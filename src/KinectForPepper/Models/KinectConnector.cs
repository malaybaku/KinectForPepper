using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baku.KinectForPepper
{
    /// <summary>Kinectとの直接的な通信を行うエージェントを表します。</summary>
    public class KinectConnector : IDisposable
    {
        public KinectConnector()
        {
            _kinectSensor = KinectSensor.GetDefault();
            _bodyFrameReader = _kinectSensor.BodyFrameSource.OpenReader();

            _bodyFrameReader.FrameArrived += OnFrameArrived;
            _kinectSensor.IsAvailableChanged += OnKinectSensorIsAvailableChanged;

            _kinectSensor.Open();
         }

        public CoordinateMapper CoordinateMapper => _kinectSensor.CoordinateMapper;
        public int DisplayWidth => _kinectSensor.DepthFrameSource.FrameDescription.Width;
        public int DisplayHeight => _kinectSensor.DepthFrameSource.FrameDescription.Height;
        public bool IsKinectSensorAvailable => _kinectSensor.IsAvailable;

        /// <summary>特定のインデックスに割り振られた人だけに追尾するかどうかを取得、設定します。インデックスはFixedBodyIndexの値として設定します。</summary>
        public bool IsBodyIndexFixed { get; set; }
        private int _fixedBodyIndex = 0;
        /// <summary>指定したインデックスに割り振られた人だけを追尾します。このプロパティを有効かするにはIsBodyIndexFixedプロパティをtrueに設定してください。</summary>
        public int FixedBodyIndex
        {
            get { return _fixedBodyIndex; }
            set
            {
                if(value >= 0 && value <= 5)
                {
                    _fixedBodyIndex = value;
                }
            }
        }

        /// <summary>センサが利用可能か不可能かが切り替わると発火します。</summary>
        public event EventHandler<IsAvailableChangedEventArgs> IsAvailableChanged;
        /// <summary>検出しているボーン情報が更新されると発火します。</summary>
        public event EventHandler<BodyEventArgs> BodyUpdated;

        public void Dispose()
        {
            _bodyFrameReader.Dispose();
            _kinectSensor.Close();
        }

        private KinectSensor _kinectSensor;
        private BodyFrameReader _bodyFrameReader;
        private Body[] _bodies;

        private void OnFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (_bodies == null)
                    {
                        _bodies = new Body[bodyFrame.BodyCount];
                    }

                    bodyFrame.GetAndRefreshBodyData(_bodies);
                    dataReceived = true;
                }
            }

            if (!dataReceived) return;

            if (IsBodyIndexFixed)
            {
                Body fixedBody = _bodies[FixedBodyIndex];
                if(fixedBody != null && fixedBody.IsTracked)
                {
                    BodyUpdated?.Invoke(this, new BodyEventArgs(fixedBody));
                }
            }
            else
            {
                Body body = _bodies.FirstOrDefault(b => b.IsTracked);
                if (body != null)
                {
                    BodyUpdated?.Invoke(this, new BodyEventArgs(body));
                }
            }

        }

        private void OnKinectSensorIsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
            => IsAvailableChanged?.Invoke(this, e);

    }

    public class BodyEventArgs : EventArgs
    {
        public BodyEventArgs(Body body)
        {
            Body = body;
        }
        public Body Body { get; }
    }    

}

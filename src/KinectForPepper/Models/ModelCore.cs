using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Baku.KinectForPepper
{
    /// <summary>モデルの寄せ集めになってるクラス</summary>
    public class ModelCore : IDisposable
    {
        public ModelCore()
        {
            //モデルどうしのイベント購読を設定
            AngleDataSender.DataSendCompleted += (_, __) => _fpsWatcherDataSend.Count();
            KinectConnector.BodyUpdated += OnBodyUpdated;

            AngleUpdated += (_, __) => AngleDataSender.SendAngleData(AngleOutputs);
        }

        public event EventHandler AngleUpdated;

        public KinectConnector KinectConnector { get; } = new KinectConnector();
        public AngleDataSender AngleDataSender { get; } = new AngleDataSender();
        public RobotJointAngles RobotJointAngles { get; } = new RobotJointAngles();

        public double FpsDataSend => _fpsWatcherDataSend.FPS;
        public double FpsFrameArrived => _fpsWatcherFrameArrived.FPS;

        public float[] AngleOutputs
        {
            get
            {
                lock(_angleOutputsLock)
                {
                    var result = new float[RobotJointAngles.JointNumberToUse];
                    Array.Copy(_angleOutputs, result, RobotJointAngles.JointNumberToUse);
                    return result;
                }
            }
            set
            {
                lock (_angleOutputsLock)
                {
                    Array.Copy(value, _angleOutputs, RobotJointAngles.JointNumberToUse);
                }
            }
        }
        private object _angleOutputsLock = new object();
        private float[] _angleOutputs = new float[RobotJointAngles.JointNumberToUse];

        private readonly FPSWatcher _fpsWatcherDataSend = new FPSWatcher();
        private readonly FPSWatcher _fpsWatcherFrameArrived = new FPSWatcher();

        private void OnBodyUpdated(object sender, BodyEventArgs e)
        {
            _fpsWatcherFrameArrived.Count();

            RobotJointAngles.SetAnglesFromBody(e.Body);

            AngleOutputs = RobotJointAngles.Angles
                .Select(f => float.IsNaN(f) ? 0.0f : f)
                .ToArray();
            AngleUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            AngleDataSender.Dispose();
            KinectConnector.Dispose();
        }
    }
}

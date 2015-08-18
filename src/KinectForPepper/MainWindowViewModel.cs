using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.TeamFoundation.MVVM;
using System.Linq;

namespace Baku.KinectForPepper
{
    public class MainWindowViewModel : ViewModelBase
    {
        const string CaptionForErrorMessageBox = "KinectForPepper : Error";

        /// <summary>ちょっとブサイクな方法: ここでオブジェクトの依存関係を全部作って終わらす</summary>
        public MainWindowViewModel()
        {
            var connector = new KinectConnector();

            var fpsWatcherDataSend = new FPSWatcher();
            var fpsWatcherFrameArrived = new FPSWatcher();
            var angleDataSender = new AngleDataSender();
            var robotJointAngles = new RobotJointAngles();

            var timerForFps = new DispatcherTimer();
            timerForFps.Interval = TimeSpan.FromMilliseconds(100.0);

            ConnectToServerCommand = new RelayCommand(() => angleDataSender.Connect(IPAddress, Port));
            DisconnectFromServerCommand = new RelayCommand(angleDataSender.Close);

            angleDataSender.IsConnectedChanged += (_, e) =>
            {
                ServerConnectionStatus = e.IsConnected ? "Connected" : "Disconnected";
                IsServerConnected = e.IsConnected;
            };
            angleDataSender.DataSendCompleted += (_, __) =>
            {
                fpsWatcherDataSend.Count();
            };

            timerForFps.Tick += (_, __) =>
            {
                FrameArrivedFramePerSec = $"Data FPS :{fpsWatcherFrameArrived.FPS,3:0.00}";
                DataSendFramePerSec = $"Socket FPS :{fpsWatcherDataSend.FPS,3:0.00}";
            };


            EventHandler<ExceptionMessageEventArgs> eHandler = (_, e) =>
                MessageBox.Show(e.Message, CaptionForErrorMessageBox, MessageBoxButton.OK, MessageBoxImage.Error);

            angleDataSender.FailedToConnect += eHandler;
            angleDataSender.ConnectionDisabled += eHandler;

            connector.IsAvailableChanged += (_, e) =>
            {
                IsKinectConnected = e.IsAvailable;
                KinectConnectionStatus = e.IsAvailable ? "Connected" : "Disconnected";
            };

            connector.BodyUpdated += (_, e) =>
            {
                fpsWatcherFrameArrived.Count();
                robotJointAngles.SetAnglesFromBody(e.Body);

                //filter.Update(robotJointAngles.Angles);
                float[] output = robotJointAngles.Angles
                    .Select(f => float.IsNaN(f) ? 0.0f : f)
                    .ToArray();

                angleDataSender.SendAngleData(output);
            };

            CloseWindowCommand = new RelayCommand(() =>
            {
                angleDataSender.Dispose();
                connector.Dispose();
                timerForFps.Stop();
                OnClosing();
            });

            IsKinectConnected = connector.IsKinectSensorAvailable;
            KinectConnectionStatus = connector.IsKinectSensorAvailable ? "Connected" : "Disconnected";

            ShowViewerCommand = new RelayCommand(this.CreateShowViewerAction(connector));

            timerForFps.Start();
        }

        private Action CreateShowViewerAction(KinectConnector connector)
        {
            return new Action(() =>
            {
                if (KinectBoneViewerViewModel.IsViewerExists) return;

                var vm = new KinectBoneViewerViewModel(connector);
                var viewer = new KinectBoneViewer();
                viewer.DataContext = vm;
                EventHandler onViewModelClosing = (_, __) => viewer.Close();
                vm.Closing += onViewModelClosing;
                //先にビューが閉じた場合はビューモデルを無視しないとClose済みWindowにアクセスしてエラー
                viewer.Closing += (_, __) => vm.Closing -= onViewModelClosing;

                this.Closing += (_, __) => vm.Close();

                viewer.Show();
            });
        }

        private string _ipAddress = "127.0.0.1";
        public string IPAddress
        {
            get { return _ipAddress; }
            set { SetAndRaisePropertyChanged<string>(ref _ipAddress, value); }
        }

        private int _port = 13000;
        public int Port
        {
            get { return _port; }
            set { SetAndRaisePropertyChanged<int>(ref _port, value); }
        }

        private bool _connectOperationEnabled = true;
        public bool ConnectOperationEnabled
        {
            get { return _connectOperationEnabled; }
            set { SetAndRaisePropertyChanged(ref _connectOperationEnabled, value); }
        }

        private bool _disconnectOperationEnabled = false;
        public bool DisconnectOperationEnabled
        {
            get { return _disconnectOperationEnabled; }
            set { SetAndRaisePropertyChanged(ref _disconnectOperationEnabled, value); }
        }

        private bool _isKinectConnected = false;
        public bool IsKinectConnected
        {
            get { return _isKinectConnected; }
            set { SetAndRaisePropertyChanged(ref _isKinectConnected, value); }
        }

        private bool _isServerConnected = false;
        public bool IsServerConnected
        {
            get { return _isServerConnected; }
            set
            {
                SetAndRaisePropertyChanged(ref _isServerConnected, value);
                if(_isServerConnected)
                {
                    ConnectOperationEnabled = false;
                    DisconnectOperationEnabled = true;
                }
                else
                {
                    ConnectOperationEnabled = true;
                    DisconnectOperationEnabled = false;
                }
            }
        }

        private string _kinectConnectionStatus = "Disconnected";
        public string KinectConnectionStatus
        {
            get { return _kinectConnectionStatus; }
            set { SetAndRaisePropertyChanged<string>(ref _kinectConnectionStatus, value); }
        }

        private string _serverConnectionStatus = "Disconnected";
        public string ServerConnectionStatus
        {
            get { return _serverConnectionStatus; }
            set { SetAndRaisePropertyChanged<string>(ref _serverConnectionStatus, value); }
        }

        private string _buttonConnectContent = "接続";
        public string ButtonConnectContent
        {
            get { return _buttonConnectContent; }
            set { SetAndRaisePropertyChanged<string>(ref _buttonConnectContent, value); }
        }

        private string _buttonDisconnectContent = "切断";
        public string ButtonDisconnectContent
        {
            get { return _buttonDisconnectContent; }
            set { SetAndRaisePropertyChanged<string>(ref _buttonDisconnectContent, value); }
        }

        private string _frameArrivedFramePerSec = "-";
        public string FrameArrivedFramePerSec
        {
            get { return _frameArrivedFramePerSec; }
            set { SetAndRaisePropertyChanged<string>(ref _frameArrivedFramePerSec, value); }
        }

        private string _dataSendFramePerSec = "-";
        public string DataSendFramePerSec
        {
            get { return _dataSendFramePerSec; }
            set { SetAndRaisePropertyChanged<string>(ref _dataSendFramePerSec, value); }
        }

        public ICommand ConnectToServerCommand { get; }
        public ICommand DisconnectFromServerCommand { get; }
        public ICommand ShowViewerCommand { get; }
        public ICommand CloseWindowCommand { get; }

        private void OnClosing() => Closing?.Invoke(this, EventArgs.Empty);
        public event EventHandler Closing;
    }
}

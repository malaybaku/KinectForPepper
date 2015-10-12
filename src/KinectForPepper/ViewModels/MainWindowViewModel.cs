using System;
using System.IO;
using System.Linq;

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.TeamFoundation.MVVM;

using Microsoft.Kinect;
using Microsoft.Win32;
using System.ComponentModel;

namespace Baku.KinectForPepper
{
    public class MainWindowViewModel : ViewModelBase
    {
        const string CaptionForErrorMessageBox = "KinectForPepper : Error";

        public MainWindowViewModel()
        {
            _modelCore = new ModelCore();
            _angleLogger = new AngleLogger(_modelCore);

            //描画とFPSの表示設定
            var drawer = new KinectBodyDrawer(_modelCore.KinectConnector);
            ImageSource = drawer.ImageSource;

            var timerForFps = new DispatcherTimer();
            timerForFps.Interval = TimeSpan.FromMilliseconds(100.0);
            timerForFps.Tick += (_, __) =>
            {
                FpsFrameArrived = _modelCore.FpsFrameArrived;
                FpsDataSend = _modelCore.FpsDataSend;
            };

            //イベントとコマンド設定
            SubscribeToModelEvents(_modelCore);
            SendDataChangeToModel(_modelCore);

            ConnectToServerCommand = new RelayCommand(() => _modelCore.AngleDataSender.Connect(IPAddress, Port));
            DisconnectFromServerCommand = new RelayCommand(() => _modelCore.AngleDataSender.Close());

            CloseWindowCommand = new RelayCommand(() =>
            {
                _modelCore.Dispose();
                _angleLogger.Dispose();
                timerForFps.Stop();
            });

            timerForFps.Start();
        }

        #region プロパティ

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

        private double _fpsFrameArrived;
        public double FpsFrameArrived
        {
            get { return _fpsFrameArrived; }
            set { SetAndRaisePropertyChanged(ref _fpsFrameArrived, value); }
        }

        private double _fpsDataSend;
        public double FpsDataSend
        {
            get { return _fpsDataSend; }
            set { SetAndRaisePropertyChanged(ref _fpsDataSend, value); }
        }

        private string _angleLogFilePath = "angles.csv";
        public string AngleLogFilePath
        {
            get { return _angleLogFilePath; }
            set { SetAndRaisePropertyChanged(ref _angleLogFilePath, value); }
        }

        private bool _isStartLoggingEnabled = true;
        public bool IsStartLoggingEnabled
        {
            get { return _isStartLoggingEnabled; }
            set { SetAndRaisePropertyChanged(ref _isStartLoggingEnabled, value); }
        }

        private bool _isEndLoggingEnabled = false;
        public bool IsEndLoggingEnabled
        {
            get { return _isEndLoggingEnabled; }
            set { SetAndRaisePropertyChanged(ref _isEndLoggingEnabled, value); }
        }


        private string _myVectorInfo = " - ";
        public string MyVectorInfo
        {
            get { return _myVectorInfo; }
            set { SetAndRaisePropertyChanged<string>(ref _myVectorInfo, value); }
        }

        private bool _isBodyIndexFixed = false;
        public bool IsBodyIndexFixed
        {
            get { return _isBodyIndexFixed; }
            set
            { SetAndRaisePropertyChanged(ref _isBodyIndexFixed, value); }
        }

        private int _fixedBodyIndex = 0;
        public int FixedBodyIndex
        {
            get { return _fixedBodyIndex; }
            set { SetAndRaisePropertyChanged(ref _fixedBodyIndex, value); }
        }

        public ImageSource ImageSource { get; }

        #endregion

        #region コマンド

        public ICommand ConnectToServerCommand { get; }
        public ICommand DisconnectFromServerCommand { get; }
        public ICommand CloseWindowCommand { get; }

        private ICommand _startLoggingCommand;
        public ICommand StartLoggingCommand
            => _startLoggingCommand ?? (_startLoggingCommand = new RelayCommand(StartLogging));

        private void StartLogging()
        {
            try
            {
                _angleLogger.StartLogging(AngleLogFilePath);
                IsStartLoggingEnabled = false;
                IsEndLoggingEnabled = true;
            }
            catch(InvalidOperationException)
            {
                //既存ファイルの上書きを許可するなら再度トライ
                var res = MessageBox.Show(
                    "指定したファイルは既に存在します。上書きしますか？",
                    "KinectForPepper: ファイル上書き確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Exclamation
                    );
                if (res == MessageBoxResult.Yes)
                {
                    _angleLogger.StartLogging(AngleLogFilePath, true);
                    IsStartLoggingEnabled = false;
                    IsEndLoggingEnabled = true;
                }
            }

        }

        private ICommand _endLoggingCommand;
        public ICommand EndLoggingCommand
            => _endLoggingCommand ?? (_endLoggingCommand = new RelayCommand(EndLogging));

        private void EndLogging()
        {
            _angleLogger.EndLogging();
            IsStartLoggingEnabled = true;
            IsEndLoggingEnabled = false;
        }

        private ICommand _browseSaveFilePathCommand;
        public ICommand BrowseSaveFilePathCommand
            => _browseSaveFilePathCommand ?? (_browseSaveFilePathCommand = new RelayCommand(BrowseSaveFilePath));

        private void BrowseSaveFilePath()
        {
            var dialog = new SaveFileDialog()
            {
                Filter = "CSV file(*.csv)|*.csv"
            };

            bool? res = dialog.ShowDialog();
            if(res.HasValue && res.Value)
            {
                AngleLogFilePath = dialog.FileName;
            }
        }

        #endregion

        private readonly ModelCore _modelCore;
        private readonly AngleLogger _angleLogger;

        private void SubscribeToModelEvents(ModelCore model)
        {
            EventHandler<ExceptionMessageEventArgs> eHandler = (_, e) =>
                MessageBox.Show(e.Message, CaptionForErrorMessageBox, MessageBoxButton.OK, MessageBoxImage.Error);

            model.AngleDataSender.IsConnectedChanged += (_, e) => IsServerConnected = e.IsConnected;
            model.AngleDataSender.FailedToConnect += eHandler;
            model.AngleDataSender.ConnectionDisabled += eHandler;

            model.KinectConnector.IsAvailableChanged += (_, e) => IsKinectConnected = e.IsAvailable;
            model.KinectConnector.BodyUpdated += (_, e) => SetVectorData(e.Body);
            IsKinectConnected = model.KinectConnector.IsKinectSensorAvailable;
        }

        private void SetVectorData(Body body)
        {
            var neck = QuartanionFactory.FromVector4(body.JointOrientations[JointType.Neck].Orientation);
            var spine = QuartanionFactory.FromVector4(body.JointOrientations[JointType.SpineShoulder].Orientation);
            var head = QuartanionFactory.FromVector4(body.JointOrientations[JointType.Head].Orientation);

            var neckZ = Quartanion.UnitZ.RotateBy(neck);
            var spineZ = Quartanion.UnitZ.RotateBy(spine);

            this.MyVectorInfo = "neck Z = " + neckZ.ToString("0.00") + ", spine Z = " + spineZ.ToString("0.00");

            //正規直交基底に使う
            //var spineX = Quartanion.UnitX.RotateBy(spine);
            //var spineY = Quartanion.UnitY.RotateBy(spine);
            //var spineZ = Quartanion.UnitZ.RotateBy(spine);

            //var neckZfromSpine = new Quartanion
            //{
            //    X = neckZ.Product(spineX),
            //    Y = neckZ.Product(spineY),
            //    Z = neckZ.Product(spineZ)
            //};

            //this.MyVectorInfo = neckZfromSpine.ToString("0.00");
        }


        //Viewから飛んできた変更をそのままモデルに書き込む
        private void SendDataChangeToModel(ModelCore model)
        {
            this.PropertyChanged += (_, e) =>
            {
                if(e.PropertyName == nameof(IsBodyIndexFixed))
                {
                    model.KinectConnector.IsBodyIndexFixed = IsBodyIndexFixed;
                }
                else if(e.PropertyName == nameof(FixedBodyIndex))
                {
                    model.KinectConnector.FixedBodyIndex = FixedBodyIndex;
                }
            };
        }
    }
}

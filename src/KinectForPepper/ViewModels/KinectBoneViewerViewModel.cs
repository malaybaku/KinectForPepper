using System.Windows.Input;
using System.Windows.Media;
using Microsoft.TeamFoundation.MVVM;
using System;

namespace Baku.KinectForPepper
{
    public class KinectBoneViewerViewModel : ViewModelBase
    {
        public KinectBoneViewerViewModel(KinectConnector connector)
        {
            IsViewerExists = true;

            var drawer = new KinectBodyDrawer(connector);
            this.ImageSource = drawer.ImageSource;
            connector.BodyUpdated += (_, e) => drawer.Draw(e.Body);

            CloseWindowCommand = new RelayCommand(() => IsViewerExists = false);
        }

        public static bool IsViewerExists { get; private set; }

        public ImageSource ImageSource { get; }
        public ICommand CloseWindowCommand { get; }
        
        /// <summary>ビューを閉じます。</summary>
        public void Close()
        {
            IsViewerExists = false;
            Closing?.Invoke(this, EventArgs.Empty);
        }
        public event EventHandler Closing;

    }
}

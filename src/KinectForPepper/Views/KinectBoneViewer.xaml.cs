using System.Windows;
using System.Windows.Input;

namespace Baku.KinectForPepper
{
    /// <summary>
    /// KinectBoneViewer.xaml の相互作用ロジック
    /// </summary>
    public partial class KinectBoneViewer : Window
    {
        public KinectBoneViewer()
        {
            InitializeComponent();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }

        static KinectBoneViewer _singletonViewer;
        /// <summary>表示中のウィンドウがあればそれを返し、無ければ新しいウィンドウを返します。</summary>
        /// <returns></returns>
        public static KinectBoneViewer GetCurrentViewer()
        {
            if (_singletonViewer == null) _singletonViewer = new KinectBoneViewer();
            _singletonViewer.Closed += (_, __) => _singletonViewer = null;

            return _singletonViewer;
        }
    }
}

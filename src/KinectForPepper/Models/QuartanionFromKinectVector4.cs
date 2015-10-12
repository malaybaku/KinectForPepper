using Microsoft.Kinect;

namespace Baku.KinectForPepper
{
    /// <summary>Kinect APIの型をクオータニオンに変換するファクトリクラスを表します。</summary>
    public static class QuartanionFactory
    {
        /// <summary>KinectアセンブリのVector4構造体を等価なQuartanionに変換します。</summary>
        public static Quartanion FromVector4(Vector4 v)
        {
            return new Quartanion
            {
                W = v.W,
                X = v.X,
                Y = v.Y,
                Z = v.Z
            };
        }

    }
}

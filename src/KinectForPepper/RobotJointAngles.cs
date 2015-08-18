namespace Baku.KinectForPepper
{
    /// <summary>
    /// Pepperの関節角度を表します。
    /// (注: 送信データの量、順序についてPythonサーバ/クライアントと仕様を合わせること)
    /// </summary>
    public class RobotJointAngles
    {

        #region 間接名の列挙

        public float HeadYaw { get; set; }
        public float HeadPitch { get; set; }

        public float LShoulderPitch { get; set; }
        public float LShoulderRoll { get; set; }
        public float LElbowYaw { get; set; }
        public float LElbowRoll { get; set; }
        public float LWristYaw { get; set; }
        public float LHand { get; set; }

        public float HipRoll { get; set; }
        public float HipPitch { get; set; }
        public float KneePitch { get; set; }

        public float RShoulderPitch { get; set; }
        public float RShoulderRoll { get; set; }
        public float RElbowYaw { get; set; }
        public float RElbowRoll { get; set; }
        public float RWristYaw { get; set; }
        public float RHand { get; set; }

        public float WheelFL { get; set; }
        public float WheelFR { get; set; }
        public float WheelB { get; set; }

        #endregion

        public const int JointNumberToUse = 13;

        /// <summary>リモート指示で用いる角度一覧を取得します。</summary>
        public float[] Angles
        {
            get
            {
                return new float[]
                {
                    LShoulderPitch,
                    LShoulderRoll,
                    LElbowYaw,
                    LElbowRoll,
                    LWristYaw,
                    RShoulderPitch,
                    RShoulderRoll,
                    RElbowYaw,
                    RElbowRoll,
                    RWristYaw,
                    HipPitch,
                    LHand,
                    RHand
                };
            }
        }

    }
}

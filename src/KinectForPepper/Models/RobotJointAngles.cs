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

        //NOTE: 首の方向はうまく取れない。

        /// <summary>リモート指示で用いる角度一覧を取得します。</summary>
        public float[] Angles
        {
            get
            {
                return new float[]
                {
                    //HeadYaw,
                    //HeadPitch,
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

        /// <summary>角度名を取得</summary>
        public static string[] AngleNames
        {
            get
            {
                return new string[]
                {
                    nameof(LShoulderPitch),
                    nameof(LShoulderRoll),
                    nameof(LElbowYaw),
                    nameof(LElbowRoll),
                    nameof(LWristYaw),
                    nameof(RShoulderPitch),
                    nameof(RShoulderRoll),
                    nameof(RElbowYaw),
                    nameof(RElbowRoll),
                    nameof(RWristYaw),
                    nameof(HeadPitch),
                    nameof(LHand),
                    nameof(RHand)
                };
            }
        }

    }
}

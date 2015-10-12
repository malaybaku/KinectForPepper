using System;
using Microsoft.Kinect;

namespace Baku.KinectForPepper
{
    /// <summary>Kinectで取得したBodyをPepperの関節角度に変換する計算の実装クラスです。</summary>
    public static class RobotJointAnglesExtension
    {
        /// <summary>Kinectで検出されたボディ情報を用いロボットへの角度指示値を設定します。</summary>
        public static void SetAnglesFromBody(this RobotJointAngles robot, Body body)
        {
            SetRArmAngles(robot, body);
            SetLArmAngles(robot, body);
            SetHands(robot, body);
            SetHip(robot, body);
            SetWristYaws(robot, body);
            SetHead(robot, body);
        }

        /// <summary>指定した関節の方向への回転を取得します。</summary>
        private static Quartanion GetOrientation(Body body, JointType jtype)
        {
            return QuartanionFactory.FromVector4(body.JointOrientations[jtype].Orientation);
        }

        /// <summary>右肩から右肘にかけての関節角度指示値を設定</summary>
        private static void SetRArmAngles(RobotJointAngles robot, Body body)
        {
            #region 諸元の準備
            //右肩の基準座標系を取得
            var rShoulder = GetOrientation(body, JointType.ShoulderRight);
            var shoulderX = Quartanion.UnitX.RotateBy(rShoulder);
            var shoulderY = Quartanion.UnitY.RotateBy(rShoulder);
            var shoulderZ = Quartanion.UnitZ.RotateBy(rShoulder);

            //ヒジの位置とヒジ回転軸ベクトルを取得
            var elbowOrientation = GetOrientation(body, JointType.ElbowRight);
            var elbowZ = Quartanion.UnitZ.RotateBy(elbowOrientation);
            var elbowY = Quartanion.UnitY.RotateBy(elbowOrientation);

            //ヒジ-手首方向の単位ベクトルを取得
            var wristY = Quartanion.UnitY.RotateBy(GetOrientation(body, JointType.WristRight));

            //肩座標を基準にするため成分を再取得
            var elbowZFromShoulder = new Quartanion
            {
                X = elbowZ.Product(shoulderX),
                Y = elbowZ.Product(shoulderY),
                Z = elbowZ.Product(shoulderZ)
            };
            var elbowPosFromShoulder = new Quartanion
            {
                X = elbowY.Product(shoulderX),
                Y = elbowY.Product(shoulderY),
                Z = elbowY.Product(shoulderZ)
            };
            #endregion

            #region 変換計算
            //NOTE: 変換計算の導出は手元のノートで手計算により事前に行われている。ソースだけ見ても分かりづらい

            //肩から見たヒジの位置情報をもとにShoulderRollおよびShoulderPitchを一意に特定
            float rShoulderRoll = (float)Math.Asin(elbowPosFromShoulder.Y);
            float rShoulderPitch = 0.0f;
            if (rShoulderRoll > -1.5f && rShoulderRoll < 1.5f)
            {
                //算術的チェック: X / Cos() が1越えてるとAsin関数がNaNを吐くのを防止
                if (Math.Abs(elbowPosFromShoulder.X) > Math.Abs(Math.Cos(rShoulderRoll)))
                {
                    rShoulderPitch = 0.5f * 3.14f;
                }
                else
                {
                    rShoulderPitch = (float)Math.Asin(elbowPosFromShoulder.X / Math.Cos(rShoulderRoll));
                    //追加処理: Pitchは360度分の自由度があるのだがAsinだと180度しか分解能ないので
                    //Z座標の正負情報を使って360度に対応させる
                    //具体的にはZ < 0 のときピッチは[-180, -90]か[90, 180]の範囲に入る
                    if (elbowPosFromShoulder.Z < 0)
                    {
                        if (rShoulderPitch > 0)
                        {
                            rShoulderPitch = (float)Math.PI - rShoulderPitch;
                        }
                        else
                        {
                            rShoulderPitch = (float)(-Math.PI) - rShoulderPitch;
                        }
                    }
                }

            }

            //ヒジのZベクトルつまりヒジの回転軸ベクトルの向き(と上のShoulderPitch)を用いてElbowYawを特定
            //このnoRollRElbowはElbowYaw=0の場合のヒジZベクトルの向きである(導出は手計算)
            var noRollRElbow = new Quartanion
            {
                X = (float)Math.Cos(rShoulderPitch),
                Z = -(float)Math.Sin(rShoulderPitch)
            };
            //noRollRElbowと実際のヒジZベクトルのズレはElbowYaw回転によって説明される、というノリで計算。
            //外積を用いているのはAcos関数だけだと回転方向が定まらない(どっち回転でもプラス扱いになる)ため
            //float rElbowYaw = (float)(
            //    Math.Acos(noRollRElbow.Product(elbowZFromShoulder)) *
            //    Math.Sign(noRollRElbow.Cross(elbowZFromShoulder).Product(elbowPosFromShoulder))
            //    );
            //修正:たぶんこっちの方が計算安定する
            float rElbowYaw = (float)Math.Asin(
                noRollRElbow.Cross(elbowZFromShoulder).Product(elbowPosFromShoulder)
                );

            //ヒジの曲がり具合を取得: 単に内積とって角度差を見ればOK
            float cos = wristY.Product(elbowY);
            float rElbowRoll = (float)Math.Acos(cos);

            #endregion

            //HACK: 正負の調整はPepperとKinectで回転方向の取り方が違うことに由来する。
            robot.RShoulderPitch = -rShoulderPitch;
            robot.RShoulderRoll = -rShoulderRoll;
            robot.RElbowYaw = rElbowYaw;
            robot.RElbowRoll = rElbowRoll;
        }

        /// <summary>左肩から左肘にかけての関節角度指示値を設定</summary>
        private static void SetLArmAngles(RobotJointAngles robot, Body body)
        {
            #region 諸元の準備
            //右肩の基準座標系を取得
            var lShoulder = GetOrientation(body, JointType.ShoulderLeft);
            var shoulderX = Quartanion.UnitX.RotateBy(lShoulder);
            var shoulderY = Quartanion.UnitY.RotateBy(lShoulder);
            var shoulderZ = Quartanion.UnitZ.RotateBy(lShoulder);

            //ヒジの位置とヒジ回転軸ベクトルを取得
            var elbowOrientation = GetOrientation(body, JointType.ElbowLeft);
            var elbowZ = Quartanion.UnitZ.RotateBy(elbowOrientation);
            var elbowY = Quartanion.UnitY.RotateBy(elbowOrientation);

            //ヒジ-手首方向の単位ベクトルを取得
            var wristY = Quartanion.UnitY.RotateBy(GetOrientation(body, JointType.WristLeft));

            //肩座標を基準にするため成分を再取得
            var elbowZFromShoulder = new Quartanion
            {
                X = elbowZ.Product(shoulderX),
                Y = elbowZ.Product(shoulderY),
                Z = elbowZ.Product(shoulderZ)
            };
            var elbowPosFromShoulder = new Quartanion
            {
                X = elbowY.Product(shoulderX),
                Y = elbowY.Product(shoulderY),
                Z = elbowY.Product(shoulderZ)
            };
            #endregion

            #region 変換計算
            //NOTE: 変換計算の導出は手元のノートで手計算により事前に行われている。ソースだけ見ても分かりづらい

            //肩から見たヒジの位置情報をもとにShoulderRollおよびShoulderPitchを一意に特定
            float lShoulderRoll = (float)Math.Asin(elbowPosFromShoulder.Y);
            float lShoulderPitch = 0.0f;
            if (lShoulderRoll > -1.5f && lShoulderRoll < 1.5f)
            {
                //算術的チェック: X / Cos() が1越えてるとAsin関数が例外を吐くのを防止
                if (Math.Abs(elbowPosFromShoulder.X) > Math.Abs(Math.Cos(lShoulderRoll)))
                {
                    lShoulderPitch = 0.5f * 3.14f;
                }
                else
                {
                    lShoulderPitch = (float)Math.Asin(-elbowPosFromShoulder.X / Math.Cos(lShoulderRoll));
                    //追加処理: Pitchは360度分の自由度があるのだがAsinだと180度しか分解能ないので
                    //Z座標の正負情報を使って360度に対応させる
                    //具体的にはZ < 0 のときピッチは[-180, -90]か[90, 180]の範囲に入る
                    if (elbowPosFromShoulder.Z < 0)
                    {
                        if (lShoulderPitch > 0)
                        {
                            lShoulderPitch = (float)Math.PI - lShoulderPitch;
                        }
                        else
                        {
                            lShoulderPitch = (float)(-Math.PI) - lShoulderPitch;
                        }
                    }
                }
            }

            //ヒジのZベクトルつまりヒジの回転軸ベクトルの向き(と上のShoulderPitch)を用いてElbowYawを特定
            //このnoRollRElbowはElbowYaw=0の場合のヒジZベクトルの向きである(導出は手計算)
            var noRollLElbow = new Quartanion
            {
                X = -(float)Math.Cos(lShoulderPitch),
                Z = -(float)Math.Sin(lShoulderPitch)
            };
            //noRollRElbowと実際のヒジZベクトルのズレはElbowYaw回転によって説明される、というノリで計算。
            //外積を用いているのはAcos関数だけだと回転方向が定まらない(どっち回転でもプラス扱いになる)ため
            //float lElbowYaw = (float)(
            //    Math.Acos(noRollLElbow.Product(elbowZFromShoulder)) *
            //    Math.Sign(noRollLElbow.Cross(elbowZFromShoulder).Product(elbowPosFromShoulder))
            //    );
            //修正:たぶんこっちの方が計算安定する
            float lElbowYaw = (float)Math.Asin(
                noRollLElbow.Cross(elbowZFromShoulder).Product(elbowPosFromShoulder)
                );

            //ヒジの曲がり具合を取得: 単に内積とって角度差を見ればOK
            float cos = wristY.Product(elbowY);
            float lElbowRoll = (float)Math.Acos(cos);
            //試しに: Pepperはどうせ90度くらいしか腕回らないハズ
            lElbowRoll = (float)Math.Min(lElbowRoll, 0.5 * 3.14);

            #endregion

            //HACK: 正負の調整はKinectとPepperで回転方向の取り方が違うことに由来
            robot.LShoulderPitch = -lShoulderPitch;
            robot.LShoulderRoll = lShoulderRoll;
            robot.LElbowYaw = lElbowYaw;
            robot.LElbowRoll = -lElbowRoll;
        }

        /// <summary>両手の開閉状態を設定</summary>
        private static void SetHands(RobotJointAngles robot, Body body)
        {
            if (body.HandLeftConfidence == TrackingConfidence.High)
            {
                if (body.HandLeftState == HandState.Open) robot.LHand = 1.0f;
                if (body.HandLeftState == HandState.Closed) robot.LHand = 0.0f;
            }

            if (body.HandRightConfidence == TrackingConfidence.High)
            {
                if (body.HandRightState == HandState.Open) robot.RHand = 1.0f;
                if (body.HandRightState == HandState.Closed) robot.RHand = 0.0f;
            }
        }

        /// <summary>体全体の傾きをHipの角度として設定</summary>
        private static void SetHip(RobotJointAngles robot, Body body)
        {
            var spine = GetOrientation(body, JointType.SpineMid);
            var spineY = Quartanion.UnitY.RotateBy(spine);
            //適当な実装: Kinect側に体を倒してるかどうかカメラ座標ベースで判定するだけ
            robot.HipPitch = (float)Math.Asin(spineY.Z);
        }

        /// <summary>両手のねじり角度を設定</summary>
        private static void SetWristYaws(RobotJointAngles robot, Body body)
        {
            #region 右
            if (body.Joints[JointType.ElbowRight].TrackingState == TrackingState.Tracked &&
                body.Joints[JointType.WristRight].TrackingState == TrackingState.Tracked)
            {
                //右肘Z軸: ウルトラマンのポーズをとったときのヒジ外側向き
                var rElbowZ = Quartanion.UnitZ.RotateBy(GetOrientation(body, JointType.ElbowRight));
                var rWristY = Quartanion.UnitY.RotateBy(GetOrientation(body, JointType.WristRight));
                //右手X軸: 手の甲向き
                var rWristX = Quartanion.UnitX.RotateBy(GetOrientation(body, JointType.WristRight));

                //外積の絶対値はSinというアレ。あとlElbowZ.Cross(lWristX)はlWristYと並行
                robot.RWristYaw = (float)Math.Asin(rElbowZ.Cross(rWristX).Product(rWristY));
            }
            #endregion

            #region 左
            if (body.Joints[JointType.ElbowLeft].TrackingState == TrackingState.Tracked &&
                body.Joints[JointType.WristLeft].TrackingState == TrackingState.Tracked)
            {
                var lElbowZ = Quartanion.UnitZ.RotateBy(GetOrientation(body, JointType.ElbowLeft));
                var lWristY = Quartanion.UnitY.RotateBy(GetOrientation(body, JointType.WristLeft));
                //左手X軸は手の平向きで右手のと逆なのでマイナスかけて戻す
                var lWristX = -Quartanion.UnitX.RotateBy(GetOrientation(body, JointType.WristLeft));

                //外積の絶対値はSinというアレ。あとlElbowZ.Cross(lWristX)はlWristYと並行
                robot.LWristYaw = (float)Math.Asin(lElbowZ.Cross(lWristX).Product(lWristY));
            }
            #endregion
        }

        /// <summary>首の角度を設定 FIXME: 現状はヨー角のみ</summary>
        private static void SetHead(RobotJointAngles robot, Body body)
        {
            var neck = QuartanionFactory.FromVector4(body.JointOrientations[JointType.Neck].Orientation);
            var spine = QuartanionFactory.FromVector4(body.JointOrientations[JointType.SpineShoulder].Orientation);

            var neckZ = Quartanion.UnitZ.RotateBy(neck);

            //正規直交基底に使う
            var spineX = Quartanion.UnitX.RotateBy(spine);
            var spineY = Quartanion.UnitY.RotateBy(spine);
            var spineZ = Quartanion.UnitZ.RotateBy(spine);

            var neckZfromSpine = new Quartanion
            {
                X = neckZ.Product(spineX),
                Y = neckZ.Product(spineY),
                Z = neckZ.Product(spineZ)
            };

            robot.HeadYaw = (float)Math.Asin(neckZfromSpine.X);


        }


    }
}

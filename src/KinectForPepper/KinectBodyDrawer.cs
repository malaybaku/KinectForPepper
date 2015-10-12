using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Baku.KinectForPepper
{
    /// <summary>Kinectで取得したボディ情報を描画するエージェントを表します。</summary>
    public class KinectBodyDrawer
    {
        #region 描画に使う色や太さの不変情報

        /// <summary>手領域の描画に使う円の大きさです。</summary>
        private const double HandSize = 30;

        /// <summary>関節の描画時サイズです。</summary>
        private const double JointThickness = 3;

        /// <summary>クリップされた領域を矩形であらわすときの厚みです。</summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>Constant for clamping Z values of camera space points from being negative</summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>Brush used for drawing hands that are currently tracked as closed</summary>
        private static readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>Brush used for drawing hands that are currently tracked as opened</summary>
        private static readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>Brush used for drawing hands that are currently tracked as in lasso (pointer) position</summary>
        private static readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>Brush used for drawing joints that are currently tracked</summary>
        private static readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>Brush used for drawing joints that are currently inferred</summary>
        private static readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>Pen used for drawing bones that are currently inferred</summary>
        private static readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>ボディの描画に使う色です。</summary>
        private static readonly Pen bodyColor = new Pen(Brushes.Red, 6);
        #endregion

        private DrawingGroup drawingGroup;

        private KinectConnector kinectConnector;

        /// <summary></summary>
        /// <param name="kinectConnector"></param>
        public KinectBodyDrawer(KinectConnector kinectConnector)
        {
            this.kinectConnector = kinectConnector;

            drawingGroup = new DrawingGroup();
            ImageSource = new DrawingImage(drawingGroup);

            kinectConnector.BodyUpdated += (_, e) => Draw(e.Body);
        }

        /// <summary>描画結果を表示可能なソースを取得します。</summary>
        public ImageSource ImageSource { get; }

        /// <summary>描画対象となるボディを指定して描画情報を更新します。</summary>
        public void Draw(Body body)
        {
            using (DrawingContext dc = drawingGroup.Open())
            {
                DrawToDrawingContext(dc, body);

                //Kinectが指定してる幅でクリッピング
                drawingGroup.ClipGeometry = new RectangleGeometry(
                    new Rect(0.0, 0.0, kinectConnector.DisplayWidth, kinectConnector.DisplayHeight)
                    );
            }
        }


        /// <summary>ボディ情報を指定されたDrawingContextに書き込む</summary>
        private void DrawToDrawingContext(DrawingContext dc, Body body)
        {
            // Draw a transparent background to set the render size
            dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, kinectConnector.DisplayWidth, kinectConnector.DisplayHeight));

            if (!body.IsTracked) return;

            this.DrawClippedEdges(body, dc);

            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

            // convert the joint points to depth (display) space
            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

            foreach (JointType jointType in joints.Keys)
            {
                //最小値をInferredZPositionClampでおさえることでマッパーがInfinityを吐くのを防止
                CameraSpacePoint position = joints[jointType].Position;
                position.Z = Math.Max(position.Z, InferredZPositionClamp);

                DepthSpacePoint depthSpacePoint = kinectConnector.CoordinateMapper.MapCameraPointToDepthSpace(position);
                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
            }

            //ボーン
            DrawBones(joints, jointPoints, dc);
            //関節
            DrawJoints(joints, jointPoints, dc);
            //両手
            DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
            DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
        }

        /// <summary>見切れてる場合はそれを描画</summary>
        private void DrawClippedEdges(Body body, DrawingContext dc)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            double t = ClipBoundsThickness;
            double h = kinectConnector.DisplayHeight;
            double w = kinectConnector.DisplayWidth;
            Action<Rect> drawRect = rect => dc.DrawRectangle(Brushes.Red, null, rect);

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawRect(new Rect(0, h - t, w, t));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawRect(new Rect(0, 0, w, t));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawRect(new Rect(0, 0, t, h));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawRect(new Rect(w - t, 0, t, h));
            }
        }

        /// <summary>ボーンを描画</summary>
        private static void DrawBones(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext dc)
        {
            foreach(var bone in KinectBoneConnections.Bones)
            {
                JointType jointType0 = bone.Item1;
                JointType jointType1 = bone.Item2;

                Joint joint0 = joints[jointType0];
                Joint joint1 = joints[jointType1];

                // If we can't find either of these joints, exit
                if (joint0.TrackingState == TrackingState.NotTracked ||
                    joint1.TrackingState == TrackingState.NotTracked)
                {
                    return;
                }

                // We assume all drawn bones are inferred unless BOTH joints are tracked
                Pen drawPen = inferredBonePen;
                if ((joint0.TrackingState == TrackingState.Tracked) &&
                    (joint1.TrackingState == TrackingState.Tracked))
                {
                    drawPen = bodyColor;
                }

                dc.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
            }
        }

        /// <summary>関節を描画</summary>
        private static void DrawJoints(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints,  DrawingContext dc)
        {
            foreach (JointType jointType in joints.Keys)
            {
                TrackingState trackingState = joints[jointType].TrackingState;

                Brush drawBrush =
                    (trackingState == TrackingState.Tracked) ? trackedJointBrush :
                    (trackingState == TrackingState.Inferred) ? inferredJointBrush :
                    Brushes.Transparent;

                dc.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
            }
        }

        /// <summary>手の状態を描画(赤:グー, 青:チョキ, 緑:パー)</summary>
        private static void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            Brush handBrush =
                (handState == HandState.Closed) ? handClosedBrush :
                (handState == HandState.Open) ? handOpenBrush :
                (handState == HandState.Lasso) ? handLassoBrush :
                Brushes.Transparent;

            drawingContext.DrawEllipse(handBrush, null, handPosition, HandSize, HandSize);
        }

    }
}

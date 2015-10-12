using System;
using System.Collections.Generic;

namespace Baku.KinectForPepper
{
    /// <summary>何かしらの行動のフレームレート(回数/sec)の記録保持を行います。</summary>
    public class FPSWatcher
    {
        /// <summary>何秒分のデータからフレームレートを算出するかをもとに初期化します。</summary>
        /// <param name="referenceTime">過去何秒分のデータをフレームレート算出に用いるか[秒]</param>
        public FPSWatcher(double referenceTime = 5.0)
        {
            records = new Queue<DateTime>();
            ReferenceTime = referenceTime;
        }

        private Queue<DateTime> records;

        /// <summary>行動記録をカウントアップします。</summary>
        public void Count()
        {
            records.Enqueue(DateTime.Now);
        }

        /// <summary>過去のデータを何秒分用いてFPSを算出するかを取得、設定します。</summary>
        public double ReferenceTime { get; set; }

        /// <summary>フレームレートを取得します。</summary>
        public double FPS
        {
            get
            {
                var now = DateTime.Now;
                //古いデータを破棄
                while(records.Count > 0 && (now - records.Peek()).TotalSeconds > ReferenceTime)
                {
                    records.Dequeue();
                }
                return records.Count / ReferenceTime;
            }
        }

    }
}

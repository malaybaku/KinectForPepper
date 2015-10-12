using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baku.KinectForPepper
{
    //連続方程式ベースのがイマイチ動いてなかったので作り直し。
    //参照元 :http://vstcpp.wpblog.jp/?page_id=523
    public class LowPassFilter
    {
        public LowPassFilter(float initialValue)
        {
            in1 = initialValue;
            in2 = initialValue;
            out1 = initialValue;
            out2 = initialValue;
        }
        public LowPassFilter() : this(0.0f) { }

        /// <summary>サンプリング周波数をHz単位で取得、設定します。</summary>
        public float SampleRate { get; set; }
        /// <summary>カットオフ周波数をHz単位で取得、設定します。</summary>
        public float Freq { get; set; }
        /// <summary>いわゆるQ値を指定します。大きくするとカットオフ付近の応答が強くなります</summary>
        public float Q { get; set; }
        /// <summary>値を入力し、出力を更新します。</summary>
        /// <param name="input">フィルタへの入力</param>
        public void Update(float input)
        {
            float latestOut = B0 / A0 * input
                + B1 / A0 * in1
                + B2 / A0 * in2
                - A1 / A0 * out1
                - A2 / A0 * out2;

            in2 = in1;
            in1 = input;
            out2 = out1;
            out1 = latestOut;
        }
        /// <summary>現在のフィルタからの出力を取得します。</summary>
        public float Output => out1;

        private float Omega => (float)(2.0f * Freq / SampleRate * Math.PI);
        private float Alpha => (float)(Math.Sin(Omega) * 0.5f / Q);

        private float A0 => 1.0f + Alpha;
        private float A1 => (float)(-2.0f * Math.Cos(Omega));
        private float A2 => 1.0f - Alpha;

        private float B0 => (float)(1.0f - Math.Cos(Omega)) * 0.5f;
        private float B1 => (float)(1.0f - Math.Cos(Omega));
        private float B2 => (float)(1.0f - Math.Cos(Omega)) * 0.5f;

        private float in2;
        private float in1;
        private float out2;
        private float out1;

    }

    /// <summary>同じ特性のローパスフィルタが並んだものを表します。</summary>
    public class LowPassFilterArray
    {
        public LowPassFilterArray(int n)
        {
            lpfs = new LowPassFilter[n];
            for (int i=0;i< n;i++)
            {
                lpfs[i] = new LowPassFilter();
            }
        }
        public float SampleRate
        {
            get { return lpfs[0].SampleRate; }
            set { foreach (var lpf in lpfs) lpf.SampleRate = value; }
        }
        public float Freq
        {
            get { return lpfs[0].Freq; }
            set { foreach (var lpf in lpfs) lpf.Freq = value; }
        }
        public float Q
        {
            get { return lpfs[0].Q; }
            set { foreach (var lpf in lpfs) lpf.Q = value; }
        }

        public void Update(IEnumerable<float> inputs)
        {
            int i = 0;
            foreach (var input in inputs)
            {
                lpfs[i].Update(input);
                i++;
            }
        }
        public IEnumerable<float> Outputs => lpfs.Select(lpf => lpf.Output);

        private LowPassFilter[] lpfs;
    }

    /// <summary>単精度小数値にローパス処理を与えるフィルタを表します。</summary>
    public class DynamicLowPassFilter
    {
        /// <summary>初期の入出力値を代入してフィルタを初期化します。</summary>
        /// <param name="x"></param>
        public DynamicLowPassFilter(float x)
        {
            currentOutput = x;
            previousOutput = x;
        }

        public DynamicLowPassFilter() : this(0.0f) { }

        float speedDump = 2.0f;
        /// <summary>速度を減衰させる効果を取得、設定します。</summary>
        public float SpeedDump
        {
            get { return speedDump; }
            set { if (value >= 0f) speedDump = value; }
        }

        float differenceDump = 1.0f;
        /// <summary>変位を減衰させる効果の大きさを取得、設定します。</summary>
        public float DifferenceDump
        {
            get { return differenceDump; }
            set { if (value >= 0f) differenceDump = value; }
        }

        float deltaT = 1.0f;
        /// <summary>フィルタを呼び出す時間間隔を設定します。</summary>
        public float DeltaT
        {
            get { return deltaT; }
            set { if (value > 0f) deltaT = value; }
        }

        /// <summary>フィルタリングで用いる、パラメタから決まる値の一つ</summary>
        private float FactorWhole => 1.0f / (1.0f / DeltaT / DeltaT + SpeedDump / 2.0f / DeltaT);

        /// <summary>フィルタリングで用いる、パラメタから決まる値の一つ</summary>
        private float FactorDelta1 => (2.0f / DeltaT / DeltaT - DifferenceDump);

        /// <summary>フィルタリングで用いる、パラメタから決まる値の一つ</summary>
        private float FactorDelta2 => (SpeedDump / 2.0f / DeltaT - 1.0f / DeltaT / DeltaT);


        /// <summary>値をフィルタリングします。</summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public float GetFilteredValue(float input)
        {
            //NOTE: ココの数式は手計算を反映したもの
            float result = FactorWhole * (
                FactorDelta1 * currentOutput +
                FactorDelta2 * previousOutput +
                DifferenceDump * input
                );

            previousOutput = currentOutput;
            currentOutput = result;

            return currentOutput;
        }

        float currentOutput;
        float previousOutput;
    }
}

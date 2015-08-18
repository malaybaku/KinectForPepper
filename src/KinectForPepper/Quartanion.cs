using System;

namespace Baku.KinectForPepper
{
    /// <summary>座標回転と3次元ベクトルの表現に使うクオータニオンを表します。</summary>
    public struct Quartanion : IEquatable<Quartanion>
    {
        public float W;
        public float X;
        public float Y;
        public float Z;

        public float NormSquare => W * W + X * X + Y * Y + Z * Z;
        public float Norm => (float)Math.Sqrt(NormSquare);

        public Quartanion Conjugate
        {
            get
            {
                return new Quartanion
                {
                    W = this.W,
                    X = -this.X,
                    Y = -this.Y,
                    Z = -this.Z
                };
            }
        }

        public float Product(Quartanion v) => W * v.W + X * v.X + Y * v.Y + Z * v.Z;
        public Quartanion Cross(Quartanion u) => this * u;

        /// <summary>3次元ベクトルを指定した回転により回転させた結果を取得します。</summary>
        /// <param name="rot">回転を表すクオータニオン</param>
        /// <returns>回転後のベクトル</returns>
        public Quartanion RotateBy(Quartanion rot) => rot * this * rot.Conjugate;

        public static bool operator ==(Quartanion a, Quartanion b)
        {
            return (
                a.W == b.W &&
                a.X == b.Z &&
                a.Y == b.Y &&
                a.Z == b.Z
                );
        }
        public static bool operator !=(Quartanion a, Quartanion b) => !a.Equals(b);

        //NOTE: 加算や減算は重要度低いし実際使わないのでスルー

        public static Quartanion operator -(Quartanion v) => new Quartanion { W = -v.W, X = -v.X, Y = -v.Y, Z = -v.Z };        
        public static Quartanion operator *(Quartanion v, Quartanion u)
        {
            return new Quartanion
            {
                W = v.W * u.W - v.X * u.X - v.Y * u.Y - v.Z * u.Z,
                X = v.W * u.X + v.X * u.W + v.Y * u.Z - v.Z * u.Y,
                Y = v.W * u.Y + v.Y * u.W + v.Z * u.X - v.X * u.Z,
                Z = v.W * u.Z + v.Z * u.W + v.X * u.Y - v.Y * u.X
            };

        }

        public static Quartanion UnitW => new Quartanion { W = 1.0f };
        public static Quartanion UnitX => new Quartanion { X = 1.0f };
        public static Quartanion UnitY => new Quartanion { Y = 1.0f };
        public static Quartanion UnitZ => new Quartanion { Z = 1.0f };

        public bool Equals(Quartanion other) => (this == other);

        public override bool Equals(object obj) => (obj is Quartanion) ? Equals((Quartanion)obj) : false;
        //NOTE: GetHashCodeの実装はかなりテキトーだがそもそもHash使うシナリオ想定してない
        public override int GetHashCode() => (int)(W + X + Y + Z);
        public override string ToString() => $"{W}, {X}, {Y}, {Z}";
        public string ToString(string format)
        {
            return String.Format("{0} {1} {2} {3}",
                W.ToString(format),
                X.ToString(format),
                Y.ToString(format),
                Z.ToString(format)
                );
        }
    }

}

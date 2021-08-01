using System;

namespace BinaryInterpolationTree
{
    public struct Vector
    {
        float[] vector;
        public readonly int nbrOfDim;

        public float this[int i] => nbrOfDim == 0 ? 0 : vector[i];
        public float Magnitude => (float)Math.Sqrt(Squared().Sum());
        public Vector Normalized => this / Magnitude;

        public Vector(params float[] vector)
        {
            this.vector = vector;
            this.nbrOfDim = vector.Length;
        }

        public Vector(int nbrOfDim)
        {
            vector = new float[nbrOfDim];
            this.nbrOfDim = nbrOfDim;
        }

        public Vector Componentwise(Func<float, float> transformation)
        {
            float[] result = new float[nbrOfDim];
            for (int i = 0; i < nbrOfDim; i++)
                result[i] = transformation(vector[i]);

            return result;
        }

        public static Vector Componentwise(Vector v1, Vector v2, Func<float, float, float> transformation)
        {
            if (v1.nbrOfDim == 0)
                return v2;

            if (v2.nbrOfDim == 0)
                return v1;

            if (v1.nbrOfDim != v2.nbrOfDim)
                throw new Exception("dimensionality does not match");

            float[] result = new float[v1.nbrOfDim];
            for (int i = 0; i < v1.nbrOfDim; i++)
                result[i] = transformation(v1.vector[i], v2.vector[i]);

            return result;
        }

        public float Aggregation(Func<float, float, float> aggregation)
        {
            if (nbrOfDim == 0)
                return 0;

            float result = vector[0];
            for (int i = 1; i < nbrOfDim; i++)
                result = aggregation(result, vector[i]);

            return result;
        }

        public static Vector operator +(Vector v1, Vector v2) => Componentwise(v1, v2, (f1, f2) => f1 + f2);
        public static Vector operator -(Vector v1, Vector v2) => Componentwise(v1, v2, (f1, f2) => f1 - f2);
        public static Vector operator /(Vector v1, Vector v2) => Componentwise(v1, v2, (f1, f2) => f1 / f2);
        public static Vector operator *(float s, Vector v) => v.Componentwise(f => f * s);
        public static Vector operator *(Vector v, float s) => s * v;
        public static Vector operator /(Vector v, float s) => v.Componentwise(f => f / s);
        public static Vector operator -(Vector v) => v.Componentwise(f => -f);
        public Vector Abs() => Componentwise(f => Math.Abs(f));
        public Vector Squared() => Componentwise(f => f * f);
        public Vector Squareroot() => Componentwise(f => (float)Math.Sqrt(f));
        public float Mean() => Sum() / nbrOfDim;

        public float Sum() => Aggregation((f1, f2) => f1 + f2);
        public float Min() => Aggregation((f1, f2) => f1 < f2 ? f1 : f2);
        public float Max() => Aggregation((f1, f2) => f1 > f2 ? f1 : f2);

        public static float InterpolationFactor(Vector p1, Vector p2, Vector arg)
        {
            Vector delta1 = (p2 - p1);
            Vector delta2 = (arg - p1);
            return Dot(delta1, delta2) / delta1.Squared().Sum();
        }

        public static float Distance(Vector a, Vector b) => (a - b).Magnitude;

        public static float Dot(Vector v1, Vector v2) => Componentwise(v1, v2, (f1, f2) => f1 * f2).Sum();

        public static Vector Random(Vector range, Random random)
        {
            float[] result = new float[range.nbrOfDim];
            for (int i = 0; i < result.Length; i++)
                result[i] = (float)(random.NextDouble() - 0.5) * range[i];

            return result;
        }

        public static implicit operator float[](Vector v) => v.vector;
        public static implicit operator Vector(float[] v) => new Vector(v);
        public static implicit operator Vector(float f) => new Vector(new float[]{f});
    }
}
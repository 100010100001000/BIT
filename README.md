# Binary Interpolation Tree (B.I.T)

Binary Interpolation Tree is a machine learning model i am working on. I am by no means an expert in this field or computer science in general and have been working on this mostly out of curiosity. Please let me know if my terminology is off, i made any other errors, or something very similar already exists. 

The main idea was to conceptionalize a model that provides:

* fast runtime on cpu's in relation to model complexity (O(n), Ω(1), input dependent)
* the ability to further simplify computations based on a maximum inaccuracy.
* simple and fast training
* very low initial model complexity that increases during training
* multithreading capabilitys for maximum cpu usage
* increased transparency compared to other ML models
* a parameter regarding determinism of the algorithm

To make my explanations simpler and since i do not want to rewrite everything in pseudo-code, i will use simplified parts of my c# implementation provided in this repo, so c# knowledge might be necessary. 

As the name sugests, this model is basicly a binary tree. This binary tree consists of nodes and end-nodes. Nodes alwas have 2 children and end-nodes have non. Every node also has a reference to its parent, except for the root-node wich has no parent. To calculate the output of this model for a given input, this binary tree is traversed recursivly from the root-node to the end-nodes and the results of both children are interpolated for each node:

```c#
namespace BinaryInterpolationTree
{
    public class Node
    {
        public virtual Vector Evaluate(Vector args)
        {
            float a1 = Vector.InterpolationFactor(child2.position, child1.position, args);
            a1 = a1 > 1 ? 1 : a1 < 0 ? 0 : a1;
            float a2 = 1 - a1;

            return FullEvaluation(args, a1, a2));
        }

        private IEnumerable<Vector> FullEvaluation(Vector args, float a1, float a2)
        {
            var e1 = child1.Evaluate(args);
            var e2 = child2.Evaluate(args);
            return a1 * e1.Current + a2 * e2.Current;
        }
    }
    
    public class EndNode : Node
    {
        public override Vector Evaluate(Vector args)
        {
            return output;
        }
    }
    
    public struct Vector
    {
        float[] vector;
        public readonly int nbrOfDim;

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
    }
}
```

the following statements are true about InterpolationFactor(Vector p1, Vector p2, Vector arg) = I(p1, p2, x):

![equation](https://latex.codecogs.com/gif.latex?I%28%5Cvec%7Bp_1%7D%2C%20%5Cvec%7Bp_2%7D%2C%20%5Cvec%7Bp_1%7D%29%20%3D%200)  
![equation](https://latex.codecogs.com/gif.latex?I%28%5Cvec%7Bp_1%7D%2C%20%5Cvec%7Bp_2%7D%2C%20%5Cvec%7Bp_2%7D%29%20%3D%201)  
![equation](https://latex.codecogs.com/gif.latex?%28%5Cvec%7Bp_2%7D%20-%20%5Cvec%7Bp_1%7D%29%20%5Ccirc%20%28%5Cvec%7Bx_1%7D%20-%20%5Cvec%7Bx_2%7D%29%3D0%20%5CLeftrightarrow%20I%28%5Cvec%7Bp_1%7D%2C%20%5Cvec%7Bp_2%7D%2C%20%5Cvec%7Bx_1%7D%29%20%3D%20I%28%5Cvec%7Bp_1%7D%2C%20%5Cvec%7Bp_2%7D%2C%20%5Cvec%7Bx_2%7D%29)

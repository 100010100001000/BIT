using System;
using System.Collections.Generic;
using System.Linq;

namespace BinaryInterpolationTree
{
    public class Node
    {
        public Vector position;
        public Node parent;
        public Node child1;
        public Node child2;
        public float maxContribution;
        public int complexity;
        public int depth;
        public int sampleSize;
        public Vector deviations;

        public Node(int nbrOfOutputDim, Vector position, Node child1, Node child2)
        {
            sampleSize = 0;
            this.position = position;
            this.child1 = child1;
            this.child2 = child2;
            child1.parent = this;
            child2.parent = this;
            child1.depth = depth + 1;
            child2.depth = depth + 1;
            deviations = new Vector(nbrOfOutputDim);
            Update();
        }

        public Node(Vector position, int nbrOfOutputDim)
        {
            sampleSize = 0;
            this.position = position;
            child1 = null;
            child2 = null;
            complexity = 1;
            deviations = new Vector(nbrOfOutputDim);
        }

        public virtual void Update()
        {
            int newComplexity = child1.complexity + child2.complexity;
            float newMaxContribution = Math.Max(
                child1.maxContribution, child2.maxContribution);

            if (newMaxContribution == maxContribution 
                && newComplexity == complexity)
                return;

            complexity = newComplexity;
            maxContribution = newMaxContribution;
            if(parent != null)
                parent.Update();
        }

        public void Exchange(Node newChild)
        {
            if (parent == null)
                return;

            if (parent.child1 == this)
                parent.child1 = newChild;

            if (parent.child2 == this)
                parent.child2 = newChild;

            newChild.parent = parent;
            newChild.depth = depth;
            if (!(newChild is EndNode))
            {
                newChild.child1.depth = depth + 1;
                newChild.child2.depth = depth + 1;
            }
        }

        public virtual Vector Evaluate(Vector args, float maxInaccuracy, float determinism, Random random, 
            out HashSet<(Node, float)> contributions)
        {
            contributions = new HashSet<(Node, float)>();
            HashSet<(Node, float)> potentialMinors = new HashSet<(Node, float)>();
            HashSet<Node> minors = new HashSet<Node>();
            Vector result = default;
            float[] inaccuraccyLeftOver = new float[] { maxInaccuracy };
            foreach (var r in Evaluate(args, determinism, random, contributions,
                potentialMinors, minors, 1, inaccuraccyLeftOver))
            {
                result = r;
                var sortedMinors = potentialMinors.OrderBy((m) =>
                    m.Item2 / m.Item1.complexity);
                minors.Clear();
                foreach (var minor in sortedMinors)
                {
                    if (inaccuraccyLeftOver[0] < minor.Item2)
                        break;

                    inaccuraccyLeftOver[0] -= minor.Item2;
                    minors.Add(minor.Item1);
                }

                potentialMinors.Clear();
            }

            return result;
        }

        public virtual IEnumerable<Vector> Evaluate(Vector args, float determinism,
            Random random, HashSet<(Node, float)> allContributions,
            HashSet<(Node, float)> potentialMinors, HashSet<Node> minors, 
            float contribution, float[] maxInaccuracy)
        {
            #region Init
            allContributions.Add((this, contribution));
            float a1 = Vector.InterpolationFactor(child2.position, child1.position, args);
            a1 = determinism * a1 + (1 - determinism) * (float) random.NextDouble();
            a1 = a1 > 1 ? 1 : a1 < 0 ? 0 : a1;
            float a2 = 1 - a1;

            float maxInaccuracy1 = a1 * child1.maxContribution * contribution;
            float maxInaccuracy2 = a2 * child2.maxContribution * contribution;
            bool child1IsMinor = maxInaccuracy1 < maxInaccuracy[0];
            bool child2IsMinor = maxInaccuracy2 < maxInaccuracy[0];
            #endregion
            if (!child2IsMinor && !child2IsMinor)
                foreach (var output in FullEvaluation(args, determinism, random, allContributions,
                    potentialMinors, minors, contribution, maxInaccuracy, a1, a2))
                    yield return output;
            else
            {
                if(a1 > 0 && a1 < 1)
                {
                    if (child1IsMinor)
                        potentialMinors.Add((child1, maxInaccuracy1));
                    if (child2IsMinor)
                        potentialMinors.Add((child2, maxInaccuracy2));

                    yield return default;
                    child1IsMinor = minors.Contains(child1);
                    child2IsMinor = minors.Contains(child2);
                }
                else
                {
                    child1IsMinor = a1 == 0;
                    child2IsMinor = !child1IsMinor;
                }
                if (child1IsMinor)
                {
                    if (!child2IsMinor || a2 >= a1)
                    {
                        maxInaccuracy[0] += maxInaccuracy2;
                        foreach (var output in child2.Evaluate(args, determinism, random,
                            allContributions, potentialMinors, minors, contribution * a2, maxInaccuracy))
                            yield return a2 * output;
                    }
                    else
                    {
                        maxInaccuracy[0] += maxInaccuracy1;
                        foreach (var output in child1.Evaluate(args, determinism, random,
                            allContributions, potentialMinors, minors, contribution * a1, maxInaccuracy))
                            yield return a1 * output;
                    }
                }
                else
                {
                    if (child2IsMinor)
                        foreach (var output in child1.Evaluate(args, determinism, random,
                            allContributions, potentialMinors, minors, contribution * a1, maxInaccuracy))
                            yield return a1 * output;
                    else
                        foreach (var output in FullEvaluation(args, determinism, random,
                            allContributions, potentialMinors, minors, contribution, maxInaccuracy, a1, a2))
                            yield return output;
                }
            }
        }

        private IEnumerable<Vector> FullEvaluation(
            Vector args, float determinism, Random random, HashSet<(Node, float)> allContributions,
            HashSet<(Node, float)> potentialMinors, HashSet<Node> minors, float contribution, 
            float[] maxInaccuracy, float a1, float a2)
        {
            var e1 = child1.Evaluate(args, determinism, random, allContributions, potentialMinors, 
                minors, contribution * a1, maxInaccuracy).GetEnumerator();
            var e2 = child2.Evaluate(args, determinism, random, allContributions, potentialMinors, minors,
                contribution * a2, maxInaccuracy).GetEnumerator();

            while (e1.MoveNext() || e2.MoveNext())
                yield return default;

            yield return a1 * e1.Current + a2 * e2.Current;
        }
    }
}

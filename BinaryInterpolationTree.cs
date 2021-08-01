using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BinaryInterpolationTree
{
    public class BinaryInterpolationTree
    {
        public readonly int inputSize;
        public readonly int outputSize;
        public readonly Node root;
        ThreadLocal<Random> randoms = new ThreadLocal<Random>(() => new Random((int)Task.CurrentId));
        public readonly Vector deviationScale;
        public readonly ConcurrentDictionary<Node, byte> nodesForRebranching;

        public Func<Vector, Vector> OutputPostprocessing { get; set; } = v => v;

        /// <summary>
        /// ErrorMetric(output, target)
        /// </summary>
        public Func<Vector, Vector, float> ErrorMetric { get; set; } = 
            (output, target) => (output - target).Squared().Mean();

        public int Complexity => root.complexity;

        public int trainingsIteration = 0;
        public BinaryInterpolationTree(Vector deviationScale, Vector p1, Vector p2, Vector o1, Vector o2, Vector p3)
        {
            this.deviationScale = deviationScale;
            inputSize = p1.nbrOfDim;
            outputSize = o1.nbrOfDim;
            EndNode eP1 = new EndNode(p1, o1, deviationScale);
            EndNode eP2 = new EndNode(p2, o2, deviationScale);
            root = new Node(deviationScale.nbrOfDim, p3, eP1, eP2);
            nodesForRebranching = new ConcurrentDictionary<Node, byte>();
            AddPointToRebranching(root);
        }

        public BinaryInterpolationTree(Vector deviationScale, Vector positionRange, Vector outputRange, Random random)
        {
            this.deviationScale = deviationScale;
            inputSize = positionRange.nbrOfDim;
            outputSize = outputRange.nbrOfDim;
            Vector p1 = -positionRange / 2;
            Vector p2 = positionRange / 2;
            Vector p3 = new Vector(inputSize);
            Vector o1 = Vector.Random(outputRange, random);
            Vector o2 = Vector.Random(outputRange, random);
            EndNode eP1 = new EndNode(p1, o1, deviationScale);
            EndNode eP2 = new EndNode(p2, o2, deviationScale);
            root = new Node(deviationScale.nbrOfDim, p3, eP1, eP2);
            nodesForRebranching = new ConcurrentDictionary<Node, byte>();
            AddPointToRebranching(root);
        }

        public Vector Forward(float maxInaccuracy, float determinism, Random random, Vector args)
        {
            var output = root.Evaluate(args, maxInaccuracy, determinism, random, out var _);
            return OutputPostprocessing(output);
        }

        public void Rebranching(int batchSize, float SubdivisionThreshold, float SimplificationThreshold) 
        {
            var nodesForRebranching = this.nodesForRebranching.ToArray();
            Parallel.ForEach(nodesForRebranching, pair =>
            {
                Node node = pair.Key;
                if (node.sampleSize < 2)
                    return;

                float deviationSum = (node.deviations / (node.sampleSize - 1)).Squareroot().Sum();
                var eN1 = node.child1 as EndNode;
                var eN2 = node.child2 as EndNode;
                var output1 = OutputPostprocessing(eN1.output);
                var output2 = OutputPostprocessing(eN2.output);
                float maxError = ErrorMetric(output1, output2) / 2;

                if (deviationSum >= SubdivisionThreshold)
                    Subdivide(node);
                else if(maxError <= SimplificationThreshold)
                    Simplify(node);
            });
        }

        float ContributionValueAdjustment(Contributions batchContributions, float maxInaccuracy,
            float determinism, Random random, Vector target, Vector args)
        {
            var output = root.Evaluate(args, maxInaccuracy, determinism, random, out var contributions);
            output = OutputPostprocessing(output);
            var error = ErrorMetric(output, target);
            foreach (var contribution in contributions)
                batchContributions.AddContribution(contribution.Item1,
                    contribution.Item2, args, output, target, error);

            return error;
        }

        public float ContributionValueAdjustment(float maxInaccuracy, float determinism, float learningrate, 
            float SubdivisionThreshold, float SimplificationThreshold, int rebranchingCycle, Vector[] args, Vector[] target)
        {
            if(trainingsIteration % rebranchingCycle == 0)
                Rebranching(target.Length, SubdivisionThreshold, SimplificationThreshold);

            ThreadLocal<Contributions> contributions = new 
                ThreadLocal<Contributions>(() => new Contributions(), true);

            float[] errors = new float[target.Length];
            Parallel.For(0, target.Length, (i) =>
            {
                errors[i] = ContributionValueAdjustment(contributions.Value, 
                    maxInaccuracy, determinism, randoms.Value, target[i], args[i]);
            });

            ContributionValueAdjustment(contributions.Values, learningrate);
            float meanError = errors.Average();
            trainingsIteration++;
            return meanError;
        }

        void ContributionValueAdjustment(IList<Contributions> contributions, float learningrate)
        {
            HashSet<Node> pointSet = new HashSet<Node>();
            foreach (var c in contributions)
                foreach (var pair in c.contributions)
                    pointSet.Add(pair.Key);

            Node[] ps = pointSet.ToArray();
            bool[] updated = new bool[ps.Length];
            Parallel.For(0, ps.Length, i =>
            {
                Node node = ps[i];
                Vector offset = new Vector(inputSize);
                Vector meanAdjustment = new Vector(outputSize);
                float meanError = 0;
                ContributionGathering(contributions, node, c =>
                {
                    meanAdjustment += c.adjustment;
                    meanError += c.error;
                }, out int sampleSize);

                meanAdjustment /= sampleSize;
                meanError /= sampleSize;
                meanError = meanError == 0 ? 1 : meanError;

                Vector deviations = new Vector(outputSize);
                ContributionGathering(contributions, node, c =>
                {
                    deviations += ((meanAdjustment - c.adjustment)).Squared();
                    float error = (float)(1 - Math.Exp(-c.error / meanError));
                    offset += error * c.contribution * (c.args - node.position);
                });

                offset /= sampleSize;
                node.position += learningrate * offset;
                if(nodesForRebranching.ContainsKey(node))
                {
                    node.deviations += deviations;
                    node.sampleSize += sampleSize;
                }
                else if (node is EndNode endPoint)
                {
                    endPoint.output += learningrate * meanAdjustment;
                    endPoint.Update();
                }
            });
        }

        void ContributionGathering(IList<Contributions> contributions, 
            Node point, Action<Contribution> action, out int sampleSize)
        {
            sampleSize = 0;
            foreach (var c in contributions)
            {
                if (!c.Contains(point))
                    continue;

                var pointContribution = c[point];
                sampleSize += pointContribution.Count;
                foreach (var contribution in pointContribution)
                {
                    action(contribution);
                }
            }
        }

        void ContributionGathering(IList<Contributions> contributions, 
            Node point, Action<Contribution> action)
        {
            foreach (var c in contributions)
            {
                if (!c.Contains(point))
                    continue;

                var pointContribution = c[point];
                foreach (var contribution in pointContribution)
                {
                    action(contribution);
                }
            }
        }

        void Simplify(Node point)
        {
            if (point.parent == null)
                return;

            var eP1 = point.child1 as EndNode;
            var eP2 = point.child2 as EndNode;
            Vector position = (eP1.position + eP2.position) / 2;
            Vector output = (eP1.output + eP2.output) / 2;
            EndNode ep = new EndNode(position, output, deviationScale);
            point.Exchange(ep);
            ep.Update();

            RemovePointFromRebranching(point);
            if (ep.parent.child1 is EndNode && ep.parent.child2 is EndNode)
                AddPointToRebranching(ep.parent);
        }

        void Subdivide(Node point)
        {
            var eP1 = point.child1 as EndNode;
            var eP2 = point.child2 as EndNode;

            var position = (eP1.position + eP2.position) / 2;
            var nEP1 = new EndNode(eP1.position, eP1.output, deviationScale);
            var nEP2 = new EndNode(position, eP1.output, deviationScale);
            var nEP3 = new EndNode(position, eP2.output, deviationScale);
            var nEP4 = new EndNode(eP2.position, eP2.output, deviationScale);
            var position1 = (eP1.position + position) / 2;
            var position2 = (eP2.position + position) / 2;
            var newPoint1 = new Node(deviationScale.nbrOfDim, position1, nEP1, nEP2);
            var newPoint2 = new Node(deviationScale.nbrOfDim, position2, nEP3, nEP4);

            AddPointToRebranching(newPoint1);
            AddPointToRebranching(newPoint2);
            RemovePointFromRebranching(point);
            point.child1.Exchange(newPoint1);
            point.child2.Exchange(newPoint2);
            point.deviations = new Vector(outputSize);
            point.sampleSize = 0;
            point.Update();
        }

        void AddPointToRebranching(Node point)
        {
            nodesForRebranching.AddOrUpdate(point, 0, (p, b1) => 0);
        }

        void RemovePointFromRebranching(Node point)
        {
            nodesForRebranching.TryRemove(point, out var b);
        }

        public static BinaryInterpolationTree RandomBIT(int nbrOfSubdivisions, 
            Vector deviationScale, Vector positionRange, Vector outputRange, Random random)
        {
            var result = new BinaryInterpolationTree(deviationScale, positionRange, outputRange, random);
            for (int i = 0; i < nbrOfSubdivisions; i++)
            {
                int index = random.Next(result.nodesForRebranching.Count);
                result.Subdivide(result.nodesForRebranching.ElementAt(index).Key);
            }

            result.UpdatePointRandomly(result.root, positionRange, outputRange, random);
            return result;
        }

        void UpdatePointRandomly(Node point, Vector positionRange, Vector outputRange, Random random)
        {
            point.position = Vector.Random(positionRange, random);
            if (point is EndNode endPoint)
            {
                endPoint.output = Vector.Random(outputRange, random);
                endPoint.Update();
                return;
            }

            UpdatePointRandomly(point.child1, positionRange, outputRange, random);
            UpdatePointRandomly(point.child2, positionRange, outputRange, random);
        }
    }
}

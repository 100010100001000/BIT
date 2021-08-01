using System;
using System.Collections.Generic;

namespace BinaryInterpolationTree
{
    public class EndNode : Node
    {
        public Vector output;
        public Vector deviationScale;

        public EndNode(Vector position, Vector output, Vector deviationScale) : 
            base(position, deviationScale.nbrOfDim)
        {
            this.deviationScale = deviationScale;
            this.output = output;
            Update();
        }

        public override void Update()
        {
            if (deviationScale.nbrOfDim != output.nbrOfDim)
                throw new Exception("dimensionality does not match");

            float max = 0;
            for (int i = 0; i < output.nbrOfDim; i++)
            {
                float o = output[i] / deviationScale[i];
                max = Math.Max(max, Math.Abs(o));
            }

            maxContribution = max;
            if (parent != null)
                parent.Update();
        }

        public override Vector Evaluate(Vector args, float maxInaccuracy, 
            float determinism, Random random, out HashSet<(Node, float)> contributions)
        {
            contributions = null;
            return output;
        }

        public override IEnumerable<Vector> Evaluate(Vector args, float determinism, Random random,
            HashSet<(Node, float)> contributions, HashSet<(Node, float)> potentialMinors, 
            HashSet<Node> minors,float contribution, float[] maxInaccuracy)
        {
            contributions.Add((this, contribution));
            yield return output;
        }
    }
}

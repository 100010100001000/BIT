using System.Collections.Generic;

namespace BinaryInterpolationTree
{
    public class Contribution
    {
        public float contribution;
        public float error;
        public float[] args;
        public float[] output;
        public float[] target;
        public float[] adjustment;

        public Contribution(float contribution, float[] args, float[] output, 
            float[] target, float[] adjustment, float error)
        {
            this.contribution = contribution;
            this.args = args;
            this.output = output;
            this.target = target;
            this.adjustment = adjustment;
            this.error = error;
        }
    }
}
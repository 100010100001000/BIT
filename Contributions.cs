using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BinaryInterpolationTree
{
    public class Contributions
    {
        public List<Contribution> this[Node p] => contributions[p];

        public readonly Dictionary<Node, List<Contribution>> contributions;

        public Contributions()
        {
            contributions = new Dictionary<Node, List<Contribution>>();
        }

        public bool Contains(Node point) => contributions.ContainsKey(point);

        public void AddContribution(Node point, float contribution,
            Vector args, Vector output, Vector target, float error)
        {
            if (!contributions.ContainsKey(point))
                contributions.Add(point, new List<Contribution>());

            Vector delta = target - output;
            Vector adjustment = contribution * delta;
            contributions[point].Add(new Contribution(contribution, 
                args, output, target, adjustment, error));
        }
    }
}
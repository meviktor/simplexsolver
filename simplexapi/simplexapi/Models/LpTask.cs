using System;

namespace simplexapi.Models
{
    public class LpTask : Entity
    {
        public DateTimeOffset SolvedAt { get; set; }

        public string LPModelAsJson { get; set; }

        public string SolutionAsJson { get; set; }
    }
}

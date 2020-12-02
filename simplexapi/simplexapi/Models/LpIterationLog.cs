using System;

namespace simplexapi.Models
{
    public class LpIterationLog : Entity
    {
        public Guid LpTaskId { get; set; }
        public virtual LpTask LpTask { get; set; }

        public string IterationLog { get; set; }
    }
}

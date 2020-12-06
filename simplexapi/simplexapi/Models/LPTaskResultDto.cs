using simplexapi.Common.Models;

namespace simplexapi.Models
{
    public class LPTaskResultDto
    {
        public bool SolutionFound { get; set; }
        
        public string Message { get; set; }

        public SimplexSolutionDto Solution { get; set; }
    }
}

using System;

namespace simplexapi.Models
{
    public class HistoryItemRowDto
    {
        public Guid Id { get; set; }

        public DateTimeOffset SolvedAt { get; set; }

        public string Name { get; set; }

        public bool IntegerProgramming { get; set; }
    }
}

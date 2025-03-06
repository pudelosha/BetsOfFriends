namespace Backend.DTOs
{
    using System;
    using System.ComponentModel.DataAnnotations;

    namespace Backend.DTOs
    {
        public class MatchDto
        {
            public int MatchId { get; set; }
            public string HomeTeam { get; set; }
            public string AwayTeam { get; set; }
            public DateTime MatchStart { get; set; }
            public int? HomeScore { get; set; }
            public int? AwayScore { get; set; }
            public string MatchStatus { get; set; }
        }

        public class MatchResultUpdateDto
        {
            [Required]
            public int MatchId { get; set; }

            [Required]
            public DateTime MatchStart { get; set; }

            public int? HomeScore { get; set; }
            public int? AwayScore { get; set; }


            public string? QualifiedTeam { get; set; } // "Home", "Away", or null
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackerLibrary.Model
{
  /// <summary>
  /// Represents one team in a matchup.
  /// </summary>
  public class MatchupEntryModel
  {
    /// <summary>
    /// The unique identifier for the MatchupEntry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Represents the Id of one team in the matchup.
    /// </summary>
    public int TeamCompetingId { get; set; }

    /// <summary>
    /// Represents one team in the matchup.
    /// </summary>
    public TeamModel TeamCompeting { get; set; }

    /// <summary>
    /// Represents the score for this particular team.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Represents the Id of the matchup that this team came
    /// from as the winner.
    /// </summary>
    public int ParentMatchupId { get; set; }

    /// <summary>
    /// Represents the matchup that this team came
    /// from as the winner.
    /// </summary>
    public MatchupModel ParentMatchup { get; set; }

  }
}

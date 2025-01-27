﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackerLibrary.Model
{
  /// <summary>
  /// Represents one match in the tournament.
  /// </summary>
  public class MatchupModel
  {
    /// <summary>
    /// The unique identifier for the Matchup.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The set of teams that were involved in this match.
    /// </summary>
    public List<MatchupEntryModel> Entries { get; set; } = new List<MatchupEntryModel>();

    /// <summary>
    /// The Id from the database that will be used to look up the winner.
    /// </summary>
    public int WinnerId { get; set; }

    /// <summary>
    /// The winner of the match.
    /// </summary>
    public TeamModel Winner { get; set; }

    /// <summary>
    /// Which round this match is part of.
    /// </summary>
    public int MatchupRound { get; set; }

    /// <summary>
    /// Which round this match is part of.
    /// </summary>
    public string DisplayName 
    {
      get
      {
        string output = "";
        foreach ( MatchupEntryModel me in Entries )
        {
          if (me.TeamCompeting != null)
          {
            if (output.Length == 0)
            {
              output = me.TeamCompeting.TeamName;
            }
            else
            {
              output += $" vs. {me.TeamCompeting.TeamName}";
            } 
          }
          else
          {
            output = "Matchup not yet determined";
            break;
          }
        }
        return output;
      }
    }

  }
}

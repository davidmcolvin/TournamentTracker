﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackerLibrary.Model;

namespace TrackerLibrary
{
  public static class TournamentLogic
  {
    // Create our matchups
    // Order our list randomly
    // Check if it is big enough and if not, add in byes
    // Create or first round of matchups
    // Create every round after that - 8 matchups -> 4 matchups -> 2 matchups -> 1 matchup

    public static void CreateRounds(TournamentModel model)
    {
      List<TeamModel> randomizedTeams = RandomizeTeamOrder(model.EnteredTeams);
      int rounds = FindNumberOfRounds(randomizedTeams.Count);
      int byes = FindNumberOfByes(rounds, randomizedTeams.Count);

      model.Rounds.Add(CreateFirstRound(byes, randomizedTeams));

      CreateOtherRounds(model, rounds);
    }

    public static void UpdateTournamentResults(TournamentModel model)
    {
      int startingRound = model.CheckCurrentRound();

      List<MatchupModel> toScore = new List<MatchupModel>();

      foreach (List<MatchupModel> round in model.Rounds)
      {
        foreach (MatchupModel rm in round)
        {
          if (rm.Winner == null && ( rm.Entries.Any(x => x.Score != 0) || rm.Entries.Count == 1) )
          {
            toScore.Add(rm);
          }
        }
      }

      MarkWinnerInMatchups(toScore);

      AdvanceWinners(toScore, model);

      toScore.ForEach(x => GlobalConfig.Connection.UpdateMatchup(x));

      int endingRound = model.CheckCurrentRound();

      if (endingRound > startingRound)
      {
        model.AlertUsersToNewRound();
      }
    }

    public static void AlertUsersToNewRound(this TournamentModel model)
    {
      int currentRoundNumber = model.CheckCurrentRound();
      List<MatchupModel> currentRound = model.Rounds.Where(x => x.First().MatchupRound == currentRoundNumber).First();

      foreach (MatchupModel matchup in currentRound)
      {
        foreach (MatchupEntryModel me in matchup.Entries)
        {
          foreach (PersonModel p in me.TeamCompeting.TeamMembers)
          {
            AlertPersonToNewRound(p, me.TeamCompeting.TeamName, matchup.Entries.Where(x => x.TeamCompeting != me.TeamCompeting).FirstOrDefault());
          }
        }
      }
    }

    private static void AlertPersonToNewRound(PersonModel p, string teamName, MatchupEntryModel competitor)
    {
      if (p.EmailAddress.Length == 0)
      {
        return;
      }

      string toAddress = p.EmailAddress;
      string subject = "";
      StringBuilder body = new StringBuilder();

      if (competitor != null)
      {
        subject = $"You have a new matchup with {competitor.TeamCompeting.TeamName}";

        body.AppendLine("<h1>You have a new matchup</h1>");
        body.Append("<strong>Competitor: </strong>");
        body.Append(competitor.TeamCompeting.TeamName);
        body.AppendLine();
        body.AppendLine();
        body.AppendLine("Have a great time!");
        body.AppendLine("~Tournament Tracker");


      }
      else
      {
        subject = "You have a bye this round";

        body.AppendLine("Enjoy your round off!");
        body.AppendLine("~Tournament Tracker");
      }

      EmailLogic.SendEmail(toAddress, subject, body.ToString());
    }

    private static int CheckCurrentRound(this TournamentModel model)
    {
      int output = 1;

      foreach (List<MatchupModel> round in model.Rounds)
      {
        if (round.All(x => x.Winner != null))
        {
            output += 1;
        }
        else
        {
          return output;
        }
      }

      CompleteTournament(model);

      return output - 1;
    }

    private static void CompleteTournament(TournamentModel model)
    {
      model.IsActive = false;
      GlobalConfig.Connection.CompleteTournament(model);

      // find the max round from all the rounds
      var result = from sublist in model.Rounds
                   select new { myMax = sublist.Max(x => x.MatchupRound) };
      int myMax = result.Max(x => x.myMax);

      // Rounds is list of list. 
      // Get the first list that contains the list of matchups where one of them has max round as round
      // get that first matchup of that matchup list (there will always be only one)
      TeamModel winner = model.Rounds.Where(x => x.First().MatchupRound == myMax)
        .First().First().Winner;
      TeamModel runnerUp = model.Rounds.Where(x => x.First().MatchupRound == myMax)
        .First().First().Entries.Where(x => x.TeamCompeting != winner).First().TeamCompeting;

      decimal winnerAmount = 0;
      decimal runnerUpAmount = 0;

      if (model.Prizes.Count > 0)
      {
        decimal totalIncome = model.EnteredTeams.Count * model.EntryFee;

        PrizeModel winnerPrize = model.Prizes.Where(x => x.PlaceNumber == 1).FirstOrDefault();
        PrizeModel runnerUpPrize = model.Prizes.Where(x => x.PlaceNumber == 2).FirstOrDefault();

        if (winnerPrize != null)
        {
          winnerAmount = winnerPrize.calculatePrizePayout(totalIncome);
        }

        if (runnerUpPrize != null)
        {
          runnerUpAmount = runnerUpPrize.calculatePrizePayout(totalIncome);
        }
      }

      // Send email to all tournament
      string subject = "";
      StringBuilder body = new StringBuilder();


      subject = $"In {model.TournamentName}, {winner.TeamName}, has won!";

      body.AppendLine("<h1>We have a winner!</h1>");
      body.AppendLine("<p>Congratulations to our winner on a great tournament </p>");
      body.AppendLine("<br />");

      if (winnerAmount > 0)
      {
        body.AppendLine($"<p>{winner.TeamName} will recieve ${winnerAmount}.</p>");
      }

      if (runnerUpAmount > 0)
      {
        body.AppendLine($"<p>{runnerUp.TeamName} will recieve ${runnerUpAmount}.</p>");
      }

      body.AppendLine("<p>Thanks for a great tournament everyone!</p>");
      body.AppendLine("~Tournament Tracker");

      List<string> bcc = new List<string>();

      foreach (TeamModel t in model.EnteredTeams)
      {
        foreach (PersonModel p in t.TeamMembers)
        {
          if (p.EmailAddress.Length > 0)
          {
            bcc.Add(p.EmailAddress);
          }
        }
      }

      EmailLogic.SendEmail(new List<string>(), bcc, subject, body.ToString());

      model.CompleteTournament();
    }

    private static decimal calculatePrizePayout(this PrizeModel prize, decimal totalIncome)
    {
      decimal output = 0;

      if (prize.PrizeAmount > 0)
      {
        output = prize.PrizeAmount;
      }
      else
      {
        output = Decimal.Multiply(totalIncome, Convert.ToDecimal(prize.PrizePercentage / 100));
      }

      return output;
    }

    private static void AdvanceWinners(List<MatchupModel> models, TournamentModel tournament)
    {
      foreach (MatchupModel m in models)
      {
        foreach (List<MatchupModel> round in tournament.Rounds)
        {
          foreach (MatchupModel matchupModel in round)
          {
            foreach (MatchupEntryModel matchupEntry in matchupModel.Entries)
            {
              if (matchupEntry.ParentMatchup != null)
              {
                if (matchupEntry.ParentMatchup.Id == m.Id)
                {
                  matchupEntry.TeamCompeting = m.Winner;
                  GlobalConfig.Connection.UpdateMatchup(matchupModel);
                }
              }
            }
          }
        }
      }
    }

    private static void MarkWinnerInMatchups(List<MatchupModel> models)
    {
      string greaterWins = ConfigurationManager.AppSettings["greaterWins"];

      foreach (MatchupModel m in models)
      {
        // This is for bye week entry
        if (m.Entries.Count == 1)
        {
          m.Winner = m.Entries[0].TeamCompeting;
          continue;
        }

        // 0 means false or low score wins
        if (greaterWins == "0")
        {
          if (m.Entries[0].Score < m.Entries[1].Score)
          {
            m.Winner = m.Entries[0].TeamCompeting;
          }
          else if (m.Entries[1].Score < m.Entries[0].Score)
          {
            m.Winner = m.Entries[1].TeamCompeting;
          }
          else
          {
            throw new Exception("I do not handle ties.");
          }

        }
        else
        {
          if (m.Entries[0].Score > m.Entries[1].Score)
          {
            m.Winner = m.Entries[0].TeamCompeting;
          }
          else if (m.Entries[1].Score > m.Entries[0].Score)
          {
            m.Winner = m.Entries[1].TeamCompeting;
          }
          else
          {
            throw new Exception("I do not handle ties.");
          }
        }
      }
    }

    private static void CreateOtherRounds(TournamentModel model, int rounds)
    {
      int round = 2;
      List<MatchupModel> previousRound = model.Rounds[0];
      List<MatchupModel> currRound = new List<MatchupModel>();
      MatchupModel currMatchup = new MatchupModel();

      while (round <= rounds)
      {
        foreach (MatchupModel match in previousRound)
        {
          currMatchup.Entries.Add(new MatchupEntryModel { ParentMatchup = match });

          if (currMatchup.Entries.Count > 1)
          {
            currMatchup.MatchupRound = round;
            currRound.Add(currMatchup);
            currMatchup = new MatchupModel();
          }
        }

        model.Rounds.Add(currRound);
        previousRound = currRound;

        currRound = new List<MatchupModel>();
        round += 1;
      }
    }

    private static List<MatchupModel> CreateFirstRound(int byes, List<TeamModel> teams)
    {
      List<MatchupModel> output = new List<MatchupModel>();
      MatchupModel curr = new MatchupModel();

      foreach (TeamModel team in teams)
      {
        curr.Entries.Add(new MatchupEntryModel { TeamCompeting = team });

        if (byes > 0 || curr.Entries.Count > 1)
        {
          curr.MatchupRound = 1;
          output.Add(curr);
          curr = new MatchupModel();

          if (byes > 0)
          {
            byes -= 1;
          }
        }
      }
      return output;
    }

    private static int FindNumberOfByes(int rounds, int numberOfTeams)
    {
      int output = 0;
      int totalTeams = 1;

      for (int i = 1; i <= rounds; i++)
      {
        totalTeams *= 2;
      }

      output = totalTeams - numberOfTeams;

      return output;
    }

    private static int FindNumberOfRounds(int teamCount)
    {
      int output = 1;
      int val = 2;

      while (val < teamCount)
      {
        output += 1;

        val *= 2;
      }

      return output;

    }

    private static List<TeamModel> RandomizeTeamOrder(List<TeamModel> teams)
    {
      return teams.OrderBy(x => Guid.NewGuid()).ToList();
    }
  }
}

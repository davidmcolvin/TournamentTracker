﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TrackerLibrary;
using TrackerLibrary.Model;

namespace TrackerUI
{
  public partial class CreateTeamForm : Form
  {
    private List<PersonModel> availableTeamMembers = GlobalConfig.Connection.GetPerson_All();
    private List<PersonModel> selectedTeamMembers = new List<PersonModel>();
    private ITeamRequester callingForm;

    public CreateTeamForm(ITeamRequester caller)
    {
      InitializeComponent();

      callingForm = caller;

      //CreateSampleData();

      WireUpLists();
    }

    private void CreateSampleData()
    {
      availableTeamMembers.Add(new PersonModel { FirstName = "Tim", LastName = "Corey" });
      availableTeamMembers.Add(new PersonModel { FirstName = "Sue", LastName = "Storm" });

      selectedTeamMembers.Add(new PersonModel { FirstName = "Jane", LastName = "Smith" });
      selectedTeamMembers.Add(new PersonModel { FirstName = "Bill", LastName = "Jones" });
    }

    private void WireUpLists()
    {
      selectTeamMemberComboBox.DataSource = null;

      selectTeamMemberComboBox.DataSource = availableTeamMembers;
      selectTeamMemberComboBox.DisplayMember = "FullName";

      teamMembersListBox.DataSource = null;

      teamMembersListBox.DataSource = selectedTeamMembers;
      teamMembersListBox.DisplayMember = "FullName";
    }

    private bool ValidateMember()
    {
      if (firstNameTextBox.Text.Length == 0)
      {
        return false;
      }

      if (lastNameTextBox.Text.Length == 0)
      {
        return false;
      }

      if (emailAddrTextBox.Text.Length == 0)
      {
        return false;
      }

      if (cellPhoneTextBox.Text.Length == 0)
      {
        return false;
      }

      return true;
    }

    private bool ValidateForm()
    {
      if (teamNameTextBox.Text == "")
      {
        return false;
      }

      if (selectedTeamMembers.Count <= 0)
      {
        return false;
      }

      return true;
    }

    private void createMemberButton_Click(object sender, EventArgs e)
    {
      if ( ValidateMember() )
      {
        PersonModel p = new PersonModel();

        p.FirstName = firstNameTextBox.Text;
        p.LastName = lastNameTextBox.Text;
        p.EmailAddress = emailAddrTextBox.Text;
        p.CellPhoneNumber = cellPhoneTextBox.Text;

        GlobalConfig.Connection.CreatePerson( p );

        selectedTeamMembers.Add(p);

        WireUpLists();

        firstNameTextBox.Text = "";
        lastNameTextBox.Text = "";
        emailAddrTextBox.Text = "";
        cellPhoneTextBox.Text = "";

      }
      else
      {
        MessageBox.Show("This member has invalid data.  Please check it and try again");
      }
    }

    private void addTeamMemberButton_Click(object sender, EventArgs e)
    {
      PersonModel p = (PersonModel)selectTeamMemberComboBox.SelectedItem;

      if (p != null)
      {
        availableTeamMembers.Remove(p);
        selectedTeamMembers.Add(p);

        WireUpLists(); 
      }
    }

    private void removeSelectedMemberButton_Click(object sender, EventArgs e)
    {
      PersonModel p = (PersonModel)teamMembersListBox.SelectedItem;

      if (p != null)
      {
        selectedTeamMembers.Remove(p);
        availableTeamMembers.Add(p);

        WireUpLists(); 
      }
    }

    private void createTeamButton_Click(object sender, EventArgs e)
    {
      if (ValidateForm())
      {
        TeamModel t = new TeamModel();

        t.TeamName = teamNameTextBox.Text;
        t.TeamMembers = selectedTeamMembers;

        GlobalConfig.Connection.CreateTeam(t);

        callingForm.TeamComplete(t);

        Close();
      }
      else
      {
        MessageBox.Show("This form has invalid data.  Please check it and try again");
      }
    }
  }
}

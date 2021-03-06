﻿namespace DotNetRu.Clients.Portable.ViewModel
{
    using System;
    using System.Linq;
    using System.Windows.Input;

    using DotNetRu.Clients.Portable.Helpers;
    using DotNetRu.DataStore.Audit.Models;
    using DotNetRu.DataStore.Audit.Services;
    using DotNetRu.Utils.Helpers;

    using FormsToolkit;

    using MvvmHelpers;

    using Xamarin.Forms;

    public class MeetupViewModel : ViewModelBase
    {
        private bool noSessionsFound;

        private TalkModel selectedTalkModel;

        private ICommand loadSessionsCommand;

        public MeetupViewModel(INavigation navigation, MeetupModel meetupModel = null, VenueModel venueModel = null)
            : base(navigation)
        {
            this.MeetupModel = meetupModel;
            this.VenueModel = venueModel;

            MessagingCenter.Subscribe<LocalizedResources>(
                this,
                MessageKeys.LanguageChanged,
                sender => this.OnPropertyChanged(nameof(this.MeetupTime)));

            this.TapVenueCommand = new Command(this.OnVenueTapped);
        }

        public ObservableRangeCollection<TalkModel> Talks { get; } = new ObservableRangeCollection<TalkModel>();                

        public MeetupModel MeetupModel { get; set; }

        public VenueModel VenueModel { get; set; }

        public string MeetupTime => this.MeetupModel.StartTime.Value.ToString("D");

        public TalkModel SelectedTalkModel
        {
            get => this.selectedTalkModel;

            set
            {
                this.selectedTalkModel = value;
                this.OnPropertyChanged();
                if (this.selectedTalkModel == null)
                {
                    return;
                }

                MessagingService.Current.SendMessage(MessageKeys.NavigateToSession, this.selectedTalkModel);

                this.SelectedTalkModel = null;
            }
        }

        public bool NoSessionsFound
        {
            get => this.noSessionsFound;

            set => this.SetProperty(ref this.noSessionsFound, value);
        }

        public ICommand TapVenueCommand { get; set; }

        public ICommand LoadTalksCommand => this.loadSessionsCommand
                                               ?? (this.loadSessionsCommand = new Command(this.ExecuteLoadSessions));

        public void OnVenueTapped()
        {
            this.LaunchBrowserCommand.Execute(this.VenueModel.MapUrl);
        }

        private void ExecuteLoadSessions()
        {
            if (this.IsBusy)
            {
                return;
            }

            try
            {
                this.IsBusy = true;
                this.NoSessionsFound = false;

                var sessions = TalkService.GetTalks(this.MeetupModel.TalkIDs).ToArray();
                this.Talks.ReplaceRange(sessions);

                this.NoSessionsFound = !sessions.Any();

                if (Device.RuntimePlatform != Device.UWP && FeatureFlags.AppLinksEnabled)
                {
                    foreach (var session in this.Talks)
                    {
                        try
                        {
                            // TODO uncomment

                            // data migration: older applinks are removed so the index is rebuilt again
                            // Application.Current.AppLinks.DeregisterLink(
                            // new Uri(
                            // $"http://{AboutThisApp.AppLinksBaseDomain}/{AboutThisApp.SessionsSiteSubdirectory}/{session.Id}"));

                            // Application.Current.AppLinks.RegisterLink(session.GetAppLink());
                        }
                        catch (Exception applinkException)
                        {
                            // don't crash the app
                            this.Logger.Report(applinkException, "AppLinks.RegisterLink", session.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.Report(ex, "Method", "ExecuteLoadSessions");
                MessagingService.Current.SendMessage(MessageKeys.Error, ex);
            }
            finally
            {
                this.IsBusy = false;
            }
        }
    }
}
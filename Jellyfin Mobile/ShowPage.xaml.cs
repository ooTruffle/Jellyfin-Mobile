using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using JellyfinMobile.Models;
using JellyfinMobile.Services;
using System.Linq;
using Windows.UI.Xaml;
using System.Collections.Generic;

namespace JellyfinMobile
{
    public sealed partial class ShowPage : Page
    {
        private MediaItem _show;
        private LibraryPageNavigationArgs _navArgs;
        private JellyfinService _jellyfinService = new JellyfinService();

        public ShowPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var args = e.Parameter as MediaPageNavigationArgs;
            _show = args.Item;
            _navArgs = args.Args;
            ShowTitle.Text = _show.Name;

            var seasons = await _jellyfinService.GetSeasonsAsync(_navArgs.ServerUrl, _show.Id, _navArgs.UserId, _navArgs.AccessToken);

            SeasonsPivot.Items.Clear();
            foreach (var season in seasons ?? new List<Season>())
            {
                {
                    var episodes = await _jellyfinService.GetEpisodesAsync(_navArgs.ServerUrl, season.Id, _navArgs.UserId, _navArgs.AccessToken);

                    var listView = new ListView
                    {
                        ItemsSource = episodes,
                        IsItemClickEnabled = true
                    };
                    listView.ItemClick += EpisodeListView_ItemClick;

                    var pivotItem = new PivotItem
                    {
                        Header = season.Name,
                        Content = listView
                    };
                    SeasonsPivot.Items.Add(pivotItem);
                }
            }

            private void EpisodeListView_ItemClick(object sender, ItemClickEventArgs e)
            {
                var episode = e.ClickedItem as MediaItem;
                if (episode != null)
                    Frame.Navigate(typeof(EpisodePage), new MediaPageNavigationArgs { Item = episode, Args = _navArgs });
            }
        }
    }
}
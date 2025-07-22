using Windows.UI.Xaml.Controls;
using System;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using JellyfinMobile.Models;
using System.Collections.Generic;

namespace JellyfinMobile
{
    public sealed partial class MoviePage : Page
    {
        private MediaItem _movie;
        private LibraryPageNavigationArgs _navArgs;

        public MoviePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var args = e.Parameter as MediaPageNavigationArgs;
            _movie = args.Item;
            _navArgs = args.Args;
            PosterImage.Source = new BitmapImage(new Uri(_movie.ImageUrl));
            MovieTitle.Text = _movie.Name;
            MovieOverview.Text = _movie.Overview;
        }

        private void PlayButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // Play logic: show a player page/control, e.g.
            Frame.Navigate(typeof(PlayerPage), new MediaPageNavigationArgs { Item = _movie, Args = _navArgs });
        }
    }
}
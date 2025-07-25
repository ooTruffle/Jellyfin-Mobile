﻿using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using JellyfinMobile.Models;

namespace JellyfinMobile
{
    public sealed partial class EpisodePage : Page
    {
        private MediaItem _episode;
        private LibraryPageNavigationArgs _navArgs;

        public EpisodePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var args = e.Parameter as MediaPageNavigationArgs;
            _episode = args.Item;
            _navArgs = args.Args;
            PosterImage.Source = new BitmapImage(new Uri(_episode.ImageUrl));
            EpisodeTitle.Text = _episode.Name;
            EpisodeOverview.Text = _episode.Overview;
        }

        private void PlayButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Frame.Navigate(typeof(PlayerPage), new MediaPageNavigationArgs { Item = _episode, Args = _navArgs });
        }
    }
}
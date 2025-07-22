using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using JellyfinMobile.Models;
using System.Collections.Generic;
using JellyfinMobile.Services;

namespace JellyfinMobile
{
    public sealed partial class LibraryPage : Page
    {
        private JellyfinService _jellyfinService = new JellyfinService();
        private LibraryPageNavigationArgs _args;

        public LibraryPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _args = e.Parameter as LibraryPageNavigationArgs;
            LibraryTitle.Text = _args.Library.Name;
            var items = await _jellyfinService.GetLibraryItemsAsync(_args.ServerUrl, _args.Library.Id, _args.UserId, _args.AccessToken);
            ItemListView.ItemsSource = items;
        }

        private void ItemListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as MediaItem;
            if (item == null) return;
            if (item.Type == "Movie")
                Frame.Navigate(typeof(MoviePage), new MediaPageNavigationArgs { Item = item, Args = _args });
            else if (item.Type == "Series")
                Frame.Navigate(typeof(ShowPage), new MediaPageNavigationArgs { Item = item, Args = _args });
        }
    }

    public class MediaPageNavigationArgs
    {
        public MediaItem Item { get; set; }
        public LibraryPageNavigationArgs Args { get; set; }
    }
}
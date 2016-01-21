/*
    Copyright (c) Microsoft Corporation All rights reserved.  
 
    MIT License: 
 
    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
    documentation files (the  "Software"), to deal in the Software without restriction, including without limitation
    the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
    and to permit persons to whom the Software is furnished to do so, subject to the following conditions: 
 
    The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. 
 
    THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
    TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
    THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using Microsoft.Band;
using Microsoft.Band.Tiles;
using Microsoft.Band.Tiles.Pages;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace TileEvents
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    partial class MainPage
    {
        private App viewModel;

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            this.viewModel.StatusMessage = "Running ...";

            try
            {
                // Get the list of Microsoft Bands paired to the phone.
                IBandInfo[] pairedBands = await BandClientManager.Instance.GetBandsAsync();
                if (pairedBands.Length < 1)
                {
                    this.viewModel.StatusMessage = "This sample app requires a Microsoft Band paired to your device. Also make sure that you have the latest firmware installed on your Band, as provided by the latest Microsoft Health app.";
                    return;
                }

                // Connect to Microsoft Band.
                using (IBandClient bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]))
                {
                    // Create a Tile with a TextButton on it.
                    Guid myTileId = new Guid("12408A60-13EB-46C2-9D24-F14BF6A033C6");
                    BandTile myTile = new BandTile(myTileId)
                    {
                        Name = "My Tile",
                        TileIcon = await LoadIcon("ms-appx:///Assets/SampleTileIconLarge.png"),
                        SmallIcon = await LoadIcon("ms-appx:///Assets/SampleTileIconSmall.png")
                    };
                    TextButton button = new TextButton() { ElementId = 1, Rect = new PageRect(10, 10, 200, 90) };
                    FilledPanel panel = new FilledPanel(button) { Rect = new PageRect(0, 0, 220, 150) };
                    myTile.PageLayouts.Add(new PageLayout(panel));

                    // Remove the Tile from the Band, if present. An application won't need to do this everytime it runs. 
                    // But in case you modify this sample code and run it again, let's make sure to start fresh.
                    await bandClient.TileManager.RemoveTileAsync(myTileId);
                    
                    // Create the Tile on the Band.
                    await bandClient.TileManager.AddTileAsync(myTile);
                    await bandClient.TileManager.SetPagesAsync(myTileId, new PageData(new Guid("5F5FD06E-BD37-4B71-B36C-3ED9D721F200"), 0, new TextButtonData(1, "Click here")));

                    // Subscribe to Tile events.
                    int buttonPressedCount = 0;
                    TaskCompletionSource<bool> closePressed = new TaskCompletionSource<bool>();

                    bandClient.TileManager.TileButtonPressed += (s, args) => 
                    {
                        var a = Dispatcher.RunAsync(
                            CoreDispatcherPriority.Normal,
                            () =>
                            {
                                buttonPressedCount++;
                                this.viewModel.StatusMessage = string.Format("TileButtonPressed = {0}", buttonPressedCount);
                            }
                        );
                    };
                    bandClient.TileManager.TileClosed += (s, args) => 
                    {
                        closePressed.TrySetResult(true);
                    };

                    await bandClient.TileManager.StartReadingsAsync();

                    // Receive events until the Tile is closed.
                    this.viewModel.StatusMessage = "Check the Tile on your Band (it's the last Tile). Waiting for events ...";

                    await closePressed.Task;
                    
                    // Stop listening for Tile events.
                    await bandClient.TileManager.StopReadingsAsync();

                    this.viewModel.StatusMessage = "Done.";
                }
            }
            catch (Exception ex)
            {
                this.viewModel.StatusMessage = ex.ToString();
            }
        }

        private async Task<BandIcon> LoadIcon(string uri)
        {
            StorageFile imageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));

            using (IRandomAccessStream fileStream = await imageFile.OpenAsync(FileAccessMode.Read))
            {
                WriteableBitmap bitmap = new WriteableBitmap(1, 1);
                await bitmap.SetSourceAsync(fileStream);
                return bitmap.ToBandIcon();
            }
        }
    }
}

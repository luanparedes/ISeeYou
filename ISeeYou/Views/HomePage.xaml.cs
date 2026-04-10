using LibVLCSharp.Shared;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using ISeeYou.AI;
using ISeeYou.ViewModels;

namespace ISeeYou
{
    public sealed partial class HomePage : Window
    {
        public HomePageViewModel ViewModel { get; } = new HomePageViewModel();

        public HomePage()
        {
            this.InitializeComponent();
        }
    }
}
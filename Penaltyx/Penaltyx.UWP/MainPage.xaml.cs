using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Penaltyx.UWP
{
    public sealed partial class MainPage
    {
        Penaltyx.App m_App;

        Penaltyx.MainPage Page()
        {
            return ((Penaltyx.MainPage)m_App.MainPage);
        }

        public MainPage()
        {
            this.InitializeComponent();

            m_App = new Penaltyx.App();
            LoadApplication(m_App);
        }
    }
}

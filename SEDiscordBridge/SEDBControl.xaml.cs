using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace SEDiscordBridge
{
    /// <summary>
    /// Interação lógica para SEDBControl.xaml
    /// </summary>
    public partial class SEDBControl : UserControl
    {
        private SEDicordBridgePlugin Plugin { get; }

        public SEDBControl()
        {
            InitializeComponent();
        }

        public SEDBControl(SEDicordBridgePlugin plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveConfig_OnClick(object sender, RoutedEventArgs e)
        {            
            Plugin.Save();
            Plugin.StopTimer();
            Plugin.DDBridge?.SendStatus(null);
            if (Plugin.Config.UseStatus)
            {
                Plugin.StartTimer();
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}

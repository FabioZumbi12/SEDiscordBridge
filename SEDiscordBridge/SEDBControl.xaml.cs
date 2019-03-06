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
        private SEDiscordBridgePlugin Plugin { get; }

        public SEDBControl()
        {
            InitializeComponent();
        }

        public SEDBControl(SEDiscordBridgePlugin plugin) : this()
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

        private void BtnAddFac_Click(object sender, RoutedEventArgs e)
        {
            if (txtAddFac.Text.Split(':').Length == 2 && !Plugin.Config.FactionChannels.Contains(txtAddFac.Text))
            {
                Plugin.Config.FactionChannels.Add(txtAddFac.Text);
                listFacs.ItemsSource = Plugin.Config.FactionChannels;
            }
        }

        private void BtnDelFac_Click(object sender, RoutedEventArgs e)
        {
            if (listFacs.SelectedIndex >= 0)
            {
                Plugin.Config.FactionChannels.Remove(listFacs.SelectedItem.ToString());
                listFacs.ItemsSource = Plugin.Config.FactionChannels;
            }
                
        }

        private void ListFacs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listFacs.SelectedIndex >= 0)
                txtAddFac.Text = listFacs.SelectedItem.ToString();
        }
    }
}

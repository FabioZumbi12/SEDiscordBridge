using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using VRage.Game;

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
            
                        
            cbFontColor.ItemsSource = new ObservableCollection<string>(typeof(MyFontEnum).GetFields().Select(x => x.Name).ToList());
            cbFacFontColor.ItemsSource = new ObservableCollection<string>(typeof(MyFontEnum).GetFields().Select(x => x.Name).ToList());
            UpdateDataGrid();
        }

        private void UpdateDataGrid()
        {
            var factions = from f in Plugin.Config.FactionChannels select new { Faction = f.Split(':')[0], Channel = f.Split(':')[1] };            
            dgFacList.ItemsSource = factions;
        }

        private void SaveConfig_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin.Save();
            Plugin.DDBridge?.SendStatus(null);

            if (Plugin.Config.Enabled)
            {
                if (Plugin.Torch.CurrentSession == null && !Plugin.Config.PreLoad)
                {
                    Plugin.LoadSEDB();

                }
                else
                {
                    Plugin.LoadSEDB();
                }
            }
            else
            {                
                Plugin.UnloadSEDB();
            }            
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void BtnAddFac_Click(object sender, RoutedEventArgs e)
        {
            if (txtFacName.Text.Length > 0 && txtFacChannel.Text.Length > 0)
            {
                Plugin.Config.FactionChannels.Add(txtFacName.Text + ":" + txtFacChannel.Text);
                UpdateDataGrid();
                dgFacList.Items.MoveCurrentToLast();
            }
        }

        private void BtnDelFac_Click(object sender, RoutedEventArgs e)
        {
            if (dgFacList.SelectedIndex >= 0)
            {
                dynamic dataRow = dgFacList.SelectedItem;
                Plugin.Config.FactionChannels.Remove(dataRow.Faction + ":" + dataRow.Channel);
                UpdateDataGrid();                
            }                
        }

        private void DgFacList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgFacList.SelectedIndex >= 0)
            {
                dynamic dataRow = dgFacList.SelectedItem;
                txtFacName.Text = dataRow.Faction;
                txtFacChannel.Text = dataRow.Channel;
            }               
        }

        private void CbFontColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Plugin.Config.GlobalColor = cbFontColor.SelectedValue.ToString();
        }
    }
}

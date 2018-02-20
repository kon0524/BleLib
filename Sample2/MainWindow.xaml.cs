using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sample2
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private BleLib.BleDevice _device;

        public MainWindow()
        {
            InitializeComponent();

            _device = new BleLib.BleDevice();
            _device.BleEvent += _device_BleEvent;
        }

        private void _device_BleEvent(object sender, BleLib.BleEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.textbox.Text += e.LocalName + Environment.NewLine;
            });
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            this.textbox.Clear();
            await _device.ConnectAsync("", 5000);
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            bool ret = await _device.ConnectAsync(this.localNameTextbox.Text);
            if (!ret) return;
            this.textbox.Clear();
            Guid[] services = _device.GetServiceUuids();
            foreach (var s in services)
            {
                Guid[] charas = await _device.GetCharacteristicUuidsAsync(s);
                this.textbox.Text += $"S:{s} -----" + Environment.NewLine;
                foreach (var c in charas)
                {
                    this.textbox.Text += $"  C:{c}" + Environment.NewLine;
                }
            }
            _device.Disconnect();
        }
    }
}

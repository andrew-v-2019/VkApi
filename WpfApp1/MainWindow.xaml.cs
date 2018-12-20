using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using VkApi.Models;
using VKApi.BL.Interfaces;
using VKApi.BL.Unity;
using VKApi.ChicksLiker;

namespace VkApi.WpfApp
{
    public partial class MainWindow : Window
    {
        private static IConfigurationProvider _configurationProvider;
        public LikeClickerConfiguration Configuration;

        public MainWindow()
        {
            ServiceInjector.ConfigureServices();
            _configurationProvider = ServiceInjector.Retrieve<IConfigurationProvider>();
            Configuration = GetConfigurations();
            InitializeComponent();
            FillControls();


            SetSizes();
        }

        private static LikeClickerConfiguration GetConfigurations()
        {
            var configuration = new LikeClickerConfiguration();
            configuration.CitiesString = _configurationProvider.GetConfig("Cities", "krasnoyarsk");
            _configurationProvider.FillConfigurationsHolder(configuration);
            return configuration;
        }


        private void FillControls()
        {
            var enumToList = Enum.GetValues(typeof(Strategy)).Cast<Strategy>().Select(x => x.ToString()).ToList();
            AlgoritmComboBox.FillComboBoxWithListItems(enumToList);
            GroupNameComboBox.FillComboBoxFromFile();
        }

        private void GroupNameComboBox_LostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            if (string.IsNullOrWhiteSpace(GroupNameComboBox.Text))
            {
                return;
            }

            var text = GroupNameComboBox.Text;
            if (GroupNameComboBox.ContainsText(text))
            {
                return;
            }

            GroupNameComboBox.AddStringToComboBoxItems(text);
            GroupNameComboBox.WriteComboBoxToFile();
        }

        private readonly int _height = 500;
        private readonly int _width = 430;
        private void SetSizes()
        {
            const int diff = 50;
            MainContainer.Height = _height;
            MainContainer.Width = _width;
            MainContainer.MinHeight = _height;
            MainContainer.MinWidth = _width;
            MainContainer.MaxHeight = _height;
            MainContainer.MaxWidth = _width;
            MainTabControl.Height = _height - diff;
            MainTabControl.Width = _width;
            MainTabControl.MinHeight = _height - diff;
            MainTabControl.MinWidth = _width;
            MainTabControl.MaxHeight = _height - diff;
            MainTabControl.MaxWidth = _width;
        }
    }
}

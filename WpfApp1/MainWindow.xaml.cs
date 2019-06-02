using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using VKApi.BL.Interfaces;
using VKApi.BL.Unity;
using VKApi.ChicksLiker;

namespace VkApi.WpfApp
{
    public partial class MainWindow : Window
    {
        private static IConfigurationProvider _configurationProvider;
        public ConfigurationViewModel Configuration;

        public MainWindow()
        {
            InitializeComponent();

            ServiceInjector.ConfigureServices();
            _configurationProvider = ServiceInjector.Retrieve<IConfigurationProvider>();
            FillControls();
            Configuration = GetConfigurations();
            var enumToList = Enum.GetValues(typeof(Strategy)).Cast<Strategy>().Select(x => x.ToString()).ToList();
            Configuration.Strategies = new List<string>();
            Configuration.Strategies.AddRange(enumToList);
            DataContext = Configuration;
           
            SetSizes();
        }

        private static ConfigurationViewModel GetConfigurations()
        {
            var configuration = new ConfigurationViewModel();
            configuration.CitiesString = _configurationProvider.GetConfig("Cities", "krasnoyarsk");
            configuration.GroupName = _configurationProvider.GetConfig(nameof(configuration.GroupName), "poisk_krk");
            configuration.MaxAge = Convert.ToInt32(_configurationProvider.GetConfig(nameof(configuration.MaxAge), "30"));
            configuration.MinAge = Convert.ToInt32(_configurationProvider.GetConfig(nameof(configuration.MinAge), "18"));
            configuration.PostsCountToAnalyze = Convert.ToInt32(_configurationProvider.GetConfig(nameof(configuration.PostsCountToAnalyze), "1000"));
            configuration.ProfilePhotosToLike = Convert.ToInt32(_configurationProvider.GetConfig(nameof(configuration.ProfilePhotosToLike), "1"));
            configuration.Password = _configurationProvider.GetConfig(nameof(configuration.Password));
            configuration.Login = _configurationProvider.GetConfig(nameof(configuration.Login));
            configuration.ApplicationId = _configurationProvider.GetConfig(nameof(configuration.ApplicationId));
            Strategy s = Strategy.PostsLikers;
            Enum.TryParse(_configurationProvider.GetConfig(nameof(configuration.Strategy), Strategy.PostsLikers.ToString()), out s);
            configuration.Strategy = s.ToString();
            return configuration;
        }


        private void FillControls()
        {
            
           // AlgoritmComboBox.FillComboBoxWithListItems(;enumToList);
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

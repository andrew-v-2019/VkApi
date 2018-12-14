using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using VKApi.ChicksLiker;

namespace VkApi.WpfApp
{
    public partial class MainWindow : Window
    {
        private readonly int _height = 500;
        private readonly int _width = 500;
        private List<string> GroupNames;

        public MainWindow()
        {
            InitializeComponent();
            FillControls();
            SetSizes();
            GroupNames = ReadGroupNamesFromFile();
        }

        private void FillControls()
        {
            var enumToList = Enum.GetValues(typeof(Strategy)).Cast<Strategy>();
            foreach (var enumItem in enumToList)
            {
                var item = new ComboBoxItem()
                {
                    Name = enumItem.ToString(),
                    Content = enumItem.ToString()
                };
                AlgoritmComboBox.Items.Add(item);
            }
        }

        private List<string> ReadGroupNamesFromFile()
        {
            var names = new List<string>();
            const string fileName = "groupNames.json";
            var filePath = AppDomain.CurrentDomain.BaseDirectory + "\\" + fileName;

            if (!File.Exists(filePath))
            {
                return names;
            }

            using (var r = new StreamReader(filePath))
            {
                var json = r.ReadToEnd();
                names = JsonConvert.DeserializeObject<List<string>>(json);
            }

            return names;
        }

        private void SetSizes()
        {
            const int diff = 38;
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

        private void GroupNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var a = e.AddedItems;
        }

        private void GroupNameComboBox_LostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            if (string.IsNullOrWhiteSpace(GroupNameComboBox.Text))
            {
                return;
            }
             
            
        }
    }
}

using System;
using System.Linq;
using System.Windows;
using VKApi.ChicksLiker;

namespace VkApi.WpfApp
{
    public partial class MainWindow : Window
    {   
        private  ulong PostsCountToAnalyze = 1000;
        private static string[] Cities = { "krasnoyarsk" };
        private int ProfilePhotosToLike = 2;
        //private int MinAge = 18;
        //private int MaxAge = 27;

        public MainWindow()
        {
            InitializeComponent();
            FillControls();
            SetSizes();        
        }

        private void FillControls()
        {
            var enumToList = Enum.GetValues(typeof(Strategy)).Cast<Strategy>().Select(x=>x.ToString()).ToList();
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

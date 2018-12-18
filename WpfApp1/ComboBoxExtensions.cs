using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;

namespace VkApi.WpfApp
{
    public static class ComboBoxExtensions
    {
        public static bool ContainsText(this ComboBox comboBox, string textToCheck)
        {
            foreach (ComboBoxItem combobBoxItem in comboBox.Items)
            {
                textToCheck = textToCheck.Trim().ToLower();
                var content = combobBoxItem.Content.ToString().Trim().ToLower();
                var contains = content.Contains(textToCheck);
                if (contains)
                {
                    return true;
                }
            }
            return false;
        }

        public static void FillComboBoxWithListItems(this ComboBox comboBox, List<string> items)
        {
            foreach (var stringToAdd in items)
            {
                comboBox.AddStringToComboBoxItems(stringToAdd);
            }
        }

        public static void AddStringToComboBoxItems(this ComboBox comboBox, string itemToAdd)
        {
            var item = new ComboBoxItem()
            {
                Content = itemToAdd
            };
            comboBox.Items.Add(item);
        }

        public static void FillComboBoxFromFile(this ComboBox comboBox)
        {
            var fileName = comboBox.GetFileNameForComboBox();
            var filePath = AppDomain.CurrentDomain.BaseDirectory + "\\" + fileName;
            if (!File.Exists(filePath))
            {
                return;
            }

            var items = new List<string>();
            using (var r = new StreamReader(filePath))
            {
                var json = r.ReadToEnd();
                items = JsonConvert.DeserializeObject<List<string>>(json);
            }

            comboBox.FillComboBoxWithListItems(items);
        }

        public static void WriteComboBoxToFile(this ComboBox comboBox)
        {
            var fileName = comboBox.GetFileNameForComboBox();
            var filePath = AppDomain.CurrentDomain.BaseDirectory + "\\" + fileName;
            using (StreamWriter file = File.CreateText(filePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                var stringItems = comboBox.ConvertComboBoxToStringList();
                serializer.Serialize(file, stringItems);
            }
        }

        private static List<string> ConvertComboBoxToStringList(this ComboBox comboBox)
        {
            var stringItems = new List<string>();
            foreach (ComboBoxItem combobBoxItem in comboBox.Items)
            {
                stringItems.Add(combobBoxItem.Content.ToString());
            }
            return stringItems;
        }

        private static string GetFileNameForComboBox(this ComboBox comboBox)
        {
            string fileName = $"{comboBox.Name}.json";
            return fileName;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Avalonia.Controls;

#pragma warning disable CS8600
#pragma warning disable CS8604
#pragma warning disable CS8602
#pragma warning disable CS8618

namespace ScePSX
{
    public class Translations
    {
        public static string LangFile = "./lang.xml";

        private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> Dictionary;

        private static SortedSet<string> _AvailableLanguages;

        public static string DefaultLanguage = "";

        public static Dictionary<string, string> Languages = new Dictionary<string, string>();

        public static string CurrentLangId = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        public static SortedSet<string> AvailableLanguages
        {
            get
            {
                if (Translations.Dictionary == null)
                {
                    Translations.Init();
                }
                return Translations._AvailableLanguages;
            }
        }

        public static void Init()
        {
            Translations.Dictionary = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            Translations._AvailableLanguages = new SortedSet<string>();
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(LangFile);
                LoadXml(xmlDocument);
                LoadLang(xmlDocument);
                SideLoad();
            } catch (Exception value4)
            {
                Console.Error.WriteLine(value4);
            }
        }

        private static void LoadXml(XmlDocument xmlDocument)
        {
            foreach (XmlNode xmlNode in xmlDocument.SelectNodes("/translations/category").Cast<XmlNode>())
            {
                string value = xmlNode.Attributes["id"].Value;
                if (!Translations.Dictionary.ContainsKey(value))
                {
                    Translations.Dictionary[value] = new Dictionary<string, Dictionary<string, string>>();
                }
                foreach (XmlNode xmlNode2 in xmlNode.SelectNodes("text").Cast<XmlNode>())
                {
                    string value2 = xmlNode2.Attributes["id"].Value;
                    if (!Translations.Dictionary[value].ContainsKey(value2))
                    {
                        Translations.Dictionary[value][value2] = new Dictionary<string, string>();
                    }
                    foreach (XmlNode xmlNode3 in xmlNode2.SelectNodes("translation").Cast<XmlNode>())
                    {
                        string value3 = xmlNode3.Attributes["lang"].Value;
                        string innerText = xmlNode3.InnerText;
                        if (Translations.DefaultLanguage == null)
                        {
                            Translations.DefaultLanguage = value3;
                        }
                        if (value3 != "xx")
                        {
                            Translations.AvailableLanguages.Add(value3);
                        }
                        Translations.Dictionary[value][value2][value3] = innerText;
                    }
                }
            }
        }

        private static void SideLoad()
        {
            foreach (var langkey in Languages.Keys)
            {
                var langpath = Path.GetDirectoryName(LangFile) + $"/lang-{langkey}.xml";
                if (!File.Exists(langpath))
                    continue;

                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(langpath);
                LoadXml(xmlDocument);
            }
        }

        private static void LoadLang(XmlDocument xmlDocument)
        {
            XmlNode langNode = xmlDocument.SelectSingleNode("//text[@id='lang']");
            if (langNode == null)
                return;
            Languages.Clear();
            foreach (XmlNode transNode in langNode.ChildNodes)
            {
                if (transNode.Name != "translation")
                    continue;

                string langCode = transNode.Attributes["lang"].Value;
                string langName = transNode.InnerText.Trim();

                Languages[langCode] = langName;
            }
        }

        public static void UpdateLang(object Target)
        {
            foreach (FieldInfo fieldInfo in Target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object fieldValue = fieldInfo.GetValue(Target);
                if (fieldValue == null)
                    continue;

                string category = null;
                string originalText = null;
                Action<string> setTextAction = null;

                switch (fieldInfo.FieldType.Name)
                {
                    case nameof(Window):
                        category = "forms";
                        var formItem = (Window)fieldValue;
                        originalText = (string)formItem.Title;
                        setTextAction = newText => formItem.Title = newText;
                        break;
                    case nameof(MenuItem):
                        category = "menus";
                        var menuItem = (MenuItem)fieldValue;
                        originalText = (string)menuItem.Header;
                        setTextAction = newText => menuItem.Header = newText;
                        break;

                    case nameof(TextBlock):
                        category = "labels";
                        var textBlock = (TextBlock)fieldValue;
                        originalText = textBlock.Text;
                        setTextAction = newText => textBlock.Text = newText;
                        break;

                    case nameof(Button):
                        category = "buttons";
                        var button = (Button)fieldValue;
                        originalText = button.Content?.ToString() ?? string.Empty;
                        setTextAction = newText => button.Content = newText;
                        break;

                    case nameof(CheckBox):
                        category = "checks";
                        var checkBox = (CheckBox)fieldValue;
                        originalText = checkBox.Content?.ToString() ?? string.Empty;
                        setTextAction = newText => checkBox.Content = newText;
                        break;

                    case nameof(RadioButton):
                        category = "radios";
                        var radioButton = (RadioButton)fieldValue;
                        originalText = radioButton.Content?.ToString() ?? string.Empty;
                        setTextAction = newText => radioButton.Content = newText;
                        break;

                    default:
                        continue;
                }

                string translatedText = Translations.GetString(category, fieldInfo.Name, CurrentLangId);
                string finalText = translatedText != string.Empty ? translatedText : originalText;
                setTextAction?.Invoke(finalText);
            }
        }

        public static string GetText(string TextId, string catrogy = "texts", string LangId = "")
        {
            if (Translations.Dictionary == null)
            {
                Translations.Init();
            }
            if (LangId == "")
            {
                LangId = CurrentLangId;
            }
            Dictionary<string, string> dictionary = [];
            string result;
            try
            {
                Dictionary<string, Dictionary<string, string>> dictionary2 = Translations.Dictionary[catrogy];
                dictionary = dictionary2[TextId];
                result = dictionary[LangId];
            } catch (Exception)
            {
                //Console.Error.WriteLine("Can't find key '{0}.{1}.{2}'", CategoryId, TextId, LangId);
                //Console.Error.WriteLine(value);
                try
                {
                    result = dictionary[Translations.DefaultLanguage];
                } catch
                {
                    result = $"{catrogy}.{TextId}";
                }
            }
            return result;
        }

        public static string GetString(string CategoryId, string TextId, string LangId = "")
        {
            if (Translations.Dictionary == null)
            {
                Translations.Init();
            }
            if (LangId == "")
            {
                LangId = CurrentLangId;
            }
            Dictionary<string, string> dictionary = [];
            string result;
            try
            {
                Dictionary<string, Dictionary<string, string>> dictionary2 = Translations.Dictionary[CategoryId];
                dictionary = dictionary2[TextId];
                result = dictionary[LangId];
            } catch (Exception)
            {
                //Console.Error.WriteLine("Can't find key '{0}.{1}.{2}'", CategoryId, TextId, LangId);
                //Console.Error.WriteLine(value);
                try
                {
                    result = dictionary[Translations.DefaultLanguage];
                } catch
                {
                    result = string.Empty;//string.Format("{0}.{1}", CategoryId, TextId);
                }
            }
            return result;
        }

        public static string GetString(string CategoryId, string TextId, CultureInfo CultureInfo)
        {
            string twoLetterISOLanguageName = CultureInfo.TwoLetterISOLanguageName;
            return Translations.GetString(CategoryId, TextId, twoLetterISOLanguageName);
        }
    }
}

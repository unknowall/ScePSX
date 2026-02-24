using LightGL.DynamicLibrary;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;

namespace ScePSX.UI
{
    public class Translations
    {
        public static string LangFile = "./lang/lang.xml";

        private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> Dictionary;

        private static SortedSet<string> _AvailableLanguages;

        public static string DefaultLanguage = null;

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
            }
            catch (Exception value4)
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
                if (fieldInfo.FieldType == typeof(ToolStripMenuItem))
                {
                    ToolStripMenuItem Item = (ToolStripMenuItem)fieldInfo.GetValue(Target);
                    if (Item == null) continue;
                    string @string = Translations.GetString("menus", Item.Name, CurrentLangId);
                    string text2 = (@string != null) ? @string : Item.Text;
                    if (Platform.IsMono)
                    {
                        text2 = text2.Replace("&", "");
                    }
                    Item.Text = text2;
                }

                if (fieldInfo.FieldType == typeof(Label))
                {
                    Label Item = (Label)fieldInfo.GetValue(Target);
                    if (Item == null) continue;
                    string @string = Translations.GetString("labels", Item.Name, CurrentLangId);
                    string text2 = (@string != null) ? @string : Item.Text;
                    Item.Text = text2;
                }

                if (fieldInfo.FieldType == typeof(Button))
                {
                    Button Item = (Button)fieldInfo.GetValue(Target);
                    if (Item == null) continue;
                    string @string = Translations.GetString("buttons", Item.Name, CurrentLangId);
                    string text2 = (@string != null) ? @string : Item.Text;
                    Item.Text = text2;
                }

                if (fieldInfo.FieldType == typeof(CheckBox))
                {
                    CheckBox Item = (CheckBox)fieldInfo.GetValue(Target);
                    if (Item == null) continue;
                    string @string = Translations.GetString("checks", Item.Name, CurrentLangId);
                    string text2 = (@string != null) ? @string : Item.Text;
                    Item.Text = text2;
                }

                if (fieldInfo.FieldType == typeof(RadioButton))
                {
                    RadioButton Item = (RadioButton)fieldInfo.GetValue(Target);
                    if (Item == null) continue;
                    string @string = Translations.GetString("radios", Item.Name, CurrentLangId);
                    string text2 = (@string != null) ? @string : Item.Text;
                    Item.Text = text2;
                }

                if (fieldInfo.FieldType == typeof(GroupBox))
                {
                    GroupBox Item = (GroupBox)fieldInfo.GetValue(Target);
                    if (Item == null) continue;
                    string @string = Translations.GetString("groups", Item.Name, CurrentLangId);
                    string text2 = (@string != null) ? @string : Item.Text;
                    Item.Text = text2;
                }
            }

            if (Target.GetType().BaseType == typeof(Form))
            {
                if (Target.GetType().Name == "MainForm") return;
                string @string = Translations.GetString("forms", Target.GetType().Name, CurrentLangId);
                string text2 = (@string != null) ? @string : (Target as Form).Text;
                (Target as Form).Text = text2;
            }
        }

        public static string GetText(string TextId, string LangId = null)
        {
            if (Translations.Dictionary == null)
            {
                Translations.Init();
            }
            if (LangId == null)
            {
                LangId = CurrentLangId;
            }
            Dictionary<string, string> dictionary = null;
            string result;
            try
            {
                Dictionary<string, Dictionary<string, string>> dictionary2 = Translations.Dictionary["texts"];
                dictionary = dictionary2[TextId];
                result = dictionary[LangId];
            }
            catch (Exception)
            {
                //Console.Error.WriteLine("Can't find key '{0}.{1}.{2}'", CategoryId, TextId, LangId);
                //Console.Error.WriteLine(value);
                try
                {
                    result = dictionary[Translations.DefaultLanguage];
                }
                catch
                {
                    result = string.Format("texts.{0}", TextId);
                }
            }
            return result;
        }

        public static string GetString(string CategoryId, string TextId, string LangId = null)
        {
            if (Translations.Dictionary == null)
            {
                Translations.Init();
            }
            if (LangId == null)
            {
                LangId = CurrentLangId;
            }
            Dictionary<string, string> dictionary = null;
            string result;
            try
            {
                Dictionary<string, Dictionary<string, string>> dictionary2 = Translations.Dictionary[CategoryId];
                dictionary = dictionary2[TextId];
                result = dictionary[LangId];
            }
            catch (Exception)
            {
                //Console.Error.WriteLine("Can't find key '{0}.{1}.{2}'", CategoryId, TextId, LangId);
                //Console.Error.WriteLine(value);
                try
                {
                    result = dictionary[Translations.DefaultLanguage];
                }
                catch
                {
                    result = null;//string.Format("{0}.{1}", CategoryId, TextId);
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

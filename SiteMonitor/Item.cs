using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Forms;

namespace SiteMonitor
{
    /// <summary>
    /// Класс, установки для одной страницы
    /// </summary>
    public class Item
    {
        public Item() { }
        public string URL;//Адрес страницы
        public string Email;//Адрес почты, куда присылать уведомления
        public bool NeedContains;//Вспомогательное поле, указывает необходимость проверки на содержимое страницы
        public bool NeedNotify;//Вспомогательное поле, указывает на необходимость уведомления
        public List<Content> cList = new List<Content>();// Список содержимого, на которое нужна проверка
        /// <summary>
        /// Запись списка адресов страниц с параметрами в файл
        /// </summary>
        /// <param name="myList">Список страниц с параметрами</param>
        /// <param name="directory">Путь для сохранения</param>
        /// <returns>True, если все получилось; False, если возникла ошибка</returns>
        public static bool SaveListToFile(List<Item> myList, string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                XmlSerializer XmlSer = new XmlSerializer(myList.GetType());
                StreamWriter Writer = new StreamWriter(directory + @"\INDEX.LIST");
                XmlSer.Serialize(Writer, myList);
                Writer.Close();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Exception saving list", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
        }
        /// <summary>
        /// Чтение списка адресов страниц с параметрами из файла
        /// </summary>
        /// <param name="directory">Путь к папке с файлом списка</param>
        /// <returns>Список страниц с параметрами в случае удачи, или null в случае неудачи</returns>
        public static List<Item> LoadListFromFile(string directory)
        {
            List<Item> myList = new List<Item>();
            try
            {
                if (Directory.Exists(directory))
                {
                    if (File.Exists(directory + @"\INDEX.LIST"))
                    {
                        XmlSerializer XmlSer = new XmlSerializer(myList.GetType());
                        FileStream Set = new FileStream(directory + @"\INDEX.LIST", FileMode.Open);
                        myList = (List<Item>)XmlSer.Deserialize(Set);
                        Set.Close();
                        return myList;
                    }
                    else return null;
                }
                else return null;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Exception loading list", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return null;
            }
        }
    };
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Forms;

namespace SiteMonitor
{
    /// <summary>
    /// Класс, статистика одного тестирования
    /// </summary>
    public class Stat
    {
        public Stat() { }
        public string DT;//Дата и время тестирования
        public string StCode;//Статусный код, возвращенный на запрос
        public string Desc;//Описание статусного кода
        public string Notify;//Статус почтового уведомления
        public bool NeedContains;//True, если была необходима проверка на содержание на момент этого теста; False, если нет.
        public int Ok;//1-если не было возвращено ошибки на запрос страницы, 0 - если была. Используется для подсчета статистики.
        public int NotiErr;//1-если была ошибка при попытке отправки уведомления, 0 - если не было ошибки. Используется для подсчета статистики.
        public string Contains;//Содержит ОК, если проверка на текст страницы прошла; либо текст с указанием не найденого нужного текста, либо найденого ненужного.
        /// <summary>
        /// Сохранение списка статистики для страницы в файл
        /// </summary>
        /// <param name="myList">Список статистики</param>
        /// <param name="url">URL страницы, для которой эта статистика</param>
        /// <param name="directory">Папка сохранения логов</param>
        /// <returns>True, если все получилось; False, если возникла ошибка</returns>
        public static bool SaveStatToFile(List<Stat> myList, string url, string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                XmlSerializer XmlSer = new XmlSerializer(myList.GetType());
                StreamWriter Writer = new StreamWriter(directory + @"\" + url.GetHashCode().ToString() + @".log");
                XmlSer.Serialize(Writer, myList);
                Writer.Close();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Exception saving statistics", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
        }
        /// <summary>
        /// Чтение статистики для страницы из файла
        /// </summary>
        /// <param name="url">URL страницы, для которой эта статистика</param>
        /// <param name="directory">Папка сохранения логов</param>
        /// <returns>Список статистики для страницы в случае удачи, или null в случае неудачи</returns>
        public static List<Stat> LoadListFromFile(string url, string directory)
        {
            List<Stat> myList = new List<Stat>();
            try
            {
                if (Directory.Exists(directory))
                {
                    if (File.Exists(directory + @"\" + url.GetHashCode().ToString() + @".log"))
                    {
                        XmlSerializer XmlSer = new XmlSerializer(myList.GetType());
                        FileStream Set = new FileStream(directory + @"\" + url.GetHashCode().ToString() + @".log", FileMode.Open);
                        myList = (List<Stat>)XmlSer.Deserialize(Set);
                        Set.Close();
                        return myList;
                    }
                    else return null;
                }
                else return null;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Exception loading statistics", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return null;
            }
        }
    }
}

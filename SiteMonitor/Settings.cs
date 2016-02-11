using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Net;
using System.Web;
using System.Windows.Forms;

namespace SiteMonitor
{
    /// <summary>
    /// Класс для хранения настроек, из записи и чтения, содержит метод для отправки почты
    /// </summary>
    public class Settings
    {
        #region ClassFields
        public Settings() { }
        private string LogFolderName = Environment.CurrentDirectory;//Путь сохранения логов
        private string SettingsPath = Environment.CurrentDirectory + @"\SMConfig.conf";//Имя файла+путь для настроек
        private string SettingsFolder = Environment.CurrentDirectory;//Путь для файла настроек
        private int TestPeriod = 60;//Период ожидания между сессиями тестирования
        private string SMTPServ;//Адрес сервера исходящей почты
        private int PortN = 25;//Номер порта сервера исходящей почты
        private string Sender;//Адрес отправителя
        private string Username;//Имя пользователя для сервера исходящей почты
        private string Password;//Пароль для сервера исходящей почты
        private bool Secure;// Использование SSL
        public bool NotifyAllow;//Вспомогательное поле, использовать уведомления или нет
        public static bool FDCall;//Вспомогательное поле, используется для определения необходимости обновления списка URL
        /// <summary>
        /// Класс для сериализации настроек
        /// </summary>
        public class Options
        {
            public Options() { }
            public string LogFolderName;
            public int TestPeriod;
            public bool NotifyAllow;
            public string SMTPServer;
            public int PortNumber;
            public string FromField;
            public string Username;
            public string Password;
            public bool SSLEnabled;
        };
        #endregion
        #region ClassProperties
        /// <summary>
        /// Свойства класса для соответствующих полей
        /// </summary>
        public bool SSL
        {
            get { return Secure; }
            set { Secure = value; }
        }
        public string From
        {
            get { return Sender; }
            set { Sender = value.ToLower(); }
        }
        public string User
        {
            get { return Username; }
            set { Username = value; }
        }
        public string Pass
        {
            get { return Password; }
            set { Password = value; }
        }
        public int Port
        {
            get { return PortN; }
            set { PortN = value; }
        }
        public string Smtp
        {
            get { return SMTPServ; }
            set { SMTPServ = value.ToLower(); }
        }
        public int Period
        {
            set { TestPeriod = value; }
            get { return TestPeriod; }
        }
        public string FolderName
        {
            set { LogFolderName = value; }
            get { return LogFolderName; }

        }
        #endregion
        #region ClassMethods
        /// <summary>
        /// Сохранение настроек в файл
        /// </summary>
        /// <returns>True, если все получилось; False, если возникла ошибка</returns>
        public bool SaveSetToFile()
        {
            Options OP = new Options();
            OP.NotifyAllow = this.NotifyAllow;
            OP.LogFolderName = this.LogFolderName;
            OP.TestPeriod = this.TestPeriod;
            OP.SMTPServer = this.SMTPServ;
            OP.PortNumber = this.PortN;
            OP.FromField = this.Sender;
            OP.Username = this.Username;
            OP.Password = this.Password;
            OP.SSLEnabled = this.Secure;
            try
            {
                if (!Directory.Exists(SettingsFolder))
                {
                    Directory.CreateDirectory(SettingsFolder);
                }
                XmlSerializer XmlSer = new XmlSerializer(OP.GetType());
                StreamWriter Writer = new StreamWriter(SettingsPath);
                XmlSer.Serialize(Writer, OP);
                Writer.Close();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Exception saving settings", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
        }
        /// <summary>
        /// Чтение настроек из файла
        /// </summary>
        /// <returns>True, если все получилось; False, если возникла ошибка</returns>
        public bool LoadSetfromFile()
        {
            Options OP = new Options();
            try
            {
                if (Directory.Exists(SettingsFolder))
                {
                    if (File.Exists(SettingsPath))
                    {
                        XmlSerializer XmlSer = new XmlSerializer(typeof(Options));
                        FileStream Set = new FileStream(SettingsPath, FileMode.Open);
                        OP = (Options)XmlSer.Deserialize(Set);
                        Set.Close();
                        this.NotifyAllow = OP.NotifyAllow;
                        this.LogFolderName = OP.LogFolderName;
                        this.TestPeriod = OP.TestPeriod;
                        this.SMTPServ = OP.SMTPServer;
                        this.PortN = OP.PortNumber;
                        this.Sender = OP.FromField;
                        this.Username = OP.Username;
                        this.Password = OP.Password;
                        this.Secure = OP.SSLEnabled;
                        return true;
                    }
                    else return false;
                }
                else return false;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Exception loading settings", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
        }
        /// <summary>
        /// Отправляет почту используя настройки сервера из класса
        /// </summary>
        /// <param name="to">Адрес получателя</param>
        /// <param name="subject">Тема письма</param>
        /// <param name="body">Текст письма</param>
        /// <returns>Пустая строка в случае успеха, сообщение об ошибке в случае неудачи</returns>
        public string SendMail(string to, string subject, string body)
        {
            try
            {
                SmtpClient Smtp = new SmtpClient(SMTPServ, PortN);
                Smtp.EnableSsl = Secure;
                Smtp.Credentials = new NetworkCredential(Username, Password);
                MailMessage Message = new MailMessage();
                Message.From = new MailAddress(Sender);
                Message.To.Add(new MailAddress(to));
                Message.Subject = subject;
                Message.Body = body;
                Smtp.Send(Message);
                return "";
            }
            catch (Exception es)
            {
                return es.Message;
            }
        }
        #endregion
        #region ValClassMeth
        /// <summary>
        /// Проверка номера порта на то, что порт положительное число от 1 до 65535
        /// </summary>
        /// <param name="port">Номер порта</param>
        /// <returns>True, если подходит под условия; False, если не подходит</returns>
        public bool PortVal(string port)
        {
            if (Regex.IsMatch(port, @"^\d+$"))
            {
                try
                {
                    if ((Convert.ToInt32(port)) > 0 && (Convert.ToInt32(port)) < 65536)
                    {
                        this.PortN = Convert.ToInt32(port);
                        return true;
                    }
                    else return false;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }
            else return false;
        }
        /// <summary>
        /// Проверка периода на то, что при выполнении не возникнет переполнения при умножении на 60000мс.
        /// </summary>
        /// <param name="period">Период в минутах</param>
        /// <returns>True, если подходит под условия; False, если не подходит</returns>
        public bool PeriodVal(string period)
        {
            if (Regex.IsMatch(period, @"^\d+$"))
            {
                try
                {
                    if ((Convert.ToInt32(period)) > 0 && (Convert.ToInt32(period)) < (Math.Abs(Int32.MaxValue / 60000 - 1)))
                    {
                        return true;
                    }
                    else return false;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }
            else return false;
        }
        /// <summary>
        /// Вызывает диалог выбора папки. В случае вызова данного метода FDCall устанавливается в true
        /// </summary>
        /// <param name="foldDial">Диалог выбора папки из интерфейса</param>
        /// <returns>Путь папки логов</returns>
        public string FolderVal(FolderBrowserDialog foldDial)
        {
            DialogResult result = foldDial.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.LogFolderName = foldDial.SelectedPath;
            }
            else if (result == DialogResult.Cancel)
            {
                MessageBox.Show(@"Site Monitor uses application run folder by default", "No folder choosen", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.LogFolderName = Environment.CurrentDirectory;
            }
            FDCall = true;
            return this.LogFolderName;
        }
        #endregion
    };
}

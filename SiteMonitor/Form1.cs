using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
using System.Web;
using System.Threading;
using System.IO; 


namespace SiteMonitor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

#region MyVariables
        /// <summary>
        /// Экземпляр класса настроек
        /// </summary>
        Settings mySet = new Settings();
        /// <summary>
        /// Экземпляр класса Item, в который записываются данные для добавляемой или редактируемой страницы, перед записью в список
        /// </summary>
        Item ValidItem = new Item();
        bool AddBtn;// Нажата кнопка ADD, то True; Нажата кнопка Edit, то False
        /// <summary>
        /// Заполняет listBox1 и listBox2
        /// </summary>
        /// <param name="myList">Список адресов страниц с параметрами</param>
        void ListBoxFiller(List<Item> myList)
        {
            listBox1.BeginUpdate();
            listBox2.BeginUpdate();
            foreach (Item It in myList)
            {
                listBox1.Items.Add(It.URL.ToString());
                listBox1.SelectedIndex = 0;
                listBox2.Items.Add(It.Email.ToString());
                listBox2.SelectedIndex = 0;
            }
            listBox1.EndUpdate();
            listBox2.EndUpdate();
        }
        /// <summary>
        /// Очищение listBox1 и listBox2 
        /// </summary>
        void ListBoxClear()
        {
            listBox1.Items.Clear();
            listBox2.Items.Clear();
        }
        /// <summary>
        /// Очищает и заполняет listBox3 из списка текста содержания объекта ValidItem
        /// </summary>
        void ListBox3Refiller()
        {
            string Sign;
            listBox3.Items.Clear();
            listBox3.BeginUpdate();
            foreach (Content Cont in ValidItem.cList)
            {
                if (Cont.Contains == true) Sign = "+";
                else Sign = "-";
                listBox3.Items.Add("["+Sign+"]{"+Cont.Text+"}");    
            }
            if(listBox3.Items.Count!=0)listBox3.SelectedIndex = 0;
            listBox3.EndUpdate();
        }
        /// <summary>
        /// Заполняет DataGridView и смежные label`ы статистикой по выбраной в listBox1 странице
        /// </summary>
        void DataGridFiller()
        {
                    dataGridView1.Rows.Clear();
                    int OK = 0;
                    int NotiErr = 0;
                    int Contains = 0;
                    bool nCont = false;
                    label9.Visible = false;
                    label10.Visible = false;
                    label31.Visible = false;
                    label30.Visible = false;
                    List<Stat> sList = new List<Stat>();
                    sList = Stat.LoadListFromFile(listBox1.SelectedItem.ToString(), mySet.FolderName);
                    if (sList != null && sList.Count != 0)
                    {
                        foreach (Stat ST in sList)
                        {
                            if (ST.NeedContains)
                            {
                                nCont = true;
                                if (ST.Contains == "OK") Contains=Contains+0;
                                else Contains=Contains+1;
                            }
                            dataGridView1.Rows.Add(ST.DT, ST.StCode, ST.Desc, ST.Contains, ST.Notify);
                            OK = OK + ST.Ok;
                            NotiErr = NotiErr + ST.NotiErr;
                        }
                        toolStripStatusLabel4.Text = "Statistics loaded successfully";
                        label4.Text = OK.ToString();
                        label6.Text = sList.Count.ToString();
                        if (nCont)
                        {
                            label31.Visible = true;
                            label30.Visible = true;
                            if (Contains > 0) label31.Text = "Problem!";
                            else label31.Text = "OK";
                        }
                        float av = (OK * 100) / sList.Count;
                        label8.Text = string.Format("{0:F2}%", av);
                        if (NotiErr > 0)
                        {
                            label9.Visible = true;
                            label10.Visible = true;
                            label10.Text = NotiErr.ToString();
                        }
                    }
                    else toolStripStatusLabel4.Text = "No statistics available";
        }
#endregion
#region BackgroundWorker
        //Работа с процессом Монитора
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e) 
        {
            if(!backgroundWorker1.CancellationPending)
            {
                int i = 0;//Используется для подсчета процента выполнения
                string MailErr;//Ошибка отправки почты
                string myResponse;//Текст страницы
                string Contains="";//Заполняется текстом, который должен был быть, но не был найден на странице
                string NotContains="";//Заполняется текстом, который не должен был быть, но был найден на странице
                List<Item> mList = new List<Item>();//Список страниц с параметрами
                List<Stat> sList = new List<Stat>();//Список статистики для каждой страницы
                Stat cStat = new Stat();//Единственная запись статистики
                backgroundWorker1.ReportProgress(170,"Monitor process started");
                backgroundWorker1.ReportProgress(150, "Started");
                while (!backgroundWorker1.CancellationPending)//Периодические проверки на запрос остановки Монитора
                {
                    mList = Item.LoadListFromFile(mySet.FolderName);
                    //Если есть страницы для проверки, то начинаем...
                    if (mList != null && mList.Count != 0)
                    {
                        backgroundWorker1.ReportProgress(170, "Monitor process running");
                        backgroundWorker1.ReportProgress(150, "Running");
                        i = 0;
                        foreach(Item IT in mList)
                        {
                            backgroundWorker1.ReportProgress(150, "Checking "+IT.URL);
                            if (!backgroundWorker1.CancellationPending)
                            {
                                cStat.NeedContains = IT.NeedContains;
                                cStat.DT=DateTime.Now.ToString();
                                //Попытка обратиться к странице
                                try
                                {
                                    HttpWebRequest myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(IT.URL);
                                    HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                                    cStat.StCode=myHttpWebResponse.StatusCode.ToString();
                                    cStat.Desc=myHttpWebResponse.StatusDescription;
                                    cStat.Ok = 1;
                                    cStat.NotiErr = 0;
                                    cStat.Notify = "No need";
                                    //Если нужна проверка на содержимое, то она будет выполнена
                                    if (IT.NeedContains)
                                    {
                                        StreamReader myStreamReader = new StreamReader(myHttpWebResponse.GetResponseStream(), Encoding.GetEncoding(1251));
                                        myResponse = myStreamReader.ReadToEnd();
                                        //Проверяем на каждое выражение из списка содержимого
                                        foreach (Content CT in IT.cList)
                                        {
                                            //Проверка на то что страница должна содержать
                                            if (CT.Contains)
                                            {
                                                if (myResponse.Contains(CT.Text))
                                                {
                                                    backgroundWorker1.ReportProgress(150, "Needed content found");
                                                }
                                                else
                                                {
                                                    backgroundWorker1.ReportProgress(150, "Needed content not found");
                                                    Contains = Contains + "{" + CT.Text + "}; ";
                                                }
                                            }
                                                //Проверка на то что страницы не должна содержать
                                            else
                                            {
                                                if (!myResponse.Contains(CT.Text))
                                                {
                                                    backgroundWorker1.ReportProgress(150, "Uneeded content not found");
                                                }
                                                else
                                                {
                                                    backgroundWorker1.ReportProgress(150, "Uneeded content found");
                                                    NotContains = NotContains + "{" + CT.Text + "}; ";
                                                }
                                            }
                                        }
                                        //Если возникли проблемы с содержимым страницы
                                        if (Contains != "" || NotContains != "")
                                        {
                                            // то заполняются строки того что было или небыло найдено
                                            if (Contains == "")
                                            {
                                                cStat.Contains = "Needed content found. ";
                                                Contains = "Needed content found. ";
                                            }
                                            else
                                            {
                                                Contains ="Needed content not found: "+Contains+" ";
                                                cStat.Contains = Contains;
                                            }
                                            if (NotContains == "")
                                            {
                                                cStat.Contains = cStat.Contains+"Unneeded content not found.";
                                                NotContains = "Unneeded content not found.";
                                            }
                                            else
                                            {
                                                NotContains="Unneeded content found: " + NotContains;
                                                cStat.Contains = cStat.Contains + NotContains;
                                            }
                                            //Если уведомления настроены, то отсылаем уведомление
                                            if (mySet.NotifyAllow && IT.NeedNotify)
                                            {
                                                backgroundWorker1.ReportProgress(150, "Sending e-mail");
                                                MailErr = mySet.SendMail(IT.Email, "Site Monitor Notification for " + IT.URL+". Content problems!", @"Site Monitor Notification
 [" + cStat.DT + "] " + IT.URL + " "+Contains+NotContains);
                                                if (MailErr == "")
                                                {
                                                    cStat.NotiErr = 0;
                                                    cStat.Notify = "Notification sent";
                                                    backgroundWorker1.ReportProgress(150, "E-mail sent");
                                                }
                                                else
                                                {
                                                    cStat.Notify = "ERROR: " + MailErr;
                                                    cStat.NotiErr = 1;
                                                    backgroundWorker1.ReportProgress(150, "ERROR sending e-mail");
                                                }
                                            }
                                            else
                                            {
                                                cStat.Notify = "Notification disabled";
                                                cStat.NotiErr = 0;
                                            }
                                        }
                                        else cStat.Contains="OK";
                                    }
                                    else cStat.Contains = "Not set";
                                    myHttpWebResponse.Close();
                                    backgroundWorker1.ReportProgress(150, cStat.StCode);
                                }
                                //Отлавливается исключение, которе возникает в случае недоступности страницы
                                catch (WebException er)
                                {
                                    cStat.Ok = 0;
                                    cStat.Contains = "Couldn't check for content";
                                    if (er.Status == WebExceptionStatus.ProtocolError)
                                    {
                                        cStat.StCode = ((HttpWebResponse)er.Response).StatusCode.ToString();
                                        cStat.Desc = ((HttpWebResponse)er.Response).StatusDescription;
                                    }
                                    else if (er.Status == WebExceptionStatus.NameResolutionFailure)
                                    {
                                        cStat.StCode = "Name Resolution Failure";
                                        cStat.Desc = "Name Resolution Failure";
                                    }
                                    else if (er.Status == WebExceptionStatus.Timeout)
                                    {
                                        cStat.StCode = "Timeout";
                                        cStat.Desc = "Timeout";
                                    }
                                    backgroundWorker1.ReportProgress(150, cStat.StCode);
                                    //Если уведомления настроены, то отсылается уведомление
                                    if (mySet.NotifyAllow && IT.NeedNotify)
                                    {
                                        backgroundWorker1.ReportProgress(150, "Sending e-mail");
                                        MailErr = mySet.SendMail(IT.Email, "Site Monitor Notification for " + IT.URL, @"Site Monitor Notification
    [" + cStat.DT + "] " + IT.URL + " :  " + er.Message);
                                        if (MailErr == "")
                                        {
                                            cStat.NotiErr = 0;
                                            cStat.Notify = "Notification sent";
                                            backgroundWorker1.ReportProgress(150, "E-mail sent");
                                        }
                                        else
                                        {
                                            cStat.Notify = "ERROR: " + MailErr;
                                            cStat.NotiErr = 1;
                                            backgroundWorker1.ReportProgress(150, "ERROR sending e-mail");
                                        }
                                    }
                                    else
                                    {
                                        cStat.Notify = "Notification disabled";
                                        cStat.NotiErr = 0;
                                    }
                                }
                                    //Отлавливаются иные исключения
                                catch (Exception er)
                                {
                                    cStat.Contains = "Couldn't check for content";
                                    cStat.Desc = er.Message;
                                    cStat.StCode = er.Message;
                                    cStat.Notify = "Local error, no need";
                                    backgroundWorker1.ReportProgress(150, "Check ERROR");
                                }
                                ++i;
                                //Сохраняем полученую статистику
                                sList = Stat.LoadListFromFile(IT.URL, mySet.FolderName);
                                if (sList == null) sList = new List<Stat>();
                                sList.Add(cStat);
                                if (Stat.SaveStatToFile(sList, IT.URL, mySet.FolderName)) backgroundWorker1.ReportProgress(150,"Log saved");
                                else backgroundWorker1.ReportProgress(150, "ERROR saving log");
                                backgroundWorker1.ReportProgress(((i * 100) / mList.Count));
                            }
                        }
                        backgroundWorker1.ReportProgress(0);
                        backgroundWorker1.ReportProgress(150, "Idle");
                        backgroundWorker1.ReportProgress(170, "Monitor running - Idle period");
                        //Ждем период между сессиями проверки; Каждую минуту проверяем на запрос остановки монитора
                            for (int j = 0; j < (mySet.Period); ++j)
                            {
                                backgroundWorker1.ReportProgress(150, "Idle: "+(mySet.Period-j).ToString()+" min. left");
                                backgroundWorker1.ReportProgress(170, "Monitor running - Idle period: " + (mySet.Period - j).ToString() + " min. left");
                                if(!backgroundWorker1.CancellationPending)
                                {
                                    Thread.Sleep(60000);
                                }
                            }
                        backgroundWorker1.ReportProgress(170, "Monitor running");
                        backgroundWorker1.ReportProgress(150, "Running");
                    }
                        //Если нет страниц для проверки, то уведомляем об этом и останавливаем процесс монитора
                    else
                    {
                        backgroundWorker1.ReportProgress(170, "Monitor list is empty. Add URL to monitor list.");
                        backgroundWorker1.ReportProgress(190, "Monitor list is empty. Add URL to monitor list.");
                        backgroundWorker1.CancelAsync();
                    }
                }
            }
        }
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e) 
        {
            //Заполнение полей на форме о прогрессе работы Монитора
            // 150 - toolStripStatusLabel3 заполнение
            // 170 - label1 заполнение
            if (e.ProgressPercentage == 150)
            {
                toolStripStatusLabel3.Text = e.UserState.ToString();
            }
            else if (e.ProgressPercentage == 170)
            {
                label1.Text = e.UserState.ToString();
            }
            else if(e.ProgressPercentage == 190)
            {
                MessageBox.Show(e.UserState.ToString(),"Monitor",MessageBoxButtons.OK,MessageBoxIcon.Information);
            }
            else
            {
                //Заполнение ProgressBar
                toolStripStatusLabel1.Text = e.ProgressPercentage.ToString()+"%";
                toolStripProgressBar1.Value = e.ProgressPercentage;
            }
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) 
        {
            //Заполнение полей на форме о завершении работы Монитора
            if (e.Error != null)
            {
                label1.Text = "Monitor runtime error";
                toolStripStatusLabel3.Text = "Runtime ERROR";
                MessageBox.Show(e.Error.Message);
            }
            toolStripStatusLabel3.Text = "Stopped";
            label1.Text = "Monitor stopped";
            toolStripProgressBar1.Value = 0;
            toolStripStatusLabel1.Text="0%";
            button1.Enabled = true;
            button2.Enabled = false;
        }
        
#endregion
#region FieldValidation
        ///В lextBox#_Validated проверяется, если были внесены исправления, то ErrorProvider для них очищается
        private void textBox1_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
        private void textBox1_Validated(object sender, System.EventArgs e)
        {
            if (textBox1.Text != "" && mySet.PeriodVal(textBox1.Text))
            {
                errorProvider1.SetError(textBox1, "");
                toolStripStatusLabel4.Text = "";
            }
        }
        private void textBox4_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
        private void textBox4_Validated(object sender, System.EventArgs e)
        {
            if (textBox4.Text != "")
            {
                errorProvider1.SetError(textBox4, "");
                toolStripStatusLabel4.Text = "";
            }
        }
        private void textBox5_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
        private void textBox5_Validated(object sender, System.EventArgs e)
        {
            if (textBox5.Text != "" && mySet.PortVal(textBox5.Text))
            {
                errorProvider1.SetError(textBox5, "");
                toolStripStatusLabel4.Text = "";
            }
        }
        private void textBox6_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
        private void textBox6_Validated(object sender, System.EventArgs e)
        {
            if (textBox6.Text != "")
            {
                errorProvider1.SetError(textBox6, "");
                toolStripStatusLabel4.Text = "";
            }
        }
        private void textBox7_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
        private void textBox7_Validated(object sender, System.EventArgs e)
        {
            if (textBox7.Text != "")
            {
                errorProvider1.SetError(textBox7, "");
                toolStripStatusLabel4.Text = "";
            }
        }
        private void textBox8_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
        private void textBox8_Validated(object sender, System.EventArgs e)
        {
            if (textBox8.Text != "")
            {
                errorProvider1.SetError(textBox8, "");
                toolStripStatusLabel4.Text = "";
            }
        }
        private void textBox3_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
        private void textBox3_Validated(object sender, System.EventArgs e)
        {
            if (textBox3.Text != "")
            {
                errorProvider1.SetError(textBox3, "");
                toolStripStatusLabel4.Text = "";
                //Проверка, если нет префикса http:// у URL, то добавляем его 
                System.Globalization.CompareInfo cmpUrl = System.Globalization.CultureInfo.InvariantCulture.CompareInfo;
                if (cmpUrl.IsPrefix(textBox3.Text, "http://") == false)
                {
                    textBox3.Text = "http://" + textBox3.Text;
                }
            }
        }
        private void textBox9_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
        private void textBox9_Validated(object sender, System.EventArgs e)
        {
            if (textBox9.Text != "")
            {
                errorProvider1.SetError(textBox9, "");
                toolStripStatusLabel4.Text = "";
            }
        }
#endregion
        private void Form1_Load(object sender, EventArgs e)
        {
            button2.Enabled = false;
            label9.Visible = false;
            label10.Visible = false;
            button5.Enabled = false;
            button8.Enabled = false;
            //Пытаемся загрузить настройки, если их нет, то напоминаем о том, что их нужно установить
            if (mySet.LoadSetfromFile()) toolStripStatusLabel4.Text = "Settings loaded successfully";
            else MessageBox.Show("Remember to adjust settings first","No settings file found",MessageBoxButtons.OK,MessageBoxIcon.Information);
            //Загружаем настройки в поля формы закладки Settings
            checkBox2.Checked = mySet.SSL;
            textBox1.Text = (mySet.Period).ToString();
            textBox4.Text = mySet.Smtp;
            textBox5.Text = mySet.Port.ToString();
            textBox6.Text = mySet.From;
            textBox7.Text = mySet.User;
            textBox8.Text = mySet.Pass;
            textBox2.Text = mySet.FolderName;
            //Пытаемся загрузить список URL, если не получается, то напоминаем пользователю о необходимости добавления URL к списку перед запуском монитора
            List<Item> mList = new List<Item>();
            mList = Item.LoadListFromFile(mySet.FolderName);
            if (mList != null)
            {
                ListBoxFiller(mList);
                if (mList.Count != 0)
                {
                    toolStripStatusLabel4.Text = "URL list loaded successfully";
                    button5.Enabled = true;
                    button8.Enabled = true;
                }
                else MessageBox.Show("Remember to add URLs to list before starting the monitor", "No URL list found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else MessageBox.Show("Remember to add URLs to list before starting the monitor", "No URL list found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //Проверяем наличие соединения с сетью
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                label1.Text = "Active network connection found";
                toolStripStatusLabel4.Text = "Active network connection found";
                toolStripStatusLabel3.Text = "Ready";
            }
            //если нет сети, то пишет, что нужна сеть для работы программы
            else
            {
                MessageBox.Show("Remember to establish network connection before starting the monitor", "No active network connection found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                toolStripStatusLabel4.Text = "No active network connection found";
                label1.Text = "No active network connection found";
            }
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Заполнение статистики по выбраному URL
            DataGridFiller();
        }
        private void tabControl1_Selecting(Object sender, TabControlCancelEventArgs e)
        {
            //В случае открытых диалогов О программе или Добавление/Редактирование не позволяем переключать панели
            if (panel1.Visible || panel2.Visible) e.Cancel = true;
            else
            {
                toolStripStatusLabel4.Text = "";
                if (listBox1.Items.Count != 0) listBox1.SelectedIndex = 0;
                //Если была попытка смены папки логов, то перезагружаем список страниц с параметрами, если в новой папке списка не оказалось, то предупреждаем о необходимости его заполнения
                if (Settings.FDCall)
                {
                    dataGridView1.Rows.Clear();
                    ListBoxClear();
                    button5.Enabled = false;
                    button8.Enabled = false;
                    List<Item> mList = new List<Item>();
                    mList = Item.LoadListFromFile(mySet.FolderName);
                    if (mList != null)
                    {
                        ListBoxFiller(mList);
                        if (mList.Count != 0)
                        {
                            toolStripStatusLabel4.Text = "URL list loaded successfully";
                            button5.Enabled = true;
                            button8.Enabled = true;
                        }
                        else MessageBox.Show("Remember to add URLs to list before starting the monitor", "No URL list found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else MessageBox.Show("Remember to add URLs to list before starting the monitor", "No URL list found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Settings.FDCall = false;
                }
                //Если осуществляется переход на закладку статистики, то перезаполняем DataGrid
                if (e.TabPageIndex == 1 && listBox1.Items.Count!=0)
                {
                    DataGridFiller();
                }
                //Если переход на страницу настроек, то перезаполняем поля
                if (e.TabPageIndex == 2)
                {
                    checkBox2.Checked = mySet.SSL;
                    textBox1.Text = (mySet.Period).ToString();
                    textBox4.Text = mySet.Smtp;
                    textBox5.Text = mySet.Port.ToString();
                    textBox6.Text = mySet.From;
                    textBox7.Text = mySet.User;
                    textBox8.Text = mySet.Pass;
                    textBox2.Text = mySet.FolderName;
                    errorProvider1.SetError(textBox1, "");
                    errorProvider1.SetError(textBox4, "");
                    errorProvider1.SetError(textBox5, "");
                    errorProvider1.SetError(textBox6, "");
                    errorProvider1.SetError(textBox7, "");
                    errorProvider1.SetError(textBox8, "");
                }
            }
   
        }
#region ButtonClicks
        /// <summary>
        /// Вызов диалога выбора папки логов
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            textBox2.Text = mySet.FolderVal(folderBrowserDialog1);
        }
        /// <summary>
        /// Сохранение настроек
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            //Проверяем обязательные поля, выдаем предупреждения и возвращем пользователя к исправлению
            if (textBox1.Text == "" || !mySet.PeriodVal(textBox1.Text))
            {
                toolStripStatusLabel4.Text = "Check period field";
                this.errorProvider1.SetError(textBox1, "Period must be integer from 1 to " + (Math.Abs(Int32.MaxValue / 60000 - 1) - 1));
                MessageBox.Show("Period field must be filled with integer number from 1 to " + (Math.Abs(Int32.MaxValue / 60000 - 1) - 1)+" to save the settings", "Check Period field", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            else
            {
                errorProvider1.SetError(textBox1, "");
                toolStripStatusLabel4.Text = "";
                mySet.Period = (Convert.ToInt32(textBox1.Text));
            }
            if (textBox5.Text == "" || !mySet.PortVal(textBox1.Text))
            {
                toolStripStatusLabel4.Text = "Check port field";
                this.errorProvider1.SetError(textBox5, "Port must be integer from 1 to 65535");
                MessageBox.Show("Port field must be filled with integer from 1 to 65535 to save the settings", "Check Port field", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                textBox5.Text = "25";
                return;
            }
            else
            {
                errorProvider1.SetError(textBox5, "");
                toolStripStatusLabel4.Text = "";
                mySet.Port = (Convert.ToInt32(textBox5.Text));
            }
            mySet.NotifyAllow = true;//Считаем, что настройки для исходящей почты установлены
            //Если одно из полей для настроек исходящей почты пустое, то выдаем предупреждение
            if (textBox4.Text == "" || textBox6.Text == "" || textBox7.Text == "" || textBox8.Text == "")
            {
                toolStripStatusLabel4.Text = "E-mail notifications won't work";
                errorProvider1.SetError(textBox4, "E-mail notifications won't be sent if you leave this field empty");
                errorProvider1.SetError(textBox6, "E-mail notifications won't be sent if you leave this field empty");
                errorProvider1.SetError(textBox7, "E-mail notifications won't be sent if you leave this field empty");
                errorProvider1.SetError(textBox8, "E-mail notifications won't be sent if you leave this field empty");
                if (MessageBox.Show("If you leave any of the fields: SMTP Server, Sender e-mail, Username, Password empty application won't be able to send E-mail notifications. If you want Site Monitor to work without notifications press OK. Or press Cancel to fill in the missing fields", "Disable e-mail notifications?", MessageBoxButtons.OKCancel ,MessageBoxIcon.Exclamation)==DialogResult.Cancel) return;
                //Если пользователь согласился работать без уведомлений, то очищаем сообщения о незаполненых полях
                toolStripStatusLabel4.Text = "";
                errorProvider1.SetError(textBox4, "");
                errorProvider1.SetError(textBox6, "");
                errorProvider1.SetError(textBox7, "");
                errorProvider1.SetError(textBox8, "");
                mySet.NotifyAllow = false;//Указываем, что уведомления отсылаться не будут
            }
            //Заполняем поля настроек
            mySet.Smtp = textBox4.Text;
            mySet.From = textBox6.Text;
            mySet.User = textBox7.Text;
            mySet.Pass = textBox8.Text;
            //Пробуем сохранить настройки в файл
            if (mySet.SaveSetToFile())toolStripStatusLabel4.Text = "Settings saved successfully";
            else
            {
                toolStripStatusLabel4.Text = "Error saving settings";
                MessageBox.Show("Error saving settings", "Settings unsaved", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        /// <summary>
        /// Восстановление настроек
        /// </summary>
        private void button6_Click(object sender, EventArgs e)
        {
            //Пытаемся считать настройки из файла в поля
            if (mySet.LoadSetfromFile())
            {
                toolStripStatusLabel4.Text = "Settings loaded successfully";
                checkBox2.Checked = mySet.SSL;
                textBox1.Text = (mySet.Period).ToString();
                textBox4.Text = mySet.Smtp;
                textBox5.Text = mySet.Port.ToString();
                textBox6.Text = mySet.From;
                textBox7.Text = mySet.User;
                textBox8.Text = mySet.Pass;
                textBox2.Text = mySet.FolderName;
                errorProvider1.SetError(textBox1, "");
                errorProvider1.SetError(textBox4, "");
                errorProvider1.SetError(textBox5, "");
                errorProvider1.SetError(textBox6, "");
                errorProvider1.SetError(textBox7, "");
                errorProvider1.SetError(textBox8, "");
            }
            else toolStripStatusLabel4.Text = "Error restoring settings";
        }
        /// <summary>
        /// Редактирование данных по странице, адрес которой выбран в listBox1
        /// </summary>
        private void button5_Click(object sender, EventArgs e)
        {
            listBox3.Items.Clear();
            ValidItem.cList.Clear();
            radioButton1.Checked = true;
            radioButton2.Checked = false;
            textBox3.Text = "";
            textBox9.Text = "";
            textBox10.Text = "";
            button13.Enabled = false;
            //Считываем установки для страницы и заполняем список текста содержимого listBox3
            List<Item> mList = new List<Item>();
            mList = Item.LoadListFromFile(mySet.FolderName);
            if (mList != null)
            {
                ValidItem=mList.ToArray()[listBox1.SelectedIndex];
                ListBox3Refiller();
                if (listBox3.Items.Count != 0) button13.Enabled = true;
            }
            toolStripStatusLabel4.Text = "Editing";
            listBox2.SelectedIndex=listBox1.SelectedIndex;
            textBox3.Text = listBox1.SelectedItem.ToString();
            textBox9.Text = listBox2.SelectedItem.ToString();
            ValidItem.URL = textBox3.Text;
            ValidItem.Email = textBox9.Text;
            textBox3.Enabled = false;
            panel1.Visible = true;
            listBox1.Enabled = false;
            button7.Enabled = false;
            button5.Enabled = false;
            button8.Enabled = false;
        }
        /// <summary>
        /// Добавление страницы с установками
        /// </summary>
        private void button7_Click(object sender, EventArgs e)
        {
            listBox3.Items.Clear();
            ValidItem.cList.Clear();
            radioButton1.Checked = true;
            radioButton2.Checked = false;
            button13.Enabled = false;
            textBox3.Text = "";
            textBox9.Text = "";
            textBox10.Text = "";
            listBox3.Items.Clear();
            toolStripStatusLabel4.Text = "Adding";
            AddBtn = true;
            panel1.Visible = true;
            listBox1.Enabled = false;
            button7.Enabled = false;
            button5.Enabled = false;
            button8.Enabled = false;
        }
        /// <summary>
        /// Отмена редактирования или добавления
        /// </summary>
        private void button10_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
            AddBtn = false;
            textBox3.Enabled = true;
            listBox1.Enabled = true;
            button7.Enabled = true;
            if (listBox1.Items.Count == 0)
            {
                button5.Enabled = false;
                button8.Enabled = false;
            }
            else
            {
                button5.Enabled = true;
                button8.Enabled = true;
            }
            textBox3.Text = "";
            textBox9.Text = "";
            textBox10.Text = "";
            errorProvider1.SetError(textBox3, "");
            errorProvider1.SetError(textBox9, "");
            ValidItem.cList.Clear();
            toolStripStatusLabel4.Text = "";
        }
        /// <summary>
        /// Сохранение адреса страницы с установками для этой страницы
        /// </summary>
        private void button9_Click(object sender, EventArgs e)
        {
            //Проверяем на обязательное заполнение поля адреса страницы
            if (textBox3.Text=="")
            {
                errorProvider1.SetError(textBox3, "URL field must be filled in order to add to list");
                toolStripStatusLabel4.Text = "Fill in the URL field";
                MessageBox.Show("URL field must be set", "Empty URL field", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            //Предупреждаем о том, что уведомления не будут отсылаться, если не указать адре эл.почты
            else if (textBox9.Text=="")
            {
                errorProvider1.SetError(textBox9, "Notifications won't be sent if you leave this field empty");
                toolStripStatusLabel4.Text = "Empty E-mail field";
                ValidItem.NeedNotify = false;
                if (MessageBox.Show("Site Monitor wont't send notifications in case of page inaccessibily or content problem. If you agree press OK, to add E-mail address press Cancel.", "Empty E-mail field", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.Cancel) return;

            }
            if (textBox9.Text != "") ValidItem.NeedNotify = true;//Нужно уведомлять, если поле адреса заполнено
            if (ValidItem.cList.Count==0) ValidItem.NeedContains = false;//Не нужно проверять на содержание страницы, если не добалено ниодной строки проверки
            else ValidItem.NeedContains = true;//иначе проверять на содержимое
            //Заполняем оставшиеся поля и записываем адрес с установками в список
            ValidItem.URL = textBox3.Text.ToLower();
            ValidItem.Email = textBox9.Text.ToLower();
            List<Item> mList = new List<Item>();
            mList = Item.LoadListFromFile(mySet.FolderName);
            if (mList != null)
            {
                //Если была нажата кнопка Редактирование, то перед добавлением удаляем из списка
                if (!AddBtn)
                {
                    mList.RemoveAt(listBox1.SelectedIndex);
                }
            }
            else mList = new List<Item>();
            mList.Add(ValidItem);
            if (Item.SaveListToFile(mList, mySet.FolderName)) toolStripStatusLabel4.Text = "List updated successfully";
            else toolStripStatusLabel4.Text = "Erro updating list";
            //Перезаполняем список страниц listBox1
            ListBoxClear();
            ListBoxFiller(mList);
            panel1.Visible = false;
            AddBtn = false;
            textBox3.Enabled = true;
            listBox1.Enabled = true;
            button7.Enabled = true;
            errorProvider1.SetError(textBox3, "");
            errorProvider1.SetError(textBox9, "");
            ValidItem.cList.Clear();
            if (listBox1.Items.Count == 0)
            {
                button5.Enabled = false;
                button8.Enabled = false;
            }
            else
            {
                button5.Enabled = true;
                button8.Enabled = true;
            }
        }
        /// <summary>
        /// Удаление адреса страницы из списка вместе с логами
        /// </summary>
        private void button8_Click(object sender, EventArgs e)
        {
            //Предупреждаем об удалении
            if (MessageBox.Show("Are you sure in removing " + ((string)listBox1.SelectedItem) + " ? All logs associated with this entry will be deleted.", "Delete?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                button7.Enabled = false;
                button5.Enabled = false;
                button8.Enabled = false;
                //Загружаем список и удаляем выбраный в listBox1 адрес с установками
                List<Item> mList = new List<Item>();
                mList = Item.LoadListFromFile(mySet.FolderName);
                if (mList != null)
                {
                    toolStripStatusLabel4.Text = "Deleting";
                    mList.RemoveAt(listBox1.SelectedIndex);
                    //Пытаемся удалить файл логов
                    try
                    {
                        File.Delete(mySet.FolderName + @"\" + ((string)listBox1.SelectedItem).GetHashCode().ToString() + @".log");
                    }
                    catch (Exception err)
                    {
                        toolStripStatusLabel4.Text = "Error deleting associated log file";
                        MessageBox.Show(err.Message, "Error deleting associated file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    //Перезаполняем список адресов страниц listBox1
                    ListBoxClear();
                    ListBoxFiller(mList);
                    //Сохраняем список без удаленного элемента обратно в файл
                    if (Item.SaveListToFile(mList, mySet.FolderName)) toolStripStatusLabel4.Text = "List updated successfully";
                    else toolStripStatusLabel4.Text = "Erro updating list";
                    dataGridView1.Rows.Clear();
                    label31.Text = "";
                    label8.Text = "";
                    label6.Text = "";
                    label4.Text = "";
                }
                if (listBox1.Items.Count == 0)
                {
                    button5.Enabled = false;
                    button8.Enabled = false;
                }
                else
                {
                    listBox1.SelectedIndex = 0;
                    DataGridFiller();
                    button5.Enabled = true;
                    button8.Enabled = true;
                }
                button7.Enabled = true;
            }
        }
        /// <summary>
        /// Создание запроса на остановку процесса Монитора
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure stoping the monitor? Next time you launch the monitor it will start from the beginning of the list. Stopping the monitor can take up to 1 minute.", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question)==DialogResult.Yes)
            {
                backgroundWorker1.CancelAsync();
                label1.Text = "Monitor stopping";
                toolStripStatusLabel3.Text = "Stopping";
            }
        }
        /// <summary>
        /// Создание запроса на запуск процесса Монитора
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            //Проверяем наличие сети
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                label1.Text = "Active network connection found";
                toolStripStatusLabel4.Text = "Active network connection found";
                backgroundWorker1.RunWorkerAsync();
                button1.Enabled = false;
                button2.Enabled = true;
            }
            //если сети нет, то не запускаем Монитор
            else
            {
                label1.Text = "No active network connection found. Establish network connection before starting the monitor.";
                toolStripStatusLabel4.Text = "No active network connection found";
            }

        }
        /// <summary>
        /// Показывает панель О Программе
        /// </summary>
        private void button11_Click(object sender, EventArgs e)
        {
            panel2.Visible = true;
        }
        /// <summary>
        /// Закрывает панель О программе
        /// </summary>
        private void button12_Click(object sender, EventArgs e)
        {
            panel2.Visible = false;
        }
        /// <summary>
        /// Добавление записи в список проверки содержимого и отображение этого списка в listBox3
        /// </summary>
        private void button14_Click(object sender, EventArgs e)
        {
            //Добавление текста и указателя должен ли этот текст быть на странице или не должен в список
            ValidItem.cList.Add(new Content(textBox10.Text,radioButton1.Checked));
            textBox10.Text = "";
            //Перезаполнение listBox3 с учетом новой записи в списке
            ListBox3Refiller();
            button13.Enabled = true;
        }
        /// <summary>
        /// Удаление записи проверки содержимого из списка
        /// </summary>
        private void button13_Click(object sender, EventArgs e)
        {
            //Удаление записи проверки содержимого из списка
            ValidItem.cList.RemoveAt(listBox3.SelectedIndex);
            //Если записи еще остались в списке, то перезаполнить список listBox3
            if (ValidItem.cList.Count != 0) ListBox3Refiller();
            //Если нет записей, то очистить listBox3 и заблокировать кнопку Удалить
            else
            {
                listBox3.Items.Clear();
                button13.Enabled = false;
            }
        }
#endregion
#region Tray
        /// <summary>
        /// Свернуть в трей, при сворачивании окна
        /// </summary>
        private void Form1_Resize(object sender, System.EventArgs e)
        {
            if (FormWindowState.Minimized == WindowState)
            {
                notifyIcon1.Visible=true;
                notifyIcon1.ShowBalloonTip(60000);
                Hide();
            }
        }
        /// <summary>
        /// При двойном нажатии на иконке в трейе развернуть окно
        /// </summary>
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }
        /// <summary>
        /// При одинарном нажатии на подсказке развернуть окно
        /// </summary>
        private void notifyIcon1_BalloonTipClicked(object sender, System.EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false; 
        }
        /// <summary>
        /// При одинарном нажатии на иконке в трейе показать подсказку
        /// </summary>
        private void notifyIcon1_Click(object sender, System.EventArgs e)
        {
            notifyIcon1.ShowBalloonTip(30000);
        }
#endregion
    }
}
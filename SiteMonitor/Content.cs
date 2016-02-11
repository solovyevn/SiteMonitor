using System;
using System.Collections.Generic;
using System.Text;

namespace SiteMonitor
{
    /// <summary>
    /// Класс, содержания страницы
    /// </summary>
    public class Content
    {
        public Content() { }
        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="text">Текст на который будет выполняться проверка</param>
        /// <param name="contains">True, если страница должна содержать этот текст; False, если на странице не должно быть этого текста</param>
        public Content(string text, bool contains)
        {
            this.Text = text;
            this.Contains = contains;
        }
        public string Text;//Текст на который будет выполняться проверка
        public bool Contains;//True, если страница должна содержать этот текст; False, если на странице не должно быть этого текста
    };
}

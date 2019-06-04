using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace orgWin
{
    public class orgNode
    {
        public Guid id;
        public Guid pid;
        public string tag;    //тип узла (org, person, in, out, absent)
        public string name;   //фамилия или название
        public string mlname; //имя отчество
        public string title;  //должность
        public string boss;  //фамилия босса
        public string birthday;  //день рождения
        public string enter;    //время входа
        public string exit;     //время выхода        
        public string mob;      //мобильный телефон
        public string corp;     //корпоративный телефон
        public string mail;
    }    
}
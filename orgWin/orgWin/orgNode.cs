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
        public string type;    //тип узла org, person
        public string name;   //фамилия или название
        public string mlname; //имя отчество
        public string title;  //должность
        public Guid? img;      //имя файла рисунка
        public string birthday;  //день рождения
        public string mob;      //мобильный телефон
        public string corp;     //корпоративный телефон
        public string mail;
    }    
}
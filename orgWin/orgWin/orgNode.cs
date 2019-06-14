﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;

namespace orgWin
{
    public class orgNode
    {
        public Guid id { get; set; }
        public Guid pid { get; set; }
        public string type { get; set; }    //тип узла org, person
        public string name { get; set; }   //фамилия или название
        public string mlname { get; set; } //имя отчество
        public string title { get; set; }  //должность
        public Guid? img { get; set; }      //имя файла рисунка
        public string birthday { get; set; }  //день рождения
        public string mob { get; set; }      //мобильный телефон
        public string corp { get; set; }     //корпоративный телефон
        public string mail { get; set; }
        public object this[string propertyName]
        {
            get
            {
                // probably faster without reflection:
                // like:  return Properties.Settings.Default.PropertyValues[propertyName] 
                // instead of the following
                Type myType = typeof(orgNode);
                return myType.GetProperty(propertyName).GetValue(this, null);
            }
            set
            {
                Type myType = typeof(orgNode);
                PropertyInfo prop = myType.GetProperty(propertyName);
                if (null != prop && prop.CanWrite)
                {
                    prop.SetValue(this, value, null);
                }
            }

        }
    }    
}
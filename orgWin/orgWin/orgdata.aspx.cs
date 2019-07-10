using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using Newtonsoft.Json;
using System.Collections;

namespace orgWin
{
    public partial class orgData : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Dictionary<Guid, orgNode> nodes = (Dictionary<Guid, orgNode>)HttpContext.Current.Application["orgdata"];
            Response.Clear();
            bool json_needed = Request.QueryString.ToString().ToUpper() == "JSON";
            bool csv_needed = Request.QueryString.ToString().ToUpper() == "CSV";
            Response.ContentType = json_needed ? "application/json; charset=utf-8" 
                : (csv_needed ? "text/csv"
                : "text/javascript; charset=utf-8");
            if (csv_needed)
                Response.Write(makeCSV(nodes.Values.ToList()));
            else {
                if (!json_needed) Response.Write("var orgData=");
                Response.Write(JsonConvert.SerializeObject(nodes.Values.ToList(),
                    Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            })
              );
            }
            Response.End();

        }
    private string makeCSV<T>(List<T> items) where T : class
    {
        var output = "";
        var delimiter = ",";
        var properties = typeof(T).GetProperties()
            .Where(n =>
            n.PropertyType == typeof(string)
         || n.PropertyType == typeof(bool)
         || n.PropertyType == typeof(char)
         || n.PropertyType == typeof(byte)
         || n.PropertyType == typeof(decimal)
         || n.PropertyType == typeof(int)
         || n.PropertyType == typeof(DateTime)
         || n.PropertyType == typeof(DateTime?));
        using (var sw = new StringWriter())
         {
         var header = properties
         .Select(n => n.Name)
         .Aggregate((a, b) => a + delimiter + b);
        sw.WriteLine(header);
        foreach (var item in items){
             var row = properties
             .Select(n => n.GetValue(item, null))
             .Select(n => n == null ? "null" : n.ToString())
             .Aggregate((a, b) => a + delimiter + b);
            sw.WriteLine(row);
        }
        output = sw.ToString();
         }
        return output;
    }
   }
}
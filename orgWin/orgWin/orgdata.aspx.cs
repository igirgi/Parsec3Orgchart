using System;
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
            Response.ContentType = json_needed ? "application/json; charset=utf-8" : "text/javascript; charset=utf-8";
            if(!json_needed) Response.Write("var orgData=");
            Response.Write(JsonConvert.SerializeObject(nodes.Values.ToList(),
                Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            })
            );
            Response.End();

        }
    }
}
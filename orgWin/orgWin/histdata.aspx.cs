using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using System.Data.SqlClient;
using Newtonsoft.Json;

namespace orgWin
{
    public partial class histdata : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string id = Request.QueryString["id"];
            var connectionString = ConfigurationManager.ConnectionStrings["ParsecReport"].ConnectionString;
            var jsonResult = new StringBuilder();
            using (SqlConnection sqlCon = new SqlConnection(connectionString))
            {
                sqlCon.Open();
                string sqlc = "select top (10000) sysdate as d, opozd as o, zader as z from ParsecOrgHistory where id=@id order by date for JSON auto";
                using (SqlCommand sqlCmd = new SqlCommand { CommandText = sqlc, Connection = sqlCon })
                {
                    sqlCmd.Parameters.AddWithValue("@id", id);
                    var reader = sqlCmd.ExecuteReader();
                    if (!reader.HasRows)
                    {
                        jsonResult.Append("[]");
                    }
                    else
                    {
                        while (reader.Read())
                        {
                            jsonResult.Append(reader.GetValue(0).ToString());
                        }
                    }
                }
                sqlCon.Close();
            }
            Response.Clear();
            Response.ContentType = "application/json; charset=utf-8";
            Response.Write(jsonResult.ToString());
            Response.End();
        }
    }
 
}
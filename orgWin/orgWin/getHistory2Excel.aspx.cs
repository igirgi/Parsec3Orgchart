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
    public partial class getHistory2Excel : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string id = Request.QueryString["id"];
            string pid = Request.QueryString["pid"];
            string from = Request.QueryString["from"];
            string to = Request.QueryString["to"];
            var connectionString = ConfigurationManager.ConnectionStrings["ParsecReport"].ConnectionString;
            var jsonResult = new StringBuilder();
            using (SqlConnection sqlCon = new SqlConnection(connectionString))
            {
                sqlCon.Open();
                string sqlc = (null == id && null != pid) ?
@"WITH parent AS (
    SELECT pid
    FROM orgChart
    WHERE pid = @pid
), tree AS (
    SELECT x.pid, x.id, (select name from orgChart  where id = x.pid ) 'department', x.name, x.tag
    FROM orgChart x	
    INNER JOIN parent ON x.pid = parent.pid
    UNION ALL
    SELECT y.pid, y.id, (select name from orgChart  where id = y.pid ) 'department', y.name, y.tag
    FROM orgChart y
    INNER JOIN tree t ON y.pid = t.id
)
SELECT c.department
      ,c.name
      ,h.date
      ,h.enter
      ,h.eexit
      ,h.eventscount
      ,h.totalworktime
      ,h.totalouttime
from (SELECT  DISTINCT pid, id, department, name, tag FROM tree 
) c 
inner join Parsec2ExcelHistory h on h.id = c.id and h.date_ >= @from and h.date_ <= @to
order by c.pid asc, c.name asc, h.date_ asc for JSON auto"
                    :
@"SELECT c.department
      ,c.name
      ,h.date
      ,h.enter
      ,h.eexit
      ,h.eventscount
      ,h.totalworktime
      ,h.totalouttime
from (select id, name, (select name from orgChart  where id = o.pid ) department from orgChart o where id = @id) c 
inner join Parsec2ExcelHistory h on h.id = c.id and h.date_ >= @from and h.date_ <= @to
order by c.name asc, h.date_ asc for JSON auto";

                using (SqlCommand sqlCmd = new SqlCommand { CommandText = sqlc, Connection = sqlCon })
                {
                    sqlCmd.Parameters.AddWithValue("@id", (id == null ? "" : id));
                    sqlCmd.Parameters.AddWithValue("@pid", (pid == null ? "" : pid) );
                    sqlCmd.Parameters.AddWithValue("@from", from);
                    sqlCmd.Parameters.AddWithValue("@to", to);
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
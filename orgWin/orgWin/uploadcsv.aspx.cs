using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using LumenWorks.Framework.IO.Csv;

namespace orgWin
{
    public partial class uploadcsv : System.Web.UI.Page
    {
                protected void Page_Load(object sender, EventArgs e)
        {
            btnSave.Enabled = false;

        }
        protected void ImportCSV(object sender, EventArgs e)
        {
            Stream stream = FileUpload1.PostedFile.InputStream;
            //Create a DataTable.
            char delimeter = delim.Text[0];
            DataTable dt = new DataTable();
            using (CsvReader csvReader = new CsvReader((new StreamReader(stream, Encoding.GetEncoding("Windows-1251"))), true, delimeter))
            {
                dt.Load(csvReader);
            }

            GridView1.DataSource = dt;
            GridView1.DataBind();
            btnSave.Enabled = true;
            Session["dt"] = dt;
        }
        string sqlc = @"if not exists (select * from Tabel where id = @id and sysdate = @date)
 insert into Tabel
 (id, sysdate, state)  values (@id, @date, @state)
 else
  update Tabel set state = @state where id = @id and  sysdate = @date";

        protected void Save(object sender, EventArgs e)
        {
            if (Session["dt"] == null) return;            
            DataTable dt = (DataTable)Session["dt"];
            int colnum = dt.Columns.Count;
            int rownum = dt.Rows.Count;
            DateTime[] date = new DateTime[colnum];
            for (int i = 3; i < colnum; i++)
                 DateTime.TryParse( dt.Columns[i].ColumnName, out  date[i]);

            var connectionString = ConfigurationManager.ConnectionStrings["ParsecReport"].ConnectionString;
            using (SqlConnection sqlCon = new SqlConnection(connectionString)){
                sqlCon.Open();
                checkTable(sqlCon);
                foreach (DataRow row in dt.Rows) {
                    string id = row.ItemArray[0].ToString().Trim();
                    for (int i = 3; i < colnum; i++) {
                        string state = row.ItemArray[i].ToString().Trim();
                        if (!state.Equals("") && date[i] != null){
                            using (SqlCommand sqlCmd1 = new SqlCommand { CommandText = sqlc, Connection = sqlCon }) {
                                sqlCmd1.Parameters.AddWithValue("@id", id);
                                sqlCmd1.Parameters.AddWithValue("@date", date[i]);
                                sqlCmd1.Parameters.AddWithValue("@state", state);
                                sqlCmd1.ExecuteNonQuery();
                            }
                        }
                    }                 
                }
                sqlCon.Close();
            }
            btnImport.Enabled = true;
            btnSave.Enabled = false;
            GridView1.DataSource = null;
            GridView1.DataBind();
        }
        static void checkTable(SqlConnection sqlCon)
        {
            string sqlcr = @"IF OBJECT_ID('dbo.Tabel', 'U') IS NULL
                CREATE TABLE [dbo].[Tabel](
	            [id] [varchar](150) NOT NULL,
                [sysdate] [date] NOT NULL,
            	[state] [varchar](150) NULL
            ) ON [PRIMARY]";
            using (SqlCommand sqlCmd = new SqlCommand { CommandText = sqlcr, Connection = sqlCon })
            {
                sqlCmd.ExecuteNonQuery();
            }
        }
        protected void Tpl(object sender, EventArgs e)
        {
            Dictionary<Guid, orgNode> nodes = (Dictionary<Guid, orgNode>)HttpContext.Current.Application["orgdata"];
            Response.Clear();
            Response.ContentType = "text/csv";
            Response.AddHeader("content-disposition", @"attachment;filename=""tabel.csv"""); 
            Response.Write(makeTpl(nodes.Values.ToList()));
            Response.End();
        }

        private string makeTpl(List<orgNode> items)
        {
            var output = "";
            using (var sw = new StringWriter())
            {
                sw.WriteLine("GUID, tabnum, fio");
                foreach (var item in items)
                {
                    if(item.type == "person")
                        sw.WriteLine(item.id+","+item.sam+","+item.name+" "+item.mlname);
                }
                output = sw.ToString();
            }
            return output;
        }

    }
}
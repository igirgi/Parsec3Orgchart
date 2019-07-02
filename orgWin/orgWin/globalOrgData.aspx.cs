using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.IO;
using System.Text;
using System.Web;
using System.Configuration;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using orgWin.IntegrationWebService;
using Newtonsoft.Json;

namespace orgWin
{
    public partial class globalOrgData : System.Web.UI.Page
    {
        public static Dictionary<Guid, orgNode> makeOrgDataAndUserimgs(string appPath)
        {
            Dictionary<Guid, orgNode> nodes = new Dictionary<Guid, orgNode>();//подназорные и подразделения вперемешку
            List<Guid> orgs = new List<Guid>(); //список Guid подразделен
            List<Guid> persons = new List<Guid>(); //список Guid поднадзорных
            Dictionary<string, string> nameMapping = (ConfigurationManager.GetSection("ParsecMapping") as System.Collections.Hashtable)
                .Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(d => (string)d.Key, d => (string)d.Value);

            IntegrationService iServ = new IntegrationService();

            string apidomain = ConfigurationManager.AppSettings.Get("domain");
            string apiuser = ConfigurationManager.AppSettings.Get("user");
            string apipassword = ConfigurationManager.AppSettings.Get("password");
            string turniketString = ConfigurationManager.AppSettings.Get("turniket");
            Guid turniket = new Guid(turniketString);
            string orgRootString = ConfigurationManager.AppSettings.Get("orgRoot");
            Guid orgRoot = new Guid(orgRootString);

            SessionResult res = iServ.OpenSession(apidomain, apiuser, apipassword);
            if (res.Result != 0)
                return null;

            Guid sessionID = res.Value.SessionID;

            Dictionary<Guid, string> extraNames = new Dictionary<Guid, string>();
            PersonExtraFieldTemplate[] extraTempl = iServ.GetPersonExtraFieldTemplates(sessionID);
            for (int j = extraTempl.Length - 1; j >= 0; j--)
            {
                extraNames.Add(extraTempl[j].ID, extraTempl[j].NAME);
            }

            BaseObject[] hierarhyList = iServ.GetOrgUnitSubItemsHierarhyWithPersons(sessionID, orgRoot);
            for (int i = hierarhyList.Length - 1; i >= 0; i--)
            {
                orgNode node = new orgNode();

                Person personItem = hierarhyList[i] as Person;
                if (personItem != null)
                {
                    node.id = personItem.ID;
                    node.pid = personItem.ORG_ID;
                    node.name = personItem.LAST_NAME.Trim();
                    node.mlname = (personItem.FIRST_NAME ?? "") + " " + (personItem.MIDDLE_NAME ?? "");
                    node.sam = personItem.TAB_NUM;
                    node.type = "person";
                    ExtraFieldValue[] extraVals = iServ.GetPersonExtraFieldValues(sessionID, node.id);
                    //map extraFields from Parsec to orgNode
                    for (int k = extraVals.Length - 1; k >= 0; k--)
                    {
                        string extraName = extraNames[extraVals[k].TEMPLATE_ID];
                        string propname ="";
                        if (nameMapping.TryGetValue(extraName, out propname)){
                            node[propname] = (string)extraVals[k].VALUE;                            
                        }
                    }

                    var pPhoto = iServ.GetPerson(sessionID, node.id);

                    if (pPhoto.PHOTO != null)
                        using (Image image = Image.FromStream(new MemoryStream(pPhoto.PHOTO)))
                        {
                            image.Save(
                                appPath + "userimg/" + pPhoto.ID.ToString() + ".jpg",
                                ImageFormat.Jpeg
                            );
                            node.img = node.id;
                        }

                    nodes.Add(node.id, node);
                    persons.Add(node.id);
                    continue;
                }
                OrgUnit orgItem = hierarhyList[i] as OrgUnit;
                if (orgItem != null)
                {
                    node.id = orgItem.ID;
                    node.pid = orgItem.PARENT_ID;
                    node.name = orgItem.NAME;
                    var boss = nodes.Values.FirstOrDefault(n => n.name.Equals(orgItem.DESC.Trim()));
                    if(boss != null) node.boss =  boss.id;
                    node.type = "org";                    
                    nodes.Add(orgItem.ID, node);
                    orgs.Add(node.id);
                }
            }

            iServ.CloseSession(sessionID);
            HttpContext.Current.Application["orgdata"] = nodes;
            HttpContext.Current.Application["orgIds"] = orgs;
            HttpContext.Current.Application["personIds"] = persons;
            return nodes;
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            var nodes = makeOrgDataAndUserimgs(Request.PhysicalApplicationPath);
            saveOrgChart(nodes.Values.ToList());

            Response.Clear();
            Response.ContentType = "application/json; charset=utf-8";
            Response.Write(JsonConvert.SerializeObject(nodes.Values.ToList(),
                Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            })
            );
            Response.End();
            
        }
        public static void saveOrgChart(List<orgNode> nodes)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["ParsecReport"].ConnectionString;
            using (SqlConnection sqlCon = new SqlConnection(connectionString))
            {
                sqlCon.Open();
                dropTable(sqlCon);
                createTable(sqlCon);
                fillTable(sqlCon, nodes);
                sqlCon.Close();
            }

        }
        static void dropTable(SqlConnection sqlCon)
        {
            string sqldrop = "IF OBJECT_ID('dbo.orgChart', 'U') IS NOT NULL DROP TABLE dbo.orgChart;";
            using (SqlCommand sqlCmd = new SqlCommand { CommandText = sqldrop, Connection = sqlCon })
            {
                sqlCmd.ExecuteNonQuery();
            }

        }
        static void createTable(SqlConnection sqlCon)
        {
            string sqlcr = @"CREATE TABLE [dbo].[orgChart](
	            [id] [varchar](150) NOT NULL,
                [pid] [varchar](150) NOT NULL,
            	[name] [varchar](150) NULL,
            	[type] [nchar](30) NULL
            ) ON [PRIMARY]";
            using (SqlCommand sqlCmd = new SqlCommand { CommandText = sqlcr, Connection = sqlCon })
            {
                sqlCmd.ExecuteNonQuery();
            }

        }
        static void fillTable(SqlConnection sqlCon, List<orgNode> nodes)
        {
            string sqlc = "INSERT INTO orgChart(name,id,pid,type) VALUES (@name, @id, @pid,@type)";
            nodes.ForEach((n) =>
            {
                using (SqlCommand sqlCmd1 = new SqlCommand { CommandText = sqlc, Connection = sqlCon })
                {
                    sqlCmd1.Parameters.AddWithValue("@name", n.name+" "+n.mlname);
                    sqlCmd1.Parameters.AddWithValue("@id", n.id.ToString());
                    sqlCmd1.Parameters.AddWithValue("@pid", n.pid.ToString());
                    sqlCmd1.Parameters.AddWithValue("@type", n.type);
                    sqlCmd1.ExecuteNonQuery();
                }
            });
        }

    }
}
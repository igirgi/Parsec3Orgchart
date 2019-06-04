using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
            List<orgNode> orgs = new List<orgNode>(); //список ссылок на подразделения
            List<orgNode> persons = new List<orgNode>(); //список ссылок на поднадзорных

            IntegrationService iServ = new IntegrationService();

            string apidomain = ConfigurationManager.AppSettings.Get("domain");
            string apiuser = ConfigurationManager.AppSettings.Get("user");
            string apipassword = ConfigurationManager.AppSettings.Get("password");
            string turniketString = ConfigurationManager.AppSettings.Get("turniket");
            Guid turniket = new Guid(turniketString);
            string orgRootString = ConfigurationManager.AppSettings.Get("orgRoot");
            Guid orgRoot = new Guid(orgRootString);

            SessionResult res = iServ.OpenSession(apidomain, apiuser, apipassword);
            if (res.Result != ClientState.Result_Success)
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
                    node.tag = "person";
                    ExtraFieldValue[] extraVals = iServ.GetPersonExtraFieldValues(sessionID, node.id);
                    for (int k = extraVals.Length - 1; k >= 0; k--)
                    {
                        switch (extraNames[extraVals[k].TEMPLATE_ID])
                        {
                            case "E-mail":
                                node.mail = (string)extraVals[k].VALUE;
                                break;
                            case "Мобильный телефон":
                                node.mob = (string)extraVals[k].VALUE;
                                break;
                            case "День рождения":
                                node.birthday = (string)extraVals[k].VALUE;
                                break;
                            case "Корпоративный телефон":
                                node.corp = (string)extraVals[k].VALUE;
                                break;
                            case "Должность":
                                node.title = (string)extraVals[k].VALUE;
                                break;
                            default:
                                break;

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
                        }

                    nodes.Add(node.id, node);
                    continue;
                }
                OrgUnit orgItem = hierarhyList[i] as OrgUnit;
                if (orgItem != null)
                {
                    node.id = orgItem.ID;
                    node.pid = orgItem.PARENT_ID;
                    node.name = orgItem.NAME;
                    var boss = nodes.Values.FirstOrDefault(n => n.name.Equals(orgItem.DESC.Trim()));
                    node.boss = boss != null ? boss.id.ToString() : ("->"+orgItem.DESC.Trim()+"<-");
                    node.tag = "org";                    
                    nodes.Add(orgItem.ID, node);
                }
            }
            orgs = nodes.Values.Where(p => p.tag.Equals("org")).ToList<orgNode>();
            persons = nodes.Values.Where(p => p.tag.Equals("person")).ToList<orgNode>();

            iServ.CloseSession(sessionID);
            HttpContext.Current.Application["orgdata"] = nodes;
            HttpContext.Current.Application["orglist"] = orgs;
            HttpContext.Current.Application["personlist"] = persons;
            HttpContext.Current.Application["orgdatatime"] = DateTime.Now - new TimeSpan(0, 2, 0);
            return nodes;
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            var nodes = makeOrgDataAndUserimgs(Request.PhysicalApplicationPath);            
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
    }
}
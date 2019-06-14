using System;
using System.Collections.Generic;
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
                    node.type = "person";
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
//                    string[] orgName = orgItem.NAME.Split(" ".ToCharArray());
//                    node.name = orgName[0];
//                    split2triple(orgName.Skip(1).ToArray(), 22, 15,  ref node.mlname, ref node.title, ref node.mob);                    
                    var boss = nodes.Values.FirstOrDefault(n => n.name.Equals(orgItem.DESC.Trim()));
                    if(boss != null) node.img =  boss.id;
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
        private static void getHead(string[] instr, int headLength, ref string head,  ref string tail)
        {
            for (int l = 0; l < instr.Length; l++)
            {
                head = String.Join(" ", instr.Take(instr.Length - l).ToArray());
                if (head.Length <= headLength)
                {
                    tail = String.Join(" ", instr.Skip(instr.Length - l).ToArray());
                    return;
                }
            }
            if (instr.Length > 0)
            {
                head = instr.Take(1).ToArray()[0];
                tail = String.Join(" ", instr.Skip(1).ToArray());
            }
        }
        protected static void split2triple(string[] instr, int headLength, int middleLength, ref string head, ref string middle, ref string tail)
        {
            string tmp = "";
            getHead(instr, headLength, ref head, ref tmp);
            if (tmp.Length > middleLength)
                getHead(tmp.Split(" ".ToCharArray()), middleLength, ref middle, ref tail);
            else
                middle = tmp;
            return;
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
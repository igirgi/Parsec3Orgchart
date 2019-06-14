using System;
using System.Collections.Generic;
using System.Configuration;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using System.Collections;
using orgWin.IntegrationWebService;

namespace orgWin
{
    public partial class parsecStatus : System.Web.UI.Page
    {
        public class pNode
        {
            public string tag;
            public string enter;
            public string exit;
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            Guid[] personIds = ((List<Guid>) HttpContext.Current.Application["personIds"]).ToArray();
            Dictionary<Guid, pNode> nodes = new Dictionary<Guid, pNode>(personIds.Length);
            for (int g = 0; g < personIds.Length; g++)
                nodes.Add(personIds[g], new pNode());
            IntegrationService igServ = new IntegrationService();
            string parsecDomain = ConfigurationManager.AppSettings.Get("domain");
            string parsecUser = ConfigurationManager.AppSettings.Get("user");
            string parsecPassword = ConfigurationManager.AppSettings.Get("password");
            string turniketString = ConfigurationManager.AppSettings.Get("turniket");
            Guid turniket = new Guid(turniketString);

            EventHistoryQueryParams param = new EventHistoryQueryParams();
            param.StartDate = DateTime.Today.ToUniversalTime();
            param.EndDate = DateTime.Today.AddDays(1).ToUniversalTime();
            param.Territories = new Guid[] { turniket };
            param.EventsWithoutUser = false;
            param.TransactionTypes = new uint[] { 590144, 590152, 65867, 590165, 590145, 590153, 65866, 590166 };
            param.MaxResultSize = (10000);
            param.Organizations = ((List<Guid>)HttpContext.Current.Application["orgIds"]).ToArray();
            param.Users = ((List<Guid>)HttpContext.Current.Application["personIds"]).ToArray();
            List<Guid> fields = new List<Guid>();
            fields.Add(EventHistoryFields.EVENT_TIME);
            fields.Add(EventHistoryFields.EVENT_CODE_HEX);
            fields.Add(new Guid("7C6D82A0-C8C8-495B-9728-357807193D23")); //ID юзера
            EventObject[] events = null;

            //дергаем Парсек
            SessionResult res = igServ.OpenSession(parsecDomain, parsecUser, parsecPassword);
            if (res.Result != 0){Response.Write("{\"err\":\"Can not open session to Parsec\"}");return;}
            Guid sessionID = res.Value.SessionID;
//сессия истории с Парсеком
            GuidResult res1 = igServ.OpenEventHistorySession(sessionID, param);
            if (res1.Result != 0) {Response.Write("{\"err\":\"Can not OpenEventHistorySession in Parsec\"}"); return; }
//получить события дня
            events = igServ.GetEventHistoryResult(sessionID, res1.Value /*сессия истории*/, fields.ToArray(), 0, 10000);
            igServ.CloseEventHistorySession(sessionID, res1.Value);
            igServ.CloseSession(sessionID);

            //ищем последний выход (просматриваем список с начала к концу)
            for (int ii = 0; ii < events.Length; ii++)
            {
                switch ((string)events[ii].Values[1])
                {
                    case "90141":
                    case "90149":
                    case "90142":
                    case "901A4":
                    case "90156":
                    case "901A5":  //КОДЫ ВЫХОДА
                        if (events[ii].Values[2] != null)
                        {
                            Guid nodeid = new Guid(events[ii].Values[2].ToString());
                            if (nodes.ContainsKey(nodeid))
                                nodes[nodeid].exit = events[ii].Values[0].ToString();
                        }
                        break;
                    case "90140":
                    case "90148":
                    case "1014B":
                    case "90155":  //КОДЫ ВХОДА - если был вход после выхода, значит выхода не было ;)
                        if (events[ii].Values[2] != null)
                        {
                            Guid nodeid = new Guid(events[ii].Values[2].ToString());
                            if (nodes.ContainsKey(nodeid))
                                nodes[nodeid].exit = null;
                        }
                        break;
                }
            }
            //ищем первый вход (просматриваем с конца к началу)
            for (int ii = events.Length - 1; ii >= 0; ii--)
            {
                switch ((string)events[ii].Values[1])
                {
                    case "90140":
                    case "90148":
                    case "1014B":
                    case "90155":  //КОДЫ ВХОДА
                        if (events[ii].Values[2] != null)
                        {
                            Guid nodeid = new Guid(events[ii].Values[2].ToString());
                            if (nodes.ContainsKey(nodeid))
                                nodes[nodeid].enter = events[ii].Values[0].ToString();
                        }
                        break;
                }
            }

            List<pNode> pl = nodes.Values.ToList<pNode>();
            DateTime vhod, vihod;
            for (int k = 0; k < pl.Count; k++)
            {
                if (!DateTime.TryParse(pl[k].enter, out vhod))
                {
                    pl[k].tag = "absent";
                    continue;
                }
                if (!DateTime.TryParse(pl[k].exit, out vihod))
                {
                    pl[k].tag = "in";
                    continue;
                }
                if (vihod > vhod)
                {
                    pl[k].tag = "out";
                }
            }


            Response.Clear();
            Response.ContentType = "application/json; charset=utf-8";
            Response.Write(JsonConvert.SerializeObject(nodes,
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
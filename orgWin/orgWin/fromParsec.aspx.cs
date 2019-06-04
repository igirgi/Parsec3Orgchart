using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using System.Collections.Specialized;
using System.Data.SqlClient;
using Newtonsoft.Json; 
using orgWin.IntegrationWebService;

namespace orgWin
{
    public partial class fromParsec : System.Web.UI.Page
    {
        Dictionary<Guid, ParsecNode> pnodes = new Dictionary<Guid, ParsecNode>();
        List<Guid> orgGuids = new List<Guid>();
        IntegrationService igServ = new IntegrationService();
        protected void Page_Load(object sender, EventArgs e)
        {
            //инициализируем из справочника оргструктуры
            getStatic();
            //добавляем из Парсека
            getDynamic();

            Response.Clear();
            Response.ContentType = "application/json; charset=utf-8";
            Response.Write(JsonConvert.SerializeObject(pnodes.Values.ToList()));
            Response.End();
        }
//инициализируем из справочника оргструктуры        
        private void getStatic()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["ParsecReport"].ConnectionString;
            using (SqlConnection sqlCon = new SqlConnection(connectionString))
            {
                sqlCon.Open();
                string sqlc = "select * from orgChart";
                SqlCommand cmd = new SqlCommand(sqlc, sqlCon);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    ParsecNode pn = new ParsecNode()
                    {
                        id = Convert.ToInt32(dr["id"]),
                        pid = Convert.ToInt32(dr["pid"]),
                        имя = dr["name"].ToString(),
                        должность = dr["title"].ToString(),
                        img = dr["img"].ToString(),
                        мобильный_тел = dr["mobileTel"].ToString(),
                        корпоративный_тел = dr["corpTel"].ToString(),
                        tags = new string[] { dr["tag"].ToString().Trim() }
                    };
                    string ttag = dr["tag"].ToString().Trim();
                    if (ttag.Equals("org"))
                        orgGuids.Add(new Guid(dr["itemGuid"].ToString()));
                    pnodes.Add(new Guid(dr["itemGuid"].ToString()), pn);
                }
                sqlCon.Close();
            }
        }
        private void getDynamic()
        {
            IntegrationService igServ = new IntegrationService();
            string domain = ConfigurationManager.AppSettings.Get("domain");
            string dbuser = ConfigurationManager.AppSettings.Get("user");
            string dbpassword = ConfigurationManager.AppSettings.Get("password");
            string turniketString = ConfigurationManager.AppSettings.Get("turniket");
            Guid turniket = new Guid(turniketString);
            SessionResult res = igServ.OpenSession(domain, dbuser, dbpassword);
            if (res.Result != ClientState.Result_Success)
            {
                Response.Write("{\"err\":\"Can not open session to Parsec\"}");
                return;
            }
            ClientState.SetSession(res.Value, domain, dbuser);

            EventHistoryQueryParams param = new EventHistoryQueryParams();
            param.StartDate = DateTime.Today.ToUniversalTime();
            param.EndDate = DateTime.Today.AddDays(1).ToUniversalTime();
            param.Territories = new Guid[] { turniket };
            param.EventsWithoutUser = false;
            param.TransactionTypes = new uint[] { 590144, 590152, 65867, 590165, 590145, 590153, 65866, 590166 };
            param.MaxResultSize = (10000);
            param.Organizations = orgGuids.ToArray();

            doIt(param);

            igServ.CloseSession(ClientState.SessionID);
        }
        private void doIt(EventHistoryQueryParams param)
        {
            GuidResult res1 = igServ.OpenEventHistorySession(ClientState.SessionID, param);
            if (res1.Result != ClientState.Result_Success) {
                Response.Write("{\"err\":\"Can not OpenEventHistorySession in Parsec\"}");
                return;
            }
            Guid eventSessionID = res1.Value;
//            int _count = igServ.GetEventHistoryResultCount(ClientState.SessionID, eventSessionID);

            List<Guid> fields = new List<Guid>();
            fields.Add(EventHistoryFields.EVENT_TIME);
            fields.Add(EventHistoryFields.EVENT_CODE_HEX);
            fields.Add(new Guid("7C6D82A0-C8C8-495B-9728-357807193D23")); //ID юзера

            EventObject[] events = null;                        
            events = igServ.GetEventHistoryResult(ClientState.SessionID, eventSessionID, fields.ToArray(), 0, 10000);
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
                            if(pnodes.ContainsKey(nodeid))
                                pnodes[nodeid].выход = events[ii].Values[0].ToString();
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
                            if(pnodes.ContainsKey(nodeid))
                                pnodes[nodeid].вход = events[ii].Values[0].ToString();
                        }
                        break;
                }
            }
            igServ.CloseEventHistorySession(ClientState.SessionID, eventSessionID);
// считаем опоздания
            var proposedEnter = DateTime.Parse("9:10:00");
            
            List<ParsecNode> pl = pnodes.Values.Where(p => p.tags[0].Equals("person")).ToList<ParsecNode>();
            DateTime vhod, vihod;
            for (int k = 0; k < pl.Count; k++){                
                if(!DateTime.TryParse(pl[k].вход, out vhod)){
                    pl[k].tags = new string[] {"не приходил"};
                    continue;
                }
                if(!DateTime.TryParse(pl[k].выход, out vihod)){
                    pl[k].tags = new string[] {"в офисе"};
                    continue;
                }
                if (vihod > vhod){
                    pl[k].tags = new string[] { "вышедши" };
                }
            }
        }
      
    }
    class ParsecNode
    {
        public int id;
        public int pid;
        public string[] tags;
        public string имя;
        public string должность;
        public string вход;
        public string выход;
        public string img;
        public string мобильный_тел;
        public string корпоративный_тел;
    }
 
  }
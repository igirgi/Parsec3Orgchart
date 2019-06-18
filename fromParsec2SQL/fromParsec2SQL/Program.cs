using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Specialized;
using System.Data.SqlClient;
using fromParsec2SQL.IntegrationWebService;

namespace fromParsec2SQL
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime JudgmentDay;
            if (args.Length < 1)
            {
                Console.WriteLine("USAGE: fromParsec2SQL <DD.MM.YYYY/TODAY/YESTERDAY>");
                return;
            }
            else if (args[0].ToUpper().StartsWith("TODAY"))
            {
                JudgmentDay = DateTime.Today;
            }
            else if (args[0].ToUpper().StartsWith("YESTERDAY"))
            {
                JudgmentDay = DateTime.Today.AddDays(-1);
            }
            else if (!DateTime.TryParse(args[0], out JudgmentDay))
            {
                Console.WriteLine("USAGE: pFillDayEvents <DD.MM.YYYY/TODAY/YESTERDAY>");
                return;
            }
            //штатное время выхода и входа для заданной даты
            DateTime proposedExit = (JudgmentDay.DayOfWeek.ToString() == "Friday") ?
                new DateTime(JudgmentDay.Year, JudgmentDay.Month, JudgmentDay.Day, 17, 0, 0)
                : new DateTime(JudgmentDay.Year, JudgmentDay.Month, JudgmentDay.Day, 18, 15, 0);
            DateTime proposedEnter = new DateTime(JudgmentDay.Year, JudgmentDay.Month, JudgmentDay.Day, 9, 0, 0);

            var connectionString = ConfigurationManager.ConnectionStrings["ParsecReport"].ConnectionString;
            using (SqlConnection sqlCon = new SqlConnection(connectionString))
            {
                sqlCon.Open();
                checkTable(sqlCon);
                sqlCon.Close();
            }

            IntegrationService igServ = new IntegrationService();
            string parsecdomain = ConfigurationManager.AppSettings.Get("domain");
            string parsecuser = ConfigurationManager.AppSettings.Get("user");
            string parsecpassword = ConfigurationManager.AppSettings.Get("password");
            string turniketString = ConfigurationManager.AppSettings.Get("turniket");
            Guid turniket = new Guid(turniketString);
            SessionResult res = igServ.OpenSession(parsecdomain, parsecuser, parsecpassword);
            if (res.Result != 0)
            {
                Console.WriteLine("Can not open session to Parsek");
                return;
            }
            Guid sessionID = res.Value.SessionID;
            
            BaseObject[] hierarhyList = igServ.GetOrgUnitsHierarhyWithPersons(sessionID);
            EventHistoryQueryParams param = new EventHistoryQueryParams();
            param.StartDate = JudgmentDay.ToUniversalTime();
            param.EndDate = JudgmentDay.AddDays(1).ToUniversalTime();
            param.Territories = new Guid[] { turniket };
            param.EventsWithoutUser = false;
            param.TransactionTypes = new uint[] { 590144, 590152, 65867, 590165, 590145, 590153, 65866, 590166 };
            param.MaxResultSize = (10000);

            EventHistoryFields constants = new EventHistoryFields();
            List<Guid> fields = new List<Guid>();
            fields.Add(EventHistoryFields.EVENT_DATE_TIME);
            fields.Add(EventHistoryFields.EVENT_CODE_HEX);

            using (SqlConnection sqlCon = new SqlConnection(connectionString))
            {
                sqlCon.Open();

                int i = 0;
                for (i = hierarhyList.Length - 1; i >= 0; i--)
                {
                    if (hierarhyList[i] == null)
                        continue;
                    Person personItem = hierarhyList[i] as Person;
                    if (personItem != null)
                    {
                        Guid userid = personItem.ID;
                        List<string> hlist = new List<string>();
                        param.Users = new Guid[] { personItem.ID };
                        GuidResult ev = igServ.OpenEventHistorySession(sessionID, param);
                        if (ev.Result != 0) continue;
                        Guid eventSessionID = ev.Value;

                        DateTime? enter = null, exit = null, lastEventTime = null;
                        DateTime tmptime;
                        bool lastEventWasEnter = false;
                        int eventscount = 0, totalworktime = 0, totalouttime = 0;
                        EventObject[] events = null;
                        events = igServ.GetEventHistoryResult(sessionID, eventSessionID, fields.ToArray(), 0, 10000);
                        for (int ii = 0; ii < events.Length; ii++)
                        {
                            switch ((string)events[ii].Values[1])
                            {
                                case "90140":
                                case "90148":
                                case "1014B":
                                case "90155":  //КОДЫ ВХОДА
                                    eventscount++;
                                    hlist.Add("Вход: " + events[ii].Values[0].ToString() + "<br>");
                                    if (DateTime.TryParse(events[ii].Values[0].ToString(), out tmptime))
                                    {
                                        enter = (enter == null || tmptime < enter) ? tmptime : enter;
                                        totalouttime += (lastEventWasEnter || lastEventTime == null) ? 0 : (int)(tmptime - (DateTime)lastEventTime).TotalMinutes;
                                        lastEventWasEnter = true;
                                        lastEventTime = tmptime;
                                    }
                                    break;
                                case "90141":
                                case "90149":
                                case "90142":
                                case "901A4":
                                case "90156":
                                case "901A5":  //КОДЫ ВЫХОДА
                                    eventscount++;
                                    hlist.Add("Выход: " + events[ii].Values[0].ToString() + "<br>");
                                    if (DateTime.TryParse(events[ii].Values[0].ToString(), out tmptime))
                                    {
                                        exit = (exit == null || tmptime > exit) ? tmptime : exit;
                                        totalworktime += (!lastEventWasEnter || lastEventTime == null) ? 0 : (int)(tmptime - (DateTime)lastEventTime).TotalMinutes;
                                        lastEventWasEnter = false;
                                        lastEventTime = tmptime;
                                    }
                                    break;
                            }
                        }

                        DateTime renter, rexit;
                        if (enter == null || exit == null) continue;
                        else
                        {
                            renter = enter ?? DateTime.Now;
                            rexit = exit ?? DateTime.Now;
                        }

                        int opozd = (int)(renter - proposedEnter).TotalMinutes;
                        int zader = (int)(rexit - proposedExit).TotalMinutes;
                        //                        hlist.Reverse();
                        string history = String.Concat(hlist);
                        string sqlc = "if not exists "
                            + "(select * from ParsecOrgHistory where id = @id and sysdate = @sysdate) "
                            + " insert into ParsecOrgHistory "
                            +" (id, sysdate, date, enter,eexit, eventscount, totalworktime, totalouttime, opozd, zader, history) "
                            + " values (@id, @sysdate, @date, @enter,@eexit, @eventscount, @totalworktime,@totalouttime, @opozd, @zader, @history)"
                            ;
                        using (SqlCommand sqlCmd1 = new SqlCommand { CommandText = sqlc, Connection = sqlCon })
                        {
                            sqlCmd1.Parameters.AddWithValue("@id", userid.ToString());
                            sqlCmd1.Parameters.AddWithValue("@sysdate", JudgmentDay);
                            sqlCmd1.Parameters.AddWithValue("@date", JudgmentDay.ToString("dd-MM-yyyy"));
                            sqlCmd1.Parameters.AddWithValue("@enter", renter.ToString("HH-mm"));
                            sqlCmd1.Parameters.AddWithValue("@eexit", rexit.ToString("HH-mm"));
                            sqlCmd1.Parameters.AddWithValue("@eventscount", eventscount);
                            sqlCmd1.Parameters.AddWithValue("@totalworktime", totalworktime);
                            sqlCmd1.Parameters.AddWithValue("@totalouttime", totalouttime);
                            sqlCmd1.Parameters.AddWithValue("@opozd", opozd);
                            sqlCmd1.Parameters.AddWithValue("@zader", zader);
                            sqlCmd1.Parameters.AddWithValue("@history", history);
                            sqlCmd1.ExecuteNonQuery();
                        }
                    }
                }

                sqlCon.Close();
            }
        }

        private static void checkTable(SqlConnection sqlCon)
        {
            string sqlc = @"IF OBJECT_ID('dbo.ParsecOrgHistory', 'U') IS NULL
            CREATE TABLE [dbo].[ParsecOrgHistory](
            	[id] [varchar](150) NOT NULL,
                [sysdate] [date] NOT NULL,
            	[date] [varchar](20) NOT NULL,
            	[enter] [varchar](20) NOT NULL,
            	[eexit] [varchar](20) NOT NULL,
            	[eventscount] [int] NOT NULL,
            	[totalworktime] [int] NOT NULL,
                [totalouttime] [int] NOT NULL,
              	[opozd] [int] NOT NULL,
            	[zader] [int] NOT NULL,
                [history] [varchar](MAX) NOT NULL
 
            )";
            using (SqlCommand sqlCmd = new SqlCommand { CommandText = sqlc, Connection = sqlCon })
            {
                sqlCmd.ExecuteNonQuery();
            }
        }


    }
}

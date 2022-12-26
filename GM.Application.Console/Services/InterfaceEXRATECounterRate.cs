using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using GM.Application.Console.Model;
using GM.ClientAPI;
using GM.CommonLibs.Common;
using GM.Model.Common;
using GM.Model.ExternalInterface;
using GM.Model.Static;

namespace GM.Application.Console.Services
{

    public class InterfaceEXTRAETCounterRate : WebAPI 
    {
        private DateTime systemDate = DateTime.Now;  
        private IConsoleService _service;  
        private static Console_Entity ConsoleEnt = new Console_Entity();
        private static Mail_AdminEntity MailAdminEnt = new Mail_AdminEntity();  

        public InterfaceEXRATECounterRate(IHttpClientFactory httpClientFactory, IConsoleService consoleService) : base(httpClientFactory)    
        {
            _service = consoleService;
        }  

        public boo Run() 
        {
            string StrMsg = string.Empty;  
            bool SendEmail = false;    
            try 
            {
                _service.WriteLogs("### START RUN FUNCTION [" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "] : [" + _service.GetFunction() + "()] ###");   

                InterfaceCounterRateExRateModel CounterRateExRateModel = new InterfaceCounterRateExRateModel();    

                //Step 1 : Check Holiday
                var isHoliday = -service.CheckHoliday(systemDate);  

                if (isHoliday)  
                {
                      _service.WriteLogs(_service.GetFunction() + "Is Holiday");  
                      return true;  
                }

                string inputDate = _service.GetInputDate();  
                systemDate = _service.GetBusinessDateOrSystemDate(inputDate);
                
                var ResultRpconfig = StaticAPI.RpConfig.GetRpConfig("RP_EXRATE_INTERFACE_COUNTER_RATE", string.Empty);
                if (!ResultRpconfig.Success)
                {
                    throw new Exception("GetRpConfig() => [" + ResultRpconfig.RefCode + "] " + ResultRpconfig.Message);
                }

                //Step 2 : Set Config
                List<RpConfigModel> rpConfigModel = ResultRpconfig.Data;
                if (!SetConfigInterfaceEXRATECounterRate(ref StrMsg, ref CounterRateExRateModel, rpConfigModel))
                {
                    throw new Exception("Set_ConfigInterfaceEXRATECounterRate() => " + StrMsg);
                }

                _service.WriteLogs("Get Config Success");
                if (ConsoleEnt.Enable == "N")
                {
                    _service.WriteLogs(_service.GetFunction() + " Disable");
                    return true;
                }

                //Step 3 : Interface Counter Rate
                _service.WriteLogs("");
                _service.WriteLogs("Run ImportCounterRateExRate");
                int CountRound = 0;
                int EndRound = 5;
                int ExRound = 0;

                ResultWithModel<List<InterfaceCounterRateExRateModel>> resultCounterRateExRate;

                do
                {
                    if (EndRound == ExRound || EndRound == CountRound)
                    {
                        throw new Exception("CounterRate Not Found And EndRound = [" + EndRound.ToString() + "]");
                    }

                    resultCounterRateExRate = ExternalInterfaceAPI.InterfaceCounterRateExRate.ImportCounterRateExRate(CounterRateExRateModel);
                    if (!resultCounterRateExRate.Success)
                    {
                        throw new Exception("ImportCounterRateExRate() => [" + resultCounterRateExRate.RefCode.ToString() + "] " + resultCounterRateExRate.Message);
                    }

                    ExRound = resultCounterRateExRate.Data[0].exRound;
                    systemDate = systemDate.AddMinutes(-10);
                    CounterRateExRateModel.exDate = systemDate.ToString("yyyyMMdd HH:mm");
                    CountRound++;
                } while (ExRound > 1);

                _service.WriteLogs("ReturnCode = [" + resultCounterRateExRate.RefCode + "] " + resultCounterRateExRate.Message);
                _service.WriteLogs("ImportCounterRateExRate Success.");

            }
            catch (Exception Ex)
            {
                _service.WriteLogs("Error " + Ex.Message);
                SendEmail = true;
            }
            finally
            {
                _service.WriteLogs("### END RUN FUNCTION [" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "] : [" + _service.GetFunction() + "()] ###");
                if (SendEmail)
                {
                    _service.SendMailError(MailAdminEnt);
                }
            }
            return true;
        }

        private bool SetConfigInterfaceEXRATECounterRate(ref string ReturnMsg, ref InterfaceCounterRateExRateModel CounterRateExRateModel, List<RpConfigModel> List_RpConfigModel)
        {
            try
            {
                ConsoleEnt.Enable = List_RpConfigModel.FirstOrDefault(a => a.item_code == "ENABLE")?.item_value;

                int.TryParse(List_RpConfigModel.FirstOrDefault(a => a.item_code == "EX_ROUND")?.item_value, out var exRound);
                CounterRateExRateModel.exRound = exRound;
                CounterRateExRateModel.exDate = systemDate.ToString("yyyyMMdd");
                CounterRateExRateModel.exTime = List_RpConfigModel.FirstOrDefault(a => a.item_code == "EX_TIME")?.item_value;

                systemDate = DateTime.ParseExact(CounterRateExRateModel.exDate + " " + CounterRateExRateModel.exTime, "yyyyMMdd HH:mm", CultureInfo.InvariantCulture);
                CounterRateExRateModel.asof_date = systemDate.Date;
                CounterRateExRateModel.channel = List_RpConfigModel.FirstOrDefault(a => a.item_code == "CHANNEL")?.item_value;
                CounterRateExRateModel.exCurrency = List_RpConfigModel.FirstOrDefault(a => a.item_code == "CURRENCY")?.item_value;
                CounterRateExRateModel.serviceID = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_ID")?.item_value;
                CounterRateExRateModel.ServiceUrl = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_URL")?.item_value;
                CounterRateExRateModel.ServiceType = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_TYPE")?.item_value;
                CounterRateExRateModel.ApiAuthenUrl = List_RpConfigModel.FirstOrDefault(a => a.item_code == "API_AUTHEN_URL")?.item_value;
                CounterRateExRateModel.ApiRateUrl = List_RpConfigModel.FirstOrDefault(a => a.item_code == "API_RATE_URL")?.item_value;
                CounterRateExRateModel.ApiUsername = List_RpConfigModel.FirstOrDefault(a => a.item_code == "API_USERNAME")?.item_value;
                CounterRateExRateModel.ApiPassword = List_RpConfigModel.FirstOrDefault(a => a.item_code == "API_PASSWORD")?.item_value;

                if (!string.IsNullOrEmpty(List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_TIMEOUT")?.item_value))
                {
                    CounterRateExRateModel.ServiceTimeOut = Convert.ToInt32(List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_TIMEOUT")?.item_value);
                }
                else
                {
                    CounterRateExRateModel.ServiceTimeOut = 600000;
                }

                MailAdminEnt.Host = List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_SERVER")?.item_value;
                if (!string.IsNullOrEmpty(List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_PORT")?.item_value))
                {
                    MailAdminEnt.Port = Convert.ToInt32(List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_PORT")?.item_value);
                }
                MailAdminEnt.From = List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_SENDER")?.item_value;
                string Mail_To_Tmp = List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_ADMIN")?.item_value;
                if (!String.IsNullOrEmpty(Mail_To_Tmp))
                {
                    string[] mail_to_array = Mail_To_Tmp.Split(',');
                    foreach (string mail_to in mail_to_array)
                    {
                        MailAdminEnt.To.Add(mail_to);
                    }
                }
                MailAdminEnt.Subject = List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_SUBJECT")?.item_value;
                MailAdminEnt.Subject = MailAdminEnt.Subject?.Replace("{1}", "Run at " + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss"));

                _service.WriteLogs("Enable       = " + ConsoleEnt.Enable);
                _service.WriteLogs("ExRound       = " + CounterRateExRateModel.exRound);
                _service.WriteLogs("ExDate       = " + CounterRateExRateModel.exDate);
                _service.WriteLogs("ExTime       = " + CounterRateExRateModel.exTime);
                _service.WriteLogs("Channel      = " + CounterRateExRateModel.channel);
                _service.WriteLogs("ExCurrency   = " + CounterRateExRateModel.exCurrency);
                _service.WriteLogs("ServiceID    = " + CounterRateExRateModel.serviceID);
                _service.WriteLogs("ServiceUrl   = " + CounterRateExRateModel.ServiceUrl);
                _service.WriteLogs("ServiceType   = " + CounterRateExRateModel.ServiceType);
                _service.WriteLogs("ApiAuthenUrl   = " + CounterRateExRateModel.ApiAuthenUrl);
                _service.WriteLogs("ApiRateUrl   = " + CounterRateExRateModel.ApiRateUrl);

                _service.WriteLogs("MAIL_SERVER  = " + MailAdminEnt.Host);ห
                _service.WriteLogs("MAIL_PORT    = " + MailAdminEnt.Port);
                _service.WriteLogs("MAIL_SENDER  = " + MailAdminEnt.From);
                _service.WriteLogs("MAIL_ADMIN   = " + Mail_To_Tmp);
                _service.WriteLogs("MAIL_SUBJECT = " + MailAdminEnt.Subject);
            }
            catch (Exception ex)
            {
                ReturnMsg = ex.Message;
                return false;
            }

            return true;
        }
    }
}

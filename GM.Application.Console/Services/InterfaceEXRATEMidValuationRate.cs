using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using GM.Application.Console.Model;
using GM.ClientAPI;
using GM.CommonLibs.Common;
using GM.Model.ExternalInterface;
using GM.Model.Static;

namespace GM.Application.Console.Services
{
    public class InterfaceEXRATEMidValuationRate : WebAPI
    {
        private DateTime systemDate = DateTime.Now;
        private IConsoleService _service;
        private static Console_Entity ConsoleEnt = new Console_Entity();
        private static Mail_AdminEntity MailAdminEnt = new Mail_AdminEntity();

        public InterfaceEXRATEMidValuationRate(IHttpClientFactory httpClientFactory, IConsoleService consoleService) : base(httpClientFactory)
        {
            _service = consoleService;
        }

        public bool Run(string type)
        {
            string StrMsg = string.Empty;
            bool SendEmail = false;
            try
            {
                _service.WriteLogs("### START RUN FUNCTION [" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "] : [" + _service.GetFunction() + "()] ###");

                InterfaceMidValuationRateExRateModel MidValuationExRateModel = new InterfaceMidValuationRateExRateModel();
                
                string inputDate = _service.GetInputDate();
                systemDate = _service.GetBusinessDateOrSystemDate(inputDate);

                var ResultRpconfig = StaticAPI.RpConfig.GetRpConfig("RP_EXRATE_INTERFACE_MID_VALUATION_RATE", string.Empty);
                if (!ResultRpconfig.Success)
                {
                    throw new Exception("GetRpConfig() => [" + ResultRpconfig.RefCode + "] " + ResultRpconfig.Message);
                }

                //Step 2 : Set Config
                List<RpConfigModel> rpConfigModel = ResultRpconfig.Data;

                if (!SetConfigInterfaceEXRATEMidValuation(ref StrMsg, ref MidValuationExRateModel, rpConfigModel, type))
                {
                    throw new Exception("Set_ConfigInterfaceEXRATEMidValuation() => " + StrMsg);
                }

                _service.WriteLogs("Get Config Success");
                if (ConsoleEnt.Enable == "N")
                {
                    _service.WriteLogs(_service.GetFunction() + " Disable");
                    return true;
                }

                //Step 3 : Interface MidValuation Rate
                _service.WriteLogs("");
                _service.WriteLogs("Run ImportMidValuationExRate");

                var ResultMidValExRate = ExternalInterfaceAPI.InterfaceMidValuationExRate.ImportMidValuationExRate(MidValuationExRateModel);
                if (!ResultMidValExRate.Success)
                {
                    throw new Exception("ImportMidValuationExRate() => [" + ResultMidValExRate.RefCode.ToString() + "] " + ResultMidValExRate.Message);
                }

                _service.WriteLogs("ReturnCode = [" + ResultMidValExRate.RefCode + "] " + ResultMidValExRate.Message);
                _service.WriteLogs("ImportMidValuationExRate Success.");

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

        private bool SetConfigInterfaceEXRATEMidValuation(ref string ReturnMsg, ref InterfaceMidValuationRateExRateModel MidValuationExRateModel, List<RpConfigModel> List_RpConfigModel, string Type)
        {
            try
            {
                ConsoleEnt.Enable = List_RpConfigModel.FirstOrDefault(a => a.item_code == "ENABLE")?.item_value;
                MidValuationExRateModel.asof_date = systemDate.Date;
                MidValuationExRateModel.exDate = systemDate.ToString("yyyyMMdd");
                MidValuationExRateModel.channel = List_RpConfigModel.FirstOrDefault(a => a.item_code == "CHANNEL")?.item_value;
                MidValuationExRateModel.exCurrency = List_RpConfigModel.FirstOrDefault(a => a.item_code == "CURRENCY")?.item_value;
                MidValuationExRateModel.serviceID = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_ID")?.item_value;
                MidValuationExRateModel.ServiceUrl = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_URL")?.item_value;
                MidValuationExRateModel.ServiceType = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_TYPE")?.item_value;
                MidValuationExRateModel.ApiAuthenUrl = List_RpConfigModel.FirstOrDefault(a => a.item_code == "API_AUTHEN_URL")?.item_value;
                MidValuationExRateModel.ApiRateUrl = List_RpConfigModel.FirstOrDefault(a => a.item_code == "API_RATE_URL")?.item_value;
                MidValuationExRateModel.ApiUsername = List_RpConfigModel.FirstOrDefault(a => a.item_code == "API_USERNAME")?.item_value;
                MidValuationExRateModel.ApiPassword = List_RpConfigModel.FirstOrDefault(a => a.item_code == "API_PASSWORD")?.item_value;
                MidValuationExRateModel.type = Type;

                if (!String.IsNullOrEmpty(List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_TIMEOUT")?.item_value))
                {
                    MidValuationExRateModel.ServiceTimeOut = Convert.ToInt32(List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_TIMEOUT")?.item_value);
                }
                else
                {
                    MidValuationExRateModel.ServiceTimeOut = 600000;
                }

                MailAdminEnt.Host = List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_SERVER")?.item_value;
                if (!String.IsNullOrEmpty(List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_PORT")?.item_value))
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
                MailAdminEnt.Subject = MailAdminEnt.Subject?.Replace("{0}", Type);
                MailAdminEnt.Subject = MailAdminEnt.Subject?.Replace("{1}", "Run at " + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss"));

                _service.WriteLogs("Enable       = " + ConsoleEnt.Enable);
                _service.WriteLogs("ExDate       = " + MidValuationExRateModel.exDate);
                _service.WriteLogs("Channel      = " + MidValuationExRateModel.channel);
                _service.WriteLogs("ExCurrency   = " + MidValuationExRateModel.exCurrency);
                _service.WriteLogs("ServiceID    = " + MidValuationExRateModel.serviceID);
                _service.WriteLogs("ServiceUrl   = " + MidValuationExRateModel.ServiceUrl);
                _service.WriteLogs("ServiceType   = " + MidValuationExRateModel.ServiceType);
                _service.WriteLogs("ApiAuthenUrl   = " + MidValuationExRateModel.ApiAuthenUrl);
                _service.WriteLogs("ApiRateUrl   = " + MidValuationExRateModel.ApiRateUrl);
                _service.WriteLogs("Type         = " + MidValuationExRateModel.type);

                _service.WriteLogs("MAIL_SERVER  = " + MailAdminEnt.Host);
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

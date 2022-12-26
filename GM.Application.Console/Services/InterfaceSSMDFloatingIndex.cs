using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using GM.Application.Console.Model;
using GM.ClientAPI;
using GM.CommonLibs.Common;
using GM.Model.ExternalInterface.FloatingIndexSummit;
using GM.Model.Static;

namespace GM.Application.Console.Services
{
    public class InterfaceSSMDFloatingIndex : WebAPI
    {
        private DateTime systemDate;
        private IConsoleService _service;
        private static Console_Entity ConsoleEnt = new Console_Entity();
        private static Mail_AdminEntity MailAdminEnt = new Mail_AdminEntity();

        public InterfaceSSMDFloatingIndex(IHttpClientFactory httpClientFactory, IConsoleService consoleService) : base(httpClientFactory)
        {
            _service = consoleService;
        }

        public bool Run()
        {
            string StrMsg = string.Empty;
            bool SendEmail = false;
            try
            {
                _service.WriteLogs("### START RUN FUNCTION [" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "] : [" + _service.GetFunction() + "()] ###");

                //Step 1 : Get Config
                InterfaceFloatingIndexSummitModel FloatingIndexModel = new InterfaceFloatingIndexSummitModel();
                string inputDate = _service.GetInputDate();
                systemDate = _service.GetBusinessDateOrSystemDate(inputDate);

                var ResultRpconfig = StaticAPI.RpConfig.GetRpConfig("RP_SSMD_INTERFACE", string.Empty);
                if (!ResultRpconfig.Success)
                {
                    throw new Exception("GetRpConfig() => [" + ResultRpconfig.RefCode.ToString() + "] " + ResultRpconfig.Message);
                }

                //Step 2 : Set Config
                List<RpConfigModel> rpConfigModel = ResultRpconfig.Data;
                if (!SetConfigInterfaceSSMDFloatingIndex(ref StrMsg, ref FloatingIndexModel, rpConfigModel))
                {
                    throw new Exception("Set_ConfigInterfaceSSMDFloatingIndex() => " + StrMsg);
                }

                _service.WriteLogs("Get Config Success");
                if (ConsoleEnt.Enable == "N")
                {
                    _service.WriteLogs(_service.GetFunction() + " Disable");
                    return true;
                }

                //Step 3 : Interface FloatingIndex
                _service.WriteLogs("");
                _service.WriteLogs("Run ImportFloatingIndexSSMD");

                var ResultFloatingIndex = ExternalInterfaceAPI.InterfaceFloatingIndexSummit.ImportFloatingIndexSSMD(FloatingIndexModel);
                if (!ResultFloatingIndex.Success)
                {
                    throw new Exception("ImportFloatingIndexSSMD() => [" + ResultFloatingIndex.RefCode + "] " + ResultFloatingIndex.Message);
                }

                _service.WriteLogs("ReturnCode = [" + ResultFloatingIndex.RefCode + "] " + ResultFloatingIndex.Message);
                _service.WriteLogs("FloatingIndex Item = [" + ResultFloatingIndex.Data[0].FloatingIndex_Item + "] Item.");
                _service.WriteLogs("ImportFloatingIndexSSMD Success.");
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

        private bool SetConfigInterfaceSSMDFloatingIndex(ref string ReturnMsg, ref InterfaceFloatingIndexSummitModel FloatingIndexModel, List<RpConfigModel> List_RpConfigModel)
        {
            try
            {
                ConsoleEnt.Enable = List_RpConfigModel.FirstOrDefault(a => a.item_code == "FLOATING_INDEX_ENABLE")?.item_value;

                FloatingIndexModel.url_ticket = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_URL_TICKET")?.item_value;
                FloatingIndexModel.url_rate = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_URL_RATE")?.item_value;
                FloatingIndexModel.mode = List_RpConfigModel.FirstOrDefault(a => a.item_code == "FLOATING_INDEX_MODE")?.item_value;
                FloatingIndexModel.authorization = List_RpConfigModel.FirstOrDefault(a => a.item_code == "AUTHORIZATION")?.item_value;
                FloatingIndexModel.as_of_date = systemDate.ToString("yyyyMMdd"); //20180103
                FloatingIndexModel.curve_id = List_RpConfigModel.FirstOrDefault(a => a.item_code == "FLOATING_INDEX_CURVE_ID")?.item_value;
                FloatingIndexModel.data_type = List_RpConfigModel.FirstOrDefault(a => a.item_code == "FLOATING_INDEX_DATA_TYPE")?.item_value;
                FloatingIndexModel.ccy = List_RpConfigModel.FirstOrDefault(a => a.item_code == "FLOATING_INDEX_CCY")?.item_value;
                FloatingIndexModel.index = string.Empty;

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
                MailAdminEnt.Subject = MailAdminEnt.Subject.Replace("{0}", "FloatingIndex");
                MailAdminEnt.Subject = MailAdminEnt.Subject.Replace("{1}", "Run at " + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss"));

                _service.WriteLogs("Authorization = " + FloatingIndexModel.authorization);
                _service.WriteLogs("Url Ticket    = " + FloatingIndexModel.url_ticket);
                _service.WriteLogs("Url Rate      = " + FloatingIndexModel.url_rate);
                _service.WriteLogs("Mode          = " + FloatingIndexModel.mode);
                _service.WriteLogs("AsOfDate      = " + FloatingIndexModel.as_of_date);
                _service.WriteLogs("CurveId       = " + FloatingIndexModel.curve_id);
                _service.WriteLogs("DataType      = " + FloatingIndexModel.data_type);
                _service.WriteLogs("Ccy           = " + FloatingIndexModel.ccy);
                _service.WriteLogs("Index         = " + FloatingIndexModel.index);

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

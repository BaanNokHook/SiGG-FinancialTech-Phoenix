using GM.Application.Console.Model;
using GM.ClientAPI;
using GM.CommonLibs.Common;
using GM.Model.ExternalInterface.InterfaceThorRate;
using GM.Model.Static;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;

namespace GM.Application.Console.Services
{
    public class InterfaceSSMDThorRate : WebAPI
    {
        private DateTime systemDate;
        private IConsoleService _service;
        private static Console_Entity ConsoleEnt = new Console_Entity();
        private static Mail_AdminEntity MailAdminEnt = new Mail_AdminEntity();

        public InterfaceSSMDThorRate(IHttpClientFactory httpClientFactory, IConsoleService consoleService) : base(httpClientFactory)
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
                InterfaceReqThorRateModel model = new InterfaceReqThorRateModel();
                string inputDate = _service.GetInputDate();

                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                if (inputDate.Trim() != string.Empty)
                {
                    systemDate = DateTime.ParseExact(inputDate, "yyyyMMdd", CultureInfo.InvariantCulture);
                }
                else
                {
                    systemDate = DateTime.Now.Date;
                }

                //Step 1 : Check Holiday
                var isHoliday = _service.CheckHoliday(systemDate);

                if (isHoliday)
                {
                    _service.WriteLogs(_service.GetFunction() + " Is Holiday");
                    return true;
                }

                var ResultRpconfig = StaticAPI.RpConfig.GetRpConfig("RP_SSMD_INTERFACE_THOR", string.Empty);
                if (!ResultRpconfig.Success)
                {
                    throw new Exception("GetRpConfig() => [" + ResultRpconfig.RefCode.ToString() + "] " + ResultRpconfig.Message);
                }

                //Step 2 : Set Config
                List<RpConfigModel> rpConfigModel = ResultRpconfig.Data;
                if (!Set_ConfigInterfaceThorRate(ref StrMsg, ref model, rpConfigModel))
                {
                    throw new Exception("Set_ConfigInterfaceThorRate() => " + StrMsg);
                }

                _service.WriteLogs("Get Config Success");
                if (ConsoleEnt.Enable == "N")
                {
                    _service.WriteLogs(_service.GetFunction() + " Disable");
                    return true;
                }

                //Step 3 : Interface ThorRate
                _service.WriteLogs("");
                _service.WriteLogs("Run ImportThorRateSSMD");

                var res = ExternalInterfaceAPI.InterfaceThorRate.ImportThorRateSSMD(model);
                if (!res.Success)
                {
                    throw new Exception("ImportThorRateSSMD() => [" + res.RefCode + "] " + res.Message);
                }

                _service.WriteLogs("ReturnCode = [" + res.RefCode + "] " + res.Message);
                _service.WriteLogs("ImportThorRateSSMD Success.");
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

        private bool Set_ConfigInterfaceThorRate(ref string ReturnMsg, ref InterfaceReqThorRateModel model, List<RpConfigModel> List_RpConfigModel)
        {
            try
            {
                ConsoleEnt.Enable = List_RpConfigModel.FirstOrDefault(a => a.item_code == "ENABLE")?.item_value;

                model.url_ticket = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_URL_TICKET")?.item_value;
                model.url_rate = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_URL_RATE")?.item_value;
                model.mode = List_RpConfigModel.FirstOrDefault(a => a.item_code == "MODE")?.item_value;
                model.authorization = List_RpConfigModel.FirstOrDefault(a => a.item_code == "AUTHORIZATION")?.item_value;
                if (List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_TIMEOUT") != null)
                {
                    model.time_out = System.Convert.ToInt32(List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_TIMEOUT").item_value);
                }
                model.reqBody.as_of_date = systemDate.ToString("yyyyMMdd"); //20180103
                model.reqBody.curve_id = List_RpConfigModel.FirstOrDefault(a => a.item_code == "CURVE_ID")?.item_value;
                model.reqBody.data_type = List_RpConfigModel.FirstOrDefault(a => a.item_code == "DATA_TYPE")?.item_value;
                model.reqBody.ccy = List_RpConfigModel.FirstOrDefault(a => a.item_code == "CCY")?.item_value;
                model.reqBody.index = List_RpConfigModel.FirstOrDefault(a => a.item_code == "INDEX")?.item_value;

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
                //MailAdminEnt.Subject = MailAdminEnt.Subject.Replace("{0}", "ThorRate");
                MailAdminEnt.Subject = MailAdminEnt.Subject.Replace("{1}", "Run at " + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss"));

                _service.WriteLogs("ENABLE        = " + ConsoleEnt.Enable);
                _service.WriteLogs("Authorization = " + model.authorization);
                _service.WriteLogs("Url Ticket    = " + model.url_ticket);
                _service.WriteLogs("Url Rate      = " + model.url_rate);
                _service.WriteLogs("Mode          = " + model.mode);
                _service.WriteLogs("AsOfDate      = " + model.reqBody.as_of_date);
                _service.WriteLogs("CurveId       = " + model.reqBody.curve_id);
                _service.WriteLogs("DataType      = " + model.reqBody.data_type);
                _service.WriteLogs("Ccy           = " + model.reqBody.ccy);
                _service.WriteLogs("Index         = " + model.reqBody.index);

                _service.WriteLogs("MAIL_SERVER  = " + MailAdminEnt.Host);
                _service.WriteLogs("MAIL_PORT    = " + MailAdminEnt.Port);
                _service.WriteLogs("MAIL_SENDER  = " + MailAdminEnt.From);
                _service.WriteLogs("MAIL_ADMIN   = " + Mail_To_Tmp);
                _service.WriteLogs("MAIL_SUBJECT = " + MailAdminEnt.Subject);
            }
            catch (Exception Ex)
            {
                ReturnMsg = Ex.Message;
                return false;
            }

            return true;
        }


    }
}

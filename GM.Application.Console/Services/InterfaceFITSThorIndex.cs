using GM.Application.Console.Model;
using GM.ClientAPI;
using GM.CommonLibs.Common;
using GM.Model.ExternalInterface.InterfaceThorIndex;
using GM.Model.Static;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;

namespace GM.Application.Console.Services
{
    public class InterfaceFITSThorIndex : WebAPI
    {
        private DateTime systemDate;
        private IConsoleService _service;
        private static Console_Entity ConsoleEnt = new Console_Entity();
        private static Mail_AdminEntity MailAdminEnt = new Mail_AdminEntity();

        public InterfaceFITSThorIndex(IHttpClientFactory httpClientFactory, IConsoleService consoleService) : base(httpClientFactory)
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

                // Step 1 : Get SystemDate
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

                // Step 2 : Get Config
                InterfaceReqThorIndexFitsModel model = new InterfaceReqThorIndexFitsModel();

                var ResultRpconfig = StaticAPI.RpConfig.GetRpConfig("RP_FITS_INTERFACE_THOR_INDEX", string.Empty);
                if (!ResultRpconfig.Success)
                {
                    throw new Exception("GetRpConfig() => [" + ResultRpconfig.RefCode.ToString() + "] " + ResultRpconfig.Message);
                }

                // Step 3 : Set Config
                List<RpConfigModel> rpConfigModel = ResultRpconfig.Data;
                if (!SetConfigInterfaceFITSThorIndex(ref StrMsg, ref model, rpConfigModel))
                {
                    throw new Exception("SetConfigInterfaceFITSThorIndex() => " + StrMsg);
                }

                _service.WriteLogs("Get Config Success");
                if (ConsoleEnt.Enable == "N")
                {
                    _service.WriteLogs(_service.GetFunction() + " Disable");
                    return true;
                }

                // Step 4 : Interface FITS BondPledge
                _service.WriteLogs("");
                _service.WriteLogs("Run ImportThorIndexFits");
                var res = ExternalInterfaceAPI.InterfaceThorIndex.ImportThorIndexFits(model);
                if (!res.Success)
                {
                    throw new Exception("ImportThorIndexFits() => [" + res.RefCode.ToString() + "] " + res.Message);
                }

                _service.WriteLogs("ReturnCode = [" + res.RefCode + "] " + res.Message);
                _service.WriteLogs("InterfaceFITSThorIndex Success.");
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

        private bool SetConfigInterfaceFITSThorIndex(ref string StrMsg, ref InterfaceReqThorIndexFitsModel model, List<RpConfigModel> lstRpConfigModel)
        {
            try
            {
                model.RPConfigModel = lstRpConfigModel;
                model.AsOfDate = systemDate;
                model.create_by = "Console";

                ConsoleEnt.Enable = lstRpConfigModel.FirstOrDefault(a => a.item_code == "ENABLE")?.item_value;

                model.ServiceUrl = lstRpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_URL")?.item_value;
                if (!String.IsNullOrEmpty(lstRpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_TIMEOUT")?.item_value))
                {
                    model.ServiceTimeOut = Convert.ToInt32(lstRpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_TIMEOUT")?.item_value);
                }

                model.AsOfDate = systemDate.Date;
                model.create_by = "Console";

                MailAdminEnt.Host = lstRpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_SERVER")?.item_value;
                if (!String.IsNullOrEmpty(lstRpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_PORT")?.item_value))
                {
                    MailAdminEnt.Port = Convert.ToInt32(lstRpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_PORT")?.item_value);
                }
                MailAdminEnt.From = lstRpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_SENDER")?.item_value;
                string Mail_To_Tmp = lstRpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_ADMIN")?.item_value;
                if (!String.IsNullOrEmpty(Mail_To_Tmp))
                {
                    string[] mail_to_array = Mail_To_Tmp.Split(',');
                    foreach (string mail_to in mail_to_array)
                    {
                        MailAdminEnt.To.Add(mail_to);
                    }
                }
                MailAdminEnt.Subject = lstRpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_SUBJECT")?.item_value;
                MailAdminEnt.Subject = MailAdminEnt.Subject?.Replace("{1}", "Run at " + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss"));

                _service.WriteLogs("ServiceUrl       = " + model.ServiceUrl);
                _service.WriteLogs("ServiceTimeOut   = " + model.ServiceTimeOut);
                _service.WriteLogs("ChannelId        = " + lstRpConfigModel.FirstOrDefault(a => a.item_code == "CHANNEL")?.item_value);
                _service.WriteLogs("Mode             = " + lstRpConfigModel.FirstOrDefault(a => a.item_code == "MODE")?.item_value);
                _service.WriteLogs("AsOfDate         = " + model.AsOfDate.ToString("yyyyMMdd"));

                _service.WriteLogs("MAIL_SERVER  = " + MailAdminEnt.Host);
                _service.WriteLogs("MAIL_PORT    = " + MailAdminEnt.Port);
                _service.WriteLogs("MAIL_SENDER  = " + MailAdminEnt.From);
                _service.WriteLogs("MAIL_ADMIN   = " + Mail_To_Tmp);
                _service.WriteLogs("MAIL_SUBJECT = " + MailAdminEnt.Subject);
            }
            catch (Exception ex)
            {
                StrMsg = ex.Message;
                return false;
            }

            return true;
        }
    }
}

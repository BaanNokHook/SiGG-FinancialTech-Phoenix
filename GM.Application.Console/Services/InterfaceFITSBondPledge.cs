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
    public class InterfaceFITSBondPledge : WebAPI
    {
        private DateTime systemDate;
        private IConsoleService _service;
        private static Console_Entity ConsoleEnt = new Console_Entity();
        private static Mail_AdminEntity MailAdminEnt = new Mail_AdminEntity();

        public InterfaceFITSBondPledge(IHttpClientFactory httpClientFactory, IConsoleService consoleService) : base(httpClientFactory)
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
                systemDate = _service.GetBusinessDateOrSystemDate(inputDate);

                //Check Holiday
                var isHoliday = _service.CheckHoliday(systemDate);
                if (isHoliday)
                {
                    _service.WriteLogs(_service.GetFunction() + " Is Holiday");
                    return true;
                }
                
                // Step 2 : Get Config
                InterfaceBondPledgeFitsModel BondPledgeFitsModel = new InterfaceBondPledgeFitsModel();
                
                var ResultRpconfig = StaticAPI.RpConfig.GetRpConfig("RP_FITS_INTERFACE_BONDPLEDGE", string.Empty);
                if (!ResultRpconfig.Success)
                {
                    throw new Exception("GetRpConfig() => [" + ResultRpconfig.RefCode.ToString() + "] " + ResultRpconfig.Message);
                }

                // Step 3 : Set Config
                List<RpConfigModel> rpConfigModel = ResultRpconfig.Data;
                if (!SetConfigInterfaceFITSBondPledge(ref StrMsg, ref BondPledgeFitsModel, rpConfigModel))
                {
                    throw new Exception("Set_ConfigInterfaceFITSBondPledge() => " + StrMsg);
                }

                _service.WriteLogs("Get Config Success");
                if (ConsoleEnt.Enable == "N")
                {
                    _service.WriteLogs(_service.GetFunction() + " Disable");
                    return true;
                }

                // Step 4 : Interface FITS BondPledge
                _service.WriteLogs("");
                _service.WriteLogs("Run ExportBondPledgeFits");
                var ResultBondPledgeFits = ExternalInterfaceAPI.InterfaceBondPledgeFits.ExportBondPledgeFits(BondPledgeFitsModel);
                if (!ResultBondPledgeFits.Success)
                {
                    throw new Exception("ExportBondPledgeFits() => [" + ResultBondPledgeFits.RefCode.ToString() + "] " + ResultBondPledgeFits.Message);
                }

                _service.WriteLogs("ReturnCode = [" + ResultBondPledgeFits.RefCode + "] " + ResultBondPledgeFits.Message);
                _service.WriteLogs("InterfaceFITSBondPledge Success.");
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

        private bool SetConfigInterfaceFITSBondPledge(ref string StrMsg, ref InterfaceBondPledgeFitsModel BondPledgeFitsModel, List<RpConfigModel> List_RpConfigModel)
        {
            try
            {
                BondPledgeFitsModel.RPConfigModel = List_RpConfigModel;
                ConsoleEnt.Enable = List_RpConfigModel.FirstOrDefault(a => a.item_code == "ENABLE")?.item_value;

                BondPledgeFitsModel.ServiceUrl = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_URL")?.item_value;
                if (!String.IsNullOrEmpty(List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_TIMEOUT")?.item_value))
                {
                    BondPledgeFitsModel.ServiceTimeOut = Convert.ToInt32(List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_TIMEOUT")?.item_value);
                }

                BondPledgeFitsModel.AsOfDate = systemDate.Date;
                BondPledgeFitsModel.create_by = "Console";

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
                MailAdminEnt.Subject = MailAdminEnt.Subject?.Replace("{1}", "Run at " + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss"));

                _service.WriteLogs("ServiceUrl       = " + BondPledgeFitsModel.ServiceUrl);
                _service.WriteLogs("ServiceTimeOut   = " + BondPledgeFitsModel.ServiceTimeOut);
                _service.WriteLogs("ChannelId        = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "CHANNEL")?.item_value);
                _service.WriteLogs("Mode             = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "MODE")?.item_value);
                _service.WriteLogs("AsOfDate         = " + BondPledgeFitsModel.AsOfDate.ToString("yyyyMMdd"));

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

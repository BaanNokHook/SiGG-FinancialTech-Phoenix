using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using GM.Application.Console.Model;
using GM.ClientAPI;
using GM.CommonLibs.Common;
using GM.Model.ExternalInterface.ExchRateSummit;
using GM.Model.Static;

namespace GM.Application.Console.Services
{
    public class InterfaceSSMDExchangeRate : WebAPI
    {
        private DateTime systemDate;
        private IConsoleService _service;
        private static Console_Entity ConsoleEnt = new Console_Entity();
        private static Mail_AdminEntity MailAdminEnt = new Mail_AdminEntity();

        public InterfaceSSMDExchangeRate(IHttpClientFactory httpClientFactory, IConsoleService consoleService) : base(httpClientFactory)
        {
            _service = consoleService;
        }

        public bool Run()
        {
            string StrMsg = string.Empty;
            bool SendEmail = false;
            InterfaceReqExchRateHeaderSummitModel reqExchRateHeaderSummitModel = new InterfaceReqExchRateHeaderSummitModel();
            try
            {
                _service.WriteLogs("### START RUN FUNCTION [" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "] : [" + _service.GetFunction() + "()] ###");
                //Step 1 : Get Config

                string inputDate = _service.GetInputDate();
                systemDate = _service.GetBusinessDateOrSystemDate(inputDate);

                var resultRpconfig = StaticAPI.RpConfig.GetRpConfig("RP_SSMD_INTERFACE", string.Empty);
                if (resultRpconfig.RefCode != 0)
                {
                    throw new Exception("Get_RP_SSMD_INTERFACE_Config() => [" + resultRpconfig.RefCode.ToString() + "]" + resultRpconfig.Message);
                }

                List<RpConfigModel> rpConfigModelList = resultRpconfig.Data;
                if (!SetConfigInterfaceExchangeRateSummit(ref StrMsg, rpConfigModelList, ref reqExchRateHeaderSummitModel))
                {
                    throw new Exception("Set_ConfigInterfaceExchangeRateSummit() => " + StrMsg);
                }

                if (ConsoleEnt.Enable == "N")
                {
                    _service.WriteLogs(_service.GetFunction() + " Disable");
                    return true;
                }

                //select RP Confirmation

                _service.WriteLogs("");
                _service.WriteLogs("Run ImportExchangeRateSSMD");
                var rwm = ExternalInterfaceAPI.InterfaceExchangeRateSummit.ImportExchangeRateSSMD(reqExchRateHeaderSummitModel);
                if (!rwm.Success)
                {
                    throw new Exception("ImportExchangeRateSSMD() => [" + rwm.RefCode.ToString() + "] " + rwm.Message);
                }

                _service.WriteLogs("ReturnCode = [" + rwm.RefCode + "] " + rwm.Message);
                _service.WriteLogs("ImportExchangeRateSSMD Success.");
            }
            catch (Exception Ex)
            {
                _service.WriteLogs(Ex.Message);
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

        private bool SetConfigInterfaceExchangeRateSummit(ref string ReturnMsg, List<RpConfigModel> List_RpConfigModel, ref InterfaceReqExchRateHeaderSummitModel reqExchRateHeaderSummitModel)
        {
            try
            {
                //Set Req Data InterfaceMarketPriceModel form config
                reqExchRateHeaderSummitModel.authorization = List_RpConfigModel.FirstOrDefault(a => a.item_code == "AUTHORIZATION")?.item_value;
                reqExchRateHeaderSummitModel.url_ticket = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_URL_TICKET")?.item_value;
                reqExchRateHeaderSummitModel.url_rate = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_URL_RATE")?.item_value;
                reqExchRateHeaderSummitModel.reqbody = new InterfaceReqExchRateBodySummitModel();
                reqExchRateHeaderSummitModel.reqbody.as_of_date = systemDate.ToString("yyyyMMdd"); //20170720
                reqExchRateHeaderSummitModel.reqbody.curve_id = List_RpConfigModel.FirstOrDefault(a => a.item_code == "EXCHANGE_RATE_CURVE_ID")?.item_value;
                reqExchRateHeaderSummitModel.reqbody.data_type = List_RpConfigModel.FirstOrDefault(a => a.item_code == "EXCHANGE_RATE_DATA_TYPE")?.item_value;
                reqExchRateHeaderSummitModel.reqbody.ccy1 = List_RpConfigModel.FirstOrDefault(a => a.item_code == "EXCHANGE_RATE_CCY_1")?.item_value;
                reqExchRateHeaderSummitModel.reqbody.ccy2 = List_RpConfigModel.FirstOrDefault(a => a.item_code == "EXCHANGE_RATE_CCY_2")?.item_value;
                //End Set
                //Set Mail config
                ConsoleEnt.Enable = List_RpConfigModel.FirstOrDefault(a => a.item_code == "EXCHANGE_RATE_ENABLE")?.item_value;

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
                MailAdminEnt.Subject = MailAdminEnt.Subject.Replace("{0}", "ExchangeRate");
                MailAdminEnt.Subject = MailAdminEnt.Subject.Replace("{1}", "Run at " + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss"));
                //End Set Mail config

                _service.WriteLogs("Authorization = " + reqExchRateHeaderSummitModel.authorization);
                _service.WriteLogs("Url Ticket    = " + reqExchRateHeaderSummitModel.url_ticket);
                _service.WriteLogs("Url Rate      = " + reqExchRateHeaderSummitModel.url_rate);
                _service.WriteLogs("AsOfDate      = " + reqExchRateHeaderSummitModel.reqbody.as_of_date);
                _service.WriteLogs("CurveId       = " + reqExchRateHeaderSummitModel.reqbody.curve_id);
                _service.WriteLogs("DataType      = " + reqExchRateHeaderSummitModel.reqbody.data_type);
                _service.WriteLogs("ccy1          = " + reqExchRateHeaderSummitModel.reqbody.ccy1);
                _service.WriteLogs("ccy2          = " + reqExchRateHeaderSummitModel.reqbody.ccy2);

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

using System;
using System.Collections.Generic;
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
    public class InterfaceMarketPriceTbmaFits : WebAPI
    {
        private DateTime systemDate = DateTime.Now;
        private IConsoleService _service;
        private static Console_Entity ConsoleEnt = new Console_Entity();
        private static Mail_AdminEntity MailAdminEnt = new Mail_AdminEntity();

        public InterfaceMarketPriceTbmaFits(IHttpClientFactory httpClientFactory, IConsoleService consoleService) : base(httpClientFactory)
        {
            _service = consoleService;
        }

        public bool Run(string market_date_t)
        {
            string StrMsg = string.Empty;
            bool SendEmail = false;
            InterfaceMarketPriceTbmaModel interfaceMarketPriceTbma = new InterfaceMarketPriceTbmaModel();
            try
            {
                _service.WriteLogs("### START RUN FUNCTION [" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "] : [" + _service.GetFunction() + "()] ###");

                string inputDate = _service.GetInputDate();

                systemDate = _service.GetBusinessDateOrSystemDate(inputDate);
        
                //Step 1 : Check Holiday
                var isHoliday = _service.CheckHoliday(systemDate);

                if (isHoliday)
                {
                    _service.WriteLogs(_service.GetFunction() + " Is Holiday");
                    return true;
                }

                //Step 2 : Get Config
                var resultRpconfig = StaticAPI.RpConfig.GetRpConfig("RP_FITS_INTERFACE_MARKET_PRICE_TBMA", string.Empty);
                if (resultRpconfig.RefCode != 0)
                {
                    throw new Exception("GetMarketPriceTBMAConfig() => [" + resultRpconfig.RefCode.ToString() + "]" + resultRpconfig.Message);
                }

                List<RpConfigModel> rpConfigModelList = resultRpconfig.Data;
                if (!SetConfigInterfaceMarketPriceTbma(ref StrMsg, rpConfigModelList, ref interfaceMarketPriceTbma, market_date_t))
                {
                    throw new Exception("SetConfigInterfaceMarketPriceTbma() => " + StrMsg);
                }

                if (ConsoleEnt.Enable == "N")
                {
                    _service.WriteLogs(_service.GetFunction() + " Disable");
                    return true;
                }

                //select RP Confirmation

                //Add Paging
                PagingModel paging = new PagingModel();
                paging.PageNumber = ConsoleEnt.PageNumber;
                paging.RecordPerPage = ConsoleEnt.RecordPerPage;
                interfaceMarketPriceTbma.paging = paging;

                //Add Orderby
                var orders = new List<OrderByModel>();
                interfaceMarketPriceTbma.ordersby = orders;
                interfaceMarketPriceTbma.create_by = "console";
                _service.WriteLogs("");
                _service.WriteLogs("Run ImportMarketPriceTbma");
                _service.WriteLogs(" - create_by : " + interfaceMarketPriceTbma.create_by);

                var rwm = ExternalInterfaceAPI.InterfaceMarketPriceTbmaFitsAPI.ImportMarketPriceTbmaFits(interfaceMarketPriceTbma);
                if (!rwm.Success)
                {
                    throw new Exception("ImportMarketPriceTbmaFits() => [" + rwm.RefCode.ToString() + "] " + rwm.Message);
                }

                _service.WriteLogs("ReturnCode = [" + rwm.RefCode + "] " + rwm.Message);
                _service.WriteLogs("ImportMarketPriceTbma Success.");

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

        private bool SetConfigInterfaceMarketPriceTbma(ref string ReturnMsg, List<RpConfigModel> List_RpConfigModel, ref InterfaceMarketPriceTbmaModel interfaceMarketPriceTbma, string market_date_t)
        {
            try
            {
                //Set Req Data InterfaceMarketPriceTbmaModel form config
                DateTime date = DateTime.Now;
                interfaceMarketPriceTbma.channel = List_RpConfigModel.FirstOrDefault(a => a.item_code == "CHANNEL")?.item_value;
                interfaceMarketPriceTbma.ref_no = date.ToString("yyyyMMddHHMMss");
                interfaceMarketPriceTbma.request_date = date.ToString("yyyyMMdd");
                interfaceMarketPriceTbma.request_time = date.ToString("HH:MM:ss");
                interfaceMarketPriceTbma.mode = int.Parse(List_RpConfigModel.FirstOrDefault(a => a.item_code == "MODE")?.item_value);
                interfaceMarketPriceTbma.asof_date = systemDate.ToString("yyyyMMdd"); //20170720
                if (market_date_t == "T1")
                {
                    interfaceMarketPriceTbma.market_date_t = "1";
                    interfaceMarketPriceTbma.source_type = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SOURCE_TYPE_T1")?.item_value;
                }
                else
                {
                    interfaceMarketPriceTbma.market_date_t = "2";
                    interfaceMarketPriceTbma.source_type = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SOURCE_TYPE_T2")?.item_value;
                }
                interfaceMarketPriceTbma.security_code = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SECURITY_CODE")?.item_value;
                interfaceMarketPriceTbma.urlservice = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_URL")?.item_value;

                //End Set
                //Set Mail config
                ConsoleEnt.Enable = List_RpConfigModel.FirstOrDefault(a => a.item_code == "ENABLE")?.item_value;

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
                //End Set Mail config

                _service.WriteLogs("channel       = " + interfaceMarketPriceTbma.channel);
                _service.WriteLogs("ref_no        = " + interfaceMarketPriceTbma.ref_no);
                _service.WriteLogs("request_date  = " + interfaceMarketPriceTbma.request_date);
                _service.WriteLogs("request_time  = " + interfaceMarketPriceTbma.request_time);
                _service.WriteLogs("mode          = " + interfaceMarketPriceTbma.mode);
                _service.WriteLogs("asof_date     = " + interfaceMarketPriceTbma.asof_date);
                _service.WriteLogs("source_type   = " + interfaceMarketPriceTbma.source_type);
                _service.WriteLogs("market_date_t = " + interfaceMarketPriceTbma.market_date_t);
                _service.WriteLogs("security_code = " + interfaceMarketPriceTbma.security_code);
                _service.WriteLogs("urlservice    = " + interfaceMarketPriceTbma.urlservice);

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

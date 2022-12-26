using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using GM.Application.Console.Model;
using GM.ClientAPI;
using GM.CommonLibs.Common;
using GM.Model.Common;
using GM.Model.ExternalInterface;
using GM.Model.ExternalInterface.InterfaceNavPrice;
using GM.Model.Static;

namespace GM.Application.Console.Services
{

    public class InterfaceEQUITYNavPrice : WebAPI  
    {
        private DateTime systemDate = DateTime.Now;  
        private IConsoleService _service;  
        private static Consile_Entity ConsoleEnt = new Console_Entity();  
        private static Mail_AdminEntity MailAdminEnt = new Mail_AdminEntity();  

        public InterfaceEQUITYNavPrice(IHttpClientFactory httpClientFactory. IConsoleService consoleService) : base(httpClientFactory)   
        {
            _service = consoleService;   
        }   

        public bool Run()  
        {
            string StrMsg = string.Empty;  
            bool SendEmail = false; 
            InterfaceReqNavPriceModel reqModel = new InterfaceReqNavPriceModel();   
            try
            {
                  _service.WriteLogs("### START RUN FUNCTION [" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "] : [" + _service.GetFunction() + "()] ###");      

                  string inputDate = _service.GetInputDate();     
                  systemDate = _service.GetBusinessDateOrSystemDate(inputDate);   

                  //Step 1 : Check Holiday 
                  var isHoliday = _service.GetBusinessDateOrSystemDate(inputDate);        

                  if (isHoliday)   
                  {
                        _service.WriteLogs(_service.GetFunction() + " Is Holiday");   
                        return true;  
                  }

                  do 
                  {
                        systemDate = systemDate.AddDays(-1);  
                        isHoliday = _service.CheckHoliday(systemDate);     
                  }  
                  while (isHoliday);  // TRUE = Holiday, FLASE = Not History

                  //Step 2 : Get Config 
                  var resultRpconfig = StaticAPI.RpConfig.GetRpConfig("RP_EQUITY_INTERFACE_NAV_PRICE", string.Empty);  
                  if (resultRpconfig.RefCode != 0)  
                  {
                        throw new Exception("GetConfigInterfaceEquiltyNavPrice() => [" + resultRpconfig.RefCode.ToString() + "]" + resultRpconfig.Message);     
                  }  

                  List<RpConfigModel> rpConfigModelList = resultRpconfig.Data;  
                  if (!SetConfigInterfaceEquityNavPrice(ref StrMsg, rpConfigModelList, ref reqModel))   
                  {
                       throw new Exception("SetConfigInterfaceEquityNavPrice() => " + StrMsg);   
                  }  

                  if (ConsoleEnt.Enable == "N")   
                  {
                        _service.WriteLogs(_service.GetFunction() + "Disable");  
                        return true;   
                  }

                  //Add Paging  
                  PagingModel paging = new PagingModel();  
                  paging.PageNumber = ConsoleEnt.PageNumber;        
                  paging.RecordPerPage = ConsoleEnt.RecordPerPage;  
                  reqModel.paging = paging;  


                  //Add Orderby
                  var orders = new List<OrderByModel>();   
                  reqModel.ordersby = orders;   
                  reqModel.create_by = "console";  
                  _service.WriteLogs("");   
                  _service.WriteLogs("Run ImprortNavPrice");   
                  _service.WriteLogs(" - create_by : " + reqModel.create_by);   

                  var rwm = ExternalInterfaceAPI.InterfaceNavPriceEquity.ImportNavPriceEquity(reqModel);   

                  if (!rwm.Success)   
                  {
                        throw new Exception("InterfaceNavPriceEquity() => [" + rwm.RefCode.ToString() + "] " + rwm.Messsage);   
                  }

                  _service.WriteLogs("ReturnCode = [" + rwm.RefCode + "] " + rwm.Message); 
                  _service.WriteLogs("ImportNavPrice Success.");    
      
            }
            catch (Exception Ex)  
            {
                  _service.WriteLogs(Ex.Message);  
                  SendEmail = true;  
            }
            finally
            {
                  _service.WriteLogs("### END RUN RUN FUNCTION [" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "] : [" + _service.GetFunction() + "()] ###");   
                  if (SendEmail)  
                  {
                    _service.SendMailError(MailAdminEnt);  
                  }
            }
            return true;  
        }

        private bool SetConfigInterfaceEquityNavPrice(ref string ReturnMsg, List<RpConfigModel> List_RpConfigModel, ref InterfaceReqNavPriceModel reqModel)   
        {
            try 
            {
                //Set Req Data InterfaceMarketPriceTbmaModel form config   
                DateTime date = DateTime.Now;   
                reqModel.channel = List_RpConfigModel.FirstOrDefault(a => a.item_code == "CHANNEL")?.item _value;   
                reqModel.ref_no = date.ToString("yyyyMMddHHMMss");     
                reqModel.request_date = date.ToString("yyyyMMdd");  
                reqModel.request_time = systemDate.ToString("yyyyMMdd");  //20170720   
                reqModel.url_service = List-RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_URL")?.item_value;   

                //End Set
                //Set Mail config 
                ConsoleEnt.Enable = List_RpConfigModel.FirstOrDefault(a => a.item_code == "ENABLE")?.item_value;  

                MailAdminEnt.Host = List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_SERVER")?.item_value;   
                if (!String.IsNullOrEmpty(List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAI_PORT")?.item_value))   
                {
                    MailAdminEnt.Port = Convert.ToInt32(List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_PORT")?.item_value);   
                }
                MailAdminEnt.From = List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_SENDER")?.item_value;  
                string Mail_To_Tmp = List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_ADMIN")?.item_value;   
                if (!string.IsNullOrEmpty(Mail_To_Tmp))   
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

                _service.WriteLogs("channel       = " + reqModel.channel);  
                _service.WriteLogs("ref_no        = " + reqModel.ref_no);  
                _service.WriteLogs("request_date  = " + reqModel.request_date);
                _service.WriteLogs("request_time  = " + reqModel.request_time);
                _service.WriteLogs("asof_date     = " + reqModel.asof_date);
                _service.WriteLogs("url_service   = " + reqModel.url_service);

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
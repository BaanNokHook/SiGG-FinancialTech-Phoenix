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

    public class InterfaceCCM : WebAPI  
    {
      private DateTime DateTime systemDate;  
      private IConsoleService _service;  
      private static Console_Entity ConsoleEnt = new Console_Entity();  
      private static Mail_AdminEntity MailAdminEnt = new Mail_AdminEntity();    

      public InterfaceCCM(IHttpClientFactory httpClientFactory, IConsoleService consoleService) : base(httpClientFactory)  
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
               string inputDate = _service.GetInputDate();  
               systemDate = _service.GetBusinessDateOrSystemDate(inputDate);   

               //Check Holiday  
               var isHoliday = _service.CheckHoliday(systemDate);   
               if (isHoliday)  
               {
                  _service.WriteLogs(_service.GetFunction() + "Is Holiday");  
                  return true;  
               }  

               var resultRpconfig = StaticAPI.RpConfig.GetRpConfig("RP_CCM_INTERFACE", string.Empty);   
               if (resultRpconfig.RefCode != 0)  
               {
                  throw new Exception("GetRpConfig() => [" + resultRpconfig.RefCode + "]" + resultRpconfig.Message);  
               }

               List<RpConfigModel> rpConfigModelList = resultRpconfig.Data;  
               if (!SetConfigInterfaceCCM(ref StrMsg, rpConfigModelList))  
               {
                  throw new Exception("Set_ConfigInterfaceCCM() => " + StrMsg);   
               }  

               if (ConsoleEnt.Enable == "N")  
               {
                  _service.WriteLogs(_service.GetFunction() + " Disable");   
                  return true;   
               }

               //select RP Confirmation  
               DateTime now = DateTime.Now;   
               InterfaceCCMSearch ccmSearch = new InterfaceCCMSearch();  
               ccmSearch.search_date = systemDate;  

               //Add Paging  
               PagingModel paging = new PagingModel();  
               paging.PageNumber = ConsoleEnt.PageNumber;   
               paging.RecordPerPage = ConsoleEnt.RecordPerPage;  
               ccmSearch.paging = paging;   

               //Add Orderby  
               var orders = new List<OrderByModel>();  
               ccmSearch.ordersby = orders;  

               var rwmCCM = ExternalInterfaceAPI.InterfaceConfirmation.GetInterfaceCCMList(ccmSearch);   
               if (rwmCCM.Success)  
               {
                  string guid = Guid.NewGuid().ToString();  
                  List<ResultCCM> resultCcms = new List<ResultCCM>();  
                  foreach (var row in rwmCCM.Data)  
                  {
                        InterfaceConfirmationModel model = row;      
                        model.TransDate = systemDate.ToString("yyyyMMdd");   
                        model.TransTime = now.ToString("HH:mm:ss");  
                        model.guid = guid;     
                        model.create_by = ConsoleEnt.CreateBy;   
                        model.RpConfigModel = rpConfigModelList;   

                        var res = ExternalInterfaceAPI.InterfaceConfirmation.SendConfirmation(model);   
                        resultCcms.Add(new ResultCCM
                        {
                              Success = res.Success,  
                              Message = res.Message
                        });  

                        _service.WriteLogs("ReturnCode = [" + res.RefCode + "] " + res.Message);                    
                  
                  }

                  if (resultCcms.Count(a => !a.Success) > 0)   
                  {
                        SendEmail = true;   

                  }

                  _service.WriteLogs("SendConfirmation Success.");  
               
            }
      }
      catch (Exception Ex)    
      {
            _service.WriteLogs(Ex.Message);  
            SendEmail = true;  
      }  
      finally 
      {
            _service.WriteLogs("### END END RUN FUNCTION [" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "] : [" + _service.GetFunction() + "()] ###");
             if (SendEmail)
                {
                    _service.SendMailError(MailAdminEnt);
                }
            }
            return true;
        }

        private bool SetConfigInterfaceCCM(ref string ReturnMsg, List<RpConfigModel> List_RpConfigModel)
        {
            try
            {
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

        private class ResultCCM
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }
    }
}
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

    public class ExportAmendCancelDailyToEMail : WebAPI  
    {
      private DateTime systemDate;   
      private readonly IConsoleService _service;  
      private static Console_Entity ConsoleEnt = new Console_Entity();  
      private static Mail_AdminEntity MailAdminEnt = new Mail_AdminEntity();   

      public ExportAmendCancelDailyToEMail(IHttpClientFactory httpClientFactory, IConsoleService consoleService) : base(httpClientFactory)    
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

                  ExportAmendCancelDailyMailModel AmendCancelDailyMailModel = new ExportAmendCancelDailyMailModel();  

                  string inputDate = _service.GetInputDate();  
                  systemDate = _service.GetBusinessDateOrSystemDate(inputDate);  

                  //Step 1 : Check Holiday  
                  var isHoliday = _service.CheckHoliday(systemDate);  

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
                  while (isHoliday);  // TRUE = Holiday, FALSE = Not Holiday

                  var ResultRpconfig = StaticAPI.RpConfig.GetRpConfig("RP_AMEND_CANCEL_DAILY_MAIL", string.Empty);   
                  if (!ResultRpconfig.Success)  
                  {
                        throw new Exception("GetRpConfig() => [" + ResultRpconfig.RefCode.ToString() + "] " + ResultRpconfig.Message);  
                  }  

                  //Step 2 : Set Config  
                  List<RpConfigModel> rpConfigModel = ResultRpConfig.Data;  
                  AmendCancelDailyMailModel.RpConfigModel = rpConfigModel;  
                  AmendCancelDailyMailModel.create_by = ConsoleEnt.CreateBy;  
                  if (!SetConfigAmendCancelDailyToEmail(ref StrMsg, ref AmendCancelDailyMailModel, rpConfigModel))     
                  {
                        throw new Exception("Set_ConfigAmendCancelDailyToEmail() => " + StrMsg);    
                  }

                  _service.WriteLogs("Get Config Success");  
                  if (ConsoleEnt.Enable == "N")  
                  {
                        _service.WriteLogs(_service.GetFunction() + "Disable");   
                        return true;   
                  }    


                  //Step 3 : Export AmendCancel Daily To Email   
                  _service.WriteLogs("");   
                  _service.WriteLogs("Run ExportAmendCancelDailyMail");   

                  var ResultAmendCancelDailyMail = ExternalInterfaceAPI.ExportAmendCancel.ExportAmendCancelDailyMail(AmendCancelDailyMailModel);   
                  if (!ResultAmendCancelDailyMail.Success)   
                  {
                        throw new Exception("ExportAmendCancelDailyMail() => [" + ResultAmendCancelDailyMail.RefCode.ToSting() + "] " + ResultAmendCancelDailyMail.Message);     
                  }   

                  _service.WriteLogs("ExportAmendCancelDailyMail.Success.");  
            }  
            catch (Exception Ex)   
            {
                  _service.WriteLogs("Error " + Ex.Message);  
                  SendEmail = true;  
            }
            finally
            {
                  _service.WriteLogs("### END RUN FUNCTION [" + DateTime.Now.Tostring("dd/MM/yyyy HH:mm:ss") + "] : [" + _service.GetFunction() + "()] ####");     
                  if (SendEmail)   
                  {
                        _service.SendMailError(MailAdminEnt);   
                  }  
            }
            return true;  

      }
      private bool SetConfigAmendCancelDailyToEmail(ref string ReturnMsg, ref ExportAmendCancelDailyMailModel AmendCancelDailyMailModel, List<RpConfigModel> List_RpConfigModel)  
      {
            try
            {

                _service.WriteLogs("");  
                consoleEnt.Enable = List_RpConfigModel.FirstOrDefault(a => a.item_code == "ENABLE")?.item_value;   
                _service.WriteLogs("ENABLE = " + ConsoleEnt.Enable);   

                AmendCancelDailyModel.AsofDate = systemDate.Date;  
                _service.WriteLogs("AsofDate = " + AmendCancelDailyModel.AsofDate.ToString("yyyyMMdd"));  
                _service.WriteLogs("File Name =  " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "FILE_NAME")?.item_value);  

                  
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
                _service.WriteLogs("MAIL_TO      = " + AmendCancelDailyModel.RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_TO")?.item_value);
                _service.WriteLogs("MAIL_CC      = " + AmendCancelDailyModel.RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_CC")?.item_value);
                _service.WriteLogs("MAIL_SUBJECT = " + MailAdminEnt.Subject);
                _service.WriteLogs("MAIL_BODY    = " + AmendCancelDailyModel.RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_BODY")?.item_value);
                _service.WriteLogs("MAIL_ADMIN   = " + Mail_To_Tmp);
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
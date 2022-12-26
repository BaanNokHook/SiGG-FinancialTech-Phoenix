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
    
    public class ExportAmendCancelMonthlyToEmail : WebAPI   
    {

        private DateTime systemDate;  
        private IConsoleService _service;  
        private static Console_Entity ConsoleEnt = new Console_Entity();  
        private static Mail_AdminEntity MailAdminEnt = new Mail_AdminEntity();   

        public ExportAmendCancelMonthlyToEMail(IHttpClientFactory httpClientFactory, IConsoleService consoleService) : base(httpClientFactory)   
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
                //bool IsHoliday = false;   

                ExportAmendCancelMonthlyMail AmendCancelMonthlyMailModel = new ExportAmendCancelMonthlyMailModel();    

                string inputDate = _service.GetInputDate();   
                systemDate = _service.GetBusinessDateOrSystemDate(inputDate);      


                //Step 2 : Check Holiday
                //if (Check_Holiday(ref StrMsg, ref IsHoliday, SystemDate) == false)
                //{
                //    throw new Exception("Check_Holiday()" + StrMsg);
                //}

                //if (IsHoliday == true)
                //{
                //    WriteLogs(Function + " Is Holiday");
                //    return true;
                //}

                //do
                //{
                //    SystemDate = SystemDate.AddDays(-1);

                //    if (Check_Holiday(ref StrMsg, ref IsHoliday, SystemDate) == false)
                //    {
                //        throw new Exception("Check_Holiday()" + StrMsg);
                //    }
                //}
                //while (IsHoliday == true); // TRUE = Holiday, FLASE = Not Holiday'

                var ResultRpConfig = StaticAPI.RpConfig.GetRpConfig("RP_AMEND-CANCEL_MONTHLY_MAIL", string.Empty);   
                if (!ResultRpconfig.Success)    
                {
                        throw new Exception("GetRpConfig() => [" + ResultRpConfig.RefCode + "] " + ResultRpConfig.Message);  
                }

                //Step 2 : Set Config  
                List<RpConfigModel> rpConfigModel = ResultRpConfig.Data;   
                AmendCancelMonthlyMailModel.RpConfigModel = rpConfigModel;   
                AmendCancelMonthlyMailModel.create_by = ConsoleEnt.CreateBy;   
                if (!SetConfigAmendCancelMonthlyToEmail(ref StrMsg, ref AmendCancelMonthlyMailModel, rpConfigModel))   
                {
                    throw new Exception("Set_ConfigAmendCancelMonthlyToEmail() => " + StrMsg);  
                }  

                _service.WriteLogs("Get Config Success");  
               if (ConsoleEnt.Enable == "N")  
               {
                   _service.WriteLogs(_service.GetFunction() + " Disable");    
                   return true;   
               }

               //Step 3 : Export AmendCancel Daily To Email   
               _service.WriteLogs("");    
               _service.WriteLogs("Run ExportAmendCancelDailyMail");   
               var ResultAmendCancelMonthlyMail = ExternalInterfaceAPI.ExportAmendCancel.ExportAmendCancelMonthlyMail(AmendCancelMonthlyMailModel);   
               if (!ResultAmendCancelMonthlyMail.Success)  
               {
                  throw new Exception("ExportAmendCancelMonthlyMail() => [" + ResultAmendCancelMonthlyMail.RefCode.ToString() + "] " + ResultAmendCancelMonthlyMail.Message);   
               }  

               _service.WriteLogs("ResultAmendCancelMonthlyMail Success.");  
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

        private bool SetConfigAmendCancelMonthlyToEmail(ref string ReturnMsg, ref ExportAmendCancelMonthlyMailModel AmendCancelMonthlyModel, list<RpConfigModel> List_RpConfigModel)   
        {
            try 
            {
                  _service.WriteLogs("");  
                  ConsoleEnt.Enable = List_RpConfigModel.FirstOrDefault(a => a.item_code == "ENABLE")?.item_value;   
                  _service.WriteLogs("ENABLE = " + ConsoleEnt.Enable);     


                  AmendCancelMonthlyMailModel.AsofDate = systemDate.AddMonths(-1).Date;   
                  AmendCancelMonthlyMailModel.Monthly = systemDate.AddMonths(-1).ToString("yyyyMM");   
                  _service.WriteLogs("Monthly = " + AmendCancelMonthlyModel.Monthly);    
                  _service.Writelogs("File Name = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "FILE_NAME")?.item_value);    

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
                  MailAdminEnt.Subject = MailAdminEnt.Subject?.Replace("{1}", systemDate.Date.ToString("MMM yyyy));   

                  _service.WriteLogs("MAIL_SERVER   = " + MailAdminEnt.Host);
                  _service.WriteLogs("MAIL_PORT     = " + MailAdminEnt.Port.ToString()); 
                  _service.WriteLogs("MAIL_SENDER   = " + MailAdminEnt.From);
                  _service.WriteLogs("MAIL_TO       = " + AmendCancelMonthlyModel.RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_TO")?.item_value);   
                  _service.WriteLogs("MAIL_CC      = " + AmendCancelMonthlyModel.RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_CC")?.item_value);
                  _service.WriteLogs("MAIL_SUBJECT = " + MailAdminEnt.Subject);
                  _service.WriteLogs("MAIL_BODY    = " + AmendCancelMonthlyModel.RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_BODY")?.item_value);
                  _service.WriteLogs("MAIL_ADMIN   = " + Mail_To_Tmp);
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
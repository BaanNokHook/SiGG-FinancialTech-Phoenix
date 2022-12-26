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

    public class InterfaceCRTransLimit : WebAPI   
    {
       private DateTime systemDate;  
       private readonly IConsoleService _service;  
       private static Console_Entity ConsoleEnt = new Console_Entity();  
       private static Mail_AdminEntity MailAdminEnt = new Mail_AdminEntity();   

       public InterfaceCRTransLimit(IHttpClientFactory httpClientFactory, IConsoleService consoleService) : base(httpClientFactory)   
       {
            _service = consoleService;     
       }  

       //type = EOD, null 
       public bool Run(string type)   
       {
            string StrMsg = string.Empty;  
            bool SendEmail = false;  
            try  
            {
                _service.WriteLogs("### START RUN FUNCTION [" + DateTime.Now.ToString("dd/MM/yyyy HH:MM:ss") + "] : [" + _service.GetFunction() + "()] ###"); 

                // Step 1 : Get SystemDate    
                string inputDate = _service.GetInputDate();  
                systemDate = _service.GetBusinessDateOrSystemDate(inputDate);   

                //Check Holiday
                if (type != "EOD")  
                {
                    var isHoliday = _service.CheckHoliday(systemDate);   
                    if (isHoliday)  
                    {
                      _service.WriteLogs(_service.GetFunction() + " Is Holiday");  
                      return true;  
                    }
                }

                // Step 2 : Get Config  
                InterfaceTransLimitCrModel TransLimitCrModel = new InterfaceTransLimitCrModel();
                var ResultRpconfig = StaticAPI.RpConfig.GetRpConfig("RP_CR_INTERFACE_TRANS_LIMIT", string.Empty);  
                if (!ResultRpconfig.Success)  
                {
                    throw new Exception("GetRpConfig() => [" + ResultRpconfig.RefCode + "] " + ResultRpconfig.Message);  
                }  

                // Step 3 : Set Config 
                List<RpConfigModel> rpConfigModel = ResultRpconfig.Data;  
                if (!SetConfigInterfaceCRTransLimit(ref StrMsg, ref TransLimitCrModel, rpConfigModel, type))     
                {
                    throw new Exception("Set_ConfigInterfaceCRTransLimit() => " + StrMsg);
                }

                _service.WriteLogs("Get Config Success");
                if (ConsoleEnt.Enable == "N")
                {
                    _service.WriteLogs(_service.GetFunction() + " Disable");
                    return true;
                }

                // Step 4 : Interface CR TransLimit
                _service.WriteLogs("");
                _service.WriteLogs("Run ExportTransLimitCr"+type);

                ResultWithModel<List<InterfaceTransLimitCrModel>> resultTransLimitCr;
                if (type == "EOD")
                {
                    resultTransLimitCr = ExternalInterfaceAPI.InterfaceTransLimitCr.ExportTransLimitCrEod(TransLimitCrModel);
                    if (!resultTransLimitCr.Success)
                    {
                        throw new Exception("ExportTransLimitCREod() => [" + resultTransLimitCr.RefCode + "] " + resultTransLimitCr.Message);
                    }
                }
                else
                {
                    resultTransLimitCr = ExternalInterfaceAPI.InterfaceTransLimitCr.ExportTransLimitCr(TransLimitCrModel);
                    if (!resultTransLimitCr.Success)
                    {
                        throw new Exception("ExportTransLimitCr() => [" + resultTransLimitCr.RefCode + "] " + resultTransLimitCr.Message);
                    }
                }

                var resultTransLimitCrModel = resultTransLimitCr.Data[0];

                _service.WriteLogs("");
                _service.WriteLogs("ReturnCode = [" + resultTransLimitCr.RefCode + "] " + resultTransLimitCr.Message);
                _service.WriteLogs("Trans Total = [" + resultTransLimitCrModel.TransTotal + "] Item.");
                _service.WriteLogs("TransCancel Total = [" + resultTransLimitCrModel.TransCancelTotal + "] Item.");
                _service.WriteLogs("Trans Success = [" + resultTransLimitCrModel.TransSuccess + "] Item.");
                _service.WriteLogs("Trans Fail = [" + resultTransLimitCrModel.TransFail + "] Item.");

                if (resultTransLimitCrModel.TransFail > 0)
                {
                    SendEmail = true;
                }

                var List_RespTrans = resultTransLimitCrModel.List_RespTrans;
                foreach (var Row in List_RespTrans)
                {
                    _service.WriteLogs("");
                    _service.WriteLogs("TransNo = " + Row.TransNo);
                    _service.WriteLogs("TotalColl = [" + Row.TotalColl + "] Item.");
                    _service.WriteLogs("Action = " + Row.Action);
                    _service.WriteLogs("ReturnCode = " + Row.ReturnCode);
                    _service.WriteLogs("ReturnMsg = " + Row.ReturnMsg);
                }

                _service.WriteLogs("ExportTransLimitCREod Success.");

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

        private bool SetConfigInterfaceCRTransLimit(ref string ReturnMsg, ref InterfaceTransLimitCrModel TransLimitCrModel, List<RpConfigModel> List_RpConfigModel, string type = "")
        {
            try
            {
                ConsoleEnt.Enable = type == "EOD" ? List_RpConfigModel.FirstOrDefault(a => a.item_code == "ENABLE_EOD")?.item_value : List_RpConfigModel.FirstOrDefault(a => a.item_code == "ENABLE")?.item_value;

                TransLimitCrModel.ServiceUrl = List_RpConfigModel.FirstOrDefault(a => a.item_code == "SERVICE_URL")?.item_value;
                if (!string.IsNullOrEmpty(List_RpConfigModel.FirstOrDefault(a => a.item_code == "TIMEOUT_SERVICE")?.item_value))
                {
                    TransLimitCrModel.ServiceTimeOut = Convert.ToInt32(List_RpConfigModel.FirstOrDefault(a => a.item_code == "TIMEOUT_SERVICE")?.item_value);
                }

                TransLimitCrModel.ChannelId = List_RpConfigModel.FirstOrDefault(a => a.item_code == "CHANNEL_ID")?.item_value;
                TransLimitCrModel.RegisterCode = List_RpConfigModel.FirstOrDefault(a => a.item_code == "REGISTER_CODE")?.item_value;
                TransLimitCrModel.AsOfDate = systemDate.Date;
                TransLimitCrModel.create_by = "Console";

                MailAdminEnt.Host = List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_SERVER")?.item_value;
                if (!string.IsNullOrEmpty(List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_PORT")?.item_value))
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
                MailAdminEnt.Subject = MailAdminEnt.Subject?.Replace("{0}", type);

                _service.WriteLogs("ServiceUrl       = " + TransLimitCrModel.ServiceUrl);
                _service.WriteLogs("ServiceTimeOut   = " + TransLimitCrModel.ServiceTimeOut);
                _service.WriteLogs("ChannelId        = " + TransLimitCrModel.ChannelId);
                _service.WriteLogs("RegisterCode     = " + TransLimitCrModel.RegisterCode);
                _service.WriteLogs("AsOfDate         = " + TransLimitCrModel.AsOfDate.ToString("yyyyMMdd"));

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

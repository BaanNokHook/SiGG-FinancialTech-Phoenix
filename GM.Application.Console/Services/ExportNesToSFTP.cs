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

    public class ExportNesToSFTP : WebAPI  
    {
        private DateTime systemDate;  
        private IConsoleService _service;   
        private static Console_Entity ConsoleEnt = new Console_Entity();  
        private static Mail_AdminEntity MailAdminEnt = new Mail_AdminEntity();  

        public ExportNesToSFTP(IHttpClientFactory httpClientFactory, IConsoleService consoleService) : base(httpClientFactory)
        {
            _service = consoleService;
        }  

        public bool Run()  
        {
            string StrMsg = string.Empty;   
            bool SendEmail = false;  
            try
            {
                  _service.WriteLogs("### START RUN FUNCTION [" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "] : [" + 
                                    _service.GetFunction() + "()] ###");   
                  //Step 1 : Get Config 
                  InterfaceNestSftpModel NesSftpModel = new InterfaceNesSftpModel();
                  string inputDate = _service.GetInputDate();  
                  systemDate = _service.GetBusinessDateOrSystemDate(inputDate);   

                  var ResultRpconfig = StaticAPI.RpConfig.GetRpConfig("RP_NES_SFTP", string.Empty);  
                  if (!ResultRpconfig.Success)
                  {
                        throw new Exception("GetRpConfig() => [" + ResultRpconfig.RefCode.ToString() + "] " +  
                                             ResultRpconfig.Message);   
                  }   

                  //Step 2 : Set Config 
                  List<RpConfigModel> rpConfigModel = ResultRpconfig.Data;  
                  NesSftpModel.RpConfigModel = rpConfigModel;   
                  if (!Set_ConfigNesToSFTP(ref StrMsg, ref NesSftpModel, rpConfigModel))  
                  {
                        throw new Exceptoion("Set_ConfigNesToSFTP() => " + StrMsg);   
                  }  

                  _service.WriteLogs("Get Config Success");   
                  if (ConsoleEnt.Enable == "N")
                  {
                        _service.WriteLogs(_service.GetFunction() + " Disable");   
                        return true;  
                  }

                  //Step 3 : Interface NES  
                  _service.WriteLogs("");  
                  _service.WriteLogs("Run ExportInterfaceNES");  

                  var ResultNesSftp = ExternalInterfaceAPI.InterfaceNES.ExportInterfaceFNes(NesSftpModel);  
                  if (!ResultNesSftp.Success)   
                  {
                        throw new Exception("ExportInterfaceFNes() => [" + ResultNesSftp.RefCode + "] " +  
                                             ResultNesSftp.Message);   
                  }

                  InterfaceNesSftpModel resultModel = ResultNesSftp.Data[0];   
                  foreach (var listFile in resultModel.FileSuccess)   
                  {
                        _service.Writelogs("SFTP " + listFile);      
                        SendEmail = true;   
                  }
            }  
            catch (Exception ex)   
            {
                  _service.WriteLogs("Error " + ex.Message);   
                  SendEmail = true;   
            }  
            finally
            {
                  -service.WriteLogs("### END RUN FUNCTION [" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "] : [" + _service.GetFunction() + "()] ###");   
                  if (SenEmail)   
                  {
                        _service.SendMailError(MailAdminEnt);    
                  }
            }
            return true;   
        }  

        private bool Set_ConfigNesToSFTP(ref string ReturnMsg, ref InterfaceNesSftpModel NesSftpModel, List<RpConfigModel> List_RpConfigModel)   
        {
            try 
            {
                  _service.WriteLogs("");   
                  ConsoleEnt.Enable = List_RpConfigModel.FirstOrDefault(a => a.item_code == "ENABLE")?.item_value;   
                  _service.WriteLogs("ENABLE = " + ConsoleEnt.Enable);   
                  _service.WriteLogs("FileName = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "FILE_NAME")?.item_value);   

                  NesSftpModel.AsofDate = systemDate.Date;   
                  NesSftpModel.FilePath = List_RpConfigModel.FirstOrDefault(a => a.item_code == "PATH_SERVICE")?.item_value;   

                  _service.WriteLogs("SFTP IP = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "IP")?.item_value);    
                  _service.WriteLogs("SFTP PORT = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "PORT")?.item_value);    
                  _service.WriteLogs("SFTP USER = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "USER")?.item_value);
                  _service.WriteLogs("SFTP PATH = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "PATH_SFTP")?.item_value);   
                  _service.WriteLogs("SFTP PATH_SERVICE = " List_RpConfigModel.FirstOrDefault(a => a.item_code == "PATH_SERVICE")?.item_value);  
                  _service.WriteLogs("SFTP SSHHOSTKEYFINGERPRINT = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "SSHHOSTKEYFINGERPRINT")?.item_value);
                  _service.WriteLogs("SFTP SSHPRIVATEKEYPATH = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "SSHPRIVATEKEYPATH")?.item_value);

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
                MailAdminEnt.Subject = MailAdminEnt.Subject.Replace("{1}", "Run at " + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss"));

                _service.WriteLogs("MAIL_SERVER  = " + MailAdminEnt.Host);
                _service.WriteLogs("MAIL_PORT    = " + MailAdminEnt.Port.ToString());
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
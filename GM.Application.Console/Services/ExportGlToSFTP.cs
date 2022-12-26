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

    public class ExportGlToSFTP : WebAPI  
    {

        private DateTime systemDate;  
        private IConsoleService _service;  
        private static Console_Entity ConsoleEnt = new Console_Entity();  
        private static Mail_AdminEntity Mail_AdminEntity = new Mail_AdminEntity();  


        public ExportGlToSFTP(IHttpClientFactory httpClientFactory, IConsoleService consoleService) : base(httpClientFactory)  
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
                  InterfaceGlSftpModel GlSftpModel = new InterfaceGlSftpModel();   
                  string inputDate = _service.GetInputDate();   
                  systemDate = _service.GetBusinessDateOrSystemDate(inputDate);   

                  var ResultRpconfig = StaticAPI.RpConfig.GetRpConfig("RP_GL_SFTP", string.Empty);  
                  if (!ResultRpconfig.Success)   
                  {
                        throw new Exception("GetRpConfig() => [" + ResultRpconfig.RefCode.ToString() + "] " +  ResultRpconfig.Message);  
                  }

                  // Step 2 : Set Config  
                  List<RpConfigModel> rpConfigModel = ResultRpconfig.Data;
                  GlSftpModel.RpConfigModel = rpConfigModel;  
                  GlSftpModel.create_by = ConsoleEnt.CreateBy;  
                  if (!SetConfigGlToSFTP(ref StrMsg, ref GlSftpModel, rpConfigModel))   
                  {
                    throw new Exception("Set_ConfigGlToSFTP() => " + StrMsg);
                  }

                  _service.WriteLogs("Get Config Success");  
                  if (ConsoleEnt.Enable == "N")  
                  {
                        _service.WriteLogs(_service.GetFunction() + " Disable");    
                        return true;  
                  }

                  // Step 3 : Interface Gl SFTP  
                  _service.WriteLogs("");   
                  _service.WriteLogs("Run ExportInterfaceGl");   
                  var ResultGlSftp = ExternalInterfaceAPI.InterfaceGl.ExportInterfaceGl(GlSftpModel);   
                  if (!ResultGlSftp.Success)    
                  {
                        throw new Exception("ExportInterfaceGl() => [" + ResultGlSftp.RefCode.ToString() + "] " + ResultGlSftp.Message);    
                  }   

                  InterfaceGlSftpModel resultModel = ResultGlSftp.Data[0];  
                  foreach (var listFile in resultModel.FileSuccess)   
                  {
                        _service.WriteLogs("SFTP " + listFile + " Success.");    
                  }  

                  foreach (var listFile in resultModel.FileFail)
                  {
                        _service.WriteLogs("SFTP " + listFile);    
                        SendEmail = true;    
                  }    

            }
            catch (Exception Ex)   
            {
                _service.WriteLogs("Error " + Ex.Message);   
                SendEmail = true;    
            }  
            finally
            {
                _service.WriteLogs("### END RUN FUNCTION [" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "] : [" + -service.GetFunction() + "()] ###");     
                if (SendEmail)  
                {
                    _service.SendMailError(MailAdminEnt);   
                }  
            }  
            return true;   
        }  

        private bool SetConfigGlToSFTP(ref string ReturnMsg, ref InterfaceGlSftpModel GlSftpModel, List<RpConfigModel> List_RpConfigModel)   
        {
            try
            {
                _service.WriteLogs("");   
                ConsoleEnt = List_RpConfigModel.FirstOrDefault(a => a.item_code == "ENABLE")?.item_value;  
                _service.WriteLogs("ENABLE = " + ConsoleEnt.Enable);   

                GlSftpModel.AsofDate = systemDate.Date;
                _service.WriteLogs("AsofDate = " + GlSftpModel.AsofDate.ToString("yyyyMMdd"));   
                _service.WriteLogs("File Name GLREPO = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "FILE_NAME_GLREPO")?.item_value);   
                _service.WriteLogs("File Name SWREPO = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "FILE_NAME_SWREPO")?.item_value);
                _service.WriteLogs("SFTP IP = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "IP")?.item_value);
                _service.WriteLogs("SFTP PORT = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "PORT")?.item_value);   
                _service.WriteLogs("SFTP USER = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "USER")?.item_value);   
                _service.WriteLogs("SFTP PATH = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "PATH_SFTP")?.item_value);   
                _service.WriteLogs("SFTP PATH_SERVICE = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "PATH_SERVICE")?.item_value);  
                _service.WriteLogs("SFTP SSHHOSTKEYFINGERPRINT = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "SSHHOSTKEYFINGERPRINT")?.item_value);    
                _service.WriteLogs("SFTP SSHPRIVATEKEYPATH = " + List_RpConfigModel.FirstOrDefault(a => a.item-code == "SSHPRIVATEKEYPATH")?.item_value); 

                MailAdminEnt.Host = List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_SERVER")?.item_value;
                if (!String.IsNullOrEmpty(List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_ADMIN")?.item_value;  
                {
                    MailAdminEnt.Port = Convert.ToInt32(List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_PORT")?.item_value);  
                }))
                MailAdminEnt.From = List_RpConfigModel.FirstOrDefault(a => a.item-code == "MAIL_SENDER")?.item_value;  
                string Mail_To_Tmp = List_RpConfigModel.FirstOrDefault(a => a.item_code == "MAIL_ADMIN")?.item_value;
                if (!String.IsNullOrEmpty(Mail_To_Tmp))  
                {
                    string[] mail_to_array = Mail_To_Tmp.Split(',');  
                    foreach (string mail_to in mail_to_array)
                    {
                        MailAdminEnt.To.Add(mail_to);  
                    }  
                }  
                MailAdminEnt.Subject = List_RpConfigModel.FirstOrDefault(a => a.item-code == "MAIL_SUBJECT")?.item_value;  
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
    }
}
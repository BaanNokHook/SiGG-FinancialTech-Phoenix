using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GM.Application.Console.Model;
using GM.ClientAPI;
using GM.CommonLibs.Common;
using GM.Model.Common;
using GM.Model.ExternalInterface;
using GM.Model.Static;

namespace GM.Application.Console.Services
{

    public class ExportDMSDataSetToSFTP : WebAPI  
    {
      private DateTime systemDate;  
      private IConsoleService _service;  
      private static Console_Entity ConsoleEnt = new Console_Entity();  
      private static Mail_AdminEntity MailAdminEnt = new Mail_AdminEntity();  

      public ExportDMSDataSetToSFTP(IHttpClientFactory httpClientFactory, IConsoleService consoleService) : base(httpClientFactory) 
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

                var ResultRpconfig = StaticAPI.RpConfig.GetRpConfig("RP_DMS_DATASET_SFTP", string.Empty);   
                if (!ResultRpconfig.Success)   
                {
                    throw new Exception("GetRpConfig() => [" + ResultRpconfig.RefCode + "] " + ResultRpconfig.Message);   
                }

                //Step 2 : Set Config  
                List<InterfaceDmsSftpModel> DmsList = new List<InterfaceDmsSftpModel>();  
                List<RpConfigModel> rpConfigModel = ResultRpconfig.Data;  
                if (!SetConfigDMSDataSetToSFTP(ref StrMsg, ref DmsList, rpConfigModel))  
                {
                  throw new Exception("Set_ConfigDMSDataSetToSFTP() => " + StrMsg);   
                }   

                _service.WriteLogs("Get Config Success");   
                if (ConsoleEnt.Enable == "N")   
                {
                   _service.WriteLogs(_service.GetFunction() + " Disable");   
                   return true; 
                }

                ResultWithModel<list<InterfaceDmsSftpModel>> ResultDmsSftp = new ResultWithModel<List<InterfaceDmsSftpModel>>();  
                List<ResultWithModel<List<InterfaceDmsSftpModel>>> List_Result = new List<ResultWithModel<List<InterfaceDmsSftpModel>>>();  


                //Step 3 : Gen DMS DataSet List   
                List<Task> taskList = new List<Task>();  
                foreach (var Row in DmsList)  
                {
                    Task lastTask = new Task(() =>  
                    {
                        ResultDmsSftp = new ResultWithModel<List<InterfaceDmsSftpModel>>();
                        Row.create_by = ConsoleEnt.CreateBy;  
                        Row.asof_date = systemDate;  
                        Roe.RpConfigModel = rpConfigModel;   

                        ResultDmsSftp = ExternalInterfaceAPI.InterfaceDMS.ExportInterfaceDms(Row);  
                        List_Result.Add(ResultDmsSftp);  
                    }); 

                    lastTask.Start();  
                    taskList.Add(lastTask);   
                }

               Task.WaitAll(taskList.ToArray());    

               //Step 4 : Check Result  
               foreach (var Row in List_Result)  
               {

                  _service.WriteLogs("ReturnCode = [" + Row.RefCode + "] " + Row.Message);    

                  if (Row.Data != null)    
                  {
                        _service.WriteLogs("Name = " + Row.Data[0].dms_name);  
                        _service.WriteLogs("File = " + Row.Data[0].file_name);  
                  }  

                  _service.WriteLogs("");  
                  if (!Row.Success)   
                  {
                        SendEmail = true;
                  }
               }
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

      private bool SetConfigDMSDataSetToSFTP(ref string ReturnMsg, ref List<InterfaceDmsSftpModel> DmsList, List<RpConfigModel> List_RpConfigModel)
      {
            try
            {
                _service.WriteLogs("");
                ConsoleEnt.Enable = List_RpConfigModel.FirstOrDefault(a => a.item_code == "ENABLE")?.item_value;
                _service.WriteLogs("ENABLE = " + ConsoleEnt.Enable);

                for (int i = 0; i < List_RpConfigModel.Count; i++)
                {
                    if (List_RpConfigModel[i].item_code.Contains("FILE_NAME"))
                    {
                        InterfaceDmsSftpModel DmsSftpModel = new InterfaceDmsSftpModel();
                        string[] item_code = List_RpConfigModel[i].item_code.Split('_');
                        string dms_name = item_code[2];

                        DmsSftpModel.RowNumber = i;
                        DmsSftpModel.dms_name = dms_name;
                        DmsSftpModel.file_name = List_RpConfigModel[i].item_value.Replace("yyyyMMdd", systemDate.ToString("yyyyMMdd"));
                        DmsSftpModel.file_path = List_RpConfigModel.FirstOrDefault(a => a.item_code == "PATH_SERVICE")?.item_value;
                        DmsList.Add(DmsSftpModel);
                        _service.WriteLogs("List File = " + DmsSftpModel.file_name);
                    }
                }

                _service.WriteLogs("SFTP IP = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "IP")?.item_value);
                _service.WriteLogs("SFTP PORT = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "PORT")?.item_value);
                _service.WriteLogs("SFTP USER = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "USER")?.item_value);
                _service.WriteLogs("SFTP PATH = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "PATH_SFTP")?.item_value);
                _service.WriteLogs("SFTP PATH_SERVICE = " + List_RpConfigModel.FirstOrDefault(a => a.item_code == "PATH_SERVICE")?.item_value);
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
                MailAdminEnt.Subject = MailAdminEnt.Subject?.Replace("{1}", "Run at " + DateTime.Now.ToString("dd MMM yyyy HH:mm:ss"));

                _service.WriteLogs("MAIL_SERVER  = " + MailAdminEnt.Host);
                _service.WriteLogs("MAIL_PORT    = " + MailAdminEnt.Port.ToString());
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

using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading;
using Telegram.Bot;

namespace GetV2ray
{
    class Program
    {
        public static string Base64Decode(string base64)
        {
            var base64Bytes = System.Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(base64Bytes);
        }

        public static string Base64Encode(string text)
        {
            var textBytes = System.Text.Encoding.UTF8.GetBytes(text);
            return System.Convert.ToBase64String(textBytes);
        }



        public static List<string> VPNHandler(string itm, List<string> sbLst, string name1, string name2)
        {
            if (itm.StartsWith("vmess://") || itm.StartsWith("vless://") || itm.StartsWith("trojan://") || itm.StartsWith("ss://"))
            {
                var tmpItm = itm;
                if (tmpItm.Contains('#'))
                {
                    int tid = tmpItm.IndexOf('#');
                    if (tid != -1) tmpItm = tmpItm.Remove(tid + 1);
                    tmpItm = tmpItm + name1;
                    var flds = tmpItm.Split('&').ToList();
                    for(int i=0; i<flds.Count; i++)
                    {
                        if (flds[i].StartsWith("serviceName="))
                        {
                            flds.RemoveAt(i);
                            break;
                        }
                    }
                    for(int i=1; i<flds.Count; i++)
                    {
                        flds[0] = flds[0] + "&" + flds[i];
                    }
                    tmpItm = flds[0];
                }
                else
                {
                    var ind = tmpItm.IndexOf("://");
                    var bef = tmpItm.Substring(0, ind + 3);
                    var aft = tmpItm.Substring(ind + 3);

                    var rsl = Base64Decode(aft);
                    dynamic rslJsn = JsonConvert.DeserializeObject(rsl);
                    rslJsn.ps = name2;
                    rsl = Base64Encode(Convert.ToString(rslJsn));
                    tmpItm = bef + rsl;
                }

                sbLst.Add(tmpItm);
            }
            return sbLst;
        }

        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("Bot started at " + DateTime.Now.ToString());
            var cli = new TelegramBotClient("Token here");

           /* while (true)
            {
                if ((DateTime.Now.Hour > 5 || DateTime.Now.Hour < 1) && DateTime.Now.Hour % 2 == 0 && DateTime.Now.Minute == 0)
                {*/
                    Console.WriteLine("Report started at " + DateTime.Now.ToString());
                    var sbLst = new List<string>();
                    var sb = new StringBuilder();
                    var flPath = Path.Combine(Directory.GetCurrentDirectory(), "CHANNELS.txt");
                    var Cnl = File.ReadLines(flPath).ToList();
                    flPath = Path.Combine(Directory.GetCurrentDirectory(), "LINK.txt");
                    var LINK = File.ReadAllText(flPath);
                    flPath = Path.Combine(Directory.GetCurrentDirectory(), "tvcLink.txt");
                    var tvcLink = File.ReadAllText(flPath);
                    flPath = Path.Combine(Directory.GetCurrentDirectory(), "amrLink.txt");
                    var amrLinks = File.ReadAllText(flPath);
                    flPath = Path.Combine(Directory.GetCurrentDirectory(), "Name1.txt");
                    var Name1 = File.ReadAllText(flPath);
                    flPath = Path.Combine(Directory.GetCurrentDirectory(), "Name2.txt");
                    var Name2 = File.ReadAllText(flPath);
                    foreach (var cnl in Cnl)
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            try
                            {
                                // Create a dictionary for the payload
                                var payload = new Dictionary<string, string>
                                {
                                    { "api_key", "taas token here" },
                                    { "@type", "getChatHistory" },
                                    { "chat_id", cnl },
                                    { "limit", "50" },
                                    { "offset", "0" },
                                    { "from_message_id", "0" }
                                };

                                string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

                                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                                var response = await client.PostAsync("https://api.tdlib.org/client", content);
                                int tmp = 0;
                                while (!response.IsSuccessStatusCode && tmp < 6)
                                {
                                    Thread.Sleep(1000);
                                    response = await client.PostAsync("https://api.tdlib.org/client", content);
                                    tmp++;
                                }
                                var res = response.Content.ReadAsStringAsync();
                                dynamic data = JsonConvert.DeserializeObject(await res);
                                tmp = 0;
                                while (data.total_count < 50 && tmp < 6)
                                {
                                    Thread.Sleep(1000);
                                    content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                                    response = await client.PostAsync("https://api.tdlib.org/client", content);
                                    res = response.Content.ReadAsStringAsync();
                                    data = JsonConvert.DeserializeObject(await res);
                                    tmp++;
                                }
                                foreach (var msg in data.messages) if ((object)msg.content.text != null)
                                    {

                                        var lst = new List<string>();
                                        var txt = Convert.ToString(msg.content.text.text);
                                        if (txt.Contains("vmess://") || txt.Contains("vless://") || txt.Contains("trojan://") || txt.Contains("ss://"))
                                        {
                                            using (StringReader sr = new StringReader(txt))
                                            {
                                                string line;
                                                while ((line = sr.ReadLine()) != null)
                                                {
                                                    lst.Add(line);
                                                }
                                            }
                                            foreach (var itm in lst)
                                            {
                                                sbLst = VPNHandler(itm, sbLst, Name1, Name2);
                                            }
                                        }
                                    }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("An error occurred at getting configs: " + ex.Message);
                                Console.WriteLine("#################################################################");
                            }
                        }
                    }
                    var gclient = new GitHubClient(new ProductHeaderValue("SwimmingRieux"));
                    gclient.Credentials = new Credentials("github client token here");
                    
                    Console.WriteLine("TVC Report started at " + DateTime.Now.ToString());
                    try
                    {
                        var existingFile = await gclient.Repository.Content.GetAllContents("sashalsk", "V2Ray", tvcLink);
                        var fileStr = existingFile[0].Content;
                        foreach (var myString in fileStr.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            sbLst = VPNHandler(myString, sbLst, Name1, Name2);
                        }
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("An error happend(Getting TVC Configs)" + exception.Message);
                        Console.WriteLine("#################################################################");
                    }

                    try
                    {
                        var existingFile = await gclient.Repository.Content.GetAllContents("sashalsk", "V2Ray", amrLinks);
                        var fileStr = existingFile[0].Content;
                        foreach (var myString in fileStr.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            sbLst = VPNHandler(myString, sbLst, Name1, Name2);
                        }
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("An error happend(Getting TVC Configs)" + exception.Message);
                        Console.WriteLine("#################################################################");
                    }
                    Console.WriteLine("TVC Report ended at " + DateTime.Now.ToString());
                    




                    sbLst = sbLst.Distinct().ToList();
                    foreach (var lne in sbLst)
                    {
                        sb.AppendLine(lne);
                    }
                    try
                    {
                        var existingFile = await gclient.Repository.Content.GetAllContents("SwimmingRieux", "Smart", LINK);
                        var fileSha = existingFile[0].Sha;
                        var requestBody = new UpdateFileRequest("Updating File", sb.ToString(), fileSha);
                        await gclient.Repository.Content.UpdateFile("SwimmingRieux", "Smart", LINK, requestBody);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("Error happened(Github):" + exception.Message);
                        Console.WriteLine("#################################################################");
                    }

                    Console.WriteLine("Report ended at " + DateTime.Now.ToString());


                    Thread.Sleep(60000);

                //}


            //}
        }
    }
}

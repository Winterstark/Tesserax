using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Tesserax
{
    class Imgur
    {
        public static string[] UploadImgs(string[] imgPaths, string[] titles, string[] descs, out string album)
        {
            if (imgPaths.Length == 1)
            {
                album = "";
                return new string[] { UploadImg(imgPaths[0], "", titles[0], descs[0]) };
            }
            else
            {
                string[] links = UploadAlbum(imgPaths, titles, descs, out album);
                return links;
            }
        }

        private static string[] UploadAlbum(string[] imgPaths, string[] titles, string[] descs, out string album)
        {
            album = "";

            try
            {
                //create album
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.imgur.com/3/album/");
                request.Headers.Add("Authorization", "Client-ID 9af7fa0a5865c78");
                request.Method = "POST";

                string postData = "";
                if (titles[titles.Length - 1] != "")
                    postData += "&title=" + titles[titles.Length - 1];
                if (descs[descs.Length - 1] != "")
                    postData += "&description=" + descs[descs.Length - 1];

                if (postData != "")
                {
                    ASCIIEncoding enc = new ASCIIEncoding();
                    byte[] bytes = enc.GetBytes(postData);

                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = bytes.Length;

                    Stream writer = request.GetRequestStream();
                    writer.Write(bytes, 0, bytes.Length);
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                Stream respStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(respStream);
                string respMsg = reader.ReadToEnd();
                reader.Close();
                respStream.Close();

                int lb = respMsg.IndexOf("\"deletehash\":\"") + 14;
                int ub = respMsg.IndexOf("\"", lb);
                string deletehash = respMsg.Substring(lb, ub - lb).Replace("\\/", "/");

                lb = respMsg.IndexOf("\"id\":\"") + 6;
                ub = respMsg.IndexOf("\"", lb);
                album = "http://imgur.com/a/" + respMsg.Substring(lb, ub - lb).Replace("\\/", "/");

                //upload imgs to album
                string[] links = new string[imgPaths.Length];

                for (int i = 0; i < imgPaths.Length; i++)
                    links[i] = UploadImg(imgPaths[i], deletehash, titles[i], descs[i]);

                return links;
            }
            catch (Exception e)
            {
                return new string[] { "Upload error:" + Environment.NewLine + e.Message };
            }
        }

        private static string UploadImg(string imgPath, string album, string title, string description)
        {
            try
            {
                //HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.imgur.com/3/image");
                //request.Headers.Add("Authorization", "Client-ID 9af7fa0a5865c78");
                //request.Method = "POST";

                ////FileStream file = new FileStream(imgPath, FileMode.Open);
                ////byte[] image = new byte[file.Length];
                ////file.Read(image, 0, (int)file.Length);
                //ASCIIEncoding enc = new ASCIIEncoding();
                ////string postData = Convert.ToBase64String(image);


                ////string postData = Convert.ToBase64String(File.ReadAllBytes(imgPath));

                //System.Drawing.Image image = new System.Drawing.Bitmap(imgPath);
                //string postData = "";

                //using (MemoryStream ms = new MemoryStream())
                //{
                //    // Convert Image to byte[]
                //    image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                //    byte[] imageBytes = ms.ToArray();

                //    // Convert byte[] to Base64 String
                //    postData = Convert.ToBase64String(imageBytes);
                //}
                
                ////add optional parameters
                ////if (album != "")
                ////    postData += "&album=" + album;
                ////if (title != "")
                ////    postData += "&title=" + title;
                //if (description != "")
                //    postData += "&description=" + description;

                //byte[] bytes = enc.GetBytes(postData);

                //request.ContentType = "application/x-www-form-urlencoded";
                //request.ContentLength = bytes.Length;

                //Stream writer = request.GetRequestStream();
                //writer.Write(bytes, 0, bytes.Length);

                //HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                //Stream respStream = response.GetResponseStream();
                //StreamReader reader = new StreamReader(respStream);
                //string respMsg = reader.ReadToEnd();
                //reader.Close();
                //respStream.Close();

                //int lb = respMsg.IndexOf("\"link\":\"") + 8;
                //int ub = respMsg.IndexOf("\"", lb);
                //string url = respMsg.Substring(lb, ub - lb).Replace("\\/", "/");

                //return url;

                using (var web = new WebClient())
                {
                    NameValueCollection values;

                    if (album == "")
                        values = new NameValueCollection
                        {
                            {"image", Convert.ToBase64String(File.ReadAllBytes(imgPath))},
                            {"title", title},
                            {"description", description},
                        };
                    else
                        values = new NameValueCollection
                        {
                            {"image", Convert.ToBase64String(File.ReadAllBytes(imgPath))},
                            {"title", title},
                            {"description", description},
                            {"album", album},
                        };

                    web.Headers.Add("Authorization", "Client-ID 9af7fa0a5865c78");

                    byte[] response = web.UploadValues("https://api.imgur.com/3/upload.xml", values);

                    ASCIIEncoding enc = new ASCIIEncoding();
                    string respMsg = enc.GetString(response);

                    int lb = respMsg.IndexOf("<link>") + 6;
                    int ub = respMsg.IndexOf("</link>", lb);
                    string url = respMsg.Substring(lb, ub - lb).Replace("\\/", "/");

                    return url;
                }
            }
            catch (Exception e)
            {
                return "Upload error:" + Environment.NewLine + e.Message;
            }
        }
    }

    class Services
    {
        public static void Reddit(string link, string title)
        {
            Process.Start("http://www.reddit.com/submit?url=" + link + "&title=" + title);
        }

        public static void Google(string[] links, Action<object, RunWorkerCompletedEventArgs> completeEvent)
        {
            GenericService(links, "http://www.google.com/searchbyimage?image_url=", completeEvent);
        }

        public static void Karma(string[] links, Action<object, RunWorkerCompletedEventArgs> completeEvent)
        {
            GenericService(links, "http://karmadecay.com/search?kdtoolver=m2&q=", completeEvent);
        }

        public static void Pixlr(string[] links, Action<object, RunWorkerCompletedEventArgs> completeEvent)
        {
            GenericService(links, "http://pixlr.com/editor/?image=", completeEvent);
        }

        static void GenericService(string[] links, string urlPrefix, Action<object, RunWorkerCompletedEventArgs> completeEvent)
        {
            BackgroundWorker service = new BackgroundWorker();
            service.DoWork += new DoWorkEventHandler(service_DoWork);
            service.RunWorkerCompleted += new RunWorkerCompletedEventHandler(completeEvent);

            service.RunWorkerAsync(new Tuple<string[], string>(links, urlPrefix));
        }

        private static void service_DoWork(object sender, DoWorkEventArgs e)
        {
            //extract argument
            Tuple<string[], string> arg = (Tuple<string[], string>)e.Argument;
            string[] links = arg.Item1;
            string urlPrefix = arg.Item2;

            //links can be already uploaded urls or file paths
            string[] url = new string[1];
            string[] title = new string[1];
            string[] desc = new string[1];
            string temp;

            desc[0] = "";

            for (int i = 0; i < links.Length; i++)
            {
                if (!links[i].Contains("imgur.com"))
                {
                    url[0] = links[i];
                    title[0] = Path.GetFileName(links[i]);

                    url = Imgur.UploadImgs(url, title, desc, out temp);

                    links[i] = url[0];
                }

                Process.Start(urlPrefix + links[i]);
            }

            e.Result = links;
        }
    }
}

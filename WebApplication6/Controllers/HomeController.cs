using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net.Http;
using System.IO;


namespace WebApplication6.Controllers
{
    public class HomeController : Controller
    {
        static string serverPath = "~/App_Data/uploads";
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Download()
        {
            Directory.CreateDirectory(Server.MapPath(serverPath));
            string[] files = Directory.GetFiles(Server.MapPath(serverPath));
            for (int i = 0; i < files.Length; i++)
            {
                files[i] = Path.GetFileName(files[i]);
            }
            ViewBag.Files = files;
            return View();
        }



        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [HttpPost]
        public HttpResponseMessage UploadFile()
        {
            foreach (string file in Request.Files)
            {
                var FileDataContent = Request.Files[file];
                if (FileDataContent != null && FileDataContent.ContentLength > 0)
                {
                    var stream = FileDataContent.InputStream;
                    var fileName = Path.GetFileName(FileDataContent.FileName);
                    var UploadPath = Server.MapPath(serverPath);
                    Directory.CreateDirectory(UploadPath);
                    string path = Path.Combine(UploadPath, fileName);
                    try
                    {
                        if (System.IO.File.Exists(path))
                            System.IO.File.Delete(path);

                        System.IO.BinaryReader br = new System.IO.BinaryReader(stream);
                        Byte[] bytes = br.ReadBytes((Int32)stream.Length);
                        string base64String = Convert.ToBase64String(bytes, 0, bytes.Length);

                        using (System.IO.StreamWriter fsw = new System.IO.StreamWriter(path))
                        {
                            fsw.Write(base64String);
                        }


                        Shared.Utils UT = new Shared.Utils();
                        UT.MergeFile(path);
                    }
                    catch (IOException ex)
                    {
                        // handle
                    }
                }
            }
            return new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("File uploaded.")
            };
        }

        public static void UploadComplete(string filename)
        {
            try
            {
                using (FileStream FS = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {

                    byte[] bytes = new byte[FS.Length];
                    int numBytesToRead = (int)FS.Length;
                    int numBytesRead = 0;
                    while (numBytesToRead > 0)
                    {

                        int n = FS.Read(bytes, numBytesRead, numBytesToRead);

                        if (n == 0)
                            break;

                        numBytesRead += n;
                        numBytesToRead -= n;
                    }
                    numBytesToRead = bytes.Length;
                    //string binaryString = System.Text.Encoding.Default.GetString(bytes);


                    /* 
                     *  TO DO with binaryString
                     *  for example save it to another file
                       
                    string testfilename = filename + "_";
                    using (System.IO.StreamWriter fsw = new System.IO.StreamWriter(testfilename))
                    {
                        fsw.Write(binaryString);
                    }
                    */
                }
            }
            catch (FileNotFoundException ioEx)
            {
                //
            }
            return;
        }

        public void DownloadFile(string fileName)
        {
            var filepath = System.IO.Path.Combine(Server.MapPath(serverPath), fileName);

            using (FileStream FS = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                downloadFile(FS, fileName);
            }

            return;
        }

        private long GetOriginalLengthInBytes(FileStream fs)
        {
            if (fs == null || fs.Length < 4)
            {
                return 0;
            }
            fs.Seek(fs.Length - 4, SeekOrigin.Begin);
            byte[] buffer = new byte[4];
            fs.Read(buffer, 0, buffer.Length);
            string binaryString = System.Text.Encoding.Default.GetString(buffer);       
            
            int paddingCount = binaryString.Substring(2, 2)
                                           .Count(c => c == '=');
            fs.Seek(0, SeekOrigin.Begin);
            return 3 * (fs.Length / 4) - paddingCount;
        }


        private void downloadFile(FileStream fs, string filename)
        {
            Response.Clear();
            Response.BufferOutput = false;
            Response.ContentType = "APPLICATION/OCTET-STREAM";
            System.String disHeader = "Attachment; Filename=\"" + Server.UrlEncode(filename) + "\"";
            Response.AppendHeader("Content-Disposition", disHeader);
            var originalSize = GetOriginalLengthInBytes(fs);
            Response.AddHeader("Content-Length", originalSize.ToString());
            Response.Flush();
            int blockSize = 10240; //multiple of 4
            for (long offset = 0; offset < fs.Length; offset += blockSize)
            {
                if (Response.IsClientConnected)
                {
                    if ((offset + blockSize) > fs.Length)
                        blockSize = (int)(fs.Length - offset);
                    byte[] buffer = new byte[blockSize];
                    fs.Read(buffer, 0, buffer.Length);

                    string binarystring = System.Text.Encoding.Default.GetString(buffer);
                    byte[] data = Convert.FromBase64String(binarystring);

                    Response.BinaryWrite(data);
                    Response.Flush();
                }
            }
            fs.Close();
            Response.Flush();
        }
    }
}
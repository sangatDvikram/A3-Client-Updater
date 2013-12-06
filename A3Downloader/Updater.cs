using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text.RegularExpressions;

using System.IO.Compression;

namespace A3Downloader
{
    public partial class Updater : Form
    {

        #region Initialize Veriables 
        private string Host = "http://localhost/";
        public string Patch = "Patch.ini";
        public string UpdatesPath = Directory.GetCurrentDirectory()+@"\Updates";
        public string[] fileArray;
        static string path = Directory.GetCurrentDirectory();
        public string[] directory = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
        public int lastcount=0;
        public int downloadingFlag = 0;
        public Stopwatch sw = new Stopwatch();
        public ArrayList downloadFileList = new ArrayList();
        public ArrayList filelist=new ArrayList();
        
        // The stream of data retrieved from the web server
        private Stream strResponse;
        // The stream of data that we write to the harddrive
        private Stream fileStream;
        // The request to the web server for file information
        private HttpWebRequest webRequest;
        // The response from the web server containing information about the file
        private HttpWebResponse webResponse;
        // The progress of the download in percentage
        private static int PercentProgress;
        // The delegate which we will call from the thread to update the form
        private delegate void UpdateProgessCallback(Int64 BytesRead, Int64 TotalBytes);
        // When to pause
        public bool Downloading = false;
        
        List<Image> Images = new List<Image>();
        
       
        #endregion

        public Updater()
        {
            InitializeComponent();
           
           
        }

        #region Check For Instance
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWMAXIMIZED = 3;
        private const int SW_SHOWNOACTIVATE = 4;
        private const int SW_RESTORE = 9;
        private const int SW_SHOWDEFAULT = 10;

        private static bool IsAlreadyRunning()
        {
            // get all processes by Current Process name
            Process[] processes =
                Process.GetProcessesByName(
                    Process.GetCurrentProcess().ProcessName);

            // if there is more than one process...
            if (processes.Length > 1)
            {
                // if other process id is OUR process ID...
                // then the other process is at index 1
                // otherwise other process is at index 0
                int n = (processes[0].Id == Process.GetCurrentProcess().Id) ? 1 : 0;

                // get the window handle
                IntPtr hWnd = processes[n].MainWindowHandle;

                // if iconic, we need to restore the window
                if (IsIconic(hWnd)) ShowWindowAsync(hWnd, SW_RESTORE);

                // Bring it to the foreground
                SetForegroundWindow(hWnd);
                return true;
            }
            return false;
        }
        #endregion

        #region Downloads
        private void UpdateProgress(Int64 BytesRead, Int64 TotalBytes)
        {
            // Calculate the download progress in percentages
            PercentProgress = Convert.ToInt32((BytesRead * 100) / TotalBytes);
            // Make progress on the progress bar
            this.CurrentProgBar.Width = Convert.ToInt32(Math.Round(PercentProgress / 100.0 * 374.0)); 
            // Display the current progress on the form
            this.percentLable.Text =  PercentProgress + "%" ;
            this.SpeedLable.Text = (Convert.ToDouble(BytesRead) / 1024 / sw.Elapsed.TotalSeconds).ToString("0.00") + " kb/s";
        }
        private void Download(object startPoint,String url,String destination,String filename){
            try
            {
                this.StatusLable.Text = "Downloading: "+filename;
                Downloading = true;
                //this.button1.Enabled = true;
                // Put the object argument into an int variable
                int startPointInt = Convert.ToInt32(startPoint);
                // Create a request to the file we are downloading
                webRequest = (HttpWebRequest)WebRequest.Create(url);
                // Set the starting point of the request
                webRequest.AddRange(startPointInt);

                // Set default authentication for retrieving the file
                webRequest.Credentials = CredentialCache.DefaultCredentials;
                // Retrieve the response from the server
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                // Ask the server for the file size and store it
                Int64 fileSize = webResponse.ContentLength;
                // Start the stopwatch which we will be using to calculate the download speed
                sw.Start();
 
                // Open the URL for download 
                strResponse = webResponse.GetResponseStream();

                // Create a new file stream where we will be saving the data (local drive)
                // Read from response and write to file
                fileStream = new FileStream(destination + @"\" + filename,FileMode.Create, FileAccess.Write, FileShare.None);
                
                // It will store the current number of bytes we retrieved from the server
                int bytesSize = 0;
                // A buffer for storing and writing the data retrieved from the server
                byte[] downBuffer = new byte[2048];

                // Loop through the buffer until the buffer is empty
                while ((bytesSize = strResponse.Read(downBuffer, 0, downBuffer.Length)) != 0)
                {
                    fileStream.Write(downBuffer, 0, bytesSize);
                    // Invoke the method that updates the form's label and progress bar
                    this.Invoke(new UpdateProgessCallback(this.UpdateProgress), new object[] { fileStream.Length, fileSize + startPointInt });

                }
                
            }
            finally
            {
                // When the above code has ended, close the streams
                strResponse.Close();
                fileStream.Close();
                
                //this.button1.Enabled = false;
            }
        }
        #endregion
       
        #region Generate MD5
        public string GetMD5HashFromFile(string fileName)
        {
            FileStream file = new FileStream(fileName, FileMode.Open);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
        #endregion

        #region Perform Downloading & Reading Patch Actions Here
        public bool checkPatchFile()
        {
            //this.label3.Text="Conncting to A3Ultimate Update Server ";
            try
            {
                WebClient PatchFile = new WebClient();
                PatchFile.DownloadFile(Host + "" + Patch, UpdatesPath + @"\" + Patch);
                PatchFile.Dispose();
            }
            catch (Exception e) {
                this.StatusLable.Text = "Unable to connect to a3 ultimate server!!";
                this.Close();
            }
            
            //Logger.Log("Patch file downloaded");
            return true;
        }
        public String readFile(String filename)
        {
            // Read the file as one string.
            System.IO.StreamReader myFile =
               new System.IO.StreamReader(filename);
            string myString = myFile.ReadToEnd();
             myFile.Close();
            return myString;
           
        }
        public void PatchCheker() {
            this.StatusLable.Text = "Checking Patch Information..";
            String abc = readFile(UpdatesPath + @"\" + Patch);
            fileArray = Regex.Split(abc, ";");
            for (int i = 0; i < fileArray.Length; i++) { fileArray[i] = fileArray[i].Trim(); }
           // MessageBox.Show("Number of Files to downloads are : "+fileArray[0], "Patch Downloaded Sucess Message !!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion

        #region Check The Patch
        public void CheckFiles()
        {
            int count = 0;
            this.TotalProgBar.Width = Convert.ToInt32(0);
            int TotalCount = Convert.ToInt32(fileArray[0]);
            this.StatusLable.Text = "Checking Client ... !!";
            for (int i = 1; i < TotalCount + 1; i++)
            {
                if (downloadingFlag == 0)
                {
                    string file = fileArray[i];
                    string[] FileInfo = file.Split('|');
                    string fileUrl = Host + "" + FileInfo[0].Replace("\r", "").Replace(@"\", "/");
                    string directoryfilename = path + @"\" + FileInfo[0];
                    this.StatusLable.Text = "Checking:" + "( " + i + "/" + TotalCount + " ) " + FileInfo[0];
                    double progrss = (i * 374 / TotalCount);
                    //  MessageBox.Show(Convert.ToString(Convert.ToInt32(progrss)));
                    double lable = progrss * 100 / 374;

                    this.percentLable.Text = "";
                    this.CurrentProgBar.Width = 0;
                    this.SpeedLable.Text = "";
                    if (File.Exists(directoryfilename))
                    {
                        //If file exsist thenpercentLablee hash and check for the hash
                        string patchHash = Convert.ToString(FileInfo[2]).Replace("\r", "");
                        string hash = GetMD5HashFromFile(directoryfilename);
                        //if hash matches ignore the file else start downloading it :) !!
                        if (string.Compare(hash, patchHash) == 0)
                        {
                            count++;

                        }
                        else
                        {
                            System.Threading.Thread.Sleep(500);

                            Download(0, fileUrl, UpdatesPath, FileInfo[0]);
                            extract(FileInfo[0], Convert.ToInt32(FileInfo[1]));
                            sw.Reset();
                            System.Threading.Thread.Sleep(500);
                        }

                    }
                    else
                    {
                        System.Threading.Thread.Sleep(500);

                        Download(0, fileUrl, UpdatesPath, FileInfo[0]);
                        extract(FileInfo[0], Convert.ToInt32(FileInfo[1]));
                        sw.Reset();
                        System.Threading.Thread.Sleep(500);
                    }
                    Downloading = false;
                    this.TotalProgBar.Width = Convert.ToInt32(progrss);
                    this.TotalLable.Text = Convert.ToInt32(lable) + "%";
                }
                else { goto End; }
            }
            End:
            this.StopButton.Enabled = false;
            this.FullCkeckButton.Enabled = true;
        }
        #endregion

        #region Clost The all webreq and reset form
        public void CloseAll()
        {

            if (Downloading)
            {
                webResponse.Close();
                strResponse.Close();
                fileStream.Close();
            }
            //backgroundWorker1.CancelAsync();
            backgroundWorker1.Dispose();
            
            this.StatusLable.Text = "Client Update Cancled!!";
            
            this.Start.Enabled = true;
            this.StopButton.Enabled = false;
            this.FullCkeckButton.Enabled = true;
            this.Start.Image = A3Downloader.Properties.Resources.startEnabled;
            this.SpeedLable.Text = "";
            this.percentLable.Text = "";
            this.TotalLable.Text = "";
            this.TotalProgBar.Width = 374;
            this.CurrentProgBar.Width = 374;
            downloadingFlag = 1;
        }
        #endregion

        #region Finished Download
        public void FinishDownload()
            {
            if (downloadFileList.Count != 0)
            {
                this.StatusLable.Text = "we need to download " + downloadFileList.Count+" no of files ";
            }
            else {

                this.StatusLable.Text = "Client is up to date";
                this.percentLable.Text = "";
                this.SpeedLable.Text = "";
                this.TotalLable.Text = "";
                this.TotalProgBar.Width = 374;
                this.CurrentProgBar.Width = 374;
                this.Start.Enabled = true;
                this.Start.Image = A3Downloader.Properties.Resources.startEnabled;
            }
        
        }
        #endregion

        #region Extract File
        public void extract(string file,int size)
        {
           // System.IO.Compression.
            this.StatusLable.Text="Extracting : "+ file;
            
            if (file.Contains(@"\"))
            {
               
                UncompressFile(UpdatesPath + @"\" + file, @"\Data", file,size);
              
            }
            else
            {
                UncompressFile(UpdatesPath + @"\" + file, "", file, size);
            }
               
               
        }
        public void UncompressFile(string path,string destination,string file,int size)
        {
            FileStream sourceFile = File.OpenRead(path);
            //MessageBox.Show(file);
            FileStream destinationFile = File.Create(file);
            //MessageBox.Show(destination + @"" + file);
            // Because the uncompressed size of the file is unknown, 
            // we are using an arbitrary buffer size.
            byte[] buffer = new byte[size];
            int n;

            using (GZipStream input = new GZipStream(sourceFile,
                CompressionMode.Decompress, false))
            {
                //Console.WriteLine("Decompressing {0} to {1}.", sourceFile.Name, destinationFile.Name);

                n = input.Read(buffer, 0, buffer.Length);
                destinationFile.Write(buffer, 0, n);
            }

            // Close the files.
            sourceFile.Close();
            destinationFile.Close();
            //Console.ReadKey();
        }
        #endregion

        #region Main Form Events 
        private void Updater_Load(object sender, EventArgs e)
        {
            this.pictureBox2.BackgroundImage = A3Downloader.Properties.Resources.bg1;
            this.pictureBox1.BackgroundImage = A3Downloader.Properties.Resources.bg1;
            this.CloseButton.BackgroundImage = A3Downloader.Properties.Resources.bg1;

            //if (File.Exists("unrar.dll"))
            //{
                //Check that whether it is alredy running or not ? 
                if (IsAlreadyRunning())
                {
                    MessageBox.Show("Another Instance is alredy running !!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error,MessageBoxDefaultButton.Button1 ,(MessageBoxOptions)0x40000);
                    Environment.Exit(0);
                }
                else
                {
                    if (!File.Exists(Directory.GetCurrentDirectory() + @"\Data\ulti.mate"))
                    {
                        Patch = "FullPatch.ini";
                    }
                    backgroundWorker1.RunWorkerAsync();
                }
                

           /* }
            else {

                MessageBox.Show("unrar.dll not found.\n Please Download Updated Patch From .\n " + "http://acp.a3ultimate.com/Downloads", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);

            }*/
            
        }

       
        private void Updater_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseAll();
          
        }
        private void Updater_Shown(object sender, EventArgs e)
        {

        }
        #endregion

        #region Start All Processing 
        public void StartAll() {
           this.StatusLable.Text = "Connecting to the A3 Ultimate Update Server";
           this.checkPatchFile();
           this.PatchCheker();
           this.CheckFiles();
           this.FinishDownload();
        }
       
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Directory.CreateDirectory("Updates");
            Directory.CreateDirectory("Updates/Data");
            this.FullCkeckButton.Enabled = false;
            this.StopButton.Enabled = true;
            this.Start.Enabled = false;
            this.Start.Image = A3Downloader.Properties.Resources.startDisabled;
            Random rand = new Random();
            int randome = rand.Next(3);
           // this.BackgroundImage = Images[randome];
            this.StartAll();
        }
        #endregion

        #region Buttone Events
        private void button1_Click(object sender, EventArgs e)
        {
            CloseAll();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Patch = "FullPatch.ini";
            downloadingFlag = 0;
            try
            {
                backgroundWorker1.RunWorkerAsync();
            }catch(Exception e1){
                MessageBox.Show("Error : "+e1.ToString());
            }
        }
        
        private void CloseButton_Click(object sender, EventArgs e)
        {
            CloseAll();
            
            DirectoryInfo directory = new DirectoryInfo("Updates");
            foreach (System.IO.FileInfo file in directory.GetFiles()) file.Delete();
            foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
            directory.Delete(true);
            System.Threading.Thread.Sleep(100);
            Environment.Exit(0);

        }

        private void CloseButton_MouseMove(object sender, MouseEventArgs e)
        {
            this.CloseButton.Image = A3Downloader.Properties.Resources.closehov;
        }

        private void CloseButton_MouseLeave(object sender, EventArgs e)
        {
            this.CloseButton.Image = A3Downloader.Properties.Resources.close;
        }

        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            this.pictureBox2.Image = A3Downloader.Properties.Resources.sethov;
        }

        private void pictureBox2_MouseLeave(object sender, EventArgs e)
        {
            this.pictureBox2.Image = A3Downloader.Properties.Resources.set;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            this.pictureBox1.Image = A3Downloader.Properties.Resources.minihov;
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            this.pictureBox1.Image = A3Downloader.Properties.Resources.mini;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void Start_Click(object sender, EventArgs e)
        {
            String client=Directory.GetCurrentDirectory()+@"\A3client.exe";
            if (File.Exists(client))
            {
                System.Diagnostics.Process.Start("A3Client.exe");
                Environment.Exit(0);
            }
            else 
            {
                MessageBox.Show("A3Client.exe Not Found Please Run Updater again!","Error!"); 
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Comming Soon!!", "Comming Soon!!");
        }
        private int mouseStartX, mouseStartY;
        private int formStartX, formStartY;
        private bool FormDragging = false;

        private void Updater_MouseDown(object sender, MouseEventArgs e)
        {
            this.mouseStartX = MousePosition.X;
            this.mouseStartY = MousePosition.Y;
            this.formStartX = this.Location.X;
            this.formStartY = this.Location.Y;
            FormDragging = true;

        }

        private void Updater_MouseMove(object sender, MouseEventArgs e)
        {
            if (FormDragging)
            {
                this.Location = new Point(
                this.formStartX + MousePosition.X - this.mouseStartX,
                this.formStartY + MousePosition.Y - this.mouseStartY
                );
            }
        }

        private void Updater_MouseUp(object sender, MouseEventArgs e)
        {
            FormDragging = false;
        }
        #endregion

        #region Links 
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://acp.a3ultimate.com/");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://acp.a3ultimate.com/ACP/");
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://acp.a3ultimate.com/Gallery/View/");
        }

        private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://acp.a3ultimate.com/Auction/View/");
        }

        private void linkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://forum.a3ultimate.com/");
        }

        private void linkLabel6_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://support.a3ultimate.com/");
        }

        private void linkLabel7_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://acp.a3ultimate.com/Downloads/"); 
        }

        private void linkLabel8_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://acp.a3ultimate.com/Register");
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestMonitorImage
{
    public partial class MainForm : Form
    {
        public static bool bUsePictureboxLoad = false;
        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var monitorPath = System.AppDomain.CurrentDomain.BaseDirectory;
            _fileWatcherEx = new FileSystemWatcherEx(monitorPath, "*.bmp", true, "", OnFileChanged, OnFileChanged, OnFileChanged);
            _fileWatcherEx.Start();
        }

        public void OnFileChanged(object sender, System.IO.FileSystemEventArgs e)
        {
            if (e.ChangeType == System.IO.WatcherChangeTypes.Created 
                || e.ChangeType == System.IO.WatcherChangeTypes.Changed)
            {
                this.Invoke(new MethodInvoker(delegate()
                {
                    try
                    {
                        if (bUsePictureboxLoad)
                        {
                            this.pictureBox1.Load(e.FullPath);
                        }
                        else
                        {
                            using (var imageStream = new FileStream(e.FullPath, FileMode.Open))
                            {
                                this.pictureBox1.Image = (Bitmap)Image.FromStream(imageStream);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(string.Format("!!!! {0}", ex.ToString()));
                    }
                }));
            }
        }

        private FileSystemWatcherEx _fileWatcherEx;
    }
}

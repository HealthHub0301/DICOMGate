namespace dicmgate
{
   using Optimums.DICOM07;
   using System;
   using System.ComponentModel;
   using System.Diagnostics;
   using System.Drawing;
   using System.IO;
   using System.Runtime.InteropServices;
   using System.Windows.Forms;

   public class Form1 : Form
   {
      private IContainer components = null;
      private ContextMenuStrip contextMenuStrip1;
      private ImageList imageList1;
      private ListBox listBox1;
      private ListView listView1;
      private NotifyIcon notifyIcon1;
      public int OldHeight;
      public int OldWidth;
      private Panel panel1;
      private Server server = null;
      private double timeout = 60000.0;
		private Timer timer1;

      public Form1()
      {
         this.InitializeComponent();
         this.OldWidth = base.Width;
         this.OldHeight = base.Height;
         string ip = "127.0.0.1";
         int port = 0x68;
         this.server = new Server();
         if (File.Exists("PACS.mdb"))
         {
            Server.ConnectionString = "Driver={Microsoft Access Driver (*.mdb)};DBQ=" + Environment.CurrentDirectory + @"\PACS.mdb";
         }

         string sErrorMsg = "";

         try
         {
            string str2;
            StreamReader reader = new StreamReader("server.ini");
            while ((str2 = reader.ReadLine()) != null)
            {
               string[] strArray = str2.Split(new char[] { '=' });
               if ((((strArray.Length >= 2) && (str2.Length >= 2)) && (str2[0] >= 'A')) && (str2[0] <= 'z'))
               {
                  if (strArray[0] == "AE_TITLE")
                  {
                     this.server.AETitle = strArray[1];
                  }
                  else if (strArray[0] == "MAX_PDU_LENGTH")
                  {
                     this.server.MaxLength = uint.Parse(strArray[1]);
                  }
                  else if (strArray[0] == "SAVE_DIRECTORY")
                  {
                     this.server.SaveDirectory = strArray[1];
                  }
                  else if (strArray[0] == "INTERVAL")
                  {
                     this.timer1.Interval = int.Parse(strArray[1]);
                  }
                  else if (strArray[0] == "DSN")
                  {
                     Server.ConnectionString = str2;
                  }
                  else if (strArray[0] == "SERVER")
                  {
                     Server.ConnectionString = "Driver={SQL Server};SERVER=" + strArray[1] + ";UID=pacsa;PWD=apacs~;Database=PACS";
                  }
                  else if (strArray[0] == "BASE_DIRECTORIES")
                  {
                     Server.BaseDirs = strArray[1].Split(new char[] { ';' });
                  }
                  else if (strArray[0] == "IP_ADDRESS")
                  {
                     ip = strArray[1];
                  }
                  else if (strArray[0] == "PORT")
                  {
                     port = int.Parse(strArray[1]);
                  }
                  else if (strArray[0] == "LOOP_COUNT")
                  {
                     Server.LoopCount = int.Parse(strArray[1]);
                  }
                  else if (strArray[0] == "TIME_OUT")
                  {
                     this.timeout = double.Parse(strArray[1]);
                  }
                  else if (strArray[0] == "CALLING_TITLE")
                  {
                     Server.AssociateList.Add(strArray[1]);
                  }
                  else if (strArray[0] == "CHARACTER_SET")
                  {
                     Server.CharacterSet = "|" + strArray[1] + "|";
                  }
                  else if (strArray[0] == "SITE_ID")
                  {
                     Server.SiteId = strArray[1];
                  }
                  else if (strArray[0] == "ORDER_QUERY")
                  {
                     Server.OrderQuery = str2.Substring(12);
                  }
                  else if (strArray[0] == "SHOW")
                  {
                     if (strArray[1] == "0")
                     {
                        base.ShowInTaskbar = false;
                        base.WindowState = FormWindowState.Minimized;
                     }
                     else
                     {
                        this.notifyIcon1.Visible = false;
                     }
                  }
               }
            }
            reader.Close();
            if (Server.AssociateList.Count > 0)
            {
               int num2 = ip.LastIndexOf('.');
               str2 = ip.Substring(0, num2 + 1);
               for (num2 = 1; num2 < 0xff; num2++)
               {
                  Server.AssociateList.Add(str2 + num2);
               }
            }
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            if (commandLineArgs.Length >= 3)
            {
               ip = commandLineArgs[1];
               port = int.Parse(commandLineArgs[2]);
            }
            Process[] processesByName = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            if ((processesByName != null) && (processesByName.Length > 1))
            {
               string windowName = this.Text + " - " + port;
               if (FindWindow(null, windowName) != IntPtr.Zero)
               {
                  MessageBox.Show("Exit");
                  this.notifyIcon1.Dispose();
                  Environment.Exit(0);
               }
               this.Text = windowName;
            }

            sErrorMsg = ip + ":" + port.ToString();

            this.server.Listen(ip, port, 100);
         }
         catch (Exception exception)
         {
            MessageBox.Show(this, exception.ToString(), sErrorMsg);

            Console.WriteLine(exception.ToString());
            this.notifyIcon1.Dispose();

            Environment.Exit(0);
         }
      }

      protected override void Dispose(bool disposing)
      {
         if (disposing && (this.components != null))
         {
            this.components.Dispose();
         }
         base.Dispose(disposing);
      }

      [DllImport("User32.dll", CharSet = CharSet.Auto)]
      public static extern IntPtr FindWindow(string className, string windowName);
      private void Form1_FormClosing(object sender, FormClosingEventArgs e)
      {
      }

      private void Form1_SizeChanged(object sender, EventArgs e)
      {
         if (!(base.ShowInTaskbar || (base.WindowState != FormWindowState.Minimized)))
         {
            base.Visible = false;
         }
         else if ((base.Width >= 640) && (base.Height >= 480))
         {
            this.panel1.Width += base.Width - this.OldWidth;
            this.panel1.Height += base.Height - this.OldHeight;
            this.listView1.Width += base.Width - this.OldWidth;
            this.listView1.Top += base.Height - this.OldHeight;
            this.OldWidth = base.Width;
            this.OldHeight = base.Height;
         }
      }

      private void InitializeComponent()
      {
			this.components = new System.ComponentModel.Container();
			this.listBox1 = new System.Windows.Forms.ListBox();
			this.listView1 = new System.Windows.Forms.ListView();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.panel1 = new System.Windows.Forms.Panel();
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// listBox1
			// 
			this.listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBox1.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.listBox1.FormattingEnabled = true;
			this.listBox1.HorizontalScrollbar = true;
			this.listBox1.ItemHeight = 15;
			this.listBox1.Location = new System.Drawing.Point(0, 0);
			this.listBox1.Name = "listBox1";
			this.listBox1.Size = new System.Drawing.Size(608, 256);
			this.listBox1.TabIndex = 0;
			// 
			// listView1
			// 
			this.listView1.Alignment = System.Windows.Forms.ListViewAlignment.Left;
			this.listView1.LargeImageList = this.imageList1;
			this.listView1.Location = new System.Drawing.Point(12, 274);
			this.listView1.Name = "listView1";
			this.listView1.Size = new System.Drawing.Size(608, 167);
			this.listView1.TabIndex = 1;
			this.listView1.UseCompatibleStateImageBehavior = false;
			// 
			// imageList1
			// 
			this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
			this.imageList1.ImageSize = new System.Drawing.Size(16, 16);
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// timer1
			// 
			this.timer1.Enabled = true;
			this.timer1.Interval = 3000;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.listBox1);
			this.panel1.Location = new System.Drawing.Point(12, 12);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(608, 256);
			this.panel1.TabIndex = 2;
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
			// 
			// notifyIcon1
			// 
			this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
			this.notifyIcon1.Text = "DICOM Gateway";
			this.notifyIcon1.Visible = true;
			// 
			// Form1
			// 
			this.ClientSize = new System.Drawing.Size(632, 453);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.listView1);
			this.Name = "Form1";
			this.Text = "DICOM Gateway - Optimum Solution";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
			this.SizeChanged += new System.EventHandler(this.Form1_SizeChanged);
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

      }

      private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
      {
         if (!base.Visible)
         {
            base.Visible = true;
         }
         base.WindowState = FormWindowState.Normal;
      }

      private void timer1_Tick(object sender, EventArgs e)
      {
         try
         {
            if ((this.server != null) && this.server.IsBound)
            {
               string str;
               this.listView1.Items.Clear();
               for (int i = 0; i < this.server.ClientCount; i++)
               {
                  Client client = this.server.GetClient(i);
                  if (client != null)
                  {
                     if (client.Connected)
                     {
                        if (DateTime.Now.Subtract(client.LastReceiveTime).TotalMilliseconds > this.timeout)
                        {
                           client.Abort();
                        }
                        else
                        {
                           this.listView1.Items.Add(client.CallingEntityTitle + "[" + client.RemoteAddress + "]", 1);
                        }
                     }
                     else
                     {
                        this.listView1.Items.Add(client.LastReceiveTime.ToString(), 2);
                     }
                  }
               }
               while ((str = Network.GetMessage()) != null)
               {
                  if (this.listBox1.Items.Count > 0x3e8)
                  {
                     this.listBox1.Items.Clear();
                  }
                  this.listBox1.SelectedIndex = this.listBox1.Items.Add(str);
                  Console.WriteLine(str);
               }
            }
         }
         catch (Exception exception)
         {
            Console.WriteLine(exception.ToString());
         }
      }
	}
}


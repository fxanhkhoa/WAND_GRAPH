﻿using _DCubeNoGimbalLock;
using CPI.Plot3D;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WAND_GRAPH
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            m_oWorker = new BackgroundWorker();
            m_oWorker.DoWork += new DoWorkEventHandler(m_oWorker_DoWork);
            m_oWorker.ProgressChanged += new ProgressChangedEventHandler(m_oWorker_ProgressChanged);
            m_oWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_oWorker_RunWorker_Completed);
            m_oWorker.WorkerReportsProgress = true;
            m_oWorker.WorkerSupportsCancellation = true;
            /*************************************************************************
            *                       Change Cursor
            *************************************************************************/
            try
            {
                this.Cursor = AdvancedCursors.Create(Path.Combine(Application.StartupPath, "RadarBusy.ani"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            Application.EnableVisualStyles();
            //Application.Run(new Form1());

            /*************************************************************************
            *                       DRAW 3D              
            *************************************************************************/
            
        }
       
        private void ChangeCursor(string curFile)
        {
            //Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Cursors\", "Arrow", curFile);
            //SystemParametersInfo(SPI_SETCURSORS, 0, null, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }

        const int SPI_SETCURSORS = 0x0057;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDCHANGE = 0x02;

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo")]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, uint? pvParam, uint fWinIni);
        /*************************************************************************
        *                       DEFINE SPACE
        *************************************************************************/
        float MAX_PITCH = 110,
              MIN_PITCH = 70,
              MAX_YAW = 90,
              MIN_YAW = 20,
              PITCH_CENTER = 0,
              YAW_CENTER = 0,
              RANGE = 20;
        public const UInt16 RES_X = 1366,
                            RES_Y = 768;
        int flip = 0;
        public const int FIRST_POINT = 1,
                         END_POINT = 2;   
        /************************************************************
        *                  Mouse and Keyboard Hook 
        ************************************************************/
        RamGecTools.MouseHook mouseHook = new RamGecTools.MouseHook();
        RamGecTools.KeyboardHook keyboardHook = new RamGecTools.KeyboardHook();
        /************************************************************
        *                  Import User32 & function
        ************************************************************/
        [DllImport("User32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("User32.dll")]
        static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
        /************************************************************
        *                  Get Pen, Point for Drawing
        ************************************************************/
        Pen p = new Pen(Color.RosyBrown, 1);
        Point sp = new Point(1386 / 2, 768 / 2);
        Point ep = new Point(1368 / 2, 768 / 2);
        int Radius = 5;
        float pitch, yaw;
        Symbol symbol = new Symbol();
        /************************************************************
        *                  Get Graphic
        ************************************************************/
        private Graphics g_desktop = null;
        /************************************************************
        *                  Get Desktop
        ************************************************************/
        IntPtr desktop = GetDC(IntPtr.Zero);
        /************************************************************
        *                  Serial Port
        ************************************************************/
        static SerialPort Serial_Port;
        string w, data = "";
        int k = 0;
        int count = 0, received_flag = 1, got_data = 0, good;
        string pair_str = "";
        int outvar = 0;
        /*************************************************************************
        *                       BACKGROUND WORKER
        *************************************************************************/
        BackgroundWorker m_oWorker;
        int i;
        /*************************************************************************
        *                       DRAW 3D
        *************************************************************************/
        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, System.Int32 dwRop);

        Math3D.Cube mainCube;
        Point drawOrigin;

        private void button1_Click(object sender, EventArgs e)
        {
            m_oWorker.RunWorkerAsync();
            mouseHook.MouseMove += new RamGecTools.MouseHook.MouseHookCallback(mouseHook_MouseMove);
            mouseHook.LeftButtonDown += new RamGecTools.MouseHook.MouseHookCallback(mouseHook_LeftButtonDown);
            mouseHook.LeftButtonUp += new RamGecTools.MouseHook.MouseHookCallback(mouseHook_LeftButtonUp);
            mouseHook.RightButtonDown += new RamGecTools.MouseHook.MouseHookCallback(mouseHook_RightButtonDown);
            mouseHook.RightButtonUp += new RamGecTools.MouseHook.MouseHookCallback(mouseHook_RightButtonUp);
            mouseHook.MiddleButtonDown += new RamGecTools.MouseHook.MouseHookCallback(mouseHook_MiddleButtonDown);
            mouseHook.MiddleButtonUp += new RamGecTools.MouseHook.MouseHookCallback(mouseHook_MiddleButtonUp);
            mouseHook.MouseWheel += new RamGecTools.MouseHook.MouseHookCallback(mouseHook_MouseWheel);

            mouseHook.Install();
            //ChangeCursor(@"%SystemRoot%\cursors\RadarPrecision.cur");
            /************************************************************
            *                  Get Desktop
            ************************************************************/
            g_desktop = Graphics.FromHdc(desktop);
            /************************************************************
             *                 Global point
             ************************************************************/
            //win32.Win32.POINT pos = new win32.Win32.POINT();
            //pos.x = 0;
            //pos.y = 0;
            //win32.Win32.ClientToScreen(desktop, ref pos);
            //win32.Win32.SetCursorPos(pos.x, pos.y);
            /************************************************************
            *                 KeyBoard Hook
            ************************************************************/
            keyboardHook.KeyDown += new RamGecTools.KeyboardHook.KeyboardHookCallback(keyboardHook_KeyDown);
            keyboardHook.KeyUp += new RamGecTools.KeyboardHook.KeyboardHookCallback(keyboardHook_KeyUp);

            keyboardHook.Install();
            /************************************************************
             *                  Serial port 
             ************************************************************/
            Serial_Port = new SerialPort();
            if (Serial_Port.IsOpen)
            {
                Serial_Port.Close();
            }
            Serial_Port.PortName = "COM3";
            Serial_Port.BaudRate = 9600;
            Serial_Port.DataBits = 8;
            Serial_Port.Parity = Parity.None;
            Serial_Port.StopBits = StopBits.One;
            try
            {
                Serial_Port.Open();
                //Serial_Port.Write("AT");
                //Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            try
            {
                Serial_Port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        void m_oWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                for (i = 0; i < 100; i++)
                {
                    //textBox1.Text = i.ToString();
                    Thread.Sleep(50);
                    m_oWorker.ReportProgress(i);
                    if (m_oWorker.CancellationPending)
                    {
                        e.Cancel = true;
                        m_oWorker.ReportProgress(0);
                        return;
                    }
                }
            }
        }
        void m_oWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            
            if ((data != null) ) 
            {
                if (data.IndexOf("*") != -1)
                {
                    try
                    {
                        pitch = Get_Pitch_val(data);
                        yaw = Get_Yaw_val(data);
                        //Invoke(new Action(() => Bankinh.Text = pitch.ToString() + yaw.ToString()));
                        if ((pitch != -1) && (yaw != -1)) got_data = 1;
                        else
                        {
                            got_data = 0;
                        }
                    }
                    catch
                    {
                        //Invoke(new Action(() => label1.Text = pitch.ToString()));
                    }
                    //Data_Output.Text = i.ToString();
                    if (received_flag == 1)
                    {

                        Invoke(new Action(new Action(() => Bankinh.Text = YAW_CENTER.ToString())));
                        if ((pitch != -1) && (yaw != -1) && (count > 0))
                        {
                            win32.Win32.POINT pos = new win32.Win32.POINT();

                            pos = CalculatePos(pitch, yaw);
                            Invoke(new Action(new Action(() => Bankinh.Text = pos.y.ToString())));
                            if (((pos.x - sp.X) > Radius) || ((pos.y - sp.Y) > Radius))
                            {
                                if (flip == 1)
                                {
                                    win32.Win32.ClientToScreen(desktop, ref pos);
                                    win32.Win32.SetCursorPos(pos.y, pos.x);
                                }
                                else
                                {
                                    win32.Win32.ClientToScreen(desktop, ref pos);
                                    win32.Win32.SetCursorPos(RES_Y - pos.y, pos.x);
                                }
                            }

                            ep.X = pos.x;
                            ep.Y = pos.y;
                            //Invoke(new Action(new Action(() => mouseLog.Text = pitch.ToString())));
                            //Invoke(new Action(new Action(() => keyboardLog.Text = pos.x.ToString())));
                            //xPosLabel.Text = (1 - ((pitch - (MIN_PITCH)) / (PITCH_CENTER - MIN_PITCH))).ToString();
                            //yPosLabel.Text = (1 - ((pitch - (MIN_PITCH)) / (PITCH_CENTER - MIN_PITCH))).ToString();
                            //g_desktop.DrawLine(p, sp, ep);
                            sp = ep;

                            /**************************************
                             *           Put to Graph
                             **************************************/

                            chart1.Series["Series1"].Points.Add(pitch);
                            chart1.Series["Series2"].Points.Add(yaw);

                            chart1.Series["Series1"].ChartType = SeriesChartType.Spline;
                            chart1.Series["Series1"].Color = Color.Red;

                            chart1.Series["Series2"].ChartType = SeriesChartType.Spline;
                            chart1.Series["Series2"].Color = Color.Blue;

                            chart1.Series["Series3"].ChartType = SeriesChartType.Spline;
                            chart1.Series["Series3"].Color = Color.Yellow;
                            /**************************************
                             *           Render cube
                             **************************************/
                            Render(pitch - PITCH_CENTER, yaw - YAW_CENTER, 0);

                        }
                        else if ((count == 0) && (pitch != -1) && (yaw != -1) && (pitch < 90) && (yaw < 300))
                        {
                            getMAX_MIN(pitch, yaw);
                            count = 1;
                        }
                        //received_flag = 0;
                    }
                    else if ((received_flag == 2))
                    {
                        Symbol.axis p = new Symbol.axis();
                        if (symbol.current_pos == 0)
                        {
                            float radius = (float)Math.Sqrt((pitch - PITCH_CENTER) * (pitch - PITCH_CENTER) + (yaw - YAW_CENTER) * (yaw - YAW_CENTER));
                            symbol.setRADIUS(radius);
                            p.x = YAW_CENTER;
                            p.y = PITCH_CENTER;
                            symbol.setCENTER(p);
                            Invoke(new Action(() => label1.Text = symbol.point[0].x.ToString()));
                        }
                        p.x = yaw;
                        p.y = pitch;
                        symbol.add(p);
                        /******* Check Circle *******/
                        if (symbol.Check_Circle())
                        {
                            //Invoke(new Action(() => Percentage.Text ="Circle " + symbol.PERCENTAGE_CIRCLE.ToString()));
                            //Invoke(new Action(() => Percentage.Text = "TRUE"));
                        }
                        else
                        {
                            float a = (float)Math.Pow(symbol.point[symbol.current_pos - 1].x - symbol.CENTRAL_POINT.x, 2);
                            float b = (float)Math.Pow(symbol.point[symbol.current_pos - 1].y - symbol.CENTRAL_POINT.y, 2);
                            //Invoke(new Action(() => Percentage.Text = "OUT SIDE"));
                            //Invoke(new Action(() => Bankinh.Text = (a + b).ToString()));
                        }
                        /******* Check Vertical *******/
                        if (symbol.Check_Vertical() == true)
                        {
                            //Invoke(new Action(() => Bankinh.Text = "Vertical " + symbol.PERCENTAGE_VERTICAL.ToString()));
                            Invoke(new Action(() => label2.Text = "Vertical "));
                        }
                        else
                        {
                            Invoke(new Action(() => label2.Text = " "));
                        }
                        /******* Check Horizontal *******/
                        if (symbol.Check_Horizontal() == true)
                        {
                            //Invoke(new Action(() => Bankinh.Text = "Horizontal " + symbol.PERCENTAGE_HORIZONTAL.ToString()));
                            Invoke(new Action(() => label3.Text = "Horizontal "));
                        }
                        else
                        {
                            Invoke(new Action(() => label3.Text = " "));
                        }
                    }
                    else if (received_flag == 3)
                    {
                        float max = symbol.PERCENTAGE_CIRCLE;
                        if (max < symbol.PERCENTAGE_HORIZONTAL)
                        {
                            max = symbol.PERCENTAGE_HORIZONTAL;
                            Invoke(new Action(() => label2.Text = "horizontal detected"));
                        }
                        if (max < symbol.PERCENTAGE_VERTICAL)
                        {
                            max = symbol.PERCENTAGE_VERTICAL;
                            Invoke(new Action(() => label3.Text = "vertical detected"));
                        }
                        Invoke(new Action(() => label1.Text = max.ToString()));
                        received_flag = 1;
                    }
                    data = "";
                    Invoke(new Action(() => label1.Text = symbol.point[0].x.ToString()));
                }
            }
        }
        void m_oWorker_RunWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {

        }
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serial_port = (SerialPort)sender;
            w = serial_port.ReadExisting();
            //w = serial_port.ReadChar().ToString();
            data += w;
            pair_str += w;
            if (data[data.Length - 1] == '*')
                Invoke(new Action(new Action(() => Percentage.Text = data)));
            else
                Invoke(new Action(new Action(() => Percentage.Text = "")));
            //w += "\n";
            if (outvar == 0)
            {
                if (Check_AT("20C38FF68CEB") == 1)
                    if (Check_AT("OK+DISCE") == 1)
                        Serial_Port.Write("AT+CON20C38FF68CEB");
                if (Check_AT("OK+CONNA") == 1)
                {
                    MessageBox.Show("paired");
                    outvar = 1;
                }
            }
            {
                switch (Check_Status(data))
                {
                    case FIRST_POINT:
                        received_flag = 2;
                        Invoke(new Action(new Action(() => label1.Text = "FIRST POINT")));
                        break;
                    case END_POINT:
                        received_flag = 3;
                        Invoke(new Action(new Action(() => label1.Text = "END POINT")));
                        break;
                    default:
                       //received_flag = 1;
                        break;
                };
            }
        }
        void mouseHook_MouseWheel(RamGecTools.MouseHook.MSLLHOOKSTRUCT mouseStruct)
        {
            //mouseLog.Text = "[" + DateTime.Now.ToLongTimeString() + "] MouseWheel Event" + Environment.NewLine + mouseLog.Text;
        }
        void mouseHook_MiddleButtonUp(RamGecTools.MouseHook.MSLLHOOKSTRUCT mouseStruct)
        {
            //mouseMiddleButton.BackColor = Color.White;
            //mouseLog.Text = "[" + DateTime.Now.ToLongTimeString() + "] MiddleButtonUp Event" + Environment.NewLine + mouseLog.Text;
        }

        void mouseHook_MiddleButtonDown(RamGecTools.MouseHook.MSLLHOOKSTRUCT mouseStruct)
        {
            //mouseMiddleButton.BackColor = Color.IndianRed;
            //mouseLog.Text = "[" + DateTime.Now.ToLongTimeString() + "] MiddleButtonDown Event" + Environment.NewLine + mouseLog.Text;
        }

        void mouseHook_RightButtonUp(RamGecTools.MouseHook.MSLLHOOKSTRUCT mouseStruct)
        {
            //mouseRightButton.BackColor = Color.White;
            //mouseLog.Text = "[" + DateTime.Now.ToLongTimeString() + "] RightButtonUp Event" + Environment.NewLine + mouseLog.Text;
        }

        void mouseHook_RightButtonDown(RamGecTools.MouseHook.MSLLHOOKSTRUCT mouseStruct)
        {
            //mouseRightButton.BackColor = Color.IndianRed;
            //mouseLog.Text = "[" + DateTime.Now.ToLongTimeString() + "] RightButtonDown Event" + Environment.NewLine + mouseLog.Text;
        }

        void mouseHook_LeftButtonUp(RamGecTools.MouseHook.MSLLHOOKSTRUCT mouseStruct)
        {
            //mouseLeftButton.BackColor = Color.White;
            //mouseLog.Text = "[" + DateTime.Now.ToLongTimeString() + "] LeftButtonUp Event" + Environment.NewLine + mouseLog.Text;
        }

        private void Bankinh_TextChanged(object sender, EventArgs e)
        {
            //SetRadius(Convert.ToInt16(this.Text));
        }

        void mouseHook_LeftButtonDown(RamGecTools.MouseHook.MSLLHOOKSTRUCT mouseStruct)
        {
            //mouseLeftButton.BackColor = Color.IndianRed;
            //mouseLog.Text = "[" + DateTime.Now.ToLongTimeString() + "] LeftButtonDown Event" + Environment.NewLine + mouseLog.Text;
        }

        void mouseHook_MouseMove(RamGecTools.MouseHook.MSLLHOOKSTRUCT mouseStruct)
        {
            //xPosLabel.Text = mouseStruct.pt.x.ToString();
            //yPosLabel.Text = mouseStruct.pt.y.ToString();
            /************************************************************
               *                 Drawline
            ************************************************************/
            ep.X = mouseStruct.pt.x;
            ep.Y = mouseStruct.pt.y;
            g_desktop.DrawLine(p, sp, ep);
            sp = ep;
        }
        void keyboardHook_KeyUp(RamGecTools.KeyboardHook.VKeys key)
        {
            //keyboardKeyPress.BackColor = Color.White;
            //keyboardLog.Text = "[" + DateTime.Now.ToLongTimeString() + "] KeyUp Event {" + key.ToString() + "}" + Environment.NewLine + keyboardLog.Text;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            mainCube = new Math3D.Cube(100, 200, 75);
            drawOrigin = new Point(pictureBox1.Width / 2, pictureBox1.Height / 2);

            Render(0, 0, 0);
        }

        void keyboardHook_KeyDown(RamGecTools.KeyboardHook.VKeys key)
        {

            //keyboardKeyPress.BackColor = Color.IndianRed;
            //keyboardLog.Text = "[" + DateTime.Now.ToLongTimeString() + "] KeyDown Event {" + key.ToString() + "}" + Environment.NewLine + keyboardLog.Text;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Serial_Port.Write("AT");
            Thread.Sleep(1000);
            if (Check_AT("OK") == 1)
                //Bankinh.Text = "AT";
            pair_str = "";
            Serial_Port.Write("AT+DISC?");
            Thread.Sleep(1000);
            if (Check_AT("OK+DISCE") == 1)
                //MessageBox.Show("OK+DISCE");
            if (Check_AT("OK+CON") == 1)
                MessageBox.Show("Paired");
        }

        private float Get_Pitch_val(string s)
        {
            try
            {
                int i = s.IndexOf("Pitch ");

                if (i == -1) return -1;
                int j = i + 6;
                //string temp = s.Substring(i, 10);// get 1st 10 characters
                while (s[j] != ' ')
                {
                    j++;
                }
                string temp = s.Substring(i + 6, (j - i - 6));
                return float.Parse(temp);
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        private void Fill_Check_CheckedChanged(object sender, EventArgs e)
        {
            mainCube.FillBack = Fill_Check.Checked;
            //mainCube.FillBottom = Fill_Check.Checked;
            //mainCube.FillFront = Fill_Check.Checked;
            mainCube.FillLeft = Fill_Check.Checked;
            mainCube.FillRight = Fill_Check.Checked;
            mainCube.FillTop = Fill_Check.Checked;

            this.Refresh();
        }

        private void Fill_Check_Paint(object sender, PaintEventArgs e)
        {
            Render(pitch - PITCH_CENTER, yaw - YAW_CENTER, 0);
        }

        private float Get_Yaw_val(string s)
        {
            try
            {
                int i = s.IndexOf("Yaw ");
                if (i == -1) return -1;
                int j = i + 4;
                //string temp = s.Substring(i, 10);// get 1st 10 characters
                while (s[j] != ' ')
                {
                    j++;
                }
                string temp = s.Substring(i + 4, (j - i - 4));
                return float.Parse(temp);
            }
            catch (Exception ex)
            {
                return -1;
            }
        }
        private void SetRadius(int radius)
        {
            Radius = radius;
        }
        private void getMAX_MIN(float pitch, float yaw)
        {
            PITCH_CENTER = pitch;
            MAX_PITCH = pitch + RANGE;
            MIN_PITCH = pitch - RANGE;

            YAW_CENTER = yaw;
            if (yaw + RANGE > 360)
            {
                MAX_YAW = RANGE - (360 - yaw);
            }
            else MAX_YAW = RANGE + yaw;
            if (yaw - RANGE < 0)
            {
                MIN_YAW = 360 - (RANGE - yaw);
            }
            else MIN_YAW = yaw - RANGE;
            //Max.Text = MAX_PITCH.ToString();
            //Min.Text = PITCH_CENTER.ToString();
            count = 1;
        }
        win32.Win32.POINT CalculatePos(float pitch, float yaw)
        {
            win32.Win32.POINT pos = new win32.Win32.POINT();

            if (pitch >= PITCH_CENTER)
                pos.x = (RES_Y / 2) + (int)((((pitch - PITCH_CENTER) / (RANGE))) * (RES_Y / 2));
            else
                pos.x = (int)((((pitch - (MIN_PITCH)) / (RANGE))) * (RES_Y / 2));
            if (pos.x == 0) pos.x = 1 * (RES_Y / 2);
            if (flip == 0)
            {
                if (YAW_CENTER + RANGE > 360) // Case YAW_CENTER is at left of 360
                {
                    if ((Math.Abs(yaw - YAW_CENTER) > RANGE) || (yaw > YAW_CENTER))
                    {
                        if (yaw - YAW_CENTER < 0)
                        {
                            float temp = 360 - YAW_CENTER + yaw;
                            pos.y = (int)((temp / RANGE) * (RES_X / 2));
                            pos.y += RES_X / 2;
                        }
                        else
                        {
                            float temp = yaw - YAW_CENTER;
                            pos.y = (int)((temp / RANGE) * (RES_X / 2));
                            pos.y += RES_X / 2;
                        }
                    }
                    else
                    {
                        float temp = YAW_CENTER - yaw;
                        pos.y = (int)((1 - (temp / RANGE)) * (RES_X / 2));
                    }
                }
                else
                {
                    if ((yaw + YAW_CENTER > 360) || (yaw < YAW_CENTER))
                    {
                        if (YAW_CENTER - yaw < 0)
                        {
                            float temp = yaw - MIN_YAW;
                            pos.y = (int)((temp / RANGE) * (RES_X / 2));
                        }
                        else
                        {
                            float temp = YAW_CENTER - yaw;
                            pos.y = (int)((1 - (temp / RANGE)) * (RES_X / 2));
                        }
                    }
                    else
                    {
                        float temp = yaw - YAW_CENTER;
                        pos.y = (int)((temp / RANGE) * (RES_X / 2));
                        pos.y += (RES_X / 2);
                    }
                }
            }
            else if (flip == 1)
            {
                if ((YAW_CENTER < 360) && (YAW_CENTER > 180)) // In the half left
                {
                    if (YAW_CENTER + RANGE < 360) // Normal
                    {
                        if (yaw > YAW_CENTER) // Turn left
                        {
                            float temp = Math.Abs(yaw - MAX_YAW);
                            pos.y = (int)((temp / RANGE) * (RES_X / 2));
                        }
                        else if ((yaw < YAW_CENTER) && (yaw > MIN_YAW)) // turn right
                        {
                            float temp = Math.Abs(yaw - YAW_CENTER);
                            pos.y = (int)((temp / RANGE) * (RES_X / 2));
                            pos.y += (RES_X / 2);
                        }
                    }
                    else if (YAW_CENTER + RANGE > 360) // out of RANGE
                    {
                        if ((yaw < YAW_CENTER) && (yaw > MIN_YAW)) // turn right
                        {
                            float temp = Math.Abs(yaw - YAW_CENTER);
                            pos.y = (int)((temp / RANGE) * (RES_X / 2));
                            pos.y += (RES_X / 2);
                        }
                        else // turn left
                        {
                            if ((yaw < 360) && (yaw > YAW_CENTER)) // still inside [YAW_CENTER, 360]
                            {
                                float temp = Math.Abs(yaw - YAW_CENTER);
                                pos.y = (int)((temp / RANGE) * (RES_X / 2));
                            }
                            else // outside [YAW_CENTER, 360]
                            {
                                float temp = Math.Abs(360 - YAW_CENTER) + yaw;
                                pos.y = (int)((temp / RANGE) * (RES_X / 2));
                            }
                        }
                    }
                }
                else if ((YAW_CENTER > 0) && (YAW_CENTER < 180)) // In the half right
                {
                    if (YAW_CENTER - RANGE > 0) // normal
                    {
                        if (yaw > YAW_CENTER) // Turn left
                        {
                            float temp = Math.Abs(yaw - MAX_YAW);
                            pos.y = (int)((temp / RANGE) * (RES_X / 2));
                        }
                        else if ((yaw < YAW_CENTER) && (yaw > MIN_YAW)) // turn right
                        {
                            float temp = Math.Abs(yaw - YAW_CENTER);
                            pos.y = (int)((temp / RANGE) * (RES_X / 2));
                            pos.y += (RES_X / 2);
                        }
                    }
                    else if (YAW_CENTER - RANGE < 0) // out of RANGE
                    {
                        if (yaw > YAW_CENTER) // Turn left
                        {
                            float temp = Math.Abs(yaw - YAW_CENTER);
                            pos.y = (int)((temp / RANGE) * (RES_X / 2));
                        }
                        else // Turn right
                        {
                            if ((yaw > 0) && (yaw < YAW_CENTER)) // still inside [0, YAW_CENTER]
                            {
                                float temp = Math.Abs(yaw - YAW_CENTER);
                                pos.y = (int)((temp / RANGE) * (RES_X / 2));
                                pos.y += (RES_X / 2);
                            }
                            else // outside [0, YAW_CENTER]
                            {
                                float temp = YAW_CENTER + Math.Abs(360 - yaw);
                                pos.y = (int)((temp / RANGE) * (RES_X / 2));
                                pos.y += (RES_X / 2);
                            }
                        }
                    }
                }
            }
            else if (flip == 2)
            {

            }
            return pos;
        }
        public void DrawSquare(Plotter3D p, float sideLength)
        {
            for (int i = 0; i < 4; i++)
            {
                p.Forward(sideLength);  // Draw a line sideLength long
                p.TurnRight(90);        // Turn right 90 degrees
            }
        }
        private void DrawRotatedSquare(Plotter3D p, float sideLength, float rotationAngle)
        {
            // Since we don't want to draw while repositioning ourselves at the
            // center of the object, we'll lift the pen up
            p.PenUp();

            // Move to the center of the square
            p.Forward(sideLength / 2);
            p.TurnRight(90);
            p.Forward(sideLength / 2);
            p.TurnLeft(90);

            // Now we rotate as much as we want
            p.TurnRight(rotationAngle);

            // Now we retrace our steps to get back
            // to the (rotated) starting point
            p.TurnLeft(90);
            p.Forward(sideLength / 2);
            p.TurnLeft(90);
            p.Forward(sideLength / 2);
            p.TurnRight(180);

            // Put the pen back down, so we start drawing again
            p.PenDown();

            // Finally we draw the square as we normally would
            DrawSquare(p, sideLength);
        }
        public void DrawCube(Plotter3D p, float sideLength)
        {
            for (int i = 0; i < 4; i++)
            {
                DrawSquare(p, sideLength);
                p.Forward(sideLength);
                p.TurnDown(90);
            }
        }
        private void Render(float tX, float tY, float tZ)
        {
            mainCube.RotateX = (float)tX;
            mainCube.RotateY = (float)tY;
            mainCube.RotateZ = (float)tZ;

            pictureBox1.Image = mainCube.DrawCube(drawOrigin);
        }
        private int Check_Status(string s)
        {
            if (s.IndexOf("1st Point") > -1)
                return FIRST_POINT;
            else if (s.IndexOf("End Point") > -1)
                return END_POINT;
            else return 0;
        }
        private int Check_AT(string s)
        {
            if (pair_str.IndexOf(s) != -1) return 1;
            else
            return 0;
        }
    }
}

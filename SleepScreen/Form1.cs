using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace SleepScreen
{
    public partial class FormSleepScreen : Form
    {
        private int SC_MONITORPOWER = 0xF170;
        private uint WM_SYSCOMMAND = 0x0112;

        private const int MONITOR_ON = -1;
        private const int MONITOR_OFF = 2;
        private const int MONITOR_STANBY = 1;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private volatile bool PreventActivation = false;
        private volatile int LastMonitorStatus = MONITOR_ON;

        private Thread LastThread = null;

        public FormSleepScreen()
        {
            InitializeComponent();
        }


        private void FormSleepScreen_KeyUp(object sender, KeyEventArgs e)
        {

            switch (e.KeyCode)
            {
                case Keys.Escape:
                    {
                        WakeUpMonitor();
                        break;
                    };
                default:
                    {
                        System.Media.SystemSounds.Beep.Play();
                        SleepMonitor();
                        break;
                    }
            }
        }

        private void WakeUpMonitor()
        {
            PreventActivation = false;

            CallSysWakeUpMonitor();
            System.Media.SystemSounds.Question.Play();
            Application.Exit();
        }

        private IntPtr CallSysWakeUpMonitor()
        {
            return SendMessage(this.Handle, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MONITOR_ON);
        }


        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SYSCOMMAND) //Intercept System Command
            {
                //Console.WriteLine("SYSCOMMAND.");
                if ((m.WParam.ToInt32() & 0xFFF0) == SC_MONITORPOWER) {
                    
                    LastMonitorStatus = m.LParam.ToInt32();
                    Console.WriteLine($"WndProc: LastMonitorStatus: {LastMonitorStatus}");
                }
            }
            base.WndProc(ref m);
        }


        private void SleepMonitor()
        {
            Console.WriteLine($"SleepMonitor: LastMonitorStatus: {LastMonitorStatus}");
            if (PreventActivation)
            {
                return;
            }

            if (LastThread != null)
            {
                LastThread.Interrupt();
            }

            PreventActivation = true;

            LastThread = new Thread(() =>
            {
                try
                {
                    while (PreventActivation)
                    {
                        this.Invoke(new MethodInvoker(delegate
                                {
                                    CallSysSleepMonitor();
                                }
                            )
                        );

                        Thread.Sleep(1000);
                    }
                } 
                catch (ThreadInterruptedException)
                {
                    WakeUpMonitor();
                }
            });
            LastThread.Start();
            
        }



        private void CallSysSleepMonitor()
        {
            Console.WriteLine($"CallSysSleepMonitor: LastMonitorStatus: {LastMonitorStatus}");
            if (LastMonitorStatus == MONITOR_OFF)
            {
                Console.WriteLine("Monitor is OFF! Preventing send message");
                return;
            }

            Console.Out.WriteLine("Sending MONITORPOWER message with MONITOR_OFF");
            SendMessage(this.Handle, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MONITOR_OFF);
        }

        private void FormSleepScreen_Activated(object sender, EventArgs e)
        {
            SleepMonitor();
        }

        private void FormSleepScreen_Deactivate(object sender, EventArgs e)
        {
            WakeUpMonitor();
        }

        private void FormSleepScreen_Click(object sender, EventArgs e)
        {
            System.Media.SystemSounds.Beep.Play();
        }
    }
}

using System;
using System.Windows.Forms;

namespace ASCOM.HakosRoof
{
    public partial class Form1 : Form
    {

        private ASCOM.DriverAccess.Dome driver;

        private Timer timer1;

        public Form1()
        {
            InitializeComponent();
            SetUIState();
        }

        public void InitTimer()
        {
            timer1 = new Timer();
            timer1.Tick += new EventHandler(Timer1_Tick);
            timer1.Interval = 2000; // in miliseconds
            timer1.Start();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            SetUIState();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (IsConnected)
                driver.Connected = false;

            Properties.Settings.Default.Save();
            timer1.Stop();

        }

        private void ButtonChoose_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.DriverId = ASCOM.DriverAccess.Dome.Choose(Properties.Settings.Default.DriverId);
            SetUIState();
        }

        private void ButtonConnect_Click(object sender, EventArgs e)
        {
            if (IsConnected)
            {
                driver.Connected = false;
            }
            else
            {
                driver = new ASCOM.DriverAccess.Dome(Properties.Settings.Default.DriverId)
                {
                    Connected = true
                };

                driver.OpenShutter();

            }
            SetUIState();
        }

        private void SetUIState()
        {
            buttonConnect.Enabled = !string.IsNullOrEmpty(Properties.Settings.Default.DriverId);
            buttonChoose.Enabled = !IsConnected;
            buttonConnect.Text = IsConnected ? "Disconnect" : "Connect";
            btnOpenRoof.Enabled = IsConnected;

            
            labelStatus.Text = IsConnected ? driver.ShutterStatus.ToString() : "Not Connected";
            if (IsConnected)
            {
                switch (driver.ShutterStatus)
                {
                    case DeviceInterface.ShutterState.shutterClosed: btnOpenRoof.Text = "Open"; break;
                    case DeviceInterface.ShutterState.shutterOpen: btnOpenRoof.Text = "Close"; break;
                    case DeviceInterface.ShutterState.shutterClosing: btnOpenRoof.Text = "Stop"; break;
                    case DeviceInterface.ShutterState.shutterOpening: btnOpenRoof.Text = "Stop"; break;
                    case DeviceInterface.ShutterState.shutterError: btnOpenRoof.Text = "Error"; break;
                }
            }
        }

        private bool IsConnected
        {
            get
            {
                return ((this.driver != null) && (driver.Connected == true));
            }
        }

        private void BtnOpenRoof_Click(object sender, EventArgs e)
        {
            switch (btnOpenRoof.Text)
            {
                case "Open": driver.OpenShutter(); break;
                case "Close": driver.CloseShutter(); break;
                case "Stop": driver.AbortSlew(); break;
            }
            SetUIState();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;
using ASCOM.HakosRoof;

namespace ASCOM.HakosRoof
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        public SetupDialogForm()
        {
            InitializeComponent();
            // Initialise current values of user settings from the ASCOM Profile
            InitUI();
        }

        private void CmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            // Place any validation constraint checks here
            // Update the state variables with results from the dialogue
            
            Dome.URL = txtURL.Text.ToString();
            Dome.Username = txtUsername.Text.ToString();
            Dome.Password = txtPassword.Text.ToString();

            Dome.tl.Enabled = chkTrace.Checked;
        }

        private void CmdCancel_Click(object sender, EventArgs e) // Cancel button event handler
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e) // Click on ASCOM logo event handler
        {
            try
            {
                System.Diagnostics.Process.Start("http://ascom-standards.org/");
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private void InitUI()
        {
            chkTrace.Checked = Dome.tl.Enabled;
            // set the list of com ports to those that are currently available
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            this.Text = "HakosRoof Setup ver. " + version.ToString();

            txtURL.Text = Dome.URL;
            txtUsername.Text = Dome.Username;
            txtPassword.Text = Dome.Password;
        }

        private void SetupDialogForm_Load(object sender, EventArgs e)
        {

        }
    }
}
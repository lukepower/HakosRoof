//tabs=4
// --------------------------------------------------------------------------------
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM Dome driver for HakosRoof
//
// Description:	Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam 
//				nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam 
//				erat, sed diam voluptua. At vero eos et accusam et justo duo 
//				dolores et ea rebum. Stet clita kasd gubergren, no sea takimata 
//				sanctus est Lorem ipsum dolor sit amet.
//
// Implements:	ASCOM Dome interface version: <To be completed by driver developer>
// Author:		(XXX) Your N. Here <your@email.here>
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	-------------------------------------------------------
// dd-mmm-yyyy	XXX	6.0.0	Initial edit, created from ASCOM driver template
// --------------------------------------------------------------------------------
//


// This is used to define code in the template that is specific to one class implementation
// unused code canbe deleted and this definition removed.
#define Dome
#define USE_MQTT

using System;
using System.Runtime.InteropServices;
using ASCOM.Astrometry.AstroUtils;
//using SimpleJson;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Diagnostics;
using System.Text;
using RestSharp;
using Newtonsoft.Json;

namespace ASCOM.HakosRoof
{
    //
    // Your driver's DeviceID is ASCOM.HakosRoof.Dome
    //
    // The Guid attribute sets the CLSID for ASCOM.HakosRoof.Dome
    // The ClassInterface/None addribute prevents an empty interface called
    // _HakosRoof from being created and used as the [default] interface
    //
    // TODO Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM Dome Driver for HakosRoof.
    /// </summary>
    [Guid("541258d6-703d-4df5-9e1a-412f37098557")]
    [ClassInterface(ClassInterfaceType.None)]
    public class Dome : IDomeV2
    {
        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        internal static string driverID = "ASCOM.HakosRoof.Dome";
        // TODO Change the descriptive string for your driver then remove this line
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        private static string driverDescription = "Hakos Dome Driver (Namibia).";

        internal static string UrlProfileName = "Server URL"; // Constants used for Profile persistence
        internal static string UrlDefault = "http://localhost";
        internal static string traceStateProfileName = "Trace Level";
        internal static string traceStateDefault = "false";

        internal static string UsernameProfileName = "APIKey"; // Constants used for Profile persistence
        internal static string UsernameDefault = "User";
        internal static string PasswordProfileName = "Password"; // Constants used for Profile persistence
        internal static string PasswordDefault = "";

        internal static string URL; // Variables to hold the currrent URL configuration
        internal static string APIKey; // Variables to hold the currrent device configuration
        internal static string Password; // Variables to hold the currrent device configuration


        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Private variable to hold the connected state
        /// </summary>
        private bool connectedState;

        /// <summary>
        /// Private variable to hold an ASCOM Utilities object
        /// </summary>
        private Util utilities;

        /// <summary>
        /// Private variable to hold an ASCOM AstroUtilities object to provide the Range method
        /// </summary>
        private AstroUtils astroUtilities;

        private RestClient client;

        // Save last command type and time to crosscheck for errors
        private ActionCodes lastRequestedCommand;

        private DateTime lastRequestedCommandTimestamp;

        /// <summary>
        /// Variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
        /// </summary>
        internal static TraceLogger tl;

        public enum ActionCodes
        {
            openRoof,
            closeRoof,
            roofStatus,
            stopRoof
        }

        public enum ReturnCodes
        {
            roofOpening,
            roofOpen,
            roofClosing,
            roofClosed,
            roofError,
            commandAccepted,
            commandError,
            credentialError
        }
        public struct CallResult
        {
            public ReturnCodes returnCode;
            public ActionCodes calledAction;
            public string resultString;
        }

        public class RestCallResult
        {
            public string val { get; set; }
            public bool ack { get; set; }
            public string _id { get; set; }
            public string msg { get; set; }
            public string stext { get; set; }

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HakosRoof"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public Dome()
        {
            tl = new TraceLogger("", "HakosRoof");
            System.Diagnostics.Debug.WriteLine("Hello World");
            ReadProfile(); // Read device configuration from the ASCOM Profile store

            tl.LogMessage("Dome", "Starting initialisation");

            

            connectedState = false; // Initialise connected to false
            utilities = new Util(); //Initialise util object
            astroUtilities = new AstroUtils(); // Initialise astro utilities object
            //TODO: Implement your additional construction here

            tl.LogMessage("Dome", "Completed initialisation");
        }


        //
        // PUBLIC COM INTERFACE IDomeV2 IMPLEMENTATION
        //

        #region Common properties and methods.

        /// <summary>
        /// Displays the Setup Dialog form.
        /// If the user clicks the OK button to dismiss the form, then
        /// the new settings are saved, otherwise the old values are reloaded.
        /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
        /// </summary>
        public void SetupDialog()
        {
            // consider only showing the setup dialog if not connected
            // or call a different dialog if connected
            if (IsConnected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            using (SetupDialogForm F = new SetupDialogForm())
            {
                var result = F.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    WriteProfile(); // Persist device configuration values to the ASCOM Profile store
                }
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                tl.LogMessage("SupportedActions Get", "Returning empty arraylist");
                return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            LogMessage("", "Action {0}, parameters {1} not implemented", actionName, actionParameters);
            throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

 

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            // Call CommandString and return as soon as it finishes
            this.CommandString(command, raw);
            // or
            throw new ASCOM.MethodNotImplementedException("CommandBlind");
            // DO NOT have both these sections!  One or the other
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            string ret = CommandString(command, raw);
            // TODO decode the return string and return true or false
            // or
            throw new ASCOM.MethodNotImplementedException("CommandBool");
            // DO NOT have both these sections!  One or the other
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            // it's a good idea to put all the low level communication with the device here,
            // then all communication calls this function
            // you need something to ensure that only one command is in progress at a time

            throw new ASCOM.MethodNotImplementedException("CommandString");
        }

        public void Dispose()
        {
            // Clean up the tracelogger and util objects
            tl.Enabled = false;
            tl.Dispose();
            tl = null;
            utilities.Dispose();
            utilities = null;
            astroUtilities.Dispose();
            astroUtilities = null;
        }

        public bool Connected
        {
            get
            {
                tl.LogMessage("Connected", "Get {0}", IsConnected);
                return IsConnected;
            }
            set
            {
                tl.LogMessage("Connected", "Set {0}", value);
                if (value == IsConnected)
                    return;

                if (value)
                {
                    connectedState = true;
                    tl.LogMessage("Connected Set", "Connecting to URL " +  URL);
                    // TODO connect to the device
                    client = new RestClient(URL);
                    // client.Authenticator = new HttpBasicAuthenticator(username, password);
                    CallResult res =  SendRequest(ActionCodes.roofStatus);
                    
                    if (res.returnCode== ReturnCodes.credentialError)
                    {
                        tl.LogMessage("Connected Set", "Connecting to URL {0} failed with API error " + URL);
                        connectedState = false;
                    }


    
                }
                else
                {
                    connectedState = false;
                    tl.LogMessage("Connected Set", "Disconnecting from Server {0}" + URL);
                    // TODO disconnect from the device
                }
                log.Info("Hello logging world!");
                System.Diagnostics.Debug.WriteLine("Hello World");
            }

        }


        public string Description
        {
            // TODO customise this device description
            get
            {
                tl.LogMessage("Description Get", driverDescription);
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                // TODO customise this driver description
                string driverInfo = "Information about the driver itself. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                LogMessage("InterfaceVersion Get", "2");
                return Convert.ToInt16("2");
            }
        }

        public string Name
        {
            get
            {
                string name = "Hakos Roof Dome";
                tl.LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region IDome Implementation

       
        private ShutterState domeShutterState; // Variable to hold the open/closed status of the shutter, true = Open
        
        

        

        public void AbortSlew()
        {
            //domeShutterState = ShutterState.shutt; // Pr‰‰emptively set to expected value

            CallResult result = SendRequest(ActionCodes.stopRoof);
            if (result.returnCode == ReturnCodes.roofError || result.returnCode == ReturnCodes.commandError)
            {
                domeShutterState = ShutterState.shutterError;
                tl.LogMessage("CloseShutter", "Shutter error while asking to stop");
            }

            tl.LogMessage("CloseShutter", "Shutter has been asked to close");
            tl.LogMessage("AbortSlew", "Completed");
        }

        public double Altitude
        {
            get
            {
                tl.LogMessage("Altitude Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("Altitude", false);
            }
        }

        public bool AtHome
        {
            get
            {
                tl.LogMessage("AtHome Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("AtHome", false);
            }
        }

        public bool AtPark
        {
            get
            {
                tl.LogMessage("AtPark Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("AtPark", false);
            }
        }

        public double Azimuth
        {
            get
            {
                tl.LogMessage("Azimuth Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("Azimuth", false);
            }
        }

        public bool CanFindHome
        {
            get
            {
                tl.LogMessage("CanFindHome Get", false.ToString());
                return false;
            }
        }

        public bool CanPark
        {
            get
            {
                tl.LogMessage("CanPark Get", false.ToString());
                return false;
            }
        }

        public bool CanSetAltitude
        {
            get
            {
                tl.LogMessage("CanSetAltitude Get", false.ToString());
                return false;
            }
        }

        public bool CanSetAzimuth
        {
            get
            {
                tl.LogMessage("CanSetAzimuth Get", false.ToString());
                return false;
            }
        }

        public bool CanSetPark
        {
            get
            {
                tl.LogMessage("CanSetPark Get", false.ToString());
                return false;
            }
        }

        public bool CanSetShutter
        {
            get
            {
                tl.LogMessage("CanSetShutter Get", true.ToString());
                return true;
            }
        }

        public bool CanSlave
        {
            get
            {
                tl.LogMessage("CanSlave Get", false.ToString());
                return false;
            }
        }

        public bool CanSyncAzimuth
        {
            get
            {
                tl.LogMessage("CanSyncAzimuth Get", false.ToString());
                return false;
            }
        }

        public void CloseShutter()
        {
            domeShutterState = ShutterState.shutterClosing; // Pr‰‰emptively set to expected value
            tl.LogMessage("CloseShutter", "Request to close Shutter");
            CallResult result = SendRequest(ActionCodes.closeRoof);
            if (result.returnCode == ReturnCodes.roofError || result.returnCode == ReturnCodes.commandError)
            {
                domeShutterState = ShutterState.shutterError;
                tl.LogMessage("CloseShutter", "Shutter error while asking to close");
            }
            
            tl.LogMessage("CloseShutter", "Shutter has been asked to close");
            
        }

        public void FindHome()
        {
            tl.LogMessage("FindHome", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("FindHome");
        }

        public void OpenShutter()
        {
            domeShutterState = ShutterState.shutterOpening; // Pr‰‰emptively set to expected value
            CallResult result = SendRequest(ActionCodes.openRoof);
            if (result.returnCode == ReturnCodes.roofError || result.returnCode == ReturnCodes.commandError)
            {
                domeShutterState = ShutterState.shutterError;
                tl.LogMessage("OpenShutter", "Shutter error while asking to open");
            }

            tl.LogMessage("OpenShutter", "Shutter has been asked to open");
        }

        public void Park()
        {
            tl.LogMessage("Park", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("Park");
        }

        public void SetPark()
        {
            tl.LogMessage("SetPark", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SetPark");
        }

        public ShutterState ShutterStatus
        {
            get
            {
                
                tl.LogMessage("ShutterStatus Get", false.ToString());
                CallResult result = SendRequest(ActionCodes.roofStatus);
                if (result.returnCode == ReturnCodes.roofError || result.returnCode == ReturnCodes.commandError)
                {
                    domeShutterState = ShutterState.shutterError;
                } else
                {
                    switch (result.returnCode)
                    {
                        case ReturnCodes.roofClosed: domeShutterState = ShutterState.shutterClosed; break;
                        case ReturnCodes.roofClosing: domeShutterState = ShutterState.shutterClosing; break;
                        case ReturnCodes.roofOpen: domeShutterState = ShutterState.shutterOpen; break;
                        case ReturnCodes.roofOpening: domeShutterState = ShutterState.shutterOpening; break;
                    }

                }
               
                    tl.LogMessage("ShutterStatus", domeShutterState.ToString());
                    return domeShutterState;
               
            }
        }

        public bool Slaved
        {
            get
            {
                tl.LogMessage("Slaved Get", false.ToString());
                return false;
            }
            set
            {
                tl.LogMessage("Slaved Set", "not implemented");
                throw new ASCOM.PropertyNotImplementedException("Slaved", true);
            }
        }

        public void SlewToAltitude(double Altitude)
        {
            tl.LogMessage("SlewToAltitude", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SlewToAltitude");
        }

        public void SlewToAzimuth(double Azimuth)
        {
            tl.LogMessage("SlewToAzimuth", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SlewToAzimuth");
        }

        public bool Slewing
        {
            get
            {
                switch (domeShutterState)
                {
                    case ShutterState.shutterClosed: return false;
                    case ShutterState.shutterOpen: return false;
                    case ShutterState.shutterClosing: return true;
                    case ShutterState.shutterOpening: return true;
                    default: return false;
                }

            }
        }

        public void SyncToAzimuth(double Azimuth)
        {
            tl.LogMessage("SyncToAzimuth", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SyncToAzimuth");
        }

        #endregion

        #region Private properties and methods
        // here are some useful properties and methods that can be used as required
        // to help with driver development

        #region ASCOM Registration

        // Register or unregister driver for ASCOM. This is harmless if already
        // registered or unregistered. 
        //
        /// <summary>
        /// Register or unregister the driver with the ASCOM Platform.
        /// This is harmless if the driver is already registered/unregistered.
        /// </summary>
        /// <param name="bRegister">If <c>true</c>, registers the driver, otherwise unregisters it.</param>
        private static void RegUnregASCOM(bool bRegister)
        {
            using (var P = new ASCOM.Utilities.Profile())
            {
                P.DeviceType = "Dome";
                if (bRegister)
                {
                    P.Register(driverID, driverDescription);
                }
                else
                {
                    P.Unregister(driverID);
                }
            }
        }

        /// <summary>
        /// This function registers the driver with the ASCOM Chooser and
        /// is called automatically whenever this class is registered for COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is successfully built.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During setup, when the installer registers the assembly for COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually register a driver with ASCOM.
        /// </remarks>
        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            RegUnregASCOM(true);
        }

        /// <summary>
        /// This function unregisters the driver from the ASCOM Chooser and
        /// is called automatically whenever this class is unregistered from COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is cleaned or prior to rebuilding.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During uninstall, when the installer unregisters the assembly from COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually unregister a driver from ASCOM.
        /// </remarks>
        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            RegUnregASCOM(false);
        }

        #endregion

        /// <summary>
        /// Returns true if there is a valid connection to the driver hardware
        /// </summary>
        private bool IsConnected
        {
            get
            {
                // TODO check that the driver hardware connection exists and is connected to the hardware
                return connectedState;
            }
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!IsConnected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Dome";
                tl.Enabled = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, string.Empty, traceStateDefault));
                URL = driverProfile.GetValue(driverID, UrlProfileName, string.Empty, UrlDefault);
                APIKey = driverProfile.GetValue(driverID, UsernameProfileName, string.Empty, UsernameDefault);
                Password = driverProfile.GetValue(driverID, PasswordProfileName, string.Empty, PasswordDefault);
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Dome";
                driverProfile.WriteValue(driverID, traceStateProfileName, tl.Enabled.ToString());
                driverProfile.WriteValue(driverID, UrlProfileName, URL.ToString());
                driverProfile.WriteValue(driverID, UsernameProfileName, APIKey.ToString());
                driverProfile.WriteValue(driverID, PasswordProfileName, Password.ToString());
            }
        }

        /// <summary>
        /// Log helper function that takes formatted strings and arguments
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        internal static void LogMessage(string identifier, string message, params object[] args)
        {
            var msg = string.Format(message, args);
            tl.LogMessage(identifier, msg);
        }
        #endregion

        #region REST tools

        public CallResult SendRequest(ActionCodes action)
        {
            

            CallResult result = new CallResult
            {
                calledAction = action
            };
            if (this.Connected == false)
            {
                result.returnCode = ReturnCodes.commandError;
                LogMessage("SendRequest", "Trying to send request while not connected");
                return result;
            }
            /*
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            request.Method = "GET";
            request.ContentType = "text/json";
            string results = string.Empty;
            
            //search word
            switch (action)
            {
                case ActionCodes.closeRoof: request.Headers.Add("action ", HttpUtility.UrlEncode("close")); break;
                case ActionCodes.openRoof: request.Headers.Add("action ", HttpUtility.UrlEncode("open")); break;
                case ActionCodes.roofStatus: request.Headers.Add("action ", HttpUtility.UrlEncode("status")); break;
                default:  result.returnCode = ReturnCodes.commandError; result.resultString = "No action given"; return result;
            }
            
            request.Headers.Add("id", HttpUtility.UrlEncode(Password));

            string returnedData;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                returnedData = reader.ReadToEnd();
            }

            switch (returnedData)
            {
                case "Ok":      result.returnCode = ReturnCodes.commandAccepted; break;
                case "Error":   result.returnCode = ReturnCodes.commandError; break;
                case "Open":    result.returnCode = ReturnCodes.roofOpen; break;
                case "Opening": result.returnCode = ReturnCodes.roofOpening; break;
                case "Closed": result.returnCode = ReturnCodes.roofClosed; break;
                case "Closing": result.returnCode = ReturnCodes.roofClosing; break;
            }*/

            // Hack begin
            RestRequest requestLocal;
            
            switch (action)
            {
                case ActionCodes.closeRoof: requestLocal = new RestRequest("remobs?action=close", Method.GET); break;
                case ActionCodes.openRoof: requestLocal = new RestRequest("remobs?action=open", Method.GET); break;
                case ActionCodes.roofStatus: requestLocal = new RestRequest("remobs?action=status", Method.GET); break;
                case ActionCodes.stopRoof: requestLocal = new RestRequest("remobs?action=stop", Method.GET); break;
                default: result.returnCode = ReturnCodes.commandError; result.resultString = "No action given"; return result;
            }

            requestLocal.AddParameter("key", APIKey); // Setze API key


            // execute the request
            //IRestResponse<RestCallResult> response = client.Execute<RestCallResult>(requestLocal);
            IRestResponse response = client.Execute(requestLocal);
            // var content = response.Content; // raw content as string
            var JSONObj = JsonConvert.DeserializeObject<RestCallResult>(response.Content);

            if  (JSONObj.msg=="invalid key")
            {
                result.returnCode = ReturnCodes.credentialError;
                return result;
            }
            if (action == ActionCodes.closeRoof || action == ActionCodes.openRoof)
            {
                // Save last command and timestamp here
                lastRequestedCommand = action;
                lastRequestedCommandTimestamp = DateTime.UtcNow;
            }
            
            
                switch (JSONObj.stext)
                {
                    case "Ok": result.returnCode = ReturnCodes.commandAccepted; break;
                    case "Error": result.returnCode = ReturnCodes.commandError; break;
                    case "open": result.returnCode = ReturnCodes.roofOpen; break;
                    case "opening": result.returnCode = ReturnCodes.roofOpening; break;
                    case "closed": result.returnCode = ReturnCodes.roofClosed; break;
                    case "closing": result.returnCode = ReturnCodes.roofClosing; break;
                }

            // Time crosscheck
            if (action == ActionCodes.roofStatus && lastRequestedCommand != ActionCodes.roofStatus)
            {
                long elapsedTicks = DateTime.UtcNow.Ticks - lastRequestedCommandTimestamp.Ticks;
                TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);

                if (elapsedSpan.TotalSeconds > 10)
                {
                    // Ok, check if there is a corresponding status change:
                    switch (lastRequestedCommand)
                    {
                        case ActionCodes.openRoof:
                            // Expect opening or open, if still closed throw an error
                            if (result.returnCode == ReturnCodes.roofOpening)
                            {
                                // ok
                                break;
                            }
                            if (result.returnCode == ReturnCodes.roofOpen)
                            {
                                // All good
                                lastRequestedCommand = ActionCodes.roofStatus;
                                break;
                            }
                            // Ok, if we are here its an error
                            result.returnCode = ReturnCodes.roofError;

                            break;
                        case ActionCodes.closeRoof:
                            // Expect opening or open, if still closed throw an error
                            if (result.returnCode == ReturnCodes.roofClosing)
                            {
                                // ok
                                break;
                            }
                            if (result.returnCode == ReturnCodes.roofClosed)
                            {
                                // All good
                                lastRequestedCommand = ActionCodes.roofStatus;
                                break;
                            }
                            // Ok, if we are here its an error
                            result.returnCode = ReturnCodes.roofError;

                            break;

                    }

                }
            }


            // Hack end
            return result;

        }

        /**
         * Async GET
         * **/
        public async Task<string> GetAsync(string uri)
        {
            System.Net.HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }

       

        #endregion
    }
}

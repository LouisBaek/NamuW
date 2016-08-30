using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Navigation;
using PortSIP;


namespace SIPSample
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window, SIPCallbackEvents
    {

        private const int MAX_LINES = 9; // Maximum lines
        private const int LINE_BASE = 1;


        private Session[] _CallSessions = new Session[MAX_LINES];

        private bool _SIPInited = false;
        private bool _SIPLogined = false;
        private int _CurrentlyLine = LINE_BASE;


        private PortSIPLib _sdkLib;


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private int findSession(int sessionId)
        {
            int index = -1;
            for (int i = LINE_BASE; i < MAX_LINES; ++i)
            {
                if (_CallSessions[i].getSessionId() == sessionId)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        private byte[] GetBytes(string str)
        {
            return System.Text.Encoding.Default.GetBytes(str);
        }

        private string GetString(byte[] bytes)
        {
            return System.Text.Encoding.Default.GetString(bytes);
        }


        private string getLocalIP()
        {
            StringBuilder localIP = new StringBuilder();
            localIP.Length = 64;
            int nics = _sdkLib.getNICNums();
            for (int i = 0; i < nics; ++i)
            {
                _sdkLib.getLocalIpAddress(i, localIP, 64);
                if (localIP.ToString().IndexOf(":") == -1)
                {
                    // No ":" in the IP then it's the IPv4 address, we use it in our sample
                    break;
                }
                else
                {
                    // the ":" is occurs in the IP then this is the IPv6 address.
                    // In our sample we don't use the IPv6.
                }

            }

            return localIP.ToString();
        }

        public Window1()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            // Create the call sessions array, allows maximum 500 lines,
            // but we just use 8 lines with this sample, we need a class to save the call sessions information but we just use 8 lines with this sample, we need a class to save the call sessions information

            int i = 0;
            for (i = 0; i < MAX_LINES; ++i)
            {
                _CallSessions[i] = new Session();
                _CallSessions[i].reset();
            }

            _SIPInited = false;
            _SIPLogined = false;
            _CurrentlyLine = LINE_BASE;



            sliderSpeaker.Value = 0;
            sliderMicrophone.Value = 0;

            ComboBoxTransport.Items.Add("UDP");
            ComboBoxTransport.Items.Add("TLS");
            ComboBoxTransport.Items.Add("TCP");
            ComboBoxTransport.Items.Add("PERS");

            ComboBoxTransport.SelectedIndex = 0;



            ComboBoxSRTP.Items.Add("None");
            ComboBoxSRTP.Items.Add("Prefer");
            ComboBoxSRTP.Items.Add("Force");

            ComboBoxSRTP.SelectedIndex = 0;



            ComboBoxVideoResolution.Items.Add("QCIF");
            ComboBoxVideoResolution.Items.Add("CIF");
            ComboBoxVideoResolution.Items.Add("VGA");
            ComboBoxVideoResolution.Items.Add("SVGA");
            ComboBoxVideoResolution.Items.Add("XVGA");
            ComboBoxVideoResolution.Items.Add("720P");
            ComboBoxVideoResolution.Items.Add("QVGA");

            ComboBoxVideoResolution.SelectedIndex = 1;

            ComboBoxLines.Items.Add("Line-1");
            ComboBoxLines.Items.Add("Line-2");
            ComboBoxLines.Items.Add("Line-3");
            ComboBoxLines.Items.Add("Line-4");
            ComboBoxLines.Items.Add("Line-5");
            ComboBoxLines.Items.Add("Line-6");
            ComboBoxLines.Items.Add("Line-7");
            ComboBoxLines.Items.Add("Line-8");


            ComboBoxLines.SelectedIndex = 0;
        }


        private void sliderSpeaker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_SIPInited == false)
            {
                return;
            }

            _sdkLib.setSpeakerVolume((Int32)e.NewValue);
        }

        private void sliderMicrophone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_SIPInited == false)
            {
                return;
            }

            _sdkLib.setMicVolume((Int32)e.NewValue);
        }


        private void joinConference(Int32 index)
        {
            if (_SIPInited == false)
            {
                return;
            }
            if (CheckBoxConf.IsChecked == false)
            {
                return;
            }

            _sdkLib.joinToConference(_CallSessions[index].getSessionId());


            // In the conference, we need to unhold the line
            if (_CallSessions[index].getHoldState())
            {
                _sdkLib.unHold(_CallSessions[index].getSessionId());
                _CallSessions[index].setHoldState(false);
            }  
        }

        private void CheckBoxDND_Checked(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false )
            {
                return;
            }

            if (CheckBoxDND.IsChecked == true)
            {
                _sdkLib.setDoNotDisturb(true);
            }
            else
            {
                _sdkLib.setDoNotDisturb(false);
            }

        }

        private void checkBoxPRACK_Checked(object sender, RoutedEventArgs e)
        {
            updatePrackSetting();
        }


        private void loadDevices()
        {
            if (_SIPInited == false)
            {
                return;
            }

            int num = _sdkLib.getNumOfPlayoutDevices();
            for (int i = 0; i < num; ++i)
            {
                StringBuilder deviceName = new StringBuilder();
                deviceName.Length = 256;

                if (_sdkLib.getPlayoutDeviceName(i, deviceName, 256) == 0)
                {
                    ComboBoxSpeakers.Items.Add(deviceName.ToString());
                }

                ComboBoxSpeakers.SelectedIndex = 0;
            }


            num = _sdkLib.getNumOfRecordingDevices();
            for (int i = 0; i < num; ++i)
            {
                StringBuilder deviceName = new StringBuilder();
                deviceName.Length = 256;

                if (_sdkLib.getRecordingDeviceName(i, deviceName, 256) == 0)
                {
                    ComboBoxMicrophones.Items.Add(deviceName.ToString());
                }

                ComboBoxMicrophones.SelectedIndex = 0;
            }


            num = _sdkLib.getNumOfVideoCaptureDevices();
            for (int i = 0; i < num; ++i)
            {
                StringBuilder uniqueId = new StringBuilder();
                uniqueId.Length = 256;
                StringBuilder deviceName = new StringBuilder();
                deviceName.Length = 256;

                if (_sdkLib.getVideoCaptureDeviceName(i, uniqueId, 256, deviceName, 256) == 0)
                {
                    ComboBoxCameras.Items.Add(deviceName.ToString());
                }

                ComboBoxCameras.SelectedIndex = 0;
            }


            int volume = _sdkLib.getSpeakerVolume();     
            sliderSpeaker.Value = volume;

            volume = _sdkLib.getMicVolume();
            sliderMicrophone.Value = volume;

        }



        private void deRegisterFromServer()
        {
            if (_SIPInited == false)
            {
                return;
            }

            for (int i = LINE_BASE; i < MAX_LINES; ++i)
            {
                if (_CallSessions[i].getRecvCallState() == true)
                {
                    _sdkLib.rejectCall(_CallSessions[i].getSessionId(), 486);
                }
                else if (_CallSessions[i].getSessionState() == true)
                {
                    _sdkLib.hangUp(_CallSessions[i].getSessionId());
                }

                _CallSessions[i].reset();
            }

            if (_SIPLogined == true)
            {
                _sdkLib.unRegisterServer();
                _SIPLogined = false;
            }


            if (_SIPInited == true)
            {
                  _sdkLib.unInitialize();

                //
                // MUST called after _sdkLib.unInitliaze();
                //
                _sdkLib.releaseCallbackHandlers();
                _SIPInited = false;
            }


            ListBoxSIPLog.Items.Clear();

            ComboBoxLines.SelectedIndex = 0;
            _CurrentlyLine = LINE_BASE;


            ComboBoxSpeakers.Items.Clear();
            ComboBoxMicrophones.Items.Clear();
            ComboBoxCameras.Items.Clear();

        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            deRegisterFromServer();
        }


        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == true)
            {
                MessageBox.Show("You are already logged in.");
                return;
            }


            if (TextBoxUserName.Text.Length <= 0)
            {
                MessageBox.Show("The user name does not allows empty.");
                return;
            }


            if (TextBoxPassword.Text.Length <= 0)
            {
                MessageBox.Show("The password does not allows empty.");
                return;
            }

            if (TextBoxServer.Text.Length <= 0)
            {
                MessageBox.Show("The SIP server does not allows empty.");
                return;
            }


            int SIPServerPort = 0;
            if (TextBoxServerPort.Text.Length > 0)
            {
                SIPServerPort = int.Parse(TextBoxServerPort.Text);
                if (SIPServerPort > 65535 || SIPServerPort <= 0)
                {
                    MessageBox.Show("The SIP server port is out of range.");

                    return;
                }
            }


            int StunServerPort = 0;
            if (TextBoxStunPort.Text.Length > 0)
            {
                StunServerPort = int.Parse(TextBoxStunPort.Text);
                if (StunServerPort > 65535 || StunServerPort <= 0)
                {
                    MessageBox.Show("The Stun server port is out of range.");

                    return;
                }
            }


            TRANSPORT_TYPE transport = TRANSPORT_TYPE.TRANSPORT_UDP;
            switch (ComboBoxTransport.SelectedIndex)
            {
                case 0:
                    transport = TRANSPORT_TYPE.TRANSPORT_UDP;
                    break;

                case 1:

                    transport = TRANSPORT_TYPE.TRANSPORT_TLS;
                    break;

                case 2:
                    transport = TRANSPORT_TYPE.TRANSPORT_TCP;
                    break;

                case 3:
                    transport = TRANSPORT_TYPE.TRANSPORT_PERS;
                    break;
            }


            //
            // Create the class instance of PortSIP VoIP SDK, you can create more than one instances and 
            // each instance register to a SIP server to support multiples accounts & providers.
            // for example:
            /*
            _sdkLib1 = new PortSIPLib(1, 1, this);
            _sdkLib2 = new PortSIPLib(2, 2, this);
            _sdkLib3 = new PortSIPLib(3, 3, this);
            */


            _sdkLib = new PortSIPLib(0, 0, this);

            //
            // Create and set the SIP callback handers, this MUST called before
            // _sdkLib.initialize();
            //
            _sdkLib.createCallbackHandlers();

            string logFilePath = "d:\\"; // The log file path, you can change it - the folder MUST exists
            string agent = "PortSIP VoIP SDK 11.2";
            string stunServer = TextBoxStunServer.Text;

            // Initialize the SDK
            int rt = _sdkLib.initialize(transport,
                             PORTSIP_LOG_LEVEL.PORTSIP_LOG_NONE,
                             logFilePath,
                             MAX_LINES,
                             agent,
                             0,
                             0);

            if (rt != 0)
            {
                _sdkLib.releaseCallbackHandlers();
                MessageBox.Show("Failed to initialize.");
                return;
            }

            ListBoxSIPLog.Items.Add("Initialized.");
            _SIPInited = true;

            loadDevices();

            string userName = TextBoxUserName.Text;
            string password = TextBoxPassword.Text;
            string userDomain = TextBoxUserDomain.Text;
            string SIPServer = TextBoxServer.Text;
            string displayName = TextBoxDisplayName.Text;
            string authName = TextBoxAuthName.Text;


            int outboundServerPort = 0;
            string outboundServer = "";


            Random rd = new Random();
            int LocalSIPPort = rd.Next(1000, 5000) + 4000; // Generate the random port for SIP

            string ip = getLocalIP();

            // Set the SIP user information
            rt = _sdkLib.setUser(userName,
                                       displayName,
                                       authName,
                                       password,
                // Use 0.0.0.0 for local IP then the SDK will choose an available local IP automatically.
                // You also can specify a certain local IP to instead of "0.0.0.0", more details please read the SDK User Manual
                                       "0.0.0.0",
                                       LocalSIPPort,
                                       userDomain,
                                       SIPServer,
                                       SIPServerPort,
                                       stunServer,
                                       StunServerPort,
                                       outboundServer,
                                       outboundServerPort);
            if (rt != 0)
            {
                _sdkLib.unInitialize();
                _sdkLib.releaseCallbackHandlers();

                _SIPInited = false;

                ListBoxSIPLog.Items.Clear();

                MessageBox.Show("Failed to setUser.");
                return;
            }

            ListBoxSIPLog.Items.Add("Succeeded set user information.");

            SetSRTPType();

            string licenseKey = "PORTSIP_TEST_LICENSE";
            rt = _sdkLib.setLicenseKey(licenseKey);
            if (rt == PortSIP_Errors.ECoreTrialVersionLicenseKey)
            {
                MessageBox.Show("This sample was built base on evaluation PortSIP VoIP SDK, which allows only three minutes conversation. The conversation will be cut off automatically after three minutes, then you can't hearing anything. Feel free contact us at: sales@portsip.com to purchase the official version.");
            }
            else if (rt == PortSIP_Errors.ECoreWrongLicenseKey)
            {
                MessageBox.Show("The wrong license key was detected, please check with sales@portsip.com or support@portsip.com");
            }

            _sdkLib.setLocalVideoWindow(localVideoWindow.Child.Handle);

            SetVideoResolution();
            SetVideoQuality();

            UpdateAudioCodecs();
            UpdateVideoCodecs();

            InitSettings();

            updatePrackSetting();

            if (checkBoxNeedRegisterServer.IsChecked == true)
            {
                rt = _sdkLib.registerServer(120, 0);
                if (rt != 0)
                {
                    _SIPInited = false;
                    _sdkLib.unInitialize();
                    _sdkLib.releaseCallbackHandlers();

                    ListBoxSIPLog.Items.Clear();

                    MessageBox.Show("Failed to register to server.");
                }

                ListBoxSIPLog.Items.Add("Registering...");
            }
        }


        private void button2_Click(object sender, RoutedEventArgs e)
        {
            deRegisterFromServer();
        }


        private void ComboBoxSRTP_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetSRTPType();
        }

        private void ButtonDial_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false || (checkBoxNeedRegisterServer.IsChecked==true && _SIPLogined == false))
            {
                return;
            }
            if (TextBoxPhoneNumber.Text.Length <= 0)
            {
                MessageBox.Show("The phone number is empty.");
                return;
            }

            if (_CallSessions[_CurrentlyLine].getSessionState() == true || _CallSessions[_CurrentlyLine].getRecvCallState() == true)
            {
                MessageBox.Show("Current line is busy now, please switch a line.");
                return;
            }


            string callTo = TextBoxPhoneNumber.Text;

            UpdateAudioCodecs();
            UpdateVideoCodecs();

            if (_sdkLib.isAudioCodecEmpty() == true)
            {
                InitDefaultAudioCodecs();
            }



            //  Usually for 3PCC need to make call without SDP

            Boolean hasSdp = true;
            if (CheckBoxSDP.IsChecked == true)
            {
                hasSdp = false;
            }

            _sdkLib.setAudioDeviceId(ComboBoxMicrophones.SelectedIndex, ComboBoxSpeakers.SelectedIndex);

            bool videoCall = false;
            if (checkBoxMakeVideoCall.IsChecked == true)
            {
                videoCall = true;
            }
            int sessionId = _sdkLib.call(callTo, hasSdp, videoCall);
            if (sessionId <= 0)
            {
                ListBoxSIPLog.Items.Add("Failed to call.");
                return;
            }

            _sdkLib.setRemoteVideoWindow(sessionId, remoteVideoWindow.Child.Handle);

            _CallSessions[_CurrentlyLine].setSessionId(sessionId);
            _CallSessions[_CurrentlyLine].setSessionState(true);

            string Text = "Line " + _CurrentlyLine.ToString();
            Text = Text + ": Calling...";
            ListBoxSIPLog.Items.Add(Text);
        }



        private void ButtonAnswer_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false || (checkBoxNeedRegisterServer.IsChecked == true && _SIPLogined == false))
            {
                return;
            }

            if (_CallSessions[_CurrentlyLine].getRecvCallState() == false)
            {
                MessageBox.Show("Current line hasn't incoming call, please switch a line.");
                return;
            }

            _CallSessions[_CurrentlyLine].setRecvCallState(false);
            _CallSessions[_CurrentlyLine].setSessionState(true);


            _sdkLib.setRemoteVideoWindow(_CallSessions[_CurrentlyLine].getSessionId(), remoteVideoWindow.Child.Handle);

            bool answerVideoCall = false;
            if (checkBoxAnswerVideoCall.IsChecked == true)
            {
                answerVideoCall = true;
            }
            int rt = _sdkLib.answerCall(_CallSessions[_CurrentlyLine].getSessionId(), answerVideoCall);
            if (rt == 0)
            {
                string Text = "Line " + _CurrentlyLine.ToString();
                Text = Text + ": Call established";
                ListBoxSIPLog.Items.Add(Text);

                joinConference(_CurrentlyLine);
            }
            else
            {
                _CallSessions[_CurrentlyLine].reset();

                string Text = "Line " + _CurrentlyLine.ToString();
                Text = Text + ": Failed to answer call.";
                ListBoxSIPLog.Items.Add(Text);
            }
        }


        private void ButtonReject_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false || (checkBoxNeedRegisterServer.IsChecked == true && _SIPLogined == false))
            {
                return;
            }

            if (_CallSessions[_CurrentlyLine].getRecvCallState() == true)
            {
                _sdkLib.rejectCall(_CallSessions[_CurrentlyLine].getSessionId(), 486);
                _CallSessions[_CurrentlyLine].reset();

                string Text = "Line " + _CurrentlyLine.ToString();
                Text = Text + ": Rejected call";
                ListBoxSIPLog.Items.Add(Text);

                return;
            }
        }


        private void ButtonHangUp_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false || (checkBoxNeedRegisterServer.IsChecked == true && _SIPLogined == false))
            {
                return;
            }

            if (_CallSessions[_CurrentlyLine].getRecvCallState() == true)
            {
                _sdkLib.rejectCall(_CallSessions[_CurrentlyLine].getSessionId(), 486);
                _CallSessions[_CurrentlyLine].reset();

                string Text = "Line " + _CurrentlyLine.ToString();
                Text = Text + ": Rejected call";
                ListBoxSIPLog.Items.Add(Text);

                return;
            }

            if (_CallSessions[_CurrentlyLine].getSessionState() == true)
            {
                _sdkLib.hangUp(_CallSessions[_CurrentlyLine].getSessionId());
                _CallSessions[_CurrentlyLine].reset();

                string Text = "Line " + _CurrentlyLine.ToString();
                Text = Text + ": Hang up";
                ListBoxSIPLog.Items.Add(Text);
            }
        }



        private void ButtonHold_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false || (checkBoxNeedRegisterServer.IsChecked == true && _SIPLogined == false))
            {
                return;
            }

            if (_CallSessions[_CurrentlyLine].getSessionState() == false || _CallSessions[_CurrentlyLine].getHoldState() == true)
            {
                return;
            }

            string Text;
            int rt = _sdkLib.hold(_CallSessions[_CurrentlyLine].getSessionId());

            if (rt != 0)
            {
                Text = "Line " + _CurrentlyLine.ToString();
                Text = Text + ": Failed to hold.";
                ListBoxSIPLog.Items.Add(Text);

                return;
            }

            _CallSessions[_CurrentlyLine].setHoldState(true);

            Text = "Line " + _CurrentlyLine.ToString();
            Text = Text + ": hold";
            ListBoxSIPLog.Items.Add(Text);
        }

        private void button20_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false || (checkBoxNeedRegisterServer.IsChecked == true && _SIPLogined == false))
            {
                return;
            }

            if (_CallSessions[_CurrentlyLine].getSessionState() == false || _CallSessions[_CurrentlyLine].getHoldState() == false)
            {
                return;
            }

            string Text;
            int rt = _sdkLib.unHold(_CallSessions[_CurrentlyLine].getSessionId());
            if (rt != 0)
            {
                _CallSessions[_CurrentlyLine].setHoldState(false);

                Text = "Line " + _CurrentlyLine.ToString();
                Text = Text + ": Failed to Un-Hold.";
                ListBoxSIPLog.Items.Add(Text);

                return;
            }

            _CallSessions[_CurrentlyLine].setHoldState(false);

            Text = "Line " + _CurrentlyLine.ToString();
            Text = Text + ": Un-Hold";
            ListBoxSIPLog.Items.Add(Text);
        }

        private void ButtonTransfer_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false || (checkBoxNeedRegisterServer.IsChecked == true && _SIPLogined == false))
            {
                return;
            }

            if (_CallSessions[_CurrentlyLine].getSessionState() == false)
            {
                MessageBox.Show("Need to make the call established first.");
                return;
            }

            Window2 TransferDlg = new Window2();
            if (TransferDlg.ShowDialog() == false)
            {
                return;
            }

            string referTo = TransferDlg.GetTransferNumber();

            if (referTo.Length <= 0)
            {
                MessageBox.Show("The transfer number is empty.");
                return;
            }


            int rt = _sdkLib.refer(_CallSessions[_CurrentlyLine].getSessionId(), referTo);
            if (rt != 0)
            {
                string Text = "Line " + _CurrentlyLine.ToString();
                Text = Text + ": Failed to Transfer.";
                ListBoxSIPLog.Items.Add(Text);
            }
            else
            {
                string Text = "Line " + _CurrentlyLine.ToString();
                Text = Text + ": Transferring";
                ListBoxSIPLog.Items.Add(Text);
            }
        }

        private void button22_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false || (checkBoxNeedRegisterServer.IsChecked == true && _SIPLogined == false))
            {
                return;
            }

            if (_CallSessions[_CurrentlyLine].getSessionState() == false)
            {
                MessageBox.Show("Need to make the call established first.");
                return;
            }

            Window2 TransferDlg = new Window2();
            if (TransferDlg.ShowDialog() == false)
            {
                return;
            }

            string referTo = TransferDlg.GetTransferNumber();

            if (referTo.Length <= 0)
            {
                MessageBox.Show("The transfer number is empty.");
                return;
            }

            int replaceLine = TransferDlg.GetReplaceLineNum();
            if (replaceLine <= 0 || replaceLine >= MAX_LINES)
            {
                MessageBox.Show("The replace line out of range.");
                return;
            }


            if (_CallSessions[replaceLine].getSessionState() == false)
            {
                MessageBox.Show("The replace line does not established yet.");
                return;
            }


            int rt = _sdkLib.attendedRefer(_CallSessions[_CurrentlyLine].getSessionId(), _CallSessions[replaceLine].getSessionId(), referTo);

            if (rt != 0)
            {
                string Text = "Line " + _CurrentlyLine.ToString();
                Text = Text + ": Failed to Transfer.";
                ListBoxSIPLog.Items.Add(Text);
            }
            else
            {
                string Text = "Line " + _CurrentlyLine.ToString();
                Text = Text + ": Transferring";
                ListBoxSIPLog.Items.Add(Text);
            }
        }



        private void button3_Click(object sender, RoutedEventArgs e)
        {
            TextBoxPhoneNumber.Text = TextBoxPhoneNumber.Text + "1";
            if (_SIPInited == true && _CallSessions[_CurrentlyLine].getSessionState() == true)
            {
                _sdkLib.sendDtmf(_CallSessions[_CurrentlyLine].getSessionId(), DTMF_METHOD.DTMF_RFC2833, 1, 160, true);
            }
        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {
            TextBoxPhoneNumber.Text = TextBoxPhoneNumber.Text + "2";
            if (_SIPInited == true && _CallSessions[_CurrentlyLine].getSessionState() == true)
            {
                _sdkLib.sendDtmf(_CallSessions[_CurrentlyLine].getSessionId(), DTMF_METHOD.DTMF_RFC2833, 2, 160, true);
            }
        }

        private void button5_Click(object sender, RoutedEventArgs e)
        {
            TextBoxPhoneNumber.Text = TextBoxPhoneNumber.Text + "3";
            if (_SIPInited == true && _CallSessions[_CurrentlyLine].getSessionState() == true)
            {
                _sdkLib.sendDtmf(_CallSessions[_CurrentlyLine].getSessionId(), DTMF_METHOD.DTMF_RFC2833, 3, 160, true);
            }
        }

        private void button6_Click(object sender, RoutedEventArgs e)
        {
            TextBoxPhoneNumber.Text = TextBoxPhoneNumber.Text + "4";
            if (_SIPInited == true && _CallSessions[_CurrentlyLine].getSessionState() == true)
            {
                _sdkLib.sendDtmf(_CallSessions[_CurrentlyLine].getSessionId(), DTMF_METHOD.DTMF_RFC2833, 4, 160, true);
            }
        }

        private void button7_Click(object sender, RoutedEventArgs e)
        {
            TextBoxPhoneNumber.Text = TextBoxPhoneNumber.Text + "5";
            if (_SIPInited == true && _CallSessions[_CurrentlyLine].getSessionState() == true)
            {
                _sdkLib.sendDtmf(_CallSessions[_CurrentlyLine].getSessionId(), DTMF_METHOD.DTMF_RFC2833, 5, 160, true);
            }
        }

        private void button8_Click(object sender, RoutedEventArgs e)
        {
            TextBoxPhoneNumber.Text = TextBoxPhoneNumber.Text + "6";
            if (_SIPInited == true && _CallSessions[_CurrentlyLine].getSessionState() == true)
            {
                _sdkLib.sendDtmf(_CallSessions[_CurrentlyLine].getSessionId(), DTMF_METHOD.DTMF_RFC2833, 6, 160, true);
            }
        }

        private void button9_Click(object sender, RoutedEventArgs e)
        {
            TextBoxPhoneNumber.Text = TextBoxPhoneNumber.Text + "7";
            if (_SIPInited == true && _CallSessions[_CurrentlyLine].getSessionState() == true)
            {
                _sdkLib.sendDtmf(_CallSessions[_CurrentlyLine].getSessionId(), DTMF_METHOD.DTMF_RFC2833, 7, 160, true);
            }
        }

        private void button10_Click(object sender, RoutedEventArgs e)
        {
            TextBoxPhoneNumber.Text = TextBoxPhoneNumber.Text + "8";
            if (_SIPInited == true && _CallSessions[_CurrentlyLine].getSessionState() == true)
            {
                _sdkLib.sendDtmf(_CallSessions[_CurrentlyLine].getSessionId(), DTMF_METHOD.DTMF_RFC2833, 8, 160, true);
            }
        }

        private void button11_Click(object sender, RoutedEventArgs e)
        {
            TextBoxPhoneNumber.Text = TextBoxPhoneNumber.Text + "9";
            if (_SIPInited == true && _CallSessions[_CurrentlyLine].getSessionState() == true)
            {
                _sdkLib.sendDtmf(_CallSessions[_CurrentlyLine].getSessionId(), DTMF_METHOD.DTMF_RFC2833, 9, 160, true);
            }
        }

        private void button12_Click(object sender, RoutedEventArgs e)
        {
            TextBoxPhoneNumber.Text = TextBoxPhoneNumber.Text + "*";
            if (_SIPInited == true && _CallSessions[_CurrentlyLine].getSessionState() == true)
            {
                _sdkLib.sendDtmf(_CallSessions[_CurrentlyLine].getSessionId(), DTMF_METHOD.DTMF_RFC2833, 10, 160, true);
            }
        }

        private void button13_Click(object sender, RoutedEventArgs e)
        {
            TextBoxPhoneNumber.Text = TextBoxPhoneNumber.Text + "0";
            if (_SIPInited == true && _CallSessions[_CurrentlyLine].getSessionState() == true)
            {
                _sdkLib.sendDtmf(_CallSessions[_CurrentlyLine].getSessionId(), DTMF_METHOD.DTMF_RFC2833, 0, 160, true);
            }
        }

        private void button14_Click(object sender, RoutedEventArgs e)
        {
            TextBoxPhoneNumber.Text = TextBoxPhoneNumber.Text + "#";
            if (_SIPInited == true && _CallSessions[_CurrentlyLine].getSessionState() == true)
            {
                _sdkLib.sendDtmf(_CallSessions[_CurrentlyLine].getSessionId(), DTMF_METHOD.DTMF_RFC2833, 11, 160, true);
            }
        }




        private void ComboBoxLines_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_SIPInited == false || (checkBoxNeedRegisterServer.IsChecked == true && _SIPLogined == false))
            {
                ComboBoxLines.SelectedIndex = 0;
                return;
            }

            if (_CurrentlyLine == (ComboBoxLines.SelectedIndex + LINE_BASE))
            {
                return;
            }

            if (CheckBoxConf.IsChecked == true)
            {
                _CurrentlyLine = ComboBoxLines.SelectedIndex + LINE_BASE;
                return;
            }

            // To switch the line, must hold currently line first
            if (_CallSessions[_CurrentlyLine].getSessionState() == true && _CallSessions[_CurrentlyLine].getHoldState() == false)
            {
                _sdkLib.hold(_CallSessions[_CurrentlyLine].getSessionId());
                _sdkLib.setRemoteVideoWindow(_CallSessions[_CurrentlyLine].getSessionId(), IntPtr.Zero);
                _CallSessions[_CurrentlyLine].setHoldState(true);

                string Text = "Line " + _CurrentlyLine.ToString();
                Text = Text + ": Hold";
                ListBoxSIPLog.Items.Add(Text);
            }

            _CurrentlyLine = ComboBoxLines.SelectedIndex + LINE_BASE;


            // If target line was in hold state, then un-hold it
            if (_CallSessions[_CurrentlyLine].getSessionState() == true && _CallSessions[_CurrentlyLine].getHoldState() == true)
            {
                _sdkLib.unHold(_CallSessions[_CurrentlyLine].getSessionId());
                _sdkLib.setRemoteVideoWindow(_CallSessions[_CurrentlyLine].getSessionId(), remoteVideoWindow.Child.Handle);
                _CallSessions[_CurrentlyLine].setHoldState(false);

                string Text = "Line " + _CurrentlyLine.ToString();
                Text = Text + ": UnHold - call established";
                ListBoxSIPLog.Items.Add(Text);
            }
        }

        private void CheckBoxConf_Checked(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false || (checkBoxNeedRegisterServer.IsChecked == true && _SIPLogined == false))
            {
                CheckBoxConf.IsChecked = false;
                return;
            }


            Int32 width = 352;
            Int32 height = 288;

            switch (ComboBoxVideoResolution.SelectedIndex)
            {
                case 0://qcif
                    width = 176;
                    height = 144;
                    break;
                case 1://cif
                    width = 352;
                    height = 288;
                    break;
                case 2://VGA
                    width = 640;
                    height = 480;
                    break;
                case 3://svga
                    width = 800;
                    height = 600;
                    break;
                case 4://xvga
                    width = 1024;
                    height = 768;
                    break;
                case 5://q720
                    width = 1280;
                    height = 720;
                    break;
                case 6://qvga
                    width = 320;
                    height = 240;
                    break;
            }


            if (CheckBoxConf.IsChecked == true)
            {
                int rt = _sdkLib.createConference(remoteVideoWindow.Child.Handle, width, height, true);
                if (rt == 0)
                {
                    ListBoxSIPLog.Items.Add("Make conference succeeded");
                    for (int i = LINE_BASE; i < MAX_LINES; ++i)
                    {
                        if (_CallSessions[i].getSessionState() == true)
                        {
                            joinConference(i);
                        }
                    }
                }
                else
                {
                    ListBoxSIPLog.Items.Add("Failed to create conference.");
                    CheckBoxConf.IsChecked = false;
                }
            }
            else
            {
                // Stop conference

                // Before stop the conference, MUST place all lines to hold state

                int[] sessionIds = new int[9] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };


                for (int i = LINE_BASE; i < MAX_LINES; ++i)
                {
                    if (_CallSessions[i].getSessionState() == true && _CallSessions[i].getHoldState() == false)
                    {

                        sessionIds[i] = _CallSessions[i].getSessionId();

                        // Hold it 
                        _sdkLib.hold(sessionIds[i]);
                        _CallSessions[i].setHoldState(true);
                    }
                }

                _sdkLib.destroyConference();
                ListBoxSIPLog.Items.Add("Taken off Conference");

            }
        }



        private void CheckBoxMute_Checked(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false || (checkBoxNeedRegisterServer.IsChecked == true && _SIPLogined == false))
            {
                return;
            }

            if (CheckBoxMute.IsChecked == true)
            {
                _sdkLib.muteMicrophone(true);
            }
            else
            {
                _sdkLib.muteMicrophone(false);
            }
        }


        private void ComboBoxSpeakers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_SIPInited == true)
            {
                _sdkLib.setAudioDeviceId(ComboBoxMicrophones.SelectedIndex, ComboBoxSpeakers.SelectedIndex);
            }
        }

        private void ComboBoxMicrophones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_SIPInited == true)
            {
                _sdkLib.setAudioDeviceId(ComboBoxMicrophones.SelectedIndex, ComboBoxSpeakers.SelectedIndex);
            }
        }

        private void ComboBoxCameras_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_SIPInited)
            {
                _sdkLib.setVideoDeviceId(ComboBoxCameras.SelectedIndex);
            }
        }




        private void ComboBoxVideoResolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetVideoResolution();
        }

        private void sliderVideoQualityLevel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetVideoQuality();
        }



        private void ButtonCameraOptions_Click(object sender, RoutedEventArgs e)
        {
            Int32 tempHandle = new WindowInteropHelper(this).Handle.ToInt32();
            IntPtr windowHandle = new IntPtr(tempHandle);
            if (_SIPInited == false)
            {
                return;
            }

            StringBuilder uniqueId = new StringBuilder();
            uniqueId.Length = 256;
            StringBuilder deviceName = new StringBuilder();
            deviceName.Length = 256;

            int rt = _sdkLib.getVideoCaptureDeviceName(ComboBoxCameras.SelectedIndex,
                                                uniqueId,
                                                256,
                                                deviceName,
                                                256);
            if (rt != 0)
            {
                MessageBox.Show("Get camera name failed.");
                return;
            }

            rt = _sdkLib.showVideoCaptureSettingsDialogBox(uniqueId.ToString(), uniqueId.Length, "Camera settings", windowHandle, 200, 200);
            if (rt != 0)
            {
                MessageBox.Show("Show the camera settings dialog failed.");
                return;
            }
        }

        private void ButtonLocalVideo_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false)
            {
                return;
            }

            string buttonContent = (string)ButtonLocalVideo.Content;
            if (buttonContent == "Local Video")
            {
                _sdkLib.displayLocalVideo(true);
                ButtonLocalVideo.Content = "Stop Local";
            }
            else
            {
                _sdkLib.displayLocalVideo(false);
                localVideoWindow.Child.Refresh();
                
                ButtonLocalVideo.Content = "Local Video";
            }


        }

        private void ButtonSendVideo_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false)
            {
                return;
            }

            if (_CallSessions[_CurrentlyLine].getSessionState() == false && _CallSessions[_CurrentlyLine].getRecvCallState() == false)
            {
                return;
            }

            int rt = _sdkLib.sendVideo(_CallSessions[_CurrentlyLine].getSessionId(), true);
            if (rt != 0)
            {
                MessageBox.Show("Failed to start video send.");
            }
        }

        private void button28_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false)
            {
                return;
            }

            if (_CallSessions[_CurrentlyLine].getSessionState() == false)
            {
                return;
            }

            _sdkLib.sendVideo(_CallSessions[_CurrentlyLine].getSessionId(), false);
        }


        private void button29_Click(object sender, RoutedEventArgs e)
        {
            ListBoxSIPLog.Items.Clear();
        }

        private void checkBoxPCMU_Checked(object sender, RoutedEventArgs e)
        {
            UpdateAudioCodecs();
        }

        private void checkBoxPCMA_Checked(object sender, RoutedEventArgs e)
        {
            UpdateAudioCodecs();
        }

        private void checkBoxG729_Checked(object sender, RoutedEventArgs e)
        {
            UpdateAudioCodecs();
        }

        private void checkBoxILBC_Checked(object sender, RoutedEventArgs e)
        {
            UpdateAudioCodecs();
        }

        private void checkBoxGSM_Checked(object sender, RoutedEventArgs e)
        {
            UpdateAudioCodecs();
        }

        private void checkBoxAMR_Checked(object sender, RoutedEventArgs e)
        {
            UpdateAudioCodecs();
        }

        private void CheckBoxG722_Checked(object sender, RoutedEventArgs e)
        {
            UpdateAudioCodecs();
        }

        private void CheckBoxSpeex_Checked(object sender, RoutedEventArgs e)
        {
            UpdateAudioCodecs();
        }

        private void CheckBoxAMRwb_Checked(object sender, RoutedEventArgs e)
        {
            UpdateAudioCodecs();
        }

        private void CheckBoxSpeexWB_Checked(object sender, RoutedEventArgs e)
        {
            UpdateAudioCodecs();
        }

        private void CheckBoxG7221_Checked(object sender, RoutedEventArgs e)
        {
            UpdateAudioCodecs();
        }

        private void checkBoxOpus_Checked(object sender, RoutedEventArgs e)
        {
            UpdateAudioCodecs();
        }


        private void checkBoxH263_Checked(object sender, RoutedEventArgs e)
        {
            UpdateVideoCodecs();
        }

        private void checkBoxH2631998_Checked(object sender, RoutedEventArgs e)
        {
            UpdateVideoCodecs();
        }

        private void checkBoxVP8_Checked(object sender, RoutedEventArgs e)
        {
            UpdateVideoCodecs();
        }


        private void checkBoxH264_Checked(object sender, RoutedEventArgs e)
        {
            UpdateVideoCodecs();
        }

        private void checkBoxAEC_Checked(object sender, RoutedEventArgs e)
        {
            InitSettings();
        }

        private void checkBoxVAD_Checked(object sender, RoutedEventArgs e)
        {
            InitSettings();
        }

        private void checkBoxCNG_Checked(object sender, RoutedEventArgs e)
        {
            InitSettings();
        }

        private void checkBoxAGC_Checked(object sender, RoutedEventArgs e)
        {
            InitSettings();
        }



        private void checkBox1_Checked(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false)
            {
                MessageBox.Show("Please initialize the SDK first.");
                return;
            }


            if (checkBox1.IsChecked == true)
            {

                // Callback the audio stream from each line
                int rt = _sdkLib.enableAudioStreamCallback(0,
                                            _CallSessions[_CurrentlyLine].getSessionId(),
                                                true,
                                                AUDIOSTREAM_CALLBACK_MODE.AUDIOSTREAM_REMOTE_PER_CHANNEL);
                if (rt != 0)
                {
                    MessageBox.Show("Enable the audio stream callback failed.");
                    checkBox1.IsChecked = false;
                }
            }
            else
            {
                // Disable audio stream callback
                _sdkLib.enableAudioStreamCallback(0,
                                                _CallSessions[_CurrentlyLine].getSessionId(),
                                                false,
                                                AUDIOSTREAM_CALLBACK_MODE.AUDIOSTREAM_REMOTE_PER_CHANNEL);
            }
        }

        private void button31_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false)
            {
                MessageBox.Show("Please initialize the SDK first.", "Information");
                return;
            }

            if (TextBoxRecordFilePath.Text.Length <= 0 || TextBoxRecordFileName.Text.Length <= 0)
            {
                MessageBox.Show("The file path or file name is empty.", "Information");
                return;
            }


            string filePath = TextBoxRecordFilePath.Text;
            string fileName = TextBoxRecordFileName.Text;

            AUDIO_RECORDING_FILEFORMAT audioRecordFileFormat = AUDIO_RECORDING_FILEFORMAT.FILEFORMAT_WAVE;

            //  Start recording
            int rt = _sdkLib.startRecord(_CallSessions[_CurrentlyLine].getSessionId(),
                                        filePath,
                                        fileName,
                                        true,
                                        audioRecordFileFormat,
                                        RECORD_MODE.RECORD_BOTH,
                                        VIDEOCODEC_TYPE.VIDEO_CODEC_H264,
                                        RECORD_MODE.RECORD_RECV);
            if (rt != 0)
            {
                MessageBox.Show("Failed to start record conversation.", "Information");
                return;
            }

            MessageBox.Show("Started record conversation.", "Information");
        }

        private void button32_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false)
            {
                MessageBox.Show("Please initialize the SDK first.");
                return;
            }

            _sdkLib.stopRecord(_CallSessions[_CurrentlyLine].getSessionId());

            MessageBox.Show("Stop record conversation.");

        }

     

        private void button36_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false)
            {
                MessageBox.Show("Please initialize the SDK first.");
                return;
            }

            if (TextBoxPlayFile.Text.Length <= 0)
            {
                MessageBox.Show("The play file is empty.");
                return;
            }

            string waveFile = TextBoxPlayFile.Text;

            _sdkLib.playAudioFileToRemote(_CallSessions[_CurrentlyLine].getSessionId(), waveFile, 16000, false);

        }

        private void button37_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false)
            {
                return;
            }

            string waveFile = TextBoxPlayFile.Text;

            _sdkLib.stopPlayAudioFileToRemote(_CallSessions[_CurrentlyLine].getSessionId());
       
        }

        private void button38_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false)
            {
                MessageBox.Show("Please initialize the SDK first.");

                return;
            }

            string forwardTo = textBoxForwardTo.Text;
            if (forwardTo.IndexOf("sip:") == -1 || forwardTo.IndexOf("@") == -1)
            {
                MessageBox.Show("The forward address must likes sip:xxxx@sip.portsip.com.");

                return;
            }

            if (checkBoxForwardCallForBusy.IsChecked == true)
            {
                _sdkLib.enableCallForward(true, forwardTo);
            }
            else
            {
                _sdkLib.enableCallForward(false, forwardTo);
            }

            MessageBox.Show("The call forward is enabled.");
            return;
        }

        private void button39_Click(object sender, RoutedEventArgs e)
        {
            if (_SIPInited == false)
            {
                MessageBox.Show("Please initialize the SDK first.");

                return;
            }

            _sdkLib.disableCallForward();

            MessageBox.Show("The call forward is disabled.");
            return;
        }



        private void button35_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".wav";

            dlg.Filter = "Wave file (.wav)|*.wav";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                string filename = dlg.FileName;
                TextBoxPlayFile.Text = filename;
            }

        }


        private void updatePrackSetting()
        {
            if (!_SIPInited)
            {
                return;
            }

            _sdkLib.enableReliableProvisional((Boolean)checkBoxPRACK.IsChecked);
        }



        private void InitSettings()
        {
            if (_SIPInited == false)
            {
                return;
            }


            if (checkBoxAEC.IsChecked == true)
            {
                _sdkLib.enableAEC(EC_MODES.EC_DEFAULT);
            }
            else
            {
                _sdkLib.enableAEC(EC_MODES.EC_NONE);
            }

            if (checkBoxVAD.IsChecked == true)
            {
                _sdkLib.enableVAD(true);
            }
            else
            {
                _sdkLib.enableVAD(false);
            }

            if (checkBoxCNG.IsChecked == true)
            {
                _sdkLib.enableCNG(true);
            }
            else
            {
                _sdkLib.enableCNG(false);
            }

            if (checkBoxAGC.IsChecked == true)
            {
                _sdkLib.enableAGC(AGC_MODES.AGC_DEFAULT);
            }
            else
            {
                _sdkLib.enableAGC(AGC_MODES.AGC_NONE);
            }

            if (checkBoxANS.IsChecked == true)
            {
                _sdkLib.enableANS(NS_MODES.NS_DEFAULT);
            }
            else
            {
                _sdkLib.enableANS(NS_MODES.NS_NONE);
            }

            if (CheckBoxDND.IsChecked == true)
            {
                _sdkLib.setDoNotDisturb(true);
            }
            else
            {
                _sdkLib.setDoNotDisturb(false);
            }
        }

        
        private void InitDefaultAudioCodecs()
        {
            if (_SIPInited == false)
            {
                return;
            }


            _sdkLib.clearAudioCodec();

            // Default we just using PCMU, PCMA, G729
            _sdkLib.addAudioCodec(AUDIOCODEC_TYPE.AUDIOCODEC_PCMU);
            _sdkLib.addAudioCodec(AUDIOCODEC_TYPE.AUDIOCODEC_PCMA);
            _sdkLib.addAudioCodec(AUDIOCODEC_TYPE.AUDIOCODEC_G729);

            _sdkLib.addAudioCodec(AUDIOCODEC_TYPE.AUDIOCODEC_DTMF);  // for DTMF as RTP Event - RFC2833
        }


        private void UpdateAudioCodecs()
        {
            if (_SIPInited == false)
            {
                return;
            }

            _sdkLib.clearAudioCodec();

            if (checkBoxPCMU.IsChecked == true)
            {
                _sdkLib.addAudioCodec(AUDIOCODEC_TYPE.AUDIOCODEC_PCMU);
            }


            if (checkBoxPCMA.IsChecked == true)
            {
                _sdkLib.addAudioCodec(AUDIOCODEC_TYPE.AUDIOCODEC_PCMA);
            }


            if (checkBoxG729.IsChecked == true)
            {
                _sdkLib.addAudioCodec(AUDIOCODEC_TYPE.AUDIOCODEC_G729);
            }


            if (checkBoxILBC.IsChecked == true)
            {
                _sdkLib.addAudioCodec(AUDIOCODEC_TYPE.AUDIOCODEC_ILBC);
            }


            if (checkBoxGSM.IsChecked == true)
            {
                _sdkLib.addAudioCodec(AUDIOCODEC_TYPE.AUDIOCODEC_GSM);
            }


            if (checkBoxAMR.IsChecked == true)
            {
                _sdkLib.addAudioCodec(AUDIOCODEC_TYPE.AUDIOCODEC_AMR);
            }

            if (CheckBoxG722.IsChecked == true)
            {
                _sdkLib.addAudioCodec(AUDIOCODEC_TYPE.AUDIOCODEC_G722);
            }

            if (CheckBoxSpeex.IsChecked == true)
            {
                _sdkLib.addAudioCodec(AUDIOCODEC_TYPE.AUDIOCODEC_SPEEX);
            }

            if (CheckBoxAMRwb.IsChecked == true)
            {
                _sdkLib.addAudioCodec(AUDIOCODEC_TYPE.AUDIOCODEC_AMRWB);
            }

            if (CheckBoxSpeexWB.IsChecked == true)
            {
                _sdkLib.addAudioCodec(AUDIOCODEC_TYPE.AUDIOCODEC_SPEEXWB);
            }

            if (CheckBoxG7221.IsChecked == true)
            {
                _sdkLib.addAudioCodec(AUDIOCODEC_TYPE.AUDIOCODEC_G7221);
            }


            if (checkBoxOpus.IsChecked == true)
            {
                _sdkLib.addAudioCodec(AUDIOCODEC_TYPE.AUDIOCODEC_OPUS);
            }

            _sdkLib.addAudioCodec(AUDIOCODEC_TYPE.AUDIOCODEC_DTMF);

        }


        private void UpdateVideoCodecs()
        {
            if (_SIPInited == false)
            {
                return;
            }

            _sdkLib.clearVideoCodec();

            if (checkBoxH263.IsChecked == true)
            {
                _sdkLib.addVideoCodec(VIDEOCODEC_TYPE.VIDEO_CODEC_H263);
            }

            if (checkBoxH2631998.IsChecked == true)
            {
                _sdkLib.addVideoCodec(VIDEOCODEC_TYPE.VIDEO_CODEC_H263_1998);
            }

            if (checkBoxH264.IsChecked == true)
            {
                _sdkLib.addVideoCodec(VIDEOCODEC_TYPE.VIDEO_CODEC_H264);
            }

            if (checkBoxVP8.IsChecked == true)
            {
                _sdkLib.addVideoCodec(VIDEOCODEC_TYPE.VIDEO_CODEC_VP8);
            }

        }


        private void SetSRTPType()
        {
            if (_SIPInited == false)
            {
                return;
            }

            SRTP_POLICY SRTPPolicy = SRTP_POLICY.SRTP_POLICY_NONE;

            switch (ComboBoxSRTP.SelectedIndex)
            {
                case 0:
                    SRTPPolicy = SRTP_POLICY.SRTP_POLICY_NONE;
                    break;

                case 1:
                    SRTPPolicy = SRTP_POLICY.SRTP_POLICY_PREFER;
                    break;

                case 2:
                    SRTPPolicy = SRTP_POLICY.SRTP_POLICY_FORCE;
                    break;
            }

            _sdkLib.setSrtpPolicy(SRTPPolicy);
        }

        private void SetVideoResolution()
        {
            if (_SIPInited == false)
            {
                return;
            }

            Int32 width = 352;
            Int32 height = 288;

            switch (ComboBoxVideoResolution.SelectedIndex)
            {
                case 0://qcif
                    width = 176;
                    height = 144;
                    break;
                case 1://cif
                    width = 352;
                    height = 288;
                    break;
                case 2://VGA
                    width = 640;
                    height = 480;
                    break;
                case 3://svga
                    width = 800;
                    height = 600;
                    break;
                case 4://xvga
                    width = 1024;
                    height = 768;
                    break;
                case 5://q720
                    width = 1280;
                    height = 720;
                    break;
                case 6://qvga
                    width = 320;
                    height = 240;
                    break;
            }

            _sdkLib.setVideoResolution(width, height);
        }

        private void SetVideoQuality()
        {
            if (_SIPInited == false)
            {
                return;
            }

            _sdkLib.setVideoBitrate((Int32)sliderVideoQualityLevel.Value);
        }





        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        ///  With below all onXXX functions, you MUST use the Invoke/BeginInvoke method if you want
        ///  modify any control on the Forms.
        ///  More details please visit: http://msdn.microsoft.com/en-us/library/ms171728.aspx
        ///  The Invoke method is recommended.
        ///  
        ///  if you don't like Invoke/BeginInvoke method, then  you can add this line to Form_Load:
        ///  Control.CheckForIllegalCrossThreadCalls = false;
        ///  This requires .NET 2.0 or higher
        /// 
        /// </summary>
        /// 
        public Int32 onRegisterSuccess(Int32 callbackIndex, Int32 callbackObject, String statusText, Int32 statusCode)
        {
            // use the Invoke method to modify the control.

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add("Registration succeeded");
            }));

            _SIPLogined = true;
  

            return 0;
        }


        public Int32 onRegisterFailure(Int32 callbackIndex, Int32 callbackObject, String statusText, Int32 statusCode)
        {
            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add("Registration failure");
            }));


            _SIPLogined = false; 

            return 0;
        }


        public Int32 onInviteIncoming(Int32 callbackIndex,
                                             Int32 callbackObject,
                                             Int32 sessionId,
                                             String callerDisplayName,
                                             String caller,
                                             String calleeDisplayName,
                                             String callee,
                                             String audioCodecNames,
                                             String videoCodecNames,
                                             Boolean existsAudio,
                                             Boolean existsVideo)
        {

            int index = -1;
            for (int i = LINE_BASE; i < MAX_LINES; ++i)
            {
                if (_CallSessions[i].getSessionState() == false && _CallSessions[i].getRecvCallState() == false)
                {
                    index = i;
                    _CallSessions[i].setRecvCallState(true);
                    break;
                }
            }

            if (index == -1)
            {
                _sdkLib.rejectCall(sessionId, 486);
                return 0;
            }


            if (existsVideo)
            {
                // If more than one codecs using, then they are separated with "#",
                // for example: "g.729#GSM#AMR", "H264#H263", you have to parse them by yourself.
            }
            if (existsAudio)
            {
                // If more than one codecs using, then they are separated with "#",
                // for example: "g.729#GSM#AMR", "H264#H263", you have to parse them by yourself.
            }


            _CallSessions[index].setSessionId(sessionId);
            string Text = string.Empty;

            bool needIgnoreAutoAnswer = false;
            int j = 0;

            for (j = LINE_BASE; j < MAX_LINES; ++j)
            {
                if (_CallSessions[j].getSessionState() == true)
                {
                    needIgnoreAutoAnswer = true;
                    break;
                }
            }


            Boolean AA = false;
            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                if (CheckBoxAA.IsChecked == true)
                {
                    AA = true;
                }
            }));

            if (existsVideo)
            {
                ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
                {
                    _sdkLib.setRemoteVideoWindow(_CallSessions[index].getSessionId(), remoteVideoWindow.Child.Handle);
                }));
            }
            


            if (needIgnoreAutoAnswer == false && AA == true)
            {
                _CallSessions[index].setRecvCallState(false);
                _CallSessions[index].setSessionState(true);


                int rt = 0;
                bool answerVideo = false;
                ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
                {
                    if (checkBoxAnswerVideoCall.IsChecked == true)
                    {
                        answerVideo = true;
                    }
                    
                }));

                rt = _sdkLib.answerCall(_CallSessions[index].getSessionId(), answerVideo);
                if (rt == 0)
                {
                    Text = "Line " + index.ToString();
                    Text = Text + ": Answered call by Auto answer";
                    ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
                    {
                        ListBoxSIPLog.Items.Add(Text);
                    }));
                }
                else
                {
                    _CallSessions[index].reset();

                    Text = "Line " + index.ToString();
                    Text = Text + ": Failed answered call by Auto answer.";

                    ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
                    {
                        ListBoxSIPLog.Items.Add(Text);
                    }));
                }

                return 0;
            }


            Text = "Line " + index.ToString();
            Text = Text + ": Call incoming from ";
            Text = Text + callerDisplayName;
            Text = Text + "<";
            Text = Text + caller;
            Text = Text + ">";


            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));

            //  You should write your own code to play the wav file here for alert the incoming call(incoming tone);

            return 0;

        }

        public Int32 onInviteTrying(Int32 callbackIndex, Int32 callbackObject, Int32 sessionId)
        {
            int i = findSession(sessionId);
            if (i == -1)
            {
                return 0;
            }

            string Text = "Line " + i.ToString();
            Text = Text + ": Call is trying...";

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));
 
            return 0;

        }



        public Int32 onInviteSessionProgress(Int32 callbackIndex,
                                            Int32 callbackObject,
                                            Int32 sessionId,
                                             String audioCodecNames,
                                             String videoCodecNames,
                                             Boolean existsEarlyMedia,
                                             Boolean existsAudio,
                                             Boolean existsVideo)
        {
            int i = findSession(sessionId);
            if (i == -1)
            {
                return 0;
            }

            if (existsVideo)
            {
                // If more than one codecs using, then they are separated with "#",
                // for example: "g.729#GSM#AMR", "H264#H263", you have to parse them by yourself.
            }
            if (existsAudio)
            {
                // If more than one codecs using, then they are separated with "#",
                // for example: "g.729#GSM#AMR", "H264#H263", you have to parse them by yourself.
            }

            _CallSessions[i].setSessionState(true);

            string Text = "Line " + i.ToString();
            Text = Text + ": Call session progress.";

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));

            _CallSessions[i].setEarlyMeida(existsEarlyMedia);

            return 0;
        }




        public Int32 onInviteRinging(Int32 callbackIndex,
                                            Int32 callbackObject,
                                            Int32 sessionId,
                                            String statusText,
                                            Int32 statusCode)
        {
            int i = findSession(sessionId);
            if (i == -1)
            {
                return 0;
            }

            if (_CallSessions[i].hasEarlyMeida() == false)
            {
                // No early media, you must play the local WAVE  file for ringing tone
            }

            string Text = "Line " + i.ToString();
            Text = Text + ": Ringing...";

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));


            return 0;
        }




        public Int32 onInviteAnswered(Int32 callbackIndex,
                                             Int32 callbackObject,
                                             Int32 sessionId,
                                             String callerDisplayName,
                                             String caller,
                                             String calleeDisplayName,
                                             String callee,
                                             String audioCodecNames,
                                             String videoCodecNames,
                                             Boolean existsAudio,
                                             Boolean existsVideo)
        {

            if (existsVideo)
            {
                // If more than one codecs using, then they are separated with "#",
                // for example: "g.729#GSM#AMR", "H264#H263", you have to parse them by yourself.
            }
            if (existsAudio)
            {
                // If more than one codecs using, then they are separated with "#",
                // for example: "g.729#GSM#AMR", "H264#H263", you have to parse them by yourself.
            }


            int i = findSession(sessionId);
            if (i == -1)
            {
                return 0;
            }


            _CallSessions[i].setSessionState(true);

            string Text = "Line " + i.ToString();
            Text = Text + ": Call established";

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);

                joinConference(i);
            }));

            // If this is the refer call then need set it to normal
            if (_CallSessions[i].isReferCall())
            {
                _CallSessions[i].setReferCall(false, 0);
            }

            return 0;
        }


        public Int32 onInviteFailure(Int32 callbackIndex, Int32 callbackObject, Int32 sessionId, String reason, Int32 code)
        {
            int index = -1;
            for (int i = LINE_BASE; i < MAX_LINES; ++i)
            {
                if (_CallSessions[i].getSessionId() == sessionId)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                return 0;
            }


            string Text = "Line " + index.ToString();
            Text += ": Call failure, ";
            Text += reason;
            Text += ", ";
            Text += code.ToString();

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));


            if (_CallSessions[index].isReferCall())
            {
                // Take off the origin call from HOLD if the refer call is failure
                int originIndex = -1;
                for (int i = LINE_BASE; i < MAX_LINES; ++i)
                {
                    // Looking for the origin call
                    if (_CallSessions[i].getSessionId() == _CallSessions[index].getOriginCallSessionId())
                    {
                        originIndex = i;
                        break;
                    }
                }

                if (originIndex != -1)
                {
                    ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
                    {
                        _sdkLib.unHold(_CallSessions[index].getOriginCallSessionId());
                        _CallSessions[originIndex].setHoldState(false);

                        // Switch the currently line to origin call line
                        _CurrentlyLine = originIndex;
                        ComboBoxLines.SelectedIndex = _CurrentlyLine - 1;

                        Text = "Current line is set to: ";
                        Text += _CurrentlyLine.ToString();
                        ListBoxSIPLog.Items.Add(Text);
                    }));


                }
            }

            _CallSessions[index].reset();

            return 0;
        }



        public Int32 onInviteUpdated(Int32 callbackIndex,
                                             Int32 callbackObject,
                                             Int32 sessionId,
                                             String audioCodecNames,
                                             String videoCodecNames,
                                             Boolean existsAudio,
                                             Boolean existsVideo)
        {
            int i = findSession(sessionId);
            if (i == -1)
            {
                return 0;
            }

            string Text = "Line " + i.ToString();
            Text = Text + ": Call is updated";


            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));

            return 0;

        }



        public Int32 onInviteConnected(Int32 callbackIndex, Int32 callbackObject, Int32 sessionId)
        {
            int i = findSession(sessionId);
            if (i == -1)
            {
                return 0;
            }

            string Text = "Line " + i.ToString();
            Text = Text + ": Call is connected";


            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));

            return 0;
        }




        public Int32 onInviteBeginingForward(Int32 callbackIndex, Int32 callbackObject, String forwardTo)
        {
            string Text = "Call has been forwarded to: ";
            Text = Text + forwardTo;

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));

            return 0;
        }


        public Int32 onInviteClosed(Int32 callbackIndex, Int32 callbackObject, Int32 sessionId)
        {
            int i = findSession(sessionId);
            if (i == -1)
            {
                return 0;
            }


            _CallSessions[i].reset();

            string Text = "Line " + i.ToString();
            Text = Text + ": Call closed";


            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));


            return 0;
        }


        public Int32 onRemoteHold(Int32 callbackIndex, Int32 callbackObject, Int32 sessionId)
        {
            int i = findSession(sessionId);
            if (i == -1)
            {
                return 0;
            }

            string Text = "Line " + i.ToString();
            Text = Text + ": Placed on hold by remote party";


            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));

            return 0;
        }


        public Int32 onRemoteUnHold(Int32 callbackIndex,
                                               Int32 callbackObject,
                                               Int32 sessionId,
                                               String audioCodecNames,
                                               String videoCodecNames,
                                               Boolean existsAudio,
                                               Boolean existsVideo)
        {
            int i = findSession(sessionId);
            if (i == -1)
            {
                return 0;
            }

            string Text = "Line " + i.ToString();
            Text = Text + ": Take off hold by remote party";

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));

            return 0;
        }


        public Int32 onReceivedRefer(Int32 callbackIndex,
                                                    Int32 callbackObject,
                                                    Int32 sessionId,
                                                    Int32 referId,
                                                    String to,
                                                    String from,
                                                    String referSipMessage)
        {
            int index = findSession(sessionId);
            if (index == -1)
            {
                _sdkLib.rejectRefer(referId);
                return 0;
            }


            string Text = "Received REFER on line ";
            Text += index.ToString();
            Text += ", refer to ";
            Text += to;

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));

            // Accept the REFER automatically
            int referSessionId = _sdkLib.acceptRefer(referId, referSipMessage);
            if (referSessionId <= 0)
            {
                Text = "Failed to accept REFER on line ";
                Text += index.ToString();

                ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
                {
                    ListBoxSIPLog.Items.Add(Text);
                }));
            }
            else
            {
                _sdkLib.hangUp(_CallSessions[index].getSessionId());
                _CallSessions[index].reset();


                _CallSessions[index].setSessionId(referSessionId);
                _CallSessions[index].setSessionState(true);

                Text = "Accepted the REFER";
                ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
                {
                    ListBoxSIPLog.Items.Add(Text);
                }));
            }

            return 0;
        }

        public Int32 onReferAccepted(Int32 callbackIndex, Int32 callbackObject, Int32 sessionId)
        {
            int index = findSession(sessionId);
            if (index == -1)
            {
                return 0;
            }

            string Text = "Line ";
            Text += index.ToString();
            Text += ", the REFER was accepted";

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));

            return 0;
        }



        public Int32 onReferRejected(Int32 callbackIndex, Int32 callbackObject, Int32 sessionId, String reason, Int32 code)
        {
            int index = -1;
            for (int i = LINE_BASE; i < MAX_LINES; ++i)
            {
                if (_CallSessions[i].getSessionId() == sessionId)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                return 0;
            }

            string Text = "Line ";
            Text += index.ToString();
            Text += ", the REFER was rejected";

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));

            return 0;
        }



        public Int32 onTransferTrying(Int32 callbackIndex, Int32 callbackObject, Int32 sessionId)
        {
            int i = findSession(sessionId);
            if (i == -1)
            {
                return 0;
            }

            // for example, if A and B is established call, A transfer B to C, the transfer is trying,
            // B will got this transferTring event, and use referTo to know C ( C is "referTo" in this case)

            string Text = "Line " + i.ToString();
            Text = Text + ": Transfer Trying";

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));


            return 0;
        }

        public Int32 onTransferRinging(Int32 callbackIndex, Int32 callbackObject, Int32 sessionId)
        {
            int i = findSession(sessionId);
            if (i == -1)
            {
                return 0;
            }

            string Text = "Line " + i.ToString();
            Text = Text + ": Transfer Ringing";

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));


            // Use hasVideo to check does this transfer call has video.
            // if hasVideo is true, then it's have video, if hasVideo is false, means has no video.


            return 0;
        }


        public Int32 onACTVTransferSuccess(Int32 callbackIndex, Int32 callbackObject, Int32 sessionId)
        {
            int i = findSession(sessionId);
            if (i == -1)
            {
                return 0;
            }

            // Close the call after succeeded transfer the call
            _sdkLib.hangUp(_CallSessions[i].getSessionId());
            _CallSessions[i].reset();

            string Text = "Line " + i.ToString();
            Text = Text + ": Transfer succeeded, call closed.";

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));

            return 0;
        }

        public Int32 onACTVTransferFailure(Int32 callbackIndex, Int32 callbackObject, Int32 sessionId, String reason, Int32 code)
        {
            int i = findSession(sessionId);
            if (i == -1)
            {
                return 0;
            }


            string Text = "Line " + i.ToString();
            Text = Text + ": Failed to Transfer.";

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));


            //  statusText is error reason
            //  statusCode is error code

            return 0;
        }


        public Int32 onReceivedSignaling(Int32 callbackIndex, Int32 callbackObject, Int32 sessionId, StringBuilder signaling)
        {
            // This event will be fired when the SDK received a SIP message
            // you can use signaling to access the SIP message.

            return 0;
        }


        public Int32 onSendingSignaling(Int32 callbackIndex, Int32 callbackObject, Int32 sessionId, StringBuilder signaling)
        {
            // This event will be fired when the SDK sent a SIP message
            // you can use signaling to access the SIP message.

            return 0;
        }




        public Int32 onWaitingVoiceMessage(Int32 callbackIndex,
                                                    Int32 callbackObject,
                                                  String messageAccount,
                                                  Int32 urgentNewMessageCount,
                                                  Int32 urgentOldMessageCount,
                                                  Int32 newMessageCount,
                                                  Int32 oldMessageCount)
        {

            string Text = messageAccount;
            Text += " has voice message.";


            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));

            // You can use these parameters to check the voice message count

            //  urgentNewMessageCount;
            //  urgentOldMessageCount;
            //  newMessageCount;
            //  oldMessageCount;

            return 0;
        }


        public Int32 onWaitingFaxMessage(Int32 callbackIndex,
                                                       Int32 callbackObject,
                                                  String messageAccount,
                                                  Int32 urgentNewMessageCount,
                                                  Int32 urgentOldMessageCount,
                                                  Int32 newMessageCount,
                                                  Int32 oldMessageCount)
        {
            string Text = messageAccount;
            Text += " has FAX message.";


            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));


            // You can use these parameters to check the FAX message count

            //  urgentNewMessageCount;
            //  urgentOldMessageCount;
            //  newMessageCount;
            //  oldMessageCount;

            return 0;
        }


        public Int32 onRecvDtmfTone(Int32 callbackIndex, Int32 callbackObject, Int32 sessionId, Int32 tone)
        {
            int i = findSession(sessionId);
            if (i == -1)
            {
                return 0;
            }

            string DTMFTone = tone.ToString();
            switch (tone)
            {
                case 10:
                    DTMFTone = "*";
                    break;

                case 11:
                    DTMFTone = "#";
                    break;

                case 12:
                    DTMFTone = "A";
                    break;

                case 13:
                    DTMFTone = "B";
                    break;

                case 14:
                    DTMFTone = "C";
                    break;

                case 15:
                    DTMFTone = "D";
                    break;

                case 16:
                    DTMFTone = "FLASH";
                    break;
            }

            string Text = "Received DTMF Tone: ";
            Text += DTMFTone;
            Text += " on line ";
            Text += i.ToString();

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));


            return 0;
        }


        public Int32 onPresenceRecvSubscribe(Int32 callbackIndex,
                                                    Int32 callbackObject,
                                                    Int32 subscribeId,
                                                    String fromDisplayName,
                                                    String from,
                                                    String subject)
        {


            return 0;
        }


        public Int32 onPresenceOnline(Int32 callbackIndex,
                                                    Int32 callbackObject,
                                                    String fromDisplayName,
                                                    String from,
                                                    String stateText)
        {

            return 0;
        }

        public Int32 onPresenceOffline(Int32 callbackIndex, Int32 callbackObject, String fromDisplayName, String from)
        {


            return 0;
        }


        public Int32 onRecvOptions(Int32 callbackIndex, Int32 callbackObject, StringBuilder optionsMessage)
        {
            //         string text = "Received an OPTIONS message: ";
            //       text += optionsMessage.ToString();
            //     MessageBox.Show(text, "Received an OPTIONS message", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            return 0;
        }

        public Int32 onRecvInfo(Int32 callbackIndex, Int32 callbackObject, StringBuilder infoMessage)
        {
            string text = "Received a INFO message: ";
            text += infoMessage.ToString();

            MessageBox.Show(text, "Received a INFO message");

            return 0;
        }


        public Int32 onRecvMessage(Int32 callbackIndex,
                                                 Int32 callbackObject,
                                                 Int32 sessionId,
                                                 String mimeType,
                                                 String subMimeType,
                                                 byte[] messageData,
                                                 Int32 messageDataLength)
        {
            int i = findSession(sessionId);
            if (i == -1)
            {
                return 0;
            }

            string text = "Received a MESSAGE message on line ";
            text += i.ToString();

            if (mimeType == "text" && subMimeType == "plain")
            {
                string mesageText = GetString(messageData);
            }
            else if (mimeType == "application" && subMimeType == "vnd.3gpp.sms")
            {
                // The messageData is binary data
            }
            else if (mimeType == "application" && subMimeType == "vnd.3gpp2.sms")
            {
                // The messageData is binary data
            }

            MessageBox.Show(text, "Received a MESSAGE message");

            return 0;
        }


        public Int32 onRecvOutOfDialogMessage(Int32 callbackIndex,
                                                 Int32 callbackObject,
                                                 String fromDisplayName,
                                                 String from,
                                                 String toDisplayName,
                                                 String to,
                                                 String mimeType,
                                                 String subMimeType,
                                                 byte[] messageData,
                                                 Int32 messageDataLength)
        {
            string text = "Received a message(out of dialog) from ";
            text += from;

            if (mimeType == "text" && subMimeType == "plain")
            {
                string mesageText = GetString(messageData);
            }
            else if (mimeType == "application" && subMimeType == "vnd.3gpp.sms")
            {
                // The messageData is binary data
            }
            else if (mimeType == "application" && subMimeType == "vnd.3gpp2.sms")
            {
                // The messageData is binary data
            }

            MessageBox.Show(text, "Received a out of dialog MESSAGE message");

            return 0;
        }

        public Int32 onSendMessageSuccess(Int32 callbackIndex,
                                                           Int32 callbackObject,
                                                           Int32 sessionId, Int32 messageId)
        {
            return 0;
        }


        public Int32 onSendMessageFailure(Int32 callbackIndex,
                                                        Int32 callbackObject,
                                                        Int32 sessionId,
                                                         Int32 messageId,
                                                        String reason,
                                                        Int32 code)
        {

            return 0;
        }



        public Int32 onSendOutOfDialogMessageSuccess(Int32 callbackIndex,
                                                        Int32 callbackObject,
                                                        Int32 messageId,
                                                        String fromDisplayName,
                                                        String from,
                                                        String toDisplayName,
                                                        String to)
        {


            return 0;
        }

        public Int32 onSendOutOfDialogMessageFailure(Int32 callbackIndex,
                                                        Int32 callbackObject,
                                                        Int32 messageId,
                                                        String fromDisplayName,
                                                        String from,
                                                        String toDisplayName,
                                                        String to,
                                                        String reason,
                                                        Int32 code)
        {
            return 0;
        }


        public Int32 onPlayAudioFileFinished(Int32 callbackIndex, Int32 callbackObject, Int32 sessionId, String fileName)
        {
            int i = findSession(sessionId);
            if (i == -1)
            {
                return 0;
            }


            string Text = "Play audio file - ";
            Text += fileName;
            Text += " end on line: ";
            Text += i.ToString();

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));

            return 0;
        }

        public Int32 onPlayVideoFileFinished(Int32 callbackIndex, Int32 callbackObject, Int32 sessionId)
        {
            int i = findSession(sessionId);
            if (i == -1)
            {
                return 0;
            }


            string Text = "Play video file - ";
            Text += " end on line: ";
            Text += i.ToString();

            ListBoxSIPLog.Dispatcher.Invoke(new Action(delegate
            {
                ListBoxSIPLog.Items.Add(Text);
            }));

            return 0;
        }


        public Int32 onReceivedRtpPacket(IntPtr callbackObject,
                                  Int32 sessionId,
                                  Boolean isAudio,
                                  byte[] RTPPacket,
                                  Int32 packetSize)
        {
            /*
                !!! IMPORTANT !!!

                Don't call any PortSIP SDK API functions in here directly. If you want to call the PortSIP API functions or 
                other code which will spend long time, you should post a message to main thread(main window) or other thread,
                let the thread to call SDK API functions or other code.

            */

            return 0;
        }

        public Int32 onSendingRtpPacket(IntPtr callbackObject,
                                  Int32 sessionId,
                                  Boolean isAudio,
                                  byte[] RTPPacket,
                                  Int32 packetSize)
        {

            /*
                !!! IMPORTANT !!!

                Don't call any PortSIP SDK API functions in here directly. If you want to call the PortSIP API functions or 
                other code which will spend long time, you should post a message to main thread(main window) or other thread,
                let the thread to call SDK API functions or other code.

            */
            return 0;
        }


        public Int32 onAudioRawCallback(IntPtr callbackObject,
                                               Int32 sessionId,
                                               Int32 callbackType,
                                               [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] data,
                                               Int32 dataLength,
                                               Int32 samplingFreqHz)
        {

            /*
                !!! IMPORTANT !!!

                Don't call any PortSIP SDK API functions in here directly. If you want to call the PortSIP API functions or 
                other code which will spend long time, you should post a message to main thread(main window) or other thread,
                let the thread to call SDK API functions or other code.

            */

            // The data parameter is audio stream as PCM format, 16bit, Mono.
            // the dataLength parameter is audio steam data length.



            //
            // IMPORTANT: the data length is stored in dataLength parameter!!!
            //

            AUDIOSTREAM_CALLBACK_MODE type = (AUDIOSTREAM_CALLBACK_MODE)callbackType;

            if (type == AUDIOSTREAM_CALLBACK_MODE.AUDIOSTREAM_LOCAL_MIX)
            {
                // The callback data is mixed from local record device - microphone
                // The sessionId is CALLBACK_SESSION_ID.PORTSIP_LOCAL_MIX_ID

            }
            else if (type == AUDIOSTREAM_CALLBACK_MODE.AUDIOSTREAM_REMOTE_MIX)
            {
                // The callback data is mixed from local record device - microphone
                // The sessionId is CALLBACK_SESSION_ID.PORTSIP_REMOTE_MIX_ID
            }
            else if (type == AUDIOSTREAM_CALLBACK_MODE.AUDIOSTREAM_LOCAL_PER_CHANNEL)
            {
                // The callback data is from local record device of each session, use the sessionId to identifying the session.
            }
            else if (type == AUDIOSTREAM_CALLBACK_MODE.AUDIOSTREAM_REMOTE_PER_CHANNEL)
            {
                // The callback data is received from remote side of each session, use the sessionId to identifying the session.
            }




            return 0;
        }


        public Int32 onVideoRawCallback(IntPtr callbackObject,
                                               Int32 sessionId,
                                               Int32 callbackType,
                                               Int32 width,
                                               Int32 height,
                                               [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 6)] byte[] data,
                                               Int32 dataLength)
        {
            /*
                !!! IMPORTANT !!!

                Don't call any PortSIP SDK API functions in here directly. If you want to call the PortSIP API functions or 
                other code which will spend long time, you should post a message to main thread(main window) or other thread,
                let the thread to call SDK API functions or other code.

                The video data format is YUV420, YV12.
            */

            //
            // IMPORTANT: the data length is stored in dataLength parameter!!!
            //

            VIDEOSTREAM_CALLBACK_MODE type = (VIDEOSTREAM_CALLBACK_MODE)callbackType;

            if (type == VIDEOSTREAM_CALLBACK_MODE.VIDEOSTREAM_LOCAL)
            {

            }
            else if (type == VIDEOSTREAM_CALLBACK_MODE.VIDEOSTREAM_REMOTE)
            {

            }


            return 0;

        }

        public Int32 onVideoDecoderCallback(IntPtr callbackObject,
                                        Int32 sessionId,
                                       Int32 width,
                                       Int32 height,
                                       Int32 framerate,
                                       Int32 bitrate)
        {
            /*
                !!! IMPORTANT !!!

                Don't call any PortSIP SDK API functions in here directly. If you want to call the PortSIP API functions or 
                other code which will spend long time, you should post a message to main thread(main window) or other thread,
                let the thread to call SDK API functions or other code.
            */
            return 0;
        }

    }
}

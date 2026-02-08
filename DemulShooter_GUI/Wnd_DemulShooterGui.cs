using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.RawInput;
using DsCore.Win32;
using System.Drawing;
using DemulShooter_GUI.Properties;

namespace DemulShooter_GUI
{    
    public partial class Wnd_DemulShooterGui : Form
    {
        private static Wnd_DemulShooterGui _This;
        private const string CONF_FILENAME = "config.ini";

        //Non-GUI settings (W.I.P -- they should be incliuded next)
        private int _HookTimeout = 0;

        //Available RawInput devices (filters by thir Type)
        private RawInputController[] _AvailableControllers;

        //Low-Level Hooks
        private Win32API.HookProc _KeyboardHookProc;
        private IntPtr _KeyboardHookID = IntPtr.Zero;        
      
        /*** XInput Controllers data ***/
        /*private const String XINPUT_DEVICE_PREFIX = "XInput Gamepad #";
        private XInputState[] _XInputStates;        
        private int _XInput_PollingInterval = 20; //ms*/
       
        /**** Directinput data ***/        
        private bool _Start_KeyRecord = false;
        private TextBox _SelectedTextBox;
        private String _SelectedTextBoxTextBackup = String.Empty;

        /*** Controllers ***/
        private List<GUI_Player> _GUI_Players;
        private List<GUI_AnalogCalibration> _GUI_AnalogCalibrations;

        /*** Timer used to display some kind of crosshair to test aim ***/
        private Timer CrosshairAimTimer;

        /// <summary>
        /// Construcor
        /// </summary>
        public Wnd_DemulShooterGui(bool IsVerbose)
        {           
            InitializeComponent();
            Logger.IsEnabled = IsVerbose;

            _This = this;
            this.Load += Wnd_DemulShooterGui_Load;

            Logger.WriteLog("");
            Logger.WriteLog("---------------- Program Start -- DemulShooter_GUI v" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString() + " ----------------");

            //Finding plugged devices
            _AvailableControllers = RawInputHelper.GetRawInputDevices(new RawInputDeviceType[] { RawInputDeviceType.RIM_TYPEHID, RawInputDeviceType.RIM_TYPEMOUSE });
            Logger.WriteLog("Found " + _AvailableControllers.Length + " available RawInput devices :");
            foreach (RawInputController c in _AvailableControllers)
            {
                try
                {
                    Logger.WriteLog(" + [" + c.DeviceType.ToString() + "] " + c.DeviceName);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Wnd_DemulShooterGui() ERROR : " + ex.Message.ToString());
                }
            }            

            //Reading config file to get parameters
            Configurator.GetInstance().ReadDsConfig(AppDomain.CurrentDomain.BaseDirectory + CONF_FILENAME);
            Configurator.GetInstance().Read_Sha_Conf();

            Logger.WriteLog("Initializing GUI [Players] pages...");
            int TabPageIndex = 0;
            _GUI_Players = new List<GUI_Player>();
            foreach (PlayerSettings PlayerData in Configurator.GetInstance().PlayersSettings)
            {
                if (PlayerData.Mode == PlayerSettings.PLAYER_MODE_RAWINPUT)
                {
                    //For each player, create a tab on the GUI
                    GUI_Player gPlayer = new GUI_Player(PlayerData, _AvailableControllers);
                    Logger.WriteLog("Adding PlayerData to the list...");
                    _GUI_Players.Add(gPlayer);
                    Logger.WriteLog("Adding Player tab to the GUI...");
                    tabControl1.TabPages[TabPageIndex].Controls.Add(gPlayer);
                    TabPageIndex++;
                }
                else
                {
                    //Logger.WriteLog("P" + Player.ID + " Gamepad ID = " + Player.GamepadID);
                }
            }

            //Fill Analog calibration Tab
            Logger.WriteLog("Initializing GUI [Analog calibration] page...");
            _GUI_AnalogCalibrations = new List<GUI_AnalogCalibration>();
            for (int i = 1; i <= 4; i++)
            {
                GUI_AnalogCalibration Calib = new GUI_AnalogCalibration(i, Configurator.GetInstance().GetPlayerSettings(i));
                TableLayout_Calib.Controls.Add(Calib);
                _GUI_AnalogCalibrations.Add(Calib);
            }            

            //Fill ActLabs tab
            Logger.WriteLog("Initializing GUI [Act Lab] page...");
            Cb_ActLabsOffset.Checked = Configurator.GetInstance().Act_Labs_Offset_Enable;
            Chk_DspCorrectedCrosshair.Checked = Configurator.GetInstance().Act_Labs_Display_Crosshair;
            Txt_ActLabs_X1.Text = Configurator.GetInstance().GetPlayerSettings(1).Act_Labs_Offset_X.ToString();
            Txt_ActLabs_Y1.Text = Configurator.GetInstance().GetPlayerSettings(1).Act_Labs_Offset_Y.ToString();
            Txt_ActLabs_X2.Text = Configurator.GetInstance().GetPlayerSettings(2).Act_Labs_Offset_X.ToString();
            Txt_ActLabs_Y2.Text = Configurator.GetInstance().GetPlayerSettings(2).Act_Labs_Offset_Y.ToString();
            Txt_ActLabs_X3.Text = Configurator.GetInstance().GetPlayerSettings(3).Act_Labs_Offset_X.ToString();
            Txt_ActLabs_Y3.Text = Configurator.GetInstance().GetPlayerSettings(3).Act_Labs_Offset_Y.ToString();
            Txt_ActLabs_X4.Text = Configurator.GetInstance().GetPlayerSettings(4).Act_Labs_Offset_X.ToString();
            Txt_ActLabs_Y4.Text = Configurator.GetInstance().GetPlayerSettings(4).Act_Labs_Offset_Y.ToString();            

            //Fill Model2 Tab
            TXT_CH_P1.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_M2_Crosshair_P1);
            TXT_CH_P2.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_M2_Crosshair_P2);
            TXT_CH_VIS.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_M2_Crosshair_Visibility);

            //Fill Silent Hill tab
            Logger.WriteLog("Initializing GUI [Silent Hill] pages...");
            TXT_P1_S.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Sha_P1_Start);
            TXT_P1_T.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Sha_P1_Trigger);
            TXT_P2_S.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Sha_P2_Start);
            TXT_P2_T.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Sha_P2_Trigger);
            TXT_EXIT.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Sha_Exit);
            TXT_SERVICE.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Sha_Service);
            TXT_TEST.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Sha_Test);

            //Fill Elevator Action Invasion tab
            Txt_EAI_P1_Start.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Eai_P1_Start);
            Txt_EAI_P2_Start.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Eai_P2_Start);
            Txt_EAI_P1_Credits.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Eai_P1_Credits);
            Txt_EAI_P2_Credits.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Eai_P2_Credits);
            Txt_EAI_Settings.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Eai_Settings);
            Txt_EAI_Up.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Eai_MenuUp);
            Txt_EAI_Down.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Eai_MenuDown);
            Txt_EAI_Enter.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Eai_MenuEnter);

            //Fill Gundam tab
            Logger.WriteLog("Initializing GUI [Gundam] pages...");
            Chk_GundamP1Pedal.Checked = Configurator.GetInstance().Gsoz_Pedal_P1_Enabled;
            Chk_GundamP2Pedal.Checked = Configurator.GetInstance().Gsoz_Pedal_P2_Enabled;
            TXT_GSOZ_PEDAL_1.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Gsoz_Pedal_P1);
            TXT_GSOZ_PEDAL_2.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Gsoz_Pedal_P2);

            //Fill Heavy Fire series tab
            Logger.WriteLog("Initializing GUI [Heavy Fire series] pages...");
            Gbox_HF_Cover.Enabled = false;
            Gbox_HF_Grenade.Enabled = false;
            Rdo_HF_MiddleGrenade.Checked = Configurator.GetInstance().HF_UseMiddleButtonAsGrenade;
            Rdo_HF_MiddleCover.Checked = !Configurator.GetInstance().HF_UseMiddleButtonAsGrenade;
            TrackBar_HF_Cover.Value = Configurator.GetInstance().HF_CoverSensibility;
            Chk_HF_ReverseCover.Checked = Configurator.GetInstance().HF_ReverseCover;
            
            //Fill Lethal Enforcers 3 tab
            Logger.WriteLog("Initializing GUI [Lethal Enforcers 3] pages...");
            Chk_Le3_EnablePedal1.Checked = Configurator.GetInstance().Le3_Pedal_P1_Enabled;
            Chk_Le3_EnablePedal2.Checked = Configurator.GetInstance().Le3_Pedal_P2_Enabled;
            TXT_LE3_PEDAL_1.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Le3_Pedal_P1);
            TXT_LE3_PEDAL_2.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Le3_Pedal_P2);

            //Fill Mission Impossible tab
            Logger.WriteLog("Initializing GUI [Mission Impossible] pages...");
            Rdo_MIA_Merge.Checked = Configurator.GetInstance().MissionImpossible_MergeTriggers;
            Rdo_MIA_Separate.Checked = !Configurator.GetInstance().MissionImpossible_MergeTriggers;

            //Fill Operation GHOST Tab
            Logger.WriteLog("Initializing GUI [Operation G.H.O.S.T] pages...");
            if (Configurator.GetInstance().OpGhost_EnableFreeplay)
                Cbox_OpGhost_Freeplay.SelectedIndex = 1;
            else
                Cbox_OpGhost_Freeplay.SelectedIndex = 0;
            Cbox_OpGhost_CreditsByCoin.SelectedIndex = Configurator.GetInstance().OpGhost_CoinsPerCredits - 1;
            Cbox_OpGhost_CreditsToStart.SelectedIndex = Configurator.GetInstance().OpGhost_CreditsToStart - 1;
            Cbox_OpGhost_CreditsToContinue.SelectedIndex = Configurator.GetInstance().OpGhost_CreditsToContinue - 1;
            Chk_OpGhost_SeparateButton.Checked = Configurator.GetInstance().OpGhost_SeparateButtons;
            TXT_OPGHOST_ACTION_P1.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_OpGhost_Action_P1);
            TXT_OPGHOST_ACTION_P2.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_OpGhost_Action_P2);

            //Fill RPCS3 Tab
            Logger.WriteLog("Initializing GUI [RPCS3] pages...");
            Txt_Rpcs3_P1_Start.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Rpcs3_P1_Start);
            Txt_Rpcs3_P2_Start.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Rpcs3_P2_Start);
            Txt_Rpcs3_Service.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Rpcs3_Service);
            Txt_Rpcs3_Up.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Rpcs3_Up);
            Txt_Rpcs3_Down.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Rpcs3_Down);
            Txt_Rpcs3_Enter.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Rpcs3_Enter);
            Txt_Rpcs3_3D_Switch.Text = GetKeyStringFromScanCode((int)Configurator.GetInstance().DIK_Rpcs3_3D_Switch);

            //Fill Output Tab
            Logger.WriteLog("Initializing GUI [Output] pages...");
            Cbox_Outputs.Checked = Configurator.GetInstance().OutputEnabled;
            Cbox_WmOutputs.Checked = Configurator.GetInstance().Wm_OutputEnabled;
            Cbox_NetOutputs.Checked = Configurator.GetInstance().Net_OutputEnabled;
            Txt_OutputDelay.Text = Configurator.GetInstance().OutputPollingDelay.ToString();
            Txt_OutputRecoilOn.Text = Configurator.GetInstance().OutputCustomRecoilOnDelay.ToString();
            Txt_OutputRecoilOff.Text = Configurator.GetInstance().OutputCustomRecoilOffDelay.ToString();
            Txt_OutputDamaged.Text = Configurator.GetInstance().OutputCustomDamagedDelay.ToString();           

            //Non-GUI settings
            _HookTimeout = Configurator.GetInstance().HookTimeout;

            // Register to rawinput
            Logger.WriteLog("Registering to RawInput service...");
            RawInputDevice[] rid = new RawInputDevice[3];
            rid[0].UsagePage = HidUsagePage.GENERIC;
            rid[0].Usage = HidUsage.Joystick;
            rid[0].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK;
            rid[0].hwndTarget = this.Handle;

            rid[1].UsagePage = HidUsagePage.GENERIC;
            rid[1].Usage = HidUsage.Mouse;
            rid[1].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK;
            rid[1].hwndTarget = this.Handle;

            rid[2].UsagePage = HidUsagePage.GENERIC;
            rid[2].Usage = HidUsage.Gamepad;
            rid[2].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK;
            rid[2].hwndTarget = this.Handle;
            if (!Win32API.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            {
                MessageBox.Show(Strings.Get("Msg_RawInputFailed"), Strings.Get("Title_Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            //Install Low Level keyboard hook
            Logger.WriteLog("Installing Low-Level Keyboard Hook...");
            ApplyKeyboardHook();                               
            
            Cbo_PageSettings.SelectedIndex = 0;

            CrosshairAimTimer = new Timer();
            CrosshairAimTimer.Tick += new EventHandler(CrosshairAimTimer_Tick);  
        }

        private void Wnd_DemulShooterGui_Load(object sender, EventArgs e)
        {
            this.Text = Strings.Get("Main_Title") + " " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
            ApplyLocalization();
        }

        /// <summary>
        /// Apply localized strings to all UI elements (Plan B - central resources).
        /// </summary>
        private void ApplyLocalization()
        {
            this.Text = Strings.Get("Main_Title") + " " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
            Btn_Save_P1.Text = Strings.Get("Btn_SaveConfig");
            Tab_P1.Text = Strings.Get("Tab_P1Config");
            Tab_P2.Text = Strings.Get("Tab_P2Config");
            Btn_SaveP2.Text = Strings.Get("Btn_SaveConfig");
            Tab_P3.Text = Strings.Get("Tab_P3Config");
            Btn_Save_P3.Text = Strings.Get("Btn_SaveConfig");
            Tab_P4.Text = Strings.Get("Tab_P4Config");
            Btn_Save_P4.Text = Strings.Get("Btn_SaveConfig");
            Tab_AnalogCalib.Text = Strings.Get("Tab_AnalogCalib");
            Btn_SaveAnalog.Text = Strings.Get("Btn_SaveConfig");
            Tab_ActLAbs.Text = Strings.Get("Tab_Calibration");
            Btn_ActLabs_Save.Text = Strings.Get("Btn_SaveConfig");
            groupBox3.Text = Strings.Get("Gbox_ActLabsPlaceholder");
            label47.Text = Strings.Get("Label_P4YOffset");
            label48.Text = Strings.Get("Label_P4XOffset");
            label49.Text = Strings.Get("Label_P3YOffset");
            label50.Text = Strings.Get("Label_P3XOffset");
            label18.Text = Strings.Get("Label_P2YOffset");
            label19.Text = Strings.Get("Label_P2XOffset");
            Chk_DspCorrectedCrosshair.Text = Strings.Get("Chk_DspCorrectedCrosshair");
            label17.Text = Strings.Get("Label_P1YOffset");
            label16.Text = Strings.Get("Label_P1XOffset");
            Cb_ActLabsOffset.Text = Strings.Get("Chk_ActLabsOffset");
            label15.Text = Strings.Get("Label_ActLabsHelp");
            Tab_Dolphin.Text = Strings.Get("Tab_Dolphin");
            label2.Text = Strings.Get("Label_DolphinHelp");
            Btn_Dolphin5.Text = Strings.Get("Btn_InstallWiimote");
            Tab_EAInvasion.Text = Strings.Get("Tab_EAInvasion");
            groupBox17.Text = Strings.Get("Gbox_SetKeyHelp");
            label76.Text = Strings.Get("Label_Settings");
            Btn_EAI_Save.Text = Strings.Get("Btn_SaveKeyConfig");
            label69.Text = Strings.Get("Label_Enter");
            label70.Text = Strings.Get("Label_Down");
            label71.Text = Strings.Get("Label_P1Credits");
            label72.Text = Strings.Get("Label_Up");
            label73.Text = Strings.Get("Label_P1Start");
            label74.Text = Strings.Get("Label_P2Start");
            label75.Text = Strings.Get("Label_P2Credits");
            groupBox16.Text = Strings.Get("Gbox_EAI_SelectDir");
            Btn_EAI_Patch.Text = Strings.Get("Btn_Patch");
            Btn_EAI_Open.Text = Strings.Get("Btn_Browse");
            Tab_GSOZ.Text = Strings.Get("Tab_GSOZ");
            Btn_Save_Gsoz.Text = Strings.Get("Btn_SaveConfig");
            label34.Text = Strings.Get("Label_GSOZ_PedalHelp");
            groupBox6.Text = Strings.Get("Gbox_Player2");
            label33.Text = Strings.Get("Label_SetPedalKey");
            Chk_GundamP2Pedal.Text = Strings.Get("Chk_EnablePedalMode");
            groupBox5.Text = Strings.Get("Gbox_Player1");
            label9.Text = Strings.Get("Label_SetPedalKey");
            Chk_GundamP1Pedal.Text = Strings.Get("Chk_EnablePedalMode");
            Tab_HeavyFire.Text = Strings.Get("Tab_HeavyFire");
            Rdo_HF_MiddleGrenade.Text = Strings.Get("Rdo_HF_MiddleGrenade");
            Rdo_HF_MiddleCover.Text = Strings.Get("Rdo_HF_MiddleCover");
            label27.Text = Strings.Get("Label_HF_CoverQTE");
            label40.Text = Strings.Get("Label_HF_GrenadeKey");
            label45.Text = Strings.Get("Label_HF_CoverSensibility");
            Chk_HF_ReverseCover.Text = Strings.Get("Chk_HF_ReverseCover");
            Btn_HF_Save.Text = Strings.Get("Btn_Save");
            Tab_LethalEnforcer3.Text = Strings.Get("Tab_LethalEnforcer3");
            Btn_Save_Le3.Text = Strings.Get("Btn_SaveConfig");
            label58.Text = Strings.Get("Label_LE3_PedalHelp");
            groupBox14.Text = Strings.Get("Gbox_Player2");
            label59.Text = Strings.Get("Label_SetPedalKey");
            Chk_Le3_EnablePedal2.Text = Strings.Get("Chk_EnablePedalMode");
            groupBox15.Text = Strings.Get("Gbox_Player1");
            label60.Text = Strings.Get("Label_SetPedalKey");
            Chk_Le3_EnablePedal1.Text = Strings.Get("Chk_EnablePedalMode");
            Tab_M2.Text = Strings.Get("Tab_M2");
            Cbox_M2_Flash.Text = Strings.Get("Cbox_M2_DisableFlash");
            label14.Text = Strings.Get("Label_CrosshairVisibility");
            label13.Text = Strings.Get("Label_P2ChangeCrosshair");
            label11.Text = Strings.Get("Label_P1ChangeCrosshair");
            Btn_M2Scripts.Text = Strings.Get("Btn_M2Scripts");
            label1.Text = Strings.Get("Label_M2_ScriptsHelp");
            Tab_MissionImpossible.Text = Strings.Get("Tab_MissionImpossible");
            groupBox4.Text = Strings.Get("Gbox_TriggersConfig");
            label42.Text = Strings.Get("Label_MergeTriggersHelp");
            Rdo_MIA_Merge.Text = Strings.Get("Rdo_MergeTriggers");
            Rdo_MIA_Separate.Text = Strings.Get("Rdo_SeparateTriggers");
            Btn_MisImp_Save.Text = Strings.Get("Btn_SaveConfig");
            Tab_OpGhost.Text = Strings.Get("Tab_OpGhost");
            label67.Text = Strings.Get("Label_OpGhost_SeparateActionHelp");
            groupBox18.Text = Strings.Get("Gbox_CoinSettings");
            label66.Text = Strings.Get("Label_CreditsToContinue");
            label65.Text = Strings.Get("Label_CreditsToStart");
            label64.Text = Strings.Get("Label_CoinsPerCredit");
            label62.Text = Strings.Get("Label_Freeplay");
            label61.Text = Strings.Get("Label_SetP2Key");
            label63.Text = Strings.Get("Label_SetP1Key");
            Chk_OpGhost_SeparateButton.Text = Strings.Get("Chk_OpGhost_SeparateAction");
            Btn_Save_OpGhost.Text = Strings.Get("Btn_SaveConfig");
            Tab_UnityPlugins.Text = Strings.Get("Tab_UnityPlugins");
            label36.Text = Strings.Get("Label_SelectUnityPlugin");
            Tab_Raccoon.Text = Strings.Get("Tab_Raccoon");
            groupBox21.Text = Strings.Get("Gbox_Raccoon_SelectDir");
            Btn_Raccoon_Patch.Text = Strings.Get("Btn_Patch");
            Btn_Raccoon_Open.Text = Strings.Get("Btn_Browse");
            Tab_RPCS3.Text = Strings.Get("Tab_RPCS3");
            Txt_Rpcs3_Save.Text = Strings.Get("Btn_SaveConfig");
            groupBox13.Text = Strings.Get("Gbox_PPUCache");
            Btn_Rpcs3_RazingStorm.Text = Strings.Get("Btn_RazingStorm");
            Btn_Rpcs3_SailorZombies.Text = Strings.Get("Btn_SailorZombie");
            Btn_Rpcs3_DarkEscape.Text = Strings.Get("Btn_DarkEscape");
            Btn_Rpcs3_DeadStorm.Text = Strings.Get("Btn_DeadStorm");
            groupBox12.Text = Strings.Get("Gbox_RPCS3_KeyConfig");
            label57.Text = Strings.Get("Label_PlayerButtons");
            label37.Text = Strings.Get("Label_TestMenuButtons");
            label56.Text = Strings.Get("Label_Service");
            label38.Text = Strings.Get("Label_3DSwitch");
            label55.Text = Strings.Get("Label_P2Start");
            label54.Text = Strings.Get("Label_P1Start_Short");
            label51.Text = Strings.Get("Label_Down");
            label53.Text = Strings.Get("Label_Up");
            label52.Text = Strings.Get("Label_Enter");
            Tab_SHA.Text = Strings.Get("Tab_SHA");
            label35.Text = Strings.Get("Gbox_SetKeyHelp");
            label8.Text = Strings.Get("Label_Service");
            Save_Sha_Keys.Text = Strings.Get("Btn_SaveKeyConfig");
            label7.Text = Strings.Get("Label_Test");
            label12.Text = Strings.Get("Label_P1Trigger");
            label6.Text = Strings.Get("Label_Exit");
            label5.Text = Strings.Get("Label_P1Start");
            label3.Text = Strings.Get("Label_P2Start");
            label4.Text = Strings.Get("Label_P2Trigger");
            Tab_Outputs.Text = Strings.Get("Tab_Outputs");
            Cbox_NetOutputs.Text = Strings.Get("Cbox_NetOutputs");
            Cbox_WmOutputs.Text = Strings.Get("Cbox_WmOutputs");
            Cbox_Outputs.Text = Strings.Get("Cbox_EnableOutputs");
            Btn_SaveOutput.Text = Strings.Get("Btn_SaveConfig");
            label21.Text = Strings.Get("Label_Milliseconds");
            label24.Text = Strings.Get("Label_OutputRecoilOff");
            label23.Text = Strings.Get("Label_Milliseconds");
            label20.Text = Strings.Get("Label_OutputDamaged");
            label10.Text = Strings.Get("Label_Milliseconds");
            label22.Text = Strings.Get("Label_OutputRecoilOn");
            label25.Text = Strings.Get("Label_Milliseconds");
            label26.Text = Strings.Get("Label_OutputDelay");
            label39.Text = Strings.Get("Label_PageSelection");
            Btn_Dcop.Text = Strings.Get("Btn_Dcop");
            Btn_Drk.Text = Strings.Get("Btn_Drakon");
            Btn_MarsS.Text = Strings.Get("Btn_MarsSortie");
            Btn_Mib.Text = Strings.Get("Btn_MIB");
            Btn_Mia.Text = Strings.Get("Btn_MissionImpossible");
            Btn_Nerfa.Text = Strings.Get("Btn_NerfArcade");
            Btn_Nha.Text = Strings.Get("Btn_NightHunter");
            Btn_Owr.Text = Strings.Get("Btn_OpWolfReturn");
            Btn_PvZ.Text = Strings.Get("Btn_PvZ");
            Btn_Rha.Text = Strings.Get("Btn_RabbidsHollywood");
            Btn_Tra.Text = Strings.Get("Btn_TombRaider");
            Btn_Pbx.Text = Strings.Get("Btn_PointBlankX");
            Btn_Wws.Text = Strings.Get("Btn_WildWestShootout");

            foreach (var p in _GUI_Players)
                p.ApplyLocalization();
            foreach (var c in _GUI_AnalogCalibrations)
                c.ApplyLocalization();
        }

        #region RAW_INPUT

        /// <summary>
        /// Get movements/click and process to Demul memory or GUI when visible
        /// </summary>
        private void ProcessRawInputMessage(IntPtr RawInputHandle)
        {
            foreach (RawInputController Controller in _AvailableControllers)
            {
                if (Controller.isSourceOfRawInputMessage(RawInputHandle))
                {
                    foreach (PlayerSettings Player in Configurator.GetInstance().PlayersSettings)
                    {
                        if (Player.DeviceName == Controller.DeviceName)
                        {
                            Controller.ProcessRawInputData(RawInputHandle);

                            if (tabControl1.SelectedTab == Tab_P1 || tabControl1.SelectedTab == Tab_P2 || tabControl1.SelectedTab == Tab_P3 || tabControl1.SelectedTab == Tab_P4)
                                _GUI_Players[Player.ID - 1].UpdateGui();
                            else if (tabControl1.SelectedTab == Tab_AnalogCalib)
                                _GUI_AnalogCalibrations[Player.ID - 1].UpdateValues();
                            else if (tabControl1.SelectedTab == Tab_ActLAbs)
                            {
                                if ((Player.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                                {
                                    DrawCrosshair(Player);
                                }
                            }

                                                       
                        }  
                    }
                }
            }
        }
        
        #endregion           
         
        /// <summary>
        /// Page Selection
        /// </summary>
        private void Cbo_PageSettings_SelectionChangeCommitted(object sender, EventArgs e)
        {
            tabControl1.SelectTab(Cbo_PageSettings.SelectedIndex);
        }

        #region Analog Calibration

        private void UpdateCalibration(int Player, int CurrentX, int CurrentY)
        {
            if (CurrentX > Configurator.GetInstance().PlayersSettings[Player - 1].AnalogManual_Xmax)
                Configurator.GetInstance().PlayersSettings[Player - 1].AnalogManual_Xmax = CurrentX;
            if (CurrentX < Configurator.GetInstance().PlayersSettings[Player - 1].AnalogManual_Xmin)
                Configurator.GetInstance().PlayersSettings[Player - 1].AnalogManual_Xmin = CurrentX;
            if (CurrentY > Configurator.GetInstance().PlayersSettings[Player - 1].AnalogManual_Ymax)
                Configurator.GetInstance().PlayersSettings[Player - 1].AnalogManual_Ymax = CurrentY;
            if (CurrentY < Configurator.GetInstance().PlayersSettings[Player - 1].AnalogManual_Ymin)
                Configurator.GetInstance().PlayersSettings[Player - 1].AnalogManual_Ymin = CurrentY;

        }

        private void Btn_SaveAnalog_Click(object sender, EventArgs e)
        {            
            if (Configurator.GetInstance().WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show(Strings.Get("Msg_ConfigSaved"));
            else
                MessageBox.Show(Strings.Get("Msg_ConfigSaveFailed"), Strings.Get("Title_DemulShooter"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion

        #region ActLabs Offset tab

        private void Cb_ActLabsOffset_CheckedChanged(object sender, EventArgs e)
        {
            if (Cb_ActLabsOffset.Checked)
            {
                Txt_ActLabs_X1.Enabled = true;
                Txt_ActLabs_Y1.Enabled = true;
                Txt_ActLabs_X2.Enabled = true;
                Txt_ActLabs_Y2.Enabled = true;
                Txt_ActLabs_X3.Enabled = true;
                Txt_ActLabs_Y3.Enabled = true;
                Txt_ActLabs_X4.Enabled = true;
                Txt_ActLabs_Y4.Enabled = true;
                Configurator.GetInstance().Act_Labs_Offset_Enable = true;
            }
            else
            {
                Txt_ActLabs_X1.Enabled = false;
                Txt_ActLabs_Y1.Enabled = false;
                Txt_ActLabs_X2.Enabled = false;
                Txt_ActLabs_Y2.Enabled = false;
                Txt_ActLabs_X3.Enabled = false;
                Txt_ActLabs_Y3.Enabled = false;
                Txt_ActLabs_X4.Enabled = false;
                Txt_ActLabs_Y4.Enabled = false;
                Configurator.GetInstance().Act_Labs_Offset_Enable = false;
            }
        }


        private void Chk_DspCorrectedCrosshair_CheckedChanged(object sender, EventArgs e)
        {
            Configurator.GetInstance().Act_Labs_Display_Crosshair = Chk_DspCorrectedCrosshair.Checked;
        }

        private void Txt_ActLabs_X1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Configurator.GetInstance().GetPlayerSettings(1).Act_Labs_Offset_X = Convert.ToInt32(Txt_ActLabs_X1.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_X1.Text + Strings.Get("Msg_InvalidXOffset"));
                Txt_ActLabs_X1.Text = Configurator.GetInstance().GetPlayerSettings(1).Act_Labs_Offset_X.ToString();
            }
        }
        private void Txt_ActLabs_Y1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Configurator.GetInstance().GetPlayerSettings(1).Act_Labs_Offset_Y = Convert.ToInt32(Txt_ActLabs_Y1.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_Y1.Text + Strings.Get("Msg_InvalidYOffset"));
                Txt_ActLabs_Y1.Text = Configurator.GetInstance().GetPlayerSettings(1).Act_Labs_Offset_Y.ToString();
            }
        }
        private void Txt_ActLabs_X2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Configurator.GetInstance().PlayersSettings[1].Act_Labs_Offset_X = Convert.ToInt32(Txt_ActLabs_X2.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_X2.Text + Strings.Get("Msg_InvalidXOffset"));
                Txt_ActLabs_X2.Text = Configurator.GetInstance().PlayersSettings[1].Act_Labs_Offset_X.ToString();
            }
        }
        private void Txt_ActLabs_Y2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Configurator.GetInstance().PlayersSettings[1].Act_Labs_Offset_Y = Convert.ToInt32(Txt_ActLabs_Y2.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_Y2.Text + Strings.Get("Msg_InvalidYOffset"));
                Txt_ActLabs_Y2.Text = Configurator.GetInstance().PlayersSettings[1].Act_Labs_Offset_Y.ToString();
            }
        }
        private void Txt_ActLabs_X3_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Configurator.GetInstance().PlayersSettings[2].Act_Labs_Offset_X = Convert.ToInt32(Txt_ActLabs_X3.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_X3.Text + Strings.Get("Msg_InvalidXOffset"));
                Txt_ActLabs_X3.Text = Configurator.GetInstance().PlayersSettings[2].Act_Labs_Offset_X.ToString();
            }
        }
        private void Txt_ActLabs_Y3_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Configurator.GetInstance().PlayersSettings[2].Act_Labs_Offset_Y = Convert.ToInt32(Txt_ActLabs_Y3.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_Y3.Text + Strings.Get("Msg_InvalidYOffset"));
                Txt_ActLabs_Y3.Text = Configurator.GetInstance().PlayersSettings[2].Act_Labs_Offset_Y.ToString();
            }
        }
        private void Txt_ActLabs_X4_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Configurator.GetInstance().PlayersSettings[3].Act_Labs_Offset_X = Convert.ToInt32(Txt_ActLabs_X4.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_X4.Text + Strings.Get("Msg_InvalidXOffset"));
                Txt_ActLabs_X4.Text = Configurator.GetInstance().PlayersSettings[3].Act_Labs_Offset_X.ToString();
            }
        }
        private void Txt_ActLabs_Y4_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Configurator.GetInstance().PlayersSettings[3].Act_Labs_Offset_Y = Convert.ToInt32(Txt_ActLabs_Y4.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_Y4.Text + Strings.Get("Msg_InvalidYOffset"));
                Txt_ActLabs_Y4.Text = Configurator.GetInstance().PlayersSettings[3].Act_Labs_Offset_Y.ToString();
            }
        }         
        private void Btn_ActLabs_Save_Click(object sender, EventArgs e)
        {
            if (Configurator.GetInstance().WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show(Strings.Get("Msg_ConfigSaved"));
            else
                MessageBox.Show(Strings.Get("Msg_ConfigSaveFailed"), Strings.Get("Title_DemulShooter"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void DrawCrosshair(PlayerSettings Player)
        {
            if (Chk_DspCorrectedCrosshair.Checked)
            {
                Color CrosshairColor = Color.Crimson;
                if (Player.ID == 2)
                    CrosshairColor = Color.Blue;
                else if (Player.ID == 3)
                    CrosshairColor = Color.LimeGreen;
                else if (Player.ID == 4)
                    CrosshairColor = Color.Gold;

                Player.RIController.Computed_X = ScreenScale(Player.RIController.Computed_X, Player.RIController.Axis_X_Min, Player.RIController.Axis_X_Max, 0, Screen.PrimaryScreen.WorkingArea.Width);
                Player.RIController.Computed_Y = ScreenScale(Player.RIController.Computed_Y, Player.RIController.Axis_Y_Min, Player.RIController.Axis_Y_Max, 0, Screen.PrimaryScreen.WorkingArea.Height);

                if (Configurator.GetInstance().Act_Labs_Offset_Enable)
                {
                    Player.RIController.Computed_X += Player.Act_Labs_Offset_X;
                    Player.RIController.Computed_Y += Player.Act_Labs_Offset_Y;
                }

                IntPtr desktopPtr = Win32API.GetDC(IntPtr.Zero);
                Graphics g = Graphics.FromHdc(desktopPtr);
                SolidBrush b = new SolidBrush(CrosshairColor);
                Pen p = new Pen(b, 2);
                g.DrawEllipse(p, Player.RIController.Computed_X - 20, Player.RIController.Computed_Y - 20, 40, 40);
                g.DrawEllipse(p, Player.RIController.Computed_X - 2, Player.RIController.Computed_Y - 2, 4, 4);

                g.Dispose();
                Win32API.ReleaseDC(IntPtr.Zero, desktopPtr);

                CrosshairAimTimer.Interval = 300;
                CrosshairAimTimer.Start();
            }
        }

        private void CrosshairAimTimer_Tick(object sender, EventArgs e)
        {
            CrosshairAimTimer.Stop();
            Win32API.InvalidateRect(IntPtr.Zero, IntPtr.Zero, true);            
        }

        /// <summary>
        /// Contains value inside min-max range
        /// </summary>
        protected int Clamp(int val, int minVal, int maxVal)
        {
            if (val > maxVal) return maxVal;
            else if (val < minVal) return minVal;
            else return val;
        }

        /// <summary>
        /// Transforming 0x0000-0xFFFF absolute rawdata to absolute x,y position on Desktop resolution
        /// </summary>
        public int ScreenScale(int val, int fromMinVal, int fromMaxVal, int toMinVal, int toMaxVal)
        {
            return ScreenScale(val, fromMinVal, fromMinVal, fromMaxVal, toMinVal, toMinVal, toMaxVal);
        }
        protected int ScreenScale(int val, int fromMinVal, int fromOffVal, int fromMaxVal, int toMinVal, int toOffVal, int toMaxVal)
        {
            double fromRange;
            double frac;
            if (fromMaxVal > fromMinVal)
            {
                val = Clamp(val, fromMinVal, fromMaxVal);
                if (val > fromOffVal)
                {
                    fromRange = (double)(fromMaxVal - fromOffVal);
                    frac = (double)(val - fromOffVal) / fromRange;
                }
                else if (val < fromOffVal)
                {
                    fromRange = (double)(fromOffVal - fromMinVal);
                    frac = (double)(val - fromOffVal) / fromRange;
                }
                else
                    return toOffVal;
            }
            else if (fromMinVal > fromMaxVal)
            {
                val = Clamp(val, fromMaxVal, fromMinVal);
                if (val > fromOffVal)
                {
                    fromRange = (double)(fromMinVal - fromOffVal);
                    frac = (double)(fromOffVal - val) / fromRange;
                }
                else if (val < fromOffVal)
                {
                    fromRange = (double)(fromOffVal - fromMaxVal);
                    frac = (double)(fromOffVal - val) / fromRange;
                }
                else
                    return toOffVal;
            }
            else
                return toOffVal;
            double toRange;
            if (toMaxVal > toMinVal)
            {
                if (frac >= 0)
                    toRange = (double)(toMaxVal - toOffVal);
                else
                    toRange = (double)(toOffVal - toMinVal);
                return toOffVal + (int)(toRange * frac);
            }
            else
            {
                if (frac >= 0)
                    toRange = (double)(toOffVal - toMaxVal);
                else
                    toRange = (double)(toMinVal - toOffVal);
                return toOffVal - (int)(toRange * frac);
            }
        }

        #endregion

        #region Modify/Create SHA key mapping

        private void Save_Sha_Keys_Click(object sender, EventArgs e)
        {
            if (Configurator.GetInstance().Write_Sha_Config())
                MessageBox.Show(Strings.Get("Msg_KeyMappingSaved"));
            else
                MessageBox.Show(Strings.Get("Msg_ShaConfigSaveFailed"), Strings.Get("Title_DemulShooter"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        
        #endregion

        #region Gundam:SoZ Tab

        private void Chk_GundamP1Pedal_CheckedChanged(object sender, EventArgs e)
        {
            Configurator.GetInstance().Gsoz_Pedal_P1_Enabled = Chk_GundamP1Pedal.Checked;
            if (Configurator.GetInstance().Gsoz_Pedal_P1_Enabled)
                TXT_GSOZ_PEDAL_1.Enabled = true;
            else
                TXT_GSOZ_PEDAL_1.Enabled = false;
        }
        private void Chk_GundamP2Pedal_CheckedChanged(object sender, EventArgs e)
        {
            Configurator.GetInstance().Gsoz_Pedal_P2_Enabled = Chk_GundamP2Pedal.Checked;
            if (Configurator.GetInstance().Gsoz_Pedal_P2_Enabled)
                TXT_GSOZ_PEDAL_2.Enabled = true;
            else
                TXT_GSOZ_PEDAL_2.Enabled = false;
        }
        private void Btn_Save_Gsoz_Click(object sender, EventArgs e)
        {
            if (Configurator.GetInstance().WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show(Strings.Get("Msg_ConfigSaved"));
            else
                MessageBox.Show(Strings.Get("Msg_ConfigSaveFailed"), Strings.Get("Title_DemulShooter"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion
        
        #region Dolphin Wiimote configuration

        private void Btn_Dolphin5_Click(object sender, EventArgs e)
        {
            InstallWiimoteconfigFile(DemulShooter_GUI.Properties.Resources.WiimoteNew_v5);
        }
        private void InstallWiimoteconfigFile(String ResourceFile)
        {
            String Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Dolphin Emulator\Config";
            bool IsAimtrak = false;
            bool Overwritten = false;
            try
            {
                if (File.Exists(Path + @"\WiimoteNew.ini"))
                {
                    File.Copy(Path + @"\WiimoteNew.ini", Path + @"\WiimoteNew.bak.ini", true);
                    Overwritten = true;
                }
                //Copying file
                using (StreamWriter sw = new StreamWriter(Path + @"\WiimoteNew.ini", false))
                {
                    sw.Write(ResourceFile);
                    sw.Close();
                }
                //Modifying Devices Names according to config.ini
                int P2_Atrak_ID = 0;
                try
                {
                    String VID = (Configurator.GetInstance().PlayersSettings[0].DeviceName.ToUpper().Split(new string[] { "VID_" }, System.StringSplitOptions.RemoveEmptyEntries))[1].Substring(0, 4);
                    String PID = (Configurator.GetInstance().PlayersSettings[1].DeviceName.ToUpper().Split(new string[] { "PID_" }, System.StringSplitOptions.RemoveEmptyEntries))[1].Substring(0, 4);
                    if (VID.Equals("D209") && PID.StartsWith("16"))
                    {
                        IsAimtrak = true;
                        P2_Atrak_ID = int.Parse(PID.Substring(2, 2));
                        int bID = 0x30 + P2_Atrak_ID;
                        using (BinaryWriter fileWriter = new BinaryWriter(File.Open(Path + @"\WiimoteNew.ini", System.IO.FileMode.Open)))
                        {
                            fileWriter.BaseStream.Position = 0x2E7; // set the offset
                            fileWriter.Write((byte)bID);
                            fileWriter.BaseStream.Position = 0x315; // set the offset
                            fileWriter.Write((byte)bID);
                            fileWriter.BaseStream.Position = 0x343; // set the offset
                            fileWriter.Write((byte)bID);
                            fileWriter.BaseStream.Position = 0x372; // set the offset
                            fileWriter.Write((byte)bID);
                        }                        
                    }
                }
                catch 
                {
                    //String m = "P2 Wiimote axis device could not be modified, default is :\n \"DInput/0/ATRAK Device #2\"";
                    //MessageBox.Show(m, "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }                

                //Dialog Message
                String message = "File \"" + Path + "\\WiimoteNew.ini\" successfully written !";
                if (Overwritten)
                    message += "\n\nThe existing WiimoteNew.ini was backed-up to \"WiimoteNew.bak.ini\"\n\n";
                MessageBox.Show(message, Strings.Get("Title_DemulShooter"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                if (IsAimtrak)
                {
                    message = "Aimtrak detected for Player2 device, Aimtrak ID = " + P2_Atrak_ID;
                    message += "\n\nAccording to DemulShooter config, P2 Wiimote axis device modified to : \n\"DInput/0/ATRAK Device #" + P2_Atrak_ID + "\"";
                    MessageBox.Show(message, Strings.Get("Title_DemulShooter"), MessageBoxButtons.OK, MessageBoxIcon.Information);                
                }
                else
                {
                    message = "No Aimtrak detected for Player2.";
                    message += "\n\nP2 Wiimote axis device will keep default value :\n \"DInput/0/ATRAK Device #2\"";
                    MessageBox.Show(message, Strings.Get("Title_DemulShooter"), MessageBoxButtons.OK, MessageBoxIcon.Warning);                
                }                
            }
            catch (Exception ex)
            {
                MessageBox.Show(Strings.Get("Msg_CantInstallWiimoteIni") + "\n\n" + ex.Message.ToString(), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Elevator Action Invasion

        private string _EAI_ExeFilename = "ESGame-Win64-Shipping.exe";
        private string _EAI_UsbDllFilename = "UsbPluginsDll.dll";

        private void Btn_EAI_Open_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select your " + _EAI_ExeFilename + " location :";
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (fbd.SelectedPath != string.Empty)
                    {
                        if (File.Exists(fbd.SelectedPath + "\\" + _EAI_ExeFilename))
                        {
                            Txt_EAI_FolderPath.Text = fbd.SelectedPath;
                            Btn_EAI_Patch.Enabled = true;
                        }
                        else
                        {
                            MessageBox.Show(_EAI_ExeFilename + Strings.Get("Msg_ExeNotFound") + fbd.SelectedPath, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void Btn_EAI_Patch_Click(object sender, EventArgs e)
        {
            string ExeFilePath = Txt_EAI_FolderPath.Text + "\\" + _EAI_ExeFilename;
            string UsbDllFilePath = Txt_EAI_FolderPath.Text + "\\..\\..\\Plugins\\" + _EAI_UsbDllFilename;

            //Main exe patching
            try
            {
                using (BinaryWriter bw = new BinaryWriter(File.Open(ExeFilePath, FileMode.Open, FileAccess.ReadWrite)))
                {
                    //Force overriding read data from the COM port by our own
                    //Creating codecave
                    bw.BaseStream.Seek(0x292C9D4, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0x4C, 0x8D, 0x35, 0x95, 0x44, 0x16, 0x01, 0x41, 0x0F, 0xB6, 0x9E, 0xCE, 0x02, 0x00, 0x00, 0xE9, 0xF6, 0x50, 0xE6, 0xFD });
                    //Injection
                    bw.BaseStream.Seek(0x791AD6, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xE9, 0xF9, 0xAE, 0x19, 0x02, 0x90, 0x90, 0x90 });

                    //Calls to the usb dll credits function will not return any good values (pointer not existing, no data to write/read)
                    //There are values that can be used in the main .exe :
                    //For Credits we will force the game to read our own value    
                    bw.BaseStream.Seek(0x7B77BF, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0x48, 0x8B, 0xF9, 0x48, 0x8D, 0x05, 0x93, 0x96, 0x2D, 0x03, 0x48, 0xC1, 0xE7, 0x04, 0x48, 0x01, 0xF8, 0x8B, 0x00, 0xE9, 0x81, 0x00, 0x00, 0x00, 0x00 });
                    //For Credits we will force the game to write to our own values
                    bw.BaseStream.Seek(0x7B767E, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0x48, 0x8D, 0x05, 0xD7, 0x97, 0x2D, 0x03, 0xC1, 0xE5, 0x04, 0x48, 0x01, 0xE8, 0x8B, 0x28, 0x29, 0xD5, 0x89, 0x28, 0xE9, 0x91, 0x00, 0x00, 0x00, 0x90 });

                    //Force overriding bad read values from GUN api with good flag, and force values to be read from another memory location
                    //which then will be written by any tool to feed data
                    //Creating codecave
                    bw.BaseStream.Seek(0x292C97F, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xCC, 0xCC, 0xCC, 0x50, 0x53, 0x48, 0xC1, 0xE3, 0x04, 0x48, 0x31, 0xC0, 0x42, 0xC7, 0x44, 0x39, 0x34, 0x01, 0x00, 0x00, 0x00, 0x48, 0x8D, 0x05, 0xB5, 0x44, 0x16, 0x01, 0x48, 0x01, 0xD8, 0x8B, 0x00, 0x42, 0x89, 0x44, 0x39, 0x14, 0x48, 0x8D, 0x05, 0xA8, 0x44, 0x16, 0x01, 0x48, 0x01, 0xD8, 0x8B, 0x00, 0x42, 0x89, 0x44, 0x39, 0x18, 0x48, 0x8D, 0x05, 0x9B, 0x44, 0x16, 0x01, 0x48, 0x01, 0xD8, 0x8B, 0x00, 0x42, 0x89, 0x44, 0x39, 0x51, 0x5B, 0x58, 0x42, 0x83, 0x7C, 0x39, 0x34, 0x00, 0xE9, 0xD9, 0x60, 0xE8, 0xFD });
                    //Injection
                    bw.BaseStream.Seek(0x7B2AA7, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xE9, 0xD6, 0x9E, 0x17, 0x02, 0x90 });

                    //Force return OK to IsGunCalibrated call
                    bw.BaseStream.Seek(0x772D17, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xB0, 0x01 });
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Strings.Get("Msg_ErrorPatchingExe") + ExeFilePath + " : \n" + Ex.Message.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            //USB dll patching
            try
            {
                using (BinaryWriter bw = new BinaryWriter(File.Open(UsbDllFilePath, FileMode.Open, FileAccess.ReadWrite)))
                {

                    //Force return true for BIND_InitDongle()
                    bw.BaseStream.Seek(0x134CB, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0x90, 0x90, });
                    //Force return true for BIND_IsDongleVerify()
                    bw.BaseStream.Seek(0x136F7, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xB0, 0x01 });

                    //Force return true to BIND_OpenESIODevice without opening COM port at all
                    bw.BaseStream.Seek(0x137B0, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 });

                    //Force return true for BIND_IsExistESIODevice()                    
                    bw.BaseStream.Seek(0xA5A0, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0x90, 0x90 });

                    //Force return true for BIND_GetESIoData()
                    //That way the game will constantly call the ABPGameState::PArseIoData where we will inject our values
                    bw.BaseStream.Seek(0x12F10, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 });
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Strings.Get("Msg_ErrorPatchingExe") + UsbDllFilePath + " : \n" + Ex.Message.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            MessageBox.Show(Strings.Get("Msg_PatchComplete"), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Btn_EAI_Save_Click(object sender, EventArgs e)
        {
            if (Configurator.GetInstance().WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show(Strings.Get("Msg_ConfigSaved"));
            else
                MessageBox.Show(Strings.Get("Msg_ConfigSaveFailed"), Strings.Get("Title_DemulShooter"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion

        #region Model2 Emulator tab

        private void Btn_M2Scripts_Click(object sender, EventArgs e)
        {
            InstallM2Scripts();
        }
        private void InstallM2Scripts()
        {
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK)
                return;
            try
            {
                StreamWriter streamWriter = new StreamWriter(folderBrowserDialog1.SelectedPath + "\\scripts\\demulshooter.lua", false);
                streamWriter.WriteLine("-- This file is automatically generated by Demulshooter\n");
                streamWriter.WriteLine("P1_ChangeCrosshairKey=0x" + ((byte)Configurator.GetInstance().DIK_M2_Crosshair_P1).ToString("X2"));
                streamWriter.WriteLine("P2_ChangeCrosshairKey=0x" + ((byte)Configurator.GetInstance().DIK_M2_Crosshair_P2).ToString("X2"));
                streamWriter.WriteLine("CrosshairVisibilityKey=0x" + ((byte)Configurator.GetInstance().DIK_M2_Crosshair_Visibility).ToString("X2"));
                streamWriter.WriteLine();
                streamWriter.WriteLine("function lines_from(file)");
                streamWriter.WriteLine("\tlines = {}");
                streamWriter.WriteLine("\tfor line in io.lines(file) do");
                streamWriter.WriteLine("\t\tlines[#lines + 1] = line");
                streamWriter.WriteLine("\tend");
                streamWriter.WriteLine("\treturn lines");
                streamWriter.WriteLine("end");
                streamWriter.Close();
                Directory.CreateDirectory(folderBrowserDialog1.SelectedPath + "\\artwork\\crosshairs");
                foreach (FileInfo file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\m2emulator\\artwork\\crosshairs").GetFiles())
                    file.CopyTo(folderBrowserDialog1.SelectedPath + "\\artwork\\crosshairs\\" + file.Name, true);

                String FlashState = "flash";
                if (Cbox_M2_Flash.Checked)
                    FlashState = "noflash";
                foreach (FileInfo file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\m2emulator\\scripts\\" + FlashState).GetFiles())
                    file.CopyTo(folderBrowserDialog1.SelectedPath + "\\scripts\\" + file.Name, true);
                MessageBox.Show(Strings.Get("Msg_ScriptsInstalled"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(Strings.Get("Msg_CantInstallM2Scripts") + "\n\n" + ex.Message.ToString(), "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        #endregion

        #region Heavy Fire Afghanistan tab

        private void Rdo_HF_MiddleCover_CheckedChanged(object sender, EventArgs e)
        {
            Gbox_HF_Cover.Enabled = Rdo_HF_MiddleCover.Checked;
            Gbox_HF_Grenade.Enabled = !Rdo_HF_MiddleCover.Checked;
            Configurator.GetInstance().HF_UseMiddleButtonAsGrenade = !Rdo_HF_MiddleCover.Checked;
        }

        private void Rdo_HF_MiddleClickGrenade_CheckedChanged(object sender, EventArgs e)
        {
            Gbox_HF_Grenade.Enabled = Rdo_HF_MiddleGrenade.Checked;
            Gbox_HF_Cover.Enabled = !Rdo_HF_MiddleGrenade.Checked;
            Configurator.GetInstance().HF_UseMiddleButtonAsGrenade = Rdo_HF_MiddleGrenade.Checked;
        }
        
        private void TrackBar_HF_Cover_ValueChanged(object sender, EventArgs e)
        {
            Configurator.GetInstance().HF_CoverSensibility = TrackBar_HF_Cover.Value;
        }
        private void Chk_HF_ReverseCover_CheckedChanged(object sender, EventArgs e)
        {
            Configurator.GetInstance().HF_ReverseCover = Chk_HF_ReverseCover.Checked;
        }
        private void Btn_HF_Save_Click(object sender, EventArgs e)
        {
            if (Configurator.GetInstance().WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show(Strings.Get("Msg_ConfigSaved"));
            else
                MessageBox.Show(Strings.Get("Msg_ConfigSaveFailed"), Strings.Get("Title_DemulShooter"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion      
        
        #region Lethal Enforcer 3 tab

        private void Chk_Le3_EnablePedal1_CheckedChanged(object sender, EventArgs e)
        {
            Configurator.GetInstance().Le3_Pedal_P1_Enabled = Chk_Le3_EnablePedal1.Checked;
            if (Configurator.GetInstance().Le3_Pedal_P1_Enabled)
                TXT_LE3_PEDAL_1.Enabled = true;
            else
                TXT_LE3_PEDAL_1.Enabled = false;
        }

        private void Chk_Le3_EnablePedal2_CheckedChanged(object sender, EventArgs e)
        {
            Configurator.GetInstance().Le3_Pedal_P2_Enabled = Chk_Le3_EnablePedal2.Checked;
            if (Configurator.GetInstance().Le3_Pedal_P2_Enabled)
                TXT_LE3_PEDAL_2.Enabled = true;
            else
                TXT_LE3_PEDAL_2.Enabled = false;
        }

        private void Btn_Save_Le3_Click(object sender, EventArgs e)
        {
            if (Configurator.GetInstance().WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show(Strings.Get("Msg_ConfigSaved"));
            else
                MessageBox.Show(Strings.Get("Msg_ConfigSaveFailed"), Strings.Get("Title_DemulShooter"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #region Mission Impossible Tab

        private void Rdo_MIA_Merge_CheckedChanged(object sender, EventArgs e)
        {
            Configurator.GetInstance().MissionImpossible_MergeTriggers = Rdo_MIA_Merge.Checked;
        }

        private void Rdo_MIA_Separate_CheckedChanged(object sender, EventArgs e)
        {
            Configurator.GetInstance().MissionImpossible_MergeTriggers = Rdo_MIA_Merge.Checked;
        }

        private void Btn_MisImp_Save_Click(object sender, EventArgs e)
        {
            if (Configurator.GetInstance().WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show(Strings.Get("Msg_ConfigSaved"));
            else
                MessageBox.Show(Strings.Get("Msg_ConfigSaveFailed"), Strings.Get("Title_DemulShooter"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        

        #endregion

        #endregion

        #region Operation Ghost Tab

        private void Cbox_OpGhost_Freeplay_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Cbox_OpGhost_Freeplay.SelectedIndex == 0)
                Configurator.GetInstance().OpGhost_EnableFreeplay = false;
            else
                Configurator.GetInstance().OpGhost_EnableFreeplay = true;
        }

        private void Cbox_OpGhost_CreditsByCoin_SelectedIndexChanged(object sender, EventArgs e)
        {
            Configurator.GetInstance().OpGhost_CoinsPerCredits = Cbox_OpGhost_CreditsByCoin.SelectedIndex + 1;
        }

        private void Cbox_OpGhost_CreditsToStart_SelectedIndexChanged(object sender, EventArgs e)
        {
            Configurator.GetInstance().OpGhost_CreditsToStart = Cbox_OpGhost_CreditsToStart.SelectedIndex + 1;
        }

        private void Cbox_OpGhost_CreditsToContinue_SelectedIndexChanged(object sender, EventArgs e)
        {
            Configurator.GetInstance().OpGhost_CreditsToContinue = Cbox_OpGhost_CreditsToContinue.SelectedIndex + 1;
        } 

        private void Chk_OpGhost_SeparateButton_CheckedChanged(object sender, EventArgs e)
        {
            Configurator.GetInstance().OpGhost_SeparateButtons = Chk_OpGhost_SeparateButton.Checked;
            if (Configurator.GetInstance().OpGhost_SeparateButtons)
            {
                TXT_OPGHOST_ACTION_P1.Enabled = true;
                TXT_OPGHOST_ACTION_P2.Enabled = true;
            }
            else
            {
                TXT_OPGHOST_ACTION_P1.Enabled = false;
                TXT_OPGHOST_ACTION_P2.Enabled = false;
            }
        }

        private void Btn_Save_OpGhost_Click(object sender, EventArgs e)
        {
            if (Configurator.GetInstance().WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show(Strings.Get("Msg_ConfigSaved"));
            else
                MessageBox.Show(Strings.Get("Msg_ConfigSaveFailed"), Strings.Get("Title_DemulShooter"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion
              
        #region Unity Plugin Installation Tab

        private void Btn_InstallUnityPlugin_Click(object sender, EventArgs e)
        {
            Button Btn = (Button)sender;
            string SrcUnityFolder = string.Empty;
            string ExeName = string.Empty;
            switch (Btn.Name)
            {
                case "Btn_Dcop": SrcUnityFolder = "DCOP"; break;
                case "Btn_Drk": SrcUnityFolder = "Drakon"; break;
                case "Btn_MarsS": SrcUnityFolder = "MarsSortie"; break;
                case "Btn_Mia": SrcUnityFolder = "MissionImpossible"; break;
                case "Btn_Mib": SrcUnityFolder = "MenInBlack"; break;
                case "Btn_Nerfa": SrcUnityFolder = "NerfArcade"; break;
                case "Btn_Nha": SrcUnityFolder = "NightHunter"; break;
                case "Btn_Owr": SrcUnityFolder = "OperationWolfReturn"; break;
                case "Btn_PvZ": SrcUnityFolder = "PlantsVsZombies"; break;
                case "Btn_Pbx": SrcUnityFolder = "PointBlankX"; break;
                case "Btn_Rha": SrcUnityFolder = "RabbidsHollywood"; break;
                case "Btn_Tra": SrcUnityFolder = "TombRaider"; break;
                case "Btn_Wws": SrcUnityFolder = "WildWestShootout"; break;
                default: break;
            }

            folderBrowserDialog1.Description = "Please select the Unity game executable installation folder";
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Install_Unity_Plugin(SrcUnityFolder, folderBrowserDialog1.SelectedPath);
            }
        }
        private void Install_Unity_Plugin(string SrcFolder, string DstFolder)
        {
            if (!CloneDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\Unity\\" + SrcFolder, DstFolder))
                MessageBox.Show(Strings.Get("Msg_UnityPluginCopyFailed") + "\n" + DstFolder, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                MessageBox.Show(Strings.Get("Msg_UnityPluginInstalled"));
        }

        #endregion

        #region Raccoon Rampage Tab

        private string _RACCOON_ExeFilename = "RSGame-Win64-Shipping.exe";
        private string _RACCOON_UsbDllFilename = "UsbPluginsDll.dll";

        private void Btn_Raccoon_Open_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select your " + _RACCOON_ExeFilename + " location :";
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (fbd.SelectedPath != string.Empty)
                    {
                        if (File.Exists(fbd.SelectedPath + "\\" + _RACCOON_ExeFilename))
                        {
                            Txt_Raccoon_FolderPath.Text = fbd.SelectedPath;
                            Btn_Raccoon_Patch.Enabled = true;
                        }
                        else
                        {
                            MessageBox.Show(_RACCOON_ExeFilename + Strings.Get("Msg_ExeNotFound") + fbd.SelectedPath, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void Btn_Raccoon_Patch_Click(object sender, EventArgs e)
        {
            string ExeFilePath = Txt_Raccoon_FolderPath.Text + "\\" + _RACCOON_ExeFilename;
            string UsbDllFilePath = Txt_Raccoon_FolderPath.Text + "\\..\\..\\Plugins\\" + _RACCOON_UsbDllFilename;
            
            //USB dll patching
            try
            {
                using (BinaryWriter bw = new BinaryWriter(File.Open(UsbDllFilePath, FileMode.Open, FileAccess.ReadWrite)))
                {
                    //Force return true for BIND_InitDongle()
                    bw.BaseStream.Seek(0x14D2B, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0x90, 0x90, });
                    //Force return true for BIND_IsDongleVerify()
                    bw.BaseStream.Seek(0x14FE9, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xB0, 0x01 });   
  
                    //Force good  BIND_InitUnisIo()
                    bw.BaseStream.Seek(0x14DEA, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xEB, 0x14 });

                    //Force return true for BIND_IsIoWorking()
                    bw.BaseStream.Seek(0x15127, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xEB, 0x1D });

                    //Force return -1 for BIND_DWInit() and Skipping 255 COM port loop opening to try to find a Card Reader
                    bw.BaseStream.Seek(0xCC80, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xE9, 0x63, 0x02, 0x00, 0x00 });                    
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Strings.Get("Msg_ErrorPatchingExe") + UsbDllFilePath + " : \n" + Ex.Message.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            MessageBox.Show(Strings.Get("Msg_PatchComplete"), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region RPCS3 Tab

        private void Btn_Rpcs3_DeadStorm_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select \"DeadStorm Pirates\" rpcs3-gun.exe folder :";
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && fbd.SelectedPath != string.Empty)
                {
                    string CacheFolder = fbd.SelectedPath + @"\cache\SCEEXE000\ppu-obiMX8TqMzUsChXLV1Ln5TAJegSZ-EBOOT.BIN\";
                    if (!Directory.Exists(CacheFolder))
                    {
                        MessageBox.Show(Strings.Get("Msg_DirNotFound") + "\n " + CacheFolder, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    foreach (FileInfo file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\RPCS3\\DeadStorm").GetFiles())
                    {
                        Patch_RPCS3_PPU_CacheFile(CacheFolder, file.Name, file);
                    }
                }
            }
        }

        private void Btn_Rpcs3_DarkEscape_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select \"Dark Escape\" rpcs3-gun.exe folder :";
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && fbd.SelectedPath != string.Empty)
                {
                    string CacheFolder = fbd.SelectedPath + @"\cache\SCEEXE000\ppu-gfm17oJj1cUecjZQ8dVv46oQv2iW-EBOOT.BIN\";
                    if (!Directory.Exists(CacheFolder))
                    {
                        MessageBox.Show(Strings.Get("Msg_DirNotFound") + "\n " + CacheFolder, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    foreach (FileInfo file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\RPCS3\\DarkEscape").GetFiles())
                    {
                        Patch_RPCS3_PPU_CacheFile(CacheFolder, file.Name, file);
                    }
                }
            }
        }

        private void Btn_Rpcs3_SailorZombie_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select \"Sailor Zombie\" rpcs3-gun.exe folder :";
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && fbd.SelectedPath != string.Empty)
                {
                    string CacheFolder = fbd.SelectedPath + @"\cache\SCEEXE000\ppu-se1PtZVS5iF9A6M5Y1vu0wNqiASu-EBOOT.BIN\";
                    if (!Directory.Exists(CacheFolder))
                    {
                        MessageBox.Show(Strings.Get("Msg_DirNotFound") + "\n " + CacheFolder, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    foreach (FileInfo file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\RPCS3\\SailorZombie").GetFiles())
                    {
                        Patch_RPCS3_PPU_CacheFile(CacheFolder, file.Name, file);
                    }
                }
            }
        }

        private void Btn_Rpcs3_RazingStorm_Click(object sender, EventArgs e)
        {

        }

        private void Txt_Rpcs3_Save_Click(object sender, EventArgs e)
        {
            if (Configurator.GetInstance().WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show(Strings.Get("Msg_ConfigSaved"));
            else
                MessageBox.Show(Strings.Get("Msg_ConfigSaveFailed"), Strings.Get("Title_DemulShooter"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        
        private void Patch_RPCS3_PPU_CacheFile(string CacheFolder, string CacheFileName, FileInfo SourceFile)
        {
            string CPU_Type = string.Empty;
            foreach (String sCacheFile in Directory.GetFiles(CacheFolder))
            {
                String sCacheFileNameShort = Path.GetFileName(sCacheFile);

                if (sCacheFileNameShort.StartsWith(CacheFileName))
                {
                    try
                    {
                        SourceFile.CopyTo(sCacheFile, true);
                        MessageBox.Show(Strings.Get("Msg_PPUCacheReplaced") + sCacheFile, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(Strings.Get("Msg_PPUCacheWriteError") + sCacheFile + "\n\n" + ex.Message.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }                    
                    return;
                }
            }
            MessageBox.Show(Strings.Get("Msg_PPUCacheNotFound") + "\n " + CacheFileName, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        #endregion

        #region Outputs tab

        private void Cbox_Outputs_CheckedChanged(object sender, EventArgs e)
        {
            Txt_OutputDelay.Enabled = Cbox_Outputs.Checked;
            Txt_OutputRecoilOn.Enabled = Cbox_Outputs.Checked;
            Txt_OutputRecoilOff.Enabled = Cbox_Outputs.Checked;
            Txt_OutputDamaged.Enabled = Cbox_Outputs.Checked;

            Cbox_WmOutputs.Enabled = Cbox_Outputs.Checked;
            Cbox_NetOutputs.Enabled = Cbox_Outputs.Checked;
            Configurator.GetInstance().OutputEnabled = Cbox_Outputs.Checked;
        }


        private void Cbox_WmOutputs_CheckedChanged(object sender, EventArgs e)
        {
            if (!Cbox_WmOutputs.Checked)
            {
                if (!Cbox_NetOutputs.Checked)
                    Cbox_WmOutputs.Checked = true;
            }
            Configurator.GetInstance().Wm_OutputEnabled = Cbox_WmOutputs.Checked;
        }

        private void Cbox_NetOutputs_CheckedChanged(object sender, EventArgs e)
        {
            if (!Cbox_NetOutputs.Checked)
            {
                if (!Cbox_WmOutputs.Checked)
                    Cbox_NetOutputs.Checked = true;
            }
            Configurator.GetInstance().Net_OutputEnabled = Cbox_NetOutputs.Checked;
        }

        private void Txt_OutputDelay_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Configurator.GetInstance().OutputPollingDelay = Convert.ToInt32(Txt_OutputDelay.Text);
            }
            catch
            {
                MessageBox.Show(Txt_OutputDelay.Text + Strings.Get("Msg_InvalidDelay"));
                Txt_OutputDelay.Text = Configurator.GetInstance().OutputPollingDelay.ToString();
            }
        }

        private void Txt_OutputRecoilOn_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Configurator.GetInstance().OutputCustomRecoilOnDelay = Convert.ToInt32(Txt_OutputRecoilOn.Text);
            }
            catch
            {
                MessageBox.Show(Txt_OutputRecoilOn.Text + Strings.Get("Msg_InvalidDelay"));
                Txt_OutputRecoilOn.Text = Configurator.GetInstance().OutputCustomRecoilOnDelay.ToString();
            }
        }

        private void Txt_OutputRecoilOff_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Configurator.GetInstance().OutputCustomRecoilOffDelay = Convert.ToInt32(Txt_OutputRecoilOff.Text);
            }
            catch
            {
                MessageBox.Show(Txt_OutputRecoilOff.Text + Strings.Get("Msg_InvalidDelay"));
                Txt_OutputRecoilOff.Text = Configurator.GetInstance().OutputCustomRecoilOffDelay.ToString();
            }
        }

        private void Txt_OutputDamaged_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Configurator.GetInstance().OutputCustomDamagedDelay = Convert.ToInt32(Txt_OutputDamaged.Text);
            }
            catch
            {
                MessageBox.Show(Txt_OutputDamaged.Text + Strings.Get("Msg_InvalidDelay"));
                Txt_OutputDamaged.Text = Configurator.GetInstance().OutputCustomDamagedDelay.ToString();
            }
        }

        private void Btn_Save_Cfg_Click(object sender, EventArgs e)
        {
            if (Configurator.GetInstance().WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show(Strings.Get("Msg_ConfigSaved"));
            else
                MessageBox.Show(Strings.Get("Msg_ConfigSaveFailed"), Strings.Get("Title_DemulShooter"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion

        #region Keyboard Handling

        /// <summary>
        /// Low-level Keyboard hook.
        /// </summary>
        protected void ApplyKeyboardHook()
        {
            _KeyboardHookProc = new Win32API.HookProc(GuiKeyboardHookCallback);
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
                _KeyboardHookID = Win32API.SetWindowsHookEx(Win32Define.WH_KEYBOARD_LL, _KeyboardHookProc, Win32API.GetModuleHandle(curModule.ModuleName), 0);
            if (_KeyboardHookID == IntPtr.Zero)
            {
                MessageBox.Show(Strings.Get("Msg_KeyboardHookFailed"), Strings.Get("Title_Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Keyboard hook for the GUI part, to detect buttons for config
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private IntPtr GuiKeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    if (_This._Start_KeyRecord)
                    {
                        KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                        //_SelectedTextBox.Text = s.scanCode.ToString();
                        _SelectedTextBox.Text = GetKeyStringFromVkCode(s.vkCode);

                        if (_SelectedTextBox == TXT_P1_S)
                            Configurator.GetInstance().DIK_Sha_P1_Start = s.scanCode;
                        else if (_SelectedTextBox == TXT_P1_T)
                            Configurator.GetInstance().DIK_Sha_P1_Trigger = s.scanCode;
                        else if (_SelectedTextBox == TXT_P2_S)
                            Configurator.GetInstance().DIK_Sha_P2_Start = s.scanCode;
                        else if (_SelectedTextBox == TXT_P2_T)
                            Configurator.GetInstance().DIK_Sha_P2_Trigger = s.scanCode;
                        else if (_SelectedTextBox == TXT_EXIT)
                            Configurator.GetInstance().DIK_Sha_Exit = s.scanCode;
                        else if (_SelectedTextBox == TXT_TEST)
                            Configurator.GetInstance().DIK_Sha_Test = s.scanCode;
                        else if (_SelectedTextBox == TXT_SERVICE)
                            Configurator.GetInstance().DIK_Sha_Service = s.scanCode;
                        else if (_SelectedTextBox == TXT_CH_P1)
                            Configurator.GetInstance().DIK_M2_Crosshair_P1 = s.scanCode;
                        else if (_SelectedTextBox == TXT_CH_P2)
                            Configurator.GetInstance().DIK_M2_Crosshair_P2 = s.scanCode;
                        else if (_SelectedTextBox == TXT_CH_VIS)
                            Configurator.GetInstance().DIK_M2_Crosshair_Visibility = s.scanCode;
                        else if (_SelectedTextBox == TXT_GSOZ_PEDAL_1)
                            Configurator.GetInstance().DIK_Gsoz_Pedal_P1 = s.scanCode;
                        else if (_SelectedTextBox == TXT_GSOZ_PEDAL_2)
                            Configurator.GetInstance().DIK_Gsoz_Pedal_P2 = s.scanCode;                        
                        else if (_SelectedTextBox == Txt_Rpcs3_P1_Start)
                            Configurator.GetInstance().DIK_Rpcs3_P1_Start = s.scanCode;
                        else if (_SelectedTextBox == Txt_Rpcs3_P2_Start)
                            Configurator.GetInstance().DIK_Rpcs3_P2_Start = s.scanCode;
                        else if (_SelectedTextBox == Txt_Rpcs3_Service)
                            Configurator.GetInstance().DIK_Rpcs3_Service = s.scanCode;
                        else if (_SelectedTextBox == Txt_Rpcs3_Up)
                            Configurator.GetInstance().DIK_Rpcs3_Up = s.scanCode;
                        else if (_SelectedTextBox == Txt_Rpcs3_Down)
                            Configurator.GetInstance().DIK_Rpcs3_Down = s.scanCode;
                        else if (_SelectedTextBox == Txt_Rpcs3_Enter)
                            Configurator.GetInstance().DIK_Rpcs3_Enter = s.scanCode;
                        else if (_SelectedTextBox == Txt_Rpcs3_3D_Switch)
                            Configurator.GetInstance().DIK_Rpcs3_3D_Switch = s.scanCode;
                        else if (_SelectedTextBox == TXT_LE3_PEDAL_1)
                            Configurator.GetInstance().DIK_Le3_Pedal_P1 = s.scanCode;
                        else if (_SelectedTextBox == TXT_LE3_PEDAL_2)
                            Configurator.GetInstance().DIK_Le3_Pedal_P2 = s.scanCode;
                        else if (_SelectedTextBox == TXT_OPGHOST_ACTION_P1)
                            Configurator.GetInstance().DIK_OpGhost_Action_P1 = s.scanCode;
                        else if (_SelectedTextBox == TXT_OPGHOST_ACTION_P2)
                            Configurator.GetInstance().DIK_OpGhost_Action_P2 = s.scanCode;
                        else if (_SelectedTextBox == Txt_EAI_P1_Start)
                            Configurator.GetInstance().DIK_Eai_P1_Start = s.scanCode;
                        else if (_SelectedTextBox == Txt_EAI_P2_Start)
                            Configurator.GetInstance().DIK_Eai_P2_Start = s.scanCode;
                        else if (_SelectedTextBox == Txt_EAI_P1_Credits)
                            Configurator.GetInstance().DIK_Eai_P1_Credits = s.scanCode;
                        else if (_SelectedTextBox == Txt_EAI_P2_Credits)
                            Configurator.GetInstance().DIK_Eai_P2_Credits = s.scanCode;
                        else if (_SelectedTextBox == Txt_EAI_Settings)
                            Configurator.GetInstance().DIK_Eai_Settings = s.scanCode;
                        else if (_SelectedTextBox == Txt_EAI_Up)
                            Configurator.GetInstance().DIK_Eai_MenuUp = s.scanCode;
                        else if (_SelectedTextBox == Txt_EAI_Down)
                            Configurator.GetInstance().DIK_Eai_MenuDown = s.scanCode;
                        else if (_SelectedTextBox == Txt_EAI_Enter)
                            Configurator.GetInstance().DIK_Eai_MenuEnter = s.scanCode;
                        
                        _SelectedTextBox = null;
                        _Start_KeyRecord = false;

                        return new IntPtr(1);
                    }
                }
            }
            return Win32API.CallNextHookEx(_This._KeyboardHookID, nCode, wParam, lParam);
        }

        private String GetKeyStringFromScanCode(int ScanCode)
        {
            uint Vk = Win32API.MapVirtualKey((uint)ScanCode, VirtualKeyMapType.MAPVK_VSC_TO_VK);
            return GetKeyStringFromVkCode((int)Vk);
        }

        private String GetKeyStringFromVkCode(int vkCode)
        {
            KeysConverter kc = new KeysConverter();
            return kc.ConvertToString((Keys)vkCode);
        }

        /// <summary>
        /// Enable a Textbox to be filled with a DIK key code
        /// </summary>
        public void TXT_DirectInput_MouseClick(object sender, MouseEventArgs e)
        {
            if (_SelectedTextBox != null && _SelectedTextBox != ((TextBox)sender))
            {
                _SelectedTextBox.Text = _SelectedTextBoxTextBackup;
            }
            _SelectedTextBox = ((TextBox)sender);
            _SelectedTextBoxTextBackup = _SelectedTextBox.Text;
            _SelectedTextBox.Text = "";
            _Start_KeyRecord = true;
        }

        #endregion

        #region WINDOW MESSAGE LOOP

        /*************** WM Boucle *********************/
        protected const int WM_INPUT = 0x00FF;
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_INPUT:
                    //read in new mouse values.
                    ProcessRawInputMessage(m.LParam);
                    break;
            }
            base.WndProc(ref m);
        }

        #endregion  

        private void Tab_AnalogCalib_Click(object sender, EventArgs e)
        {

        }

        private bool CloneDirectory(string root, string dest)
        {
            try
            {

                foreach (var directory in Directory.GetDirectories(root))
                {
                    string dirName = Path.GetFileName(directory);
                    if (!Directory.Exists(Path.Combine(dest, dirName)))
                    {
                        Directory.CreateDirectory(Path.Combine(dest, dirName));
                    }
                    CloneDirectory(directory, Path.Combine(dest, dirName));
                }

                foreach (var file in Directory.GetFiles(root))
                {
                    File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

       
    }        
}
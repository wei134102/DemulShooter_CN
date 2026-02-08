using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using DsCore.Config;
using DemulShooter_GUI.Properties;

namespace DemulShooter_GUI
{
    public partial class GUI_AnalogCalibration : UserControl
    {
        private PlayerSettings _PlayerSettings;
        private int _Player = 0;
        private Boolean _IsCalibrationRunning = false;

        public int Player
        {
            get { return _Player; }
        }

        public GUI_AnalogCalibration(int Player, PlayerSettings Ps)
        {
            InitializeComponent();
            _PlayerSettings = Ps;
            _Player = Player;

            ApplyLocalization();
            Cbox_Player.Checked = _PlayerSettings.AnalogAxisRangeOverride;
            if (_PlayerSettings.AnalogAxisRangeOverride)
            {
                Txt_Calib_Xmin.Enabled = true;
                Txt_Calib_Xmax.Enabled = true;
                Txt_Calib_Ymin.Enabled = true;
                Txt_Calib_Ymax.Enabled = true;
                Txt_Calib_Xmin.Text = _PlayerSettings.AnalogManual_Xmin.ToString();
                Txt_Calib_Ymin.Text = _PlayerSettings.AnalogManual_Ymin.ToString();
                Txt_Calib_Xmax.Text = _PlayerSettings.AnalogManual_Xmax.ToString();
                Txt_Calib_Ymax.Text = _PlayerSettings.AnalogManual_Ymax.ToString();
            }
        }

        /// <summary>
        /// Apply localized strings (Plan B).
        /// </summary>
        public void ApplyLocalization()
        {
            Cbox_Player.Text = string.Format(Strings.Get("AnalogCalib_OverrideP"), _Player);
            Gbox_P1_Calib.Text = Strings.Get("Gbox_ActLabsPlaceholder");
            Btn_Init_Calib.Text = Strings.Get("AnalogCalib_Default");
            Btn_Stop_Calib.Text = Strings.Get("AnalogCalib_Stop");
            Btn_Start_Calib.Text = Strings.Get("AnalogCalib_Start");
            label28.Text = Strings.Get("AnalogCalib_YMax");
            label29.Text = Strings.Get("AnalogCalib_YMin");
            label32.Text = Strings.Get("AnalogCalib_XMax");
            label36.Text = Strings.Get("AnalogCalib_XMin");
        }

        private void Cbox_Player_CheckedChanged(object sender, EventArgs e)
        {
            if (Cbox_Player.Checked)
            {
                _PlayerSettings.AnalogAxisRangeOverride = true;
                Txt_Calib_Xmin.Enabled = true;
                Txt_Calib_Xmax.Enabled = true;
                Txt_Calib_Ymin.Enabled = true;
                Txt_Calib_Ymax.Enabled = true;
                Btn_Init_Calib.Enabled = true;
                Btn_Start_Calib.Enabled = true;
                Txt_Calib_Xmin.Text = _PlayerSettings.AnalogManual_Xmin.ToString();
                Txt_Calib_Ymin.Text = _PlayerSettings.AnalogManual_Ymin.ToString();
                Txt_Calib_Xmax.Text = _PlayerSettings.AnalogManual_Xmax.ToString();
                Txt_Calib_Ymax.Text = _PlayerSettings.AnalogManual_Ymax.ToString();
            }
            else
            {
                _PlayerSettings.AnalogAxisRangeOverride = false;                
                Txt_Calib_Xmin.Enabled = false;
                Txt_Calib_Xmax.Enabled = false;
                Txt_Calib_Ymin.Enabled = false;
                Txt_Calib_Ymax.Enabled = false;
                Txt_Calib_Xmin.Text = String.Empty;
                Txt_Calib_Ymin.Text = String.Empty;
                Txt_Calib_Xmax.Text = String.Empty;
                Txt_Calib_Ymax.Text = String.Empty;
                Btn_Init_Calib.Enabled = false;
                Btn_Start_Calib.Enabled = false;
                Btn_Stop_Calib.Enabled = false;
            }
        }

        private void Txt_Calib_Xmin_TextChanged(object sender, EventArgs e)
        {
            /*if (Txt_Calib_Xmin.Text != String.Empty)
                _PlayerSettings.AnalogManual_Xmin = int.Parse(Txt_Calib_Xmin.Text);*/
        }

        private void Txt_Calib_Xmax_TextChanged(object sender, EventArgs e)
        {
            /*if (Txt_Calib_Xmax.Text != String.Empty)
            _PlayerSettings.AnalogManual_Xmax = int.Parse(Txt_Calib_Xmax.Text);*/
        }

        private void Txt_Calib_Ymin_TextChanged(object sender, EventArgs e)
        {
            /*if (Txt_Calib_Ymin.Text != String.Empty)
            _PlayerSettings.AnalogManual_Ymin = int.Parse(Txt_Calib_Ymin.Text);*/
        }

        private void Txt_Calib_Ymax_TextChanged(object sender, EventArgs e)
        {
            /*if (Txt_Calib_Ymin.Text != String.Empty)
            _PlayerSettings.AnalogManual_Ymax = int.Parse(Txt_Calib_Ymin.Text);*/
        }

        private void Btn_Start_Calib_Click(object sender, EventArgs e)
        {
            if (_PlayerSettings.RIController.DeviceType == DsCore.RawInput.RawInputDeviceType.RIM_TYPEHID)
            {
                Btn_Init_Calib.Enabled = false;
                Btn_Start_Calib.Enabled = false;
                Btn_Stop_Calib.Enabled = true;
                _PlayerSettings.AnalogManual_Xmin = Int32.MaxValue;
                _PlayerSettings.AnalogManual_Xmax = Int32.MinValue;
                _PlayerSettings.AnalogManual_Ymin = Int32.MaxValue;
                _PlayerSettings.AnalogManual_Ymax = Int32.MinValue;
                _IsCalibrationRunning = true;
            }
            else
                MessageBox.Show(Strings.Get("Msg_DeviceCantBeCalibrated") + _PlayerSettings.RIController.DeviceType.ToString(), Strings.Get("Title_Warning"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void Btn_Stop_Calib_Click(object sender, EventArgs e)
        {
            Btn_Init_Calib.Enabled = true;
            Btn_Start_Calib.Enabled = true;
            Btn_Stop_Calib.Enabled = false;
            _IsCalibrationRunning = false;
        }

        private void Btn_Init_Calib_Click(object sender, EventArgs e)
        {
            if (_PlayerSettings.RIController.DeviceType == DsCore.RawInput.RawInputDeviceType.RIM_TYPEHID)
            {
                Btn_Start_Calib.Enabled = true;
                Btn_Stop_Calib.Enabled = false;
                _PlayerSettings.AnalogManual_Xmin = _PlayerSettings.RIController.Axis_X_Min;
                _PlayerSettings.AnalogManual_Xmax = _PlayerSettings.RIController.Axis_X_Max;
                _PlayerSettings.AnalogManual_Ymin = _PlayerSettings.RIController.Axis_Y_Min;
                _PlayerSettings.AnalogManual_Ymax = _PlayerSettings.RIController.Axis_Y_Max;
                Txt_Calib_Xmin.Text = _PlayerSettings.RIController.Axis_X_Min.ToString();
                Txt_Calib_Xmax.Text = _PlayerSettings.RIController.Axis_X_Max.ToString();
                Txt_Calib_Ymin.Text = _PlayerSettings.RIController.Axis_Y_Min.ToString();
                Txt_Calib_Ymax.Text = _PlayerSettings.RIController.Axis_Y_Max.ToString();
            }
            else
                MessageBox.Show(Strings.Get("Msg_DeviceCantBeCalibrated") + _PlayerSettings.RIController.DeviceType.ToString(), Strings.Get("Title_Warning"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        public void UpdateValues()
        {
            if (Cbox_Player.Checked && _IsCalibrationRunning)
            {
                if (_PlayerSettings.RIController.Computed_X < _PlayerSettings.AnalogManual_Xmin)
                    _PlayerSettings.AnalogManual_Xmin = _PlayerSettings.RIController.Computed_X;
                if (_PlayerSettings.RIController.Computed_X > _PlayerSettings.AnalogManual_Xmax)
                    _PlayerSettings.AnalogManual_Xmax = _PlayerSettings.RIController.Computed_X;
                if (_PlayerSettings.RIController.Computed_Y < _PlayerSettings.AnalogManual_Ymin)
                    _PlayerSettings.AnalogManual_Ymin = _PlayerSettings.RIController.Computed_Y;
                if (_PlayerSettings.RIController.Computed_Y > _PlayerSettings.AnalogManual_Ymax)
                    _PlayerSettings.AnalogManual_Ymax = _PlayerSettings.RIController.Computed_Y;

                Txt_Calib_Xmin.Text = _PlayerSettings.AnalogManual_Xmin.ToString();
                Txt_Calib_Ymin.Text = _PlayerSettings.AnalogManual_Ymin.ToString();
                Txt_Calib_Xmax.Text = _PlayerSettings.AnalogManual_Xmax.ToString();
                Txt_Calib_Ymax.Text = _PlayerSettings.AnalogManual_Ymax.ToString();
            }
        }
    }
}

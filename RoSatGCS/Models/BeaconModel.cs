using CommunityToolkit.Mvvm.ComponentModel;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace RoSatGCS.Models
{
    [MessagePackObject(AllowPrivate = true)]
    public partial class BeaconModel : ObservableObject
    {

        [MessagePackObject]
        public struct BeaconFileStructure
        {
            [Key("uhf_data")]
            public UhfData uhf_data { get; set; }

            [Key("gnss_data")]
            public GnssData gnss_data { get; set; }

            [Key("imu_data")]
            public ImuData imu_data { get; set; }

            [Key("eps_data")]
            public EpsData eps_data { get; set; }

            [Key("obc_data")]
            public ObcData obc_data { get; set; }

            [Key("adcs_data")]
            public AdcsData adcs_data { get; set; }

            [Key("mission_data")]
            public MissionData mission_data { get; set; }
        }

        #region UHF

        [MessagePackObject]
        public struct UhfData
        {
            [Key("beacon_status")]
            public bool beacon_status { get; set; }

            [Key("uhf_mode")]
            public bool uhf_mode { get; set; }

            [Key("fram_status")]
            public bool fram_status { get; set; }

            [Key("radio_status")]
            public bool radio_status { get; set; }

            [Key("gcs_connection_status")]
            public bool gcs_connection_status { get; set; }

            [Key("ocillator_status")]
            public bool ocillator_status { get; set; }

            [Key("reset_status")]
            public bool reset_status { get; set; }

            [Key("rf_mode")]
            public string rf_mode { get; set; }

            [Key("uhf_rx_packets")]
            public int uhf_rx_packets { get; set; }

            [Key("uhf_rx_error_packets")]
            public int uhf_rx_error_packets { get; set; }

            [Key("antenna_algorithm_1_status")]
            public bool antenna_algorithm_1_status { get; set; }

            [Key("antenna_algorithm_2_status")]
            public bool antenna_algorithm_2_status { get; set; }

            [Key("antenna_1_status")]
            public bool antenna_1_status { get; set; }

            [Key("antenna_2_status")]
            public bool antenna_2_status { get; set; }

            [Key("antenna_3_status")]
            public bool antenna_3_status { get; set; }

            [Key("antenna_4_status")]
            public bool antenna_4_status { get; set; }
        }

        #endregion

        #region GNSS / IMU / EPS

        [MessagePackObject]
        public struct GnssData
        {
            [Key("year")]
            public int year { get; set; }

            [Key("month")]
            public int month { get; set; }

            [Key("day")]
            public int day { get; set; }

            [Key("hour")]
            public int hour { get; set; }

            [Key("minute")]
            public int minute { get; set; }

            [Key("second")]
            public int second { get; set; }

            [Key("px")]
            public double px { get; set; }

            [Key("py")]
            public double py { get; set; }

            [Key("pz")]
            public double pz { get; set; }

            [Key("vx")]
            public double vx { get; set; }

            [Key("vy")]
            public double vy { get; set; }

            [Key("vz")]
            public double vz { get; set; }
        }

        [MessagePackObject]
        public struct ImuData
        {
            [Key("yaw")]
            public double yaw { get; set; }

            [Key("pitch")]
            public double pitch { get; set; }

            [Key("roll")]
            public double roll { get; set; }
        }

        [MessagePackObject]
        public struct EpsData
        {
            [Key("voltage")]
            public int voltage { get; set; }

            [Key("current")]
            public int current { get; set; }

            [Key("temperature")]
            public int temperature { get; set; }

            [Key("bp_mode")]
            public int bp_mode { get; set; }
        }

        #endregion

        #region OBC

        [MessagePackObject]
        public struct ObcData
        {
            [Key("conops_mode")]
            public int conops_mode { get; set; }

            [Key("totalResetCount")]
            public int totalResetCount { get; set; }

            [Key("resetReasonBitField")]
            public int resetReasonBitField { get; set; }

            [Key("fdir_eps_ii_pdm_cmd_exec_status")]
            public FdirEpsIiPdmCmdExecStatus fdir_eps_ii_pdm_cmd_exec_status { get; set; }

            [Key("fdir_s_x_band_cmd_exec_status")]
            public FdirSXBandCmdExecStatus fdir_s_x_band_cmd_exec_status { get; set; }

            [Key("eps2inst0_fdir_cmd_exec_status")]
            public Eps2Inst0FdirCmdExecStatus eps2inst0_fdir_cmd_exec_status { get; set; }

            [Key("Irst_Opsmode")]
            public IrstOpsmode Irst_Opsmode { get; set; }

            [Key("OBC_Uptime")]
            public int OBC_Uptime { get; set; }

            [Key("IRST_OpsMode_flags")]
            public IrstOpsModeFlags IRST_OpsMode_flags { get; set; }

            [Key("LEOP_State")]
            public LeOpState LEOP_State { get; set; }

            [Key("LEOP_Context_Bit_Field")]
            public LeOpContextBitField LEOP_Context_Bit_Field { get; set; }

            [Key("Commission_State")]
            public CommissionState Commission_State { get; set; }

            [Key("Commission_Context_Bit_Field")]
            public CommissionContextBitField Commission_Context_Bit_Field { get; set; }

            [Key("Mission_Status")]
            public MissionStatus Mission_Status { get; set; }

            [Key("Mission_Mode")]
            public int Mission_Mode { get; set; }

            [Key("Mission_Context_Bit_Field")]
            public int Mission_Context_Bit_Field { get; set; }

            [Key("Emergency_State")]
            public int Emergency_State { get; set; }

            [Key("Emergency_Context_Bit_Field")]
            public int Emergency_Context_Bit_Field { get; set; }

            [Key("PowerSaving_State")]
            public int PowerSaving_State { get; set; }

            [Key("PowerSaving_Context_Bit_Field")]
            public int PowerSaving_Context_Bit_Field { get; set; }
        }

        [MessagePackObject]
        public struct FdirEpsIiPdmCmdExecStatus
        {
            [Key("EPSII_PDM_1_GetPowerDistributionInfo_ErrorCode")]
            public int EPSII_PDM_1_GetPowerDistributionInfo_ErrorCode { get; set; }

            [Key("EPSII_PDM_1_GetDeviceHealthInfo_ErrorCode")]
            public int EPSII_PDM_1_GetDeviceHealthInfo_ErrorCode { get; set; }

            [Key("EPSII_PDM_1_GetRAWSensors_PDM1_ErrorCode")]
            public int EPSII_PDM_1_GetRAWSensors_PDM1_ErrorCode { get; set; }

            [Key("EPSII_PDM_1_GetRAWSensors_PDM2_ErrorCode")]
            public int EPSII_PDM_1_GetRAWSensors_PDM2_ErrorCode { get; set; }

            [Key("EPSII_PDM_1_GetBasicSettings_ErrorCode")]
            public int EPSII_PDM_1_GetBasicSettings_ErrorCode { get; set; }

            [Key("EPSII_PDM_1_SetBasicSettings_ErrorCode")]
            public int EPSII_PDM_1_SetBasicSettings_ErrorCode { get; set; }
        }

        [MessagePackObject]
        public struct FdirSXBandCmdExecStatus
        {
            [Key("error_code")]
            public int error_code { get; set; }
        }

        [MessagePackObject]
        public struct Eps2Inst0FdirCmdExecStatus
        {
            [Key("EPSII_BP_1_GetBatteryInfo_ErrorCode")]
            public int EPSII_BP_1_GetBatteryInfo_ErrorCode { get; set; }

            [Key("EPSII_BP_1_GetDeviceHealthInfo_ErrorCode")]
            public int EPSII_BP_1_GetDeviceHealthInfo_ErrorCode { get; set; }

            [Key("EPSII_BP_1_GetRAWSensors_1_ErrorCode")]
            public int EPSII_BP_1_GetRAWSensors_1_ErrorCode { get; set; }

            [Key("EPSII_BP_1_GetRAWSensors_2_ErrorCode")]
            public int EPSII_BP_1_GetRAWSensors_2_ErrorCode { get; set; }

            [Key("EPSII_BP_1_GetRAWSensors_3_ErrorCode")]
            public int EPSII_BP_1_GetRAWSensors_3_ErrorCode { get; set; }
        }

        [MessagePackObject]
        public struct IrstOpsmode
        {
            [Key("ROOT")]
            public bool ROOT { get; set; }

            [Key("LEOP")]
            public bool LEOP { get; set; }

            [Key("CMS")]
            public bool CMS { get; set; }

            [Key("NM")]
            public bool NM { get; set; }

            [Key("NM_STATE_CHG")]
            public bool NM_STATE_CHG { get; set; }

            [Key("NM_STATE_MS")]
            public bool NM_STATE_MS { get; set; }

            [Key("NM_STATE_PS")]
            public bool NM_STATE_PS { get; set; }

            [Key("EM")]
            public bool EM { get; set; }
        }

        [MessagePackObject]
        public struct IrstOpsModeFlags
        {
            [Key("LEOP_SCH_Done")]
            public bool LEOP_SCH_Done { get; set; }

            [Key("CMS_SCH_Done")]
            public bool CMS_SCH_Done { get; set; }

            [Key("Major_Fault_Discover")]
            public bool Major_Fault_Discover { get; set; }

            [Key("Mission_Completed")]
            public bool Mission_Completed { get; set; }

            [Key("Mission_Timeout")]
            public bool Mission_Timeout { get; set; }

            [Key("Mission_Battery_Low")]
            public bool Mission_Battery_Low { get; set; }

            [Key("BP_Critical_Fault")]
            public bool BP_Critical_Fault { get; set; }

            [Key("ADCS_RD_Complete")]
            public bool ADCS_RD_Complete { get; set; }

            [Key("Mission_Execute_Ready")]
            public bool Mission_Execute_Ready { get; set; }

            [Key("Force_ALL_HW_OFF")]
            public bool Force_ALL_HW_OFF { get; set; }

            [Key("Force_ALL_HW_ON")]
            public bool Force_ALL_HW_ON { get; set; }

            [Key("RW_CMS_recheck")]
            public bool RW_CMS_recheck { get; set; }
        }

        [MessagePackObject]
        public struct LeOpState
        {
            [Key("IDLE")]
            public bool IDLE { get; set; }

            [Key("EJECTION")]
            public bool EJECTION { get; set; }

            [Key("DEPLOY_MAGNETOMETER")]
            public bool DEPLOY_MAGNETOMETER { get; set; }

            [Key("DEPLOY_ANTENNA")]
            public bool DEPLOY_ANTENNA { get; set; }

            [Key("ACTIVATE_BEACON")]
            public bool ACTIVATE_BEACON { get; set; }

            [Key("DETUMBLING")]
            public bool DETUMBLING { get; set; }

            [Key("FINALIZE")]
            public bool FINALIZE { get; set; }
        }

        [MessagePackObject]
        public struct LeOpContextBitField
        {
            [Key("is_wait_finished")]
            public bool is_wait_finished { get; set; }

            [Key("is_ejected")]
            public bool is_ejected { get; set; }

            [Key("is_magnetometer_deployed")]
            public bool is_magnetometer_deployed { get; set; }

            [Key("is_antenna_deployed")]
            public bool is_antenna_deployed { get; set; }

            [Key("is_beacon_on")]
            public bool is_beacon_on { get; set; }

            [Key("is_finalized")]
            public bool is_finalized { get; set; }
        }

        [MessagePackObject]
        public struct CommissionState
        {
            [Key("IDLE")]
            public bool IDLE { get; set; }

            [Key("RW_CMS_READY")]
            public bool RW_CMS_READY { get; set; }

            [Key("RW_CMS_TC_START")]
            public bool RW_CMS_TC_START { get; set; }

            [Key("RW_CMS_AUTO_START")]
            public bool RW_CMS_AUTO_START { get; set; }

            [Key("RW_CMS_ERROR")]
            public bool RW_CMS_ERROR { get; set; }

            [Key("RW_CMS_DONE")]
            public bool RW_CMS_DONE { get; set; }

            [Key("FINALIZE")]
            public bool FINALIZE { get; set; }
        }

        [MessagePackObject]
        public struct CommissionContextBitField
        {
            [Key("is_initial_rd_done")]
            public bool is_initial_rd_done { get; set; }

            [Key("is_adcs_rd_done")]
            public bool is_adcs_rd_done { get; set; }

            [Key("is_adcs_csp_entry")]
            public bool is_adcs_csp_entry { get; set; }

            [Key("is_tc_rw_cms_recvd")]
            public bool is_tc_rw_cms_recvd { get; set; }

            [Key("is_auto_rw_cms_start")]
            public bool is_auto_rw_cms_start { get; set; }

            [Key("is_adcs_rw_cms_error")]
            public bool is_adcs_rw_cms_error { get; set; }

            [Key("is_adcs_rw_cms_done")]
            public bool is_adcs_rw_cms_done { get; set; }

            [Key("is_finalized")]
            public bool is_finalized { get; set; }
        }

        [MessagePackObject]
        public struct MissionStatus
        {
            [Key("mission_status_1")]
            public int mission_status_1 { get; set; }

            [Key("mission_status_2")]
            public int mission_status_2 { get; set; }

            [Key("mission_status_3")]
            public int mission_status_3 { get; set; }

            [Key("mission_status_4")]
            public int mission_status_4 { get; set; }

            [Key("mission_status_5")]
            public int mission_status_5 { get; set; }

            [Key("mission_status_6")]
            public int mission_status_6 { get; set; }
        }

        #endregion

        #region ADCS

        [MessagePackObject]
        public struct AdcsData
        {
            [Key("mode")]
            public int mode { get; set; }

            [Key("modeAD")]
            public int modeAD { get; set; }

            [Key("modeAC")]
            public int modeAC { get; set; }

            [Key("HW_status")]
            public HwStatus HW_status { get; set; }

            [Key("angle_rate")]
            public AngleRate angle_rate { get; set; }

            [Key("sun_flags")]
            public int sun_flags { get; set; }
        }

        [MessagePackObject]
        public struct HwStatus
        {
            [Key("mag_status")]
            public int mag_status { get; set; }

            [Key("sun_status")]
            public int sun_status { get; set; }

            [Key("imu_status")]
            public int imu_status { get; set; }

            [Key("gnss_status")]
            public int gnss_status { get; set; }

            [Key("mtq_status")]
            public int mtq_status { get; set; }

            [Key("rwx_status")]
            public int rwx_status { get; set; }

            [Key("rwy_status")]
            public int rwy_status { get; set; }
        }

        [MessagePackObject]
        public struct AngleRate
        {
            [Key("wx")]
            public double wx { get; set; }

            [Key("wy")]
            public double wy { get; set; }

            [Key("wz")]
            public double wz { get; set; }
        }

        #endregion

        #region Mission data

        [MessagePackObject]
        public struct MissionData
        {
            [Key("p2")]
            public int p2 { get; set; }

            [Key("p5")]
            public int p5 { get; set; }
        }

        #endregion

        #region fields
        [Key("timestamp")]
        private double timestamp = 0;
        [Key("filename")]
        private string filename = "";
        [Key("filepath")]
        private string filepath = "";
        [Key("structure")]
        private BeaconFileStructure structure;
        [Key("valid")]
        private bool valid = false;
        [IgnoreMember]
        private bool plotReady = false;
        #endregion
        #region properties
        [Key("Timestamp")]
        public double Timestamp { get => timestamp; }

        [IgnoreMember]
        public DateTime DateTime { get => DateTimeOffset.FromUnixTimeSeconds((long)timestamp).LocalDateTime; }

        [Key("Filename")]
        public string Filename { get => filename; }
        [Key("Filepath")]
        public string Filepath { get => filepath; }
        [Key("Structure")]
        public BeaconFileStructure Structure { get => structure; }
        [Key("Valid")]
        public bool Valid { get => valid; }
        [IgnoreMember]
        public bool PlotReady { get => plotReady; set => plotReady = value; }
        #endregion
        public BeaconModel() {
            structure = new BeaconFileStructure();
            var uhf= new UhfData();
            uhf.rf_mode = "N/A";
            structure.uhf_data = uhf;

        }
        public void Initialize(string path)
        {
            FileCheck(path);
            filename = Path.GetFileName(path);
            filepath = path;

            if(!Load(out string error))
            {
                throw new InvalidCastException(error);
            }
        }

        private static void FileCheck(string path)
        {
            if (!Path.Exists(path))
            {
                throw new FileNotFoundException(path);
            }
            if (Path.GetExtension(path) != ".json")
            {
                throw new FileFormatException("File must be .json file");
            }
        }

        private readonly static JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private bool Load(out string error)
        {
            valid = false;

            using (StreamReader r = new(filepath))
            {
                string json = r.ReadToEnd();
                var data = JsonSerializer.Deserialize<BeaconFileStructure>(json, jsonOptions);

                // Validate Check
                // TODO: Add more validation if needed

                // Initialize Time

                // File name format: beacon_YYYYMMDD_HHMMSS.json
                // assume local time
                var split = filename.Split('_', '.');
                if (split.Length < 3 || split[0] != "beacon")
                {
                    error = "Invalid file name format";
                    return false;
                }

                if (!DateTime.TryParseExact(split[1] + split[2], "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.AssumeLocal, out DateTime dt))
                {
                    error = "Invalid file name format";
                    return false;
                }
                timestamp = new DateTimeOffset(dt).ToUnixTimeSeconds();

                structure = data;
            }
            error = "";
            valid = true;
            return true;
        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace COTTask_wpf
{
    class Config
    {
        [JsonProperty(PropertyName = "NHP Name")]
        public string NHPName;

        [JsonProperty(PropertyName = "saved folder")]
        public string saved_folder;

        [JsonProperty(PropertyName = "Times")]
        public ConfigTimes configTimes;


        [JsonProperty(PropertyName = "Target")]
        public ConfigTarget configTarget;

        [JsonProperty(PropertyName = "Colors")]
        public ConfigColors configColors;


        public string audioFile_Correct, audioFile_Error;
    }

    class ConfigTimes
    {
        [JsonProperty(PropertyName = "Ready Show Time Range")]
        public float[] tRange_ReadyTime;

        [JsonProperty(PropertyName = "Max Reaction Time")]
        public float tMax_ReactionTime;

        [JsonProperty(PropertyName = "Max Reach Time")]
        public float tMax_ReachTime;

        [JsonProperty(PropertyName = "Visual Feedback Show Time")]
        public float t_VisfeedbackShow;

        [JsonProperty(PropertyName = "Inter Trials Time")]
        public float t_InterTrial;

        [JsonProperty(PropertyName = "Juice Correct Given Time")]
        public float t_JuicerCorrectGiven;

        [JsonProperty(PropertyName = "Flash Target On Time")]
        public float t_FlashTargetOn;

        [JsonProperty(PropertyName = "Flash Target Off Time")]
        public float t_FlashTargetOff;
    }


    class ConfigTarget
    {
        [JsonProperty(PropertyName = "Target Diameter (cm)")]
        public float targetDiaCM;

        [JsonProperty(PropertyName = "Target No of Positions")]
        public int targetNoOfPositions;

        public List<int[]> optPostions_OCenter_List;
    }


    class ConfigColors
    {
        [JsonProperty(PropertyName = "Wait Start Background")]
        public string BKWaitTrialColorStr;

        [JsonProperty(PropertyName = "Ready Background")]
        public string BKReadyColorStr;


        [JsonProperty(PropertyName = "Target Shown Background")]
        public string BKTargetShownColorStr;

        [JsonProperty(PropertyName = "Correct Fill")]
        public string CorrFillColorStr;

        [JsonProperty(PropertyName = "Correct Outline")]
        public string CorrOutlineColorStr;

        [JsonProperty(PropertyName = "Error Fill")]
        public string ErrorFillColorStr;

        [JsonProperty(PropertyName = "Error Outline")]
        public string ErrorOutlineColorStr;

        [JsonProperty(PropertyName = "Error Crossing")]
        public string ErrorCrossingColorStr;

        [JsonProperty(PropertyName = "Target Fill")]
        public string targetFillColorStr;

        [JsonProperty(PropertyName = "Target Outline")]
        public string targetOutlineColorStr;

        [JsonProperty(PropertyName = "Background")]
        public string BKColorStr;
    }
}

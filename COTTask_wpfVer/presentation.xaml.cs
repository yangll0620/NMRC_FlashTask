using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO.Ports;
using System.IO;
using System.Reflection;
using sd = System.Drawing;
using System.Windows.Interop;

namespace COTTask_wpf
{
    /// <summary>
    /// Interaction logic for presentation.xaml
    /// </summary>
    /// 
    public partial class presentation : Window
    {

        /***** predefined parameters*******/
        int wpoints_radius = 15;



        /***********enumerate *****************/
        public enum TargetType
        {
            Go,
        }

        public enum TrialExeResult
        {
            readyWaitTooShort,
            cueWaitTooShort,
            goReactionTimeToolong,
            goReachTimeToolong,
            goHit,
            goMiss
        }

        public enum ScreenTouchState
        {
            Idle,
            Touched
        }

        private enum GoTargetTouchState
        {
            goHit, // at least one finger inside the circleGo
            goMissed, // touched with longer distance 
        }

        /*startpad related enumerate*/
        private enum ReadStartpad
        {
            No,
            Yes
        }

        private enum PressedStartpad
        {
            No,
            Yes
        }

        private enum GiveJuicerState
        {
            No,
            CorrectGiven
        }

        private enum StartPadHoldState
        {
            HoldEnough,
            HoldTooShort
        }



        /*****************parameters ************************/
        MainWindow parent;


        string file_saved;

        // diameter for crossing, circle, square and white points
        int crossing_LineThickness = 3;
        int crossing_len = 10;


        TargetType targetType;



        // objects of Go cirle,  lines of the crossing, and two white points
        Ellipse circleGo;
        Line crossing_vertLine, crossing_horiLine;

        // ColorBrushes 
        private SolidColorBrush brush_goCircleFill;
        private SolidColorBrush brush_BKWaitTrialStart, brush_BDWaitTrialStart, brush_BKReady, brush_BKTargetShown;
        private SolidColorBrush brush_CorrectFill, brush_CorrOutline;
        private SolidColorBrush brush_ErrorCrossing, brush_ErrorFill, brush_ErrorOutline;



        // Center Point and Radius of CircleGo (in Pixal)
        Point circleGo_cPoint_Pixal; 
        double circleGo_Radius_Pixal;
        double circleGo_StrokeThickness = 3;

        // audio feedback
        private string audioFile_Correct, audioFile_Error;
        System.Media.SoundPlayer player_Correct, player_Error;


        // name of all the objects
        string name_circleGo = "circleGo";
        string name_crossingVLine = "crossing_vLine", name_crossingHLine = "crossing_hLine";


        // rng for shuffling 
        private static Random rng = new Random();


        // Wait Time Range for Each Event, and Max Reaction and Reach Time (ms)
        float tMax_ReactionTimeMS, tMax_ReachTimeMS; 
        Int32 t_VisfeedbackShowMS, t_InterTrialMS; // Visual Feedback Show Time (ms)

        bool PresentTrial;

        // time stamp
        long timestamp_0;


        // set storing the touch point id (no replicates)
        HashSet<int> touchPoints_Id = new HashSet<int>();

        // list storing the position/Timepoint of the touch points when touched down
        List<double[]> downPoints_Pos = new List<double[]>();

        // list storing the position, touched and left Timepoints of the touch points
        // one element: [point_id, touched_timepoint, touched_x, touched_y, left_timepoint, left_x, left_y]
        List<double[]> touchPoints_PosTime = new List<double[]>(); 

        // Stop Watch for recording the time interval between the first touchpoint and the last touchpoint within One Touch
        Stopwatch tpoints1TouchWatch;
        // the Max Duration for One Touch (ms)
        long tMax_1Touch = 40;
        GoTargetTouchState gotargetTouchstate;


        // Executed Trial Information
        public int totalGoTrialNum, successGoTrialNum;
        public int missGoTrialNum, noreactionGoTrialNum, noreachGoTrialNum;


        // executeresult of each trial
        TrialExeResult trialExeResult;

        ScreenTouchState screenTouchstate;

        // hold states for Ready, Cue Interfaces
        StartPadHoldState startpadHoldstate;



        // serial port for DLP-IO8-G
        SerialPort serialPort_IO8;
        int baudRate = 115200;
        Thread thread_ReadWrite_IO8;

        // commands for setting dig out high/low for channels
        static string cmdDigIn1 = "A";

        static string cmdHigh3 = "3";
        static string cmdLow3 = "E";


        static string startpad_DigIn= cmdDigIn1;
        static int startpad_DigIn_Pressed = 0;
        static int startpad_DigIn_Unpressed = 1;


        static string Code_InitState = "0000";
        static string Code_TouchTriggerTrial = "1110";
        static string Code_LeaveStartpad = "1001";
        static string Code_ReadyShown = "0110";
        static string Code_ReadyWaitTooShort = "0011";
        static string Code_GoTargetShown = "1010";
        static string Code_GoReactionTooLong = "1100";
        static string Code_GoReachTooLong = "1011";
        static string Code_GoTouched = "1101";
        static string Code_GoTouchedHit = "0100";
        static string Code_GoTouchedMiss = "1000";
        

        string TDTCmd_InitState, TDTCmd_TouchTriggerTrial, TDTCmd_LeaveStartpad, TDTCmd_ReadyShown, TDTCmd_ReadyWaitTooShort;
        string TDTCmd_GoTargetShown, TDTCmd_GoReactionTooLong, TDTCmd_GoReachTooLong, TDTCmd_GoTouched, TDTCmd_GoTouchedHit, TDTCmd_GoTouchedMiss;



        /* startpad parameters */
        PressedStartpad pressedStartpad;
        public delegate void UpdateTextCallback(string message);

        /*Juicer Parameters*/
        GiveJuicerState giveJuicerState;
        // juicer given duration(ms)
        int t_JuicerCorrectGiven;


        // Global stopwatch
        Stopwatch globalWatch;


        // Variables for Various Time Points during trials
        long timePoint_StartpadTouched, timePoint_StartpadLeft;
        long timePoint_Interface_ReadyOnset, timePoint_Interface_TargetOnset;


        // variables for counting total trials and blockN
        int totalTriali;
        int blockN;


        readonly bool AudioFeedbackNeeded = false;
        readonly bool SaveSummaryofExpNeeded = false;

        /*****Methods*******/
        public presentation(MainWindow mainWindow)
        {
            InitializeComponent();
            
            Touch.FrameReported += new TouchFrameEventHandler(Touch_FrameReported);

            // parent
            parent = mainWindow;

        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {//  Called by Window.show() at the first time
            //WindowState = WindowState.Maximized;


            if(parent.IO8Used) // Create Serial port 
                Create_SerialPort();

            // Create stopwatches
            Create_StopWatches();

            // get the setup from the parent interface
            GetSetupParameters();

            // Create necessary elements: go circle, nogo rect, two white points and one crossing
            Create_GoCircle();
            Create_OneCrossing();

            if(AudioFeedbackNeeded) // Set audio Feedback related members 
                SetAudioFeedback();

            if (parent.IO8Used)// IO8EventTDT Cmd
                Generate_IO8EventTDTCmd();
        }


        private void Create_SerialPort()
        {
            serialPort_IO8 = new SerialPort();

            serialPort_IO8.PortName = parent.serialPortIO8_name;
            serialPort_IO8.BaudRate = baudRate;
        }


        private void Create_StopWatches()
        {
            globalWatch = new Stopwatch();
            tpoints1TouchWatch = new Stopwatch();
        }

        public static void Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        private string Convert2_IO8EventCmd_Bit5to8(string EventCode)
        {/*
            Generate IO8 Event Command based on EventCode using bit 5-8
            E.g. "0000" -> "TYUI", "1111" -> "5678", "1010" -> "5Y7I"
            */

            string cmdHigh5 = "5";
            string cmdLow5 = "T";
            string cmdHigh6 = "6";
            string cmdLow6 = "Y";
            string cmdHigh7 = "7";
            string cmdLow7 = "U";
            string cmdHigh8 = "8";
            string cmdLow8 = "I";

            string IO8EventCmd = cmdLow5 + cmdLow6 + cmdLow7 + cmdLow8;
            if (EventCode[0] == '1')
                IO8EventCmd = IO8EventCmd.Remove(0, 1).Insert(0, cmdHigh5);
            if (EventCode[1] == '1')
                IO8EventCmd = IO8EventCmd.Remove(1, 1).Insert(1, cmdHigh6);
            if (EventCode[2] == '1')
                IO8EventCmd = IO8EventCmd.Remove(2, 1).Insert(2, cmdHigh7);
            if (EventCode[3] == '1')
                IO8EventCmd = IO8EventCmd.Remove(3, 1).Insert(3, cmdHigh8);

            return IO8EventCmd;
        }

        private void Generate_IO8EventTDTCmd()
        {
            TDTCmd_InitState = Convert2_IO8EventCmd_Bit5to8(Code_InitState);
            TDTCmd_TouchTriggerTrial = Convert2_IO8EventCmd_Bit5to8(Code_TouchTriggerTrial);
            TDTCmd_LeaveStartpad = Convert2_IO8EventCmd_Bit5to8(Code_LeaveStartpad);
            TDTCmd_ReadyShown = Convert2_IO8EventCmd_Bit5to8(Code_ReadyShown);
            TDTCmd_ReadyWaitTooShort = Convert2_IO8EventCmd_Bit5to8(Code_ReadyWaitTooShort);
            TDTCmd_GoTargetShown = Convert2_IO8EventCmd_Bit5to8(Code_GoTargetShown);
            TDTCmd_GoReactionTooLong = Convert2_IO8EventCmd_Bit5to8(Code_GoReactionTooLong);
            TDTCmd_GoReachTooLong = Convert2_IO8EventCmd_Bit5to8(Code_GoReachTooLong);
            TDTCmd_GoTouched = Convert2_IO8EventCmd_Bit5to8(Code_GoTouched);
            TDTCmd_GoTouchedHit = Convert2_IO8EventCmd_Bit5to8(Code_GoTouchedHit);
            TDTCmd_GoTouchedMiss = Convert2_IO8EventCmd_Bit5to8(Code_GoTouchedMiss);
        }

        public void Prepare_bef_Present()
        {
            if(parent.IO8Used)
            {// create a serial Port IO8 instance, and open it
                serialPort_IO8 = new SerialPort();
                try
                {
                    serialPort_SetOpen(parent.serialPortIO8_name, baudRate);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message, "Error Message", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                // Thread for Read and Write IO8
                thread_ReadWrite_IO8 = new Thread(new ThreadStart(Thread_ReadWrite_IO8));
            }
            

            // init a global stopwatch
            globalWatch = new Stopwatch();
            tpoints1TouchWatch = new Stopwatch();

            // Init Trial Information
            Init_FeedbackTrialsInformation();


            //Write Trial Setup Information
            Save_TrialSetupInformation();

            totalTriali = 0;
            blockN = 0;

        }

        private void serialPort_SetOpen(string portName, int baudRate)
        {
            try
            {
                serialPort_IO8.PortName = portName;
                serialPort_IO8.BaudRate = baudRate;
                serialPort_IO8.Open();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void Save_TrialSetupInformation()
        {
            using (StreamWriter file = File.AppendText(file_saved))
            {
                file.WriteLine("\n\n");

                file.WriteLine(String.Format("{0, -40}", "Trial Information"));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Unit of Touch Point X Y Position", "Pixal"));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Touch Point X Y Coordinate System", "(0,0) in Top Left Corner, Right and Down Direction is Positive"));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Unit of Event TimePoint/Time", "Second"));
                file.WriteLine(String.Format("{0, -40}:  {1}", "Center Coordinates of Each Target", "((0,0) in Center of the Screen, Right and Down Direction is Positive)"));
                for (int i = 0; i < parent.optPostions_OCenter_List.Count; i++)
                {
                    int[] position = parent.optPostions_OCenter_List[i];
                    file.WriteLine(String.Format("{0, -40}:{1}, {2}", "Postion " + i.ToString(), position[0], position[1]));
                }
                file.WriteLine("\n");

                file.WriteLine(String.Format("{0, -40}", "Event Codes in TDT System:"));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_InitState), Code_InitState));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_TouchTriggerTrial), Code_TouchTriggerTrial));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_LeaveStartpad), Code_LeaveStartpad));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_ReadyShown), Code_ReadyShown));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_ReadyWaitTooShort), Code_ReadyWaitTooShort));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_GoTargetShown), Code_GoTargetShown));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_GoReactionTooLong), Code_GoReactionTooLong));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_GoReachTooLong), Code_GoReachTooLong));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_GoTouched), Code_GoTouched));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_GoTouchedHit), Code_GoTouchedHit));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(Code_GoTouchedMiss), Code_GoTouchedMiss));
                file.WriteLine("\n");


                file.WriteLine(String.Format("{0, -40}", "IO8 Commands:"));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_InitState), TDTCmd_InitState));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_TouchTriggerTrial), TDTCmd_TouchTriggerTrial));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_LeaveStartpad), TDTCmd_LeaveStartpad));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_ReadyShown), TDTCmd_ReadyShown));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_ReadyWaitTooShort), TDTCmd_ReadyWaitTooShort));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_GoTargetShown), TDTCmd_GoTargetShown));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_GoReactionTooLong), TDTCmd_GoReactionTooLong));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_GoReachTooLong), TDTCmd_GoReachTooLong));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_GoTouched), TDTCmd_GoTouched));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_GoTouchedHit), TDTCmd_GoTouchedHit));
                file.WriteLine(String.Format("{0, -40}:  {1}", nameof(TDTCmd_GoTouchedMiss), TDTCmd_GoTouchedMiss));
                file.WriteLine("\n");
            }
        }



        private void Show_Flash()
        {
            int i = 1;
            PresentTrial = true;
            while (PresentTrial && i < 10000000)
            {
                myGrid.Background = brush_BKTargetShown;
                i++;
            }
            myGrid.Background = brush_BDWaitTrialStart;
        }


        public async void Present_Start2()
        {
            myGrid.Background = brush_BKReady;
            PresentTrial = true;
            int i = 1;
            while(PresentTrial && i < 10)
            {
                myGrid.Background = brush_BKTargetShown;
                await Task.Delay(200);
                myGrid.Background = brush_BKReady;
                await Task.Delay(200);
                

                i++;
            }
            
        }

        public async void Present_Start()
        {   
            


            int[] pos_OCenter_Taget;
            int t_ReadyMS;
            Random rnd = new Random();

            // restart globalWatch and thread for IO8
            globalWatch.Restart();
            if (thread_ReadWrite_IO8 != null)
                thread_ReadWrite_IO8.Start();


            // Present Each Trial
            PresentTrial = false;
            timestamp_0 = DateTime.Now.Ticks;
            while (PresentTrial)
            {
                blockN++;
                Shuffle(parent.optPostions_OCenter_List);

                // Write blockN
                using (StreamWriter file = File.AppendText(file_saved))
                {
                    file.WriteLine("\n\n");
                    
                    if(totalTriali == 0)
                    {
                        file.WriteLine("Trial Information:");
                        file.WriteLine("XY Position Unit is Pixal, (0,0) in Screen Top Left Corner");
                        file.WriteLine("Unit of Event TimePoint/Time is second");
                        file.WriteLine("\n\n");
                    }

                    file.WriteLine(String.Format("{0, -40}: {1}", "BlockN", blockN.ToString()));
                }

                int opi = 0;
                while (opi < parent.optPostions_OCenter_List.Count)
                {
                    // Extract trial parameters
                    targetType = TargetType.Go;
                    pos_OCenter_Taget = parent.optPostions_OCenter_List[opi];

                    t_ReadyMS = (int)Utility.TransferTo((float)rnd.NextDouble(), parent.tRange_ReadyTimeS[0], parent.tRange_ReadyTimeS[1]) * 1000;

                    totalTriali++;
                    if(serialPort_IO8.IsOpen)
                        serialPort_IO8.WriteLine(TDTCmd_InitState);

                    /*----- WaitStartTrial Interface ------*/
                    pressedStartpad = PressedStartpad.No;
                    await Interface_WaitStartTrial();

                    if (PresentTrial == false)
                    {
                        break;
                    }

                    /*-------- Trial Interfaces -------*/
                    try
                    {
                        // Ready Interface
                        await Interface_Ready(t_ReadyMS);

                        if (PresentTrial == false)
                        {
                            break;
                        }


                        // Target Interface
                        opi = opi + 1;
                        await Interface_Go(pos_OCenter_Taget);

                        if (PresentTrial == false)
                        {
                            break;
                        }


                        Update_FeedbackTrialsInformation();
                        Remove_All();
                    }
                    catch (TaskCanceledException)
                    {
                        Update_FeedbackTrialsInformation();
                        Remove_All();
                    }
                    if (serialPort_IO8.IsOpen)
                        serialPort_IO8.WriteLine(TDTCmd_InitState);

                    /*-------- Write Trial Information ------*/
                    List<String> strExeSubResult = new List<String>();
                    strExeSubResult.Add("readyWaitTooShort");
                    strExeSubResult.Add("cueWaitTooShort");
                    strExeSubResult.Add("ReactionTimeToolong");
                    strExeSubResult.Add("ReachTimeToolong");
                    strExeSubResult.Add("Miss");
                    strExeSubResult.Add("Success");
                    String strExeFail = "Failed";
                    String strExeSuccess = "Success";

                    using (StreamWriter file = File.AppendText(file_saved))
                    {
                        decimal ms2sRatio = 1000;

                        if (totalTriali > 1)
                        { // Startpad touched in trial i+1 treated as the return point as in trial i        
                            file.WriteLine(String.Format("{0, -40}: {1}", "Returned to Startpad TimePoint", timePoint_StartpadTouched.ToString()));
                        }

                        /* Current Trial Written Inf*/
                        file.WriteLine("\n");

                        // Trial Num
                        file.WriteLine(String.Format("{0, -40}: {1}", "TrialNum", totalTriali.ToString()));
                        // the timepoint when touching the startpad to initial a new trial
                        file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Touched TimePoint", timePoint_StartpadTouched.ToString()));

                        // Start Interface showed TimePoint
                        file.WriteLine(String.Format("{0, -40}: {1}", "Ready Start TimePoint", timePoint_Interface_ReadyOnset.ToString()));

                        // Ready Time
                        file.WriteLine(String.Format("{0, -40}: {1}", "Ready Interface Time", t_ReadyMS.ToString()));

                        // trialExeResult
                        if (trialExeResult == TrialExeResult.readyWaitTooShort)
                        {// case: ready WaitTooShort

                            // Left startpad early during ready
                            file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Left TimePoint", timePoint_StartpadLeft.ToString()));

                            // trial exe result : success or fail
                            file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeFail, strExeSubResult[0]));
                        }
                        else if (trialExeResult == TrialExeResult.goReactionTimeToolong)
                        {// case : goReactionTimeToolong 

                            // Target Interface Timepoint, and Target position 
                            file.WriteLine(String.Format("{0, -40}: {1}", "Target Start TimePoint", timePoint_Interface_TargetOnset.ToString()));
                            file.WriteLine(String.Format("{0, -40}: {1}, {2}", "TargetPosition", pos_OCenter_Taget[0].ToString(), pos_OCenter_Taget[1].ToString()));


                            // trial exe result
                            file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeFail, strExeSubResult[2]));
                        }
                        else if (trialExeResult == TrialExeResult.goReachTimeToolong)
                        {// case : goReachTimeToolong

                            // Target Interface Timepoint, and Target position 
                            file.WriteLine(String.Format("{0, -40}: {1}", "Target Start TimePoint", timePoint_Interface_TargetOnset.ToString()));
                            file.WriteLine(String.Format("{0, -40}: {1}, {2}", "TargetPosition", pos_OCenter_Taget[0].ToString(), pos_OCenter_Taget[1].ToString()));


                            // Target interface:  Left Startpad Time Point
                            file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Left TimePoint", timePoint_StartpadLeft.ToString()));

                            // trial exe result
                            file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeFail, strExeSubResult[3]));
                        }
                        else if (trialExeResult == TrialExeResult.goHit | trialExeResult == TrialExeResult.goMiss)
                        {// case: Go success (goClose or goHit) or goMiss

                            // Target Interface Timepoint, and Target position 
                            file.WriteLine(String.Format("{0, -40}: {1}", "Target Start TimePoint", timePoint_Interface_TargetOnset.ToString()));
                            file.WriteLine(String.Format("{0, -40}: {1}, {2}", "TargetPosition", pos_OCenter_Taget[0].ToString(), pos_OCenter_Taget[1].ToString()));


                            // Target interface:  Left Startpad Time Point
                            file.WriteLine(String.Format("{0, -40}: {1}", "Startpad Left TimePoint", timePoint_StartpadLeft.ToString()));

                            //  Target interface:  touched  timepoint and (x, y position) of all touch points
                            for (int pointi = 0; pointi < touchPoints_PosTime.Count; pointi++)
                            {
                                double[] downPoint = touchPoints_PosTime[pointi];

                                // touched pointi touchpoint
                                file.WriteLine(String.Format("{0, -40}: {1, -40}", "Touch Point " + pointi.ToString() + " TimePoint", ((decimal)downPoint[1] / ms2sRatio).ToString()));

                                // touched pointi position
                                file.WriteLine(String.Format("{0, -40}: {1}", "Touch Point " + pointi.ToString() + " XY Position", downPoint[2].ToString() + ", " + downPoint[3].ToString()));

                            }

                            //  Target interface:  left timepoint and (x, y position) of all touch points
                            for (int pointi = 0; pointi < touchPoints_PosTime.Count; pointi++)
                            {
                                double[] downPoint = touchPoints_PosTime[pointi];

                                // left pointi touchpoint
                                file.WriteLine(String.Format("{0, -40}: {1, -40}", "Left Point " + pointi.ToString() + " TimePoint", ((decimal)downPoint[4] / ms2sRatio).ToString()));

                                // left pointi position
                                file.WriteLine(String.Format("{0, -40}: {1}", "Left Point " + pointi.ToString() + " XY Position", downPoint[5].ToString() + ", " + downPoint[6].ToString()));
                            }


                            // trial exe result : success or fail
                            if (trialExeResult == TrialExeResult.goMiss)
                                file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeFail, strExeSubResult[4]));
                            else
                                file.WriteLine(String.Format("{0, -40}: {1}, {2}", "Trial Result", strExeSuccess, strExeSubResult[5]));

                        }
                    }

                    if(PresentTrial==false)
                    {
                        break;
                    }

                    // Wait t_InterTrialMS between the end of the trial and before the next
                    Stopwatch waitWatch = new Stopwatch();
                    waitWatch.Start();
                    while (PresentTrial)
                    {
                        if (waitWatch.ElapsedMilliseconds >= t_InterTrialMS)
                        {
                            waitWatch.Stop();
                            break;
                        }
                    }
                            
                }
            }

        }

        public void Present_Stop()
        {
            PresentTrial = false;

            // After Trials Presentation
            if (serialPort_IO8 != null && serialPort_IO8.IsOpen)
                serialPort_IO8.Close();

            if (thread_ReadWrite_IO8 != null)
                thread_ReadWrite_IO8.Abort();

            globalWatch.Stop();
            
            tpoints1TouchWatch.Stop();

            if(SaveSummaryofExpNeeded)// save the summary of exp
                SaveSummaryofExp();
        }

        public void Present_Pause()
        {
            PresentTrial = false;

            // After Trials Presentation
            if (serialPort_IO8.IsOpen)
                serialPort_IO8.Close();

            globalWatch.Stop();
            tpoints1TouchWatch.Stop();
            
            myGrid.Background = brush_BKWaitTrialStart;
            Remove_All();
        }



        private void SaveSummaryofExp()
        {
            /*Save the summary information of the exp*/

            using (StreamWriter file = File.AppendText(file_saved))
            {
                file.WriteLine("\n\n");

                file.WriteLine(String.Format("{0}", "Summary of the Trials"));
                file.WriteLine(String.Format("{0, -40}: {1}", "Total Trials", totalGoTrialNum.ToString()));
                file.WriteLine(String.Format("{0, -40}: {1}", "Success Trials", successGoTrialNum.ToString()));
                file.WriteLine(String.Format("{0, -40}: {1}", "Miss Trials", missGoTrialNum.ToString()));
                file.WriteLine(String.Format("{0, -40}: {1}", "No Reaction Trials", noreactionGoTrialNum.ToString()));
                file.WriteLine(String.Format("{0, -40}: {1}", "No Reach Trials", noreachGoTrialNum.ToString()));
            }

        }


        public void Update_FeedbackTrialsInformation()
        {/* Update the Feedback Trial Information in the Mainwindow */

            // Go trials
            parent.textBox_totalGoTrialNum.Text = totalGoTrialNum.ToString();
            parent.textBox_successGoTrialNum.Text = successGoTrialNum.ToString();
            parent.textBox_missGoTrialNum.Text = missGoTrialNum.ToString();
            parent.textBox_noreactionGoTrialNum.Text = noreactionGoTrialNum.ToString();
            parent.textBox_noreachGoTrialNum.Text = noreachGoTrialNum.ToString();

        }

        private void Init_FeedbackTrialsInformation()
        {/* Update the Feedback Trial Information in the Mainwindow */


            totalGoTrialNum = 0;
            successGoTrialNum = 0;
            missGoTrialNum = 0;
            noreactionGoTrialNum = 0;
            noreachGoTrialNum = 0;

            // Update Main Window Feedback 
            parent.textBox_totalGoTrialNum.Text = totalGoTrialNum.ToString();
            parent.textBox_successGoTrialNum.Text = successGoTrialNum.ToString();
            parent.textBox_missGoTrialNum.Text = missGoTrialNum.ToString();
            parent.textBox_noreactionGoTrialNum.Text = noreactionGoTrialNum.ToString();
            parent.textBox_noreachGoTrialNum.Text = noreachGoTrialNum.ToString();

        }


        private void SetAudioFeedback()
        {/*set the player_Correct and player_Error members
            */

            player_Correct = new System.Media.SoundPlayer(Properties.Resources.Correct);
            player_Error = new System.Media.SoundPlayer(Properties.Resources.Error);


            // Assign new audios
            if (String.Compare(audioFile_Correct, "default", true) != 0)
            {
                player_Correct.SoundLocation = audioFile_Correct;
            }
            if (String.Compare(audioFile_Error, "default", true) != 0)
            {
                player_Error.SoundLocation = audioFile_Error;
            }
        }


        

        private void GetSetupParameters()
        {/* get the setup from the parent interface */


            // interfaces time related parameters
            tMax_ReactionTimeMS = parent.tMax_ReactionTimeS * 1000;
            tMax_ReachTimeMS = parent.tMax_ReachTimeS * 1000;
            t_VisfeedbackShowMS = (Int32)(parent.t_VisfeedbackShowS * 1000);
            t_InterTrialMS = (Int32)(parent.t_InterTrialS * 1000);

            // Juicer Time
            t_JuicerCorrectGiven = (Int32)(parent.t_JuicerCorrectGivenS * 1000);



            /* ---- Get all the Set Colors ----- */
            Color selectedColor;
            
            // goCircle Color
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.targetFillColorStr) as PropertyInfo).GetValue(null, null);
            brush_goCircleFill = new SolidColorBrush(selectedColor);


            // Wait Background 
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.BKWaitTrialColorStr) as PropertyInfo).GetValue(null, null);
            brush_BKWaitTrialStart = new SolidColorBrush(selectedColor);
            
            // Wait Boarder
            brush_BDWaitTrialStart = brush_BKWaitTrialStart;

            // Ready Background
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.BKReadyColorStr) as PropertyInfo).GetValue(null, null);
            brush_BKReady = new SolidColorBrush(selectedColor);

            // Target Shown Background
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.BKTargetShownColorStr) as PropertyInfo).GetValue(null, null);
            brush_BKTargetShown = new SolidColorBrush(selectedColor);

            // Correct Fill Color
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.CorrFillColorStr) as PropertyInfo).GetValue(null, null);
            brush_CorrectFill = new SolidColorBrush(selectedColor);

            // Correct Outline Color
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.CorrOutlineColorStr) as PropertyInfo).GetValue(null, null);
            brush_CorrOutline = new SolidColorBrush(selectedColor);

            // Error Fill Color
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.ErrorFillColorStr) as PropertyInfo).GetValue(null, null);
            brush_ErrorFill = new SolidColorBrush(selectedColor);

            // Error Outline Color
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.ErrorOutlineColorStr) as PropertyInfo).GetValue(null, null);
            brush_ErrorOutline = new SolidColorBrush(selectedColor);


            // Error Crossing Color
            selectedColor = (Color)(typeof(Colors).GetProperty(parent.ErrorCrossingColorStr) as PropertyInfo).GetValue(null, null);
            brush_ErrorCrossing = new SolidColorBrush(selectedColor);

            
            
            // get the file for saving 
            file_saved = parent.file_saved;
            audioFile_Correct = parent.textBox_audioFile_Correct.Text;
            audioFile_Error = parent.textBox_audioFile_Error.Text;
        }


        private void Create_GoCircle()
        {/*
            Create the go circle: circleGo

            */

            // Create an Ellipse 
            if(circleGo == null)
            {
                circleGo = Utility.Create_Circle(parent.targetDiaPixal, brush_goCircleFill);
            }
            else
            {
                circleGo.Height = parent.targetDiaPixal;
                circleGo.Width = parent.targetDiaPixal;

                circleGo.Fill = brush_goCircleFill;
            }
            
            circleGo.Name = name_circleGo;
            circleGo.Visibility = Visibility.Hidden;
            circleGo.IsEnabled = false;
            circleGo.StrokeThickness = circleGo_StrokeThickness;

            // add to myGrid
            if(!myGrid.Children.Contains(circleGo))
            {
                myGrid.Children.Add(circleGo);
                myGrid.RegisterName(circleGo.Name, circleGo);
            }

            myGrid.UpdateLayout();

        }

        private void Show_GoCircle(int[] cPoint_Pos_OCenter)
        {/*
            Show the GoCircle into cPoint_Pos_OCenter (Origin in the center of the Screen)

            Arg:
                cPoint_Pos_OCenter: the x, y Positions of the Circle center in Pixal (Origin in the center of the Screen)

             */

            // Change the cPoint  into Top Left Coordinate System
            sd.Rectangle Rect_touchScreen = Utility.Detect_PrimaryScreen_Rect();
            int[] cPoint_Pos_OTopLeft = new int[] { cPoint_Pos_OCenter[0] + Rect_touchScreen.Width/2, cPoint_Pos_OCenter[1] + Rect_touchScreen.Height/2 };


            circleGo  = Utility.Move_Circle_OTopLeft(circleGo, cPoint_Pos_OTopLeft);
            circleGo.Fill = brush_goCircleFill;
            circleGo.Stroke = brush_goCircleFill;
            circleGo.Visibility = Visibility.Visible;
            circleGo.IsEnabled = true;
            myGrid.UpdateLayout();


            // Extract circleGo_cPoint_Pixal 
            circleGo_cPoint_Pixal = new Point(cPoint_Pos_OTopLeft[0], cPoint_Pos_OTopLeft[1]);

            
            Point circleGo_cPoint_Pixal2 = new Point(cPoint_Pos_OCenter[0] + Rect_touchScreen.Width/ 2, cPoint_Pos_OCenter[1] + Rect_touchScreen .Height/ 2);

            circleGo_Radius_Pixal = ((circleGo.Height + circleGo.Width) / 2) / 2;

        }
        private void Remove_GoCircle()
        {
            circleGo.Visibility = Visibility.Hidden;
            circleGo.IsEnabled = false;
            myGrid.UpdateLayout();

        }


        private void Create_OneCrossing()
        {/*create the crossing cue*/


            // Create the horizontal line
            if (crossing_horiLine == null)
                crossing_horiLine = new Line();

            crossing_horiLine.X1 = 0;
            crossing_horiLine.Y1 = 0;
            crossing_horiLine.X2 = crossing_len;
            crossing_horiLine.Y2 = crossing_horiLine.Y1;

            // horizontal line position
            crossing_horiLine.HorizontalAlignment = HorizontalAlignment.Left;
            crossing_horiLine.VerticalAlignment = VerticalAlignment.Top;

            // horizontal line color
            crossing_horiLine.Stroke = brush_ErrorCrossing;
            // horizontal line stroke thickness
            crossing_horiLine.StrokeThickness = crossing_LineThickness;
            // name
            crossing_horiLine.Name = name_crossingHLine;


            // Create the vertical line
            if (crossing_vertLine == null)
                crossing_vertLine = new Line();
            crossing_vertLine.X1 = 0;
            crossing_vertLine.Y1 = 0;
            crossing_vertLine.X2 = crossing_vertLine.X1;
            crossing_vertLine.Y2 = crossing_len;
            // vertical line position
            crossing_vertLine.HorizontalAlignment = HorizontalAlignment.Left;
            crossing_vertLine.VerticalAlignment = VerticalAlignment.Top;

            // vertical line color
            crossing_vertLine.Stroke = brush_ErrorCrossing;
            // vertical line stroke thickness
            crossing_vertLine.StrokeThickness = crossing_LineThickness;
            //name
            crossing_vertLine.Name = name_crossingVLine;


            // Add, Hidden and Disable the Crossing
            crossing_horiLine.Visibility = Visibility.Hidden;
            crossing_horiLine.IsEnabled = false;
            if (!myGrid.Children.Contains(crossing_horiLine))
            {
                myGrid.Children.Add(crossing_horiLine);
                myGrid.RegisterName(crossing_horiLine.Name, crossing_horiLine);
            }


            crossing_vertLine.Visibility = Visibility.Hidden;
            crossing_vertLine.IsEnabled = false;
            if (!myGrid.Children.Contains(crossing_vertLine))
            {
                myGrid.Children.Add(crossing_vertLine);
                myGrid.RegisterName(crossing_vertLine.Name, crossing_vertLine);
            }


            myGrid.UpdateLayout();
        }

        private void Show_OneCrossing(int[] centerPoint_Pos)
        {/*     Show One Crossing Containing One Horizontal Line and One Vertical Line
            *   The Center Points of the Two Lines Intersect at centerPoint_Pos
            * 
             */

            int centerPoint_X = centerPoint_Pos[0], centerPoint_Y = centerPoint_Pos[1];

            crossing_horiLine.Margin = new Thickness(centerPoint_X - crossing_len / 2, centerPoint_Y, 0, 0);
            crossing_vertLine.Margin = new Thickness(centerPoint_X, centerPoint_Y - crossing_len / 2, 0, 0);

            crossing_horiLine.Visibility = Visibility.Visible;
            crossing_vertLine.Visibility = Visibility.Visible;
            myGrid.UpdateLayout();
        }

        private void Remove_OneCrossing()
        {
            crossing_horiLine.Visibility = Visibility.Hidden;
            crossing_vertLine.Visibility = Visibility.Hidden;
            myGrid.UpdateLayout();
        }


        private void Remove_All()
        {
            Remove_GoCircle();
            Remove_OneCrossing();
        }

        private void Thread_ReadWrite_IO8()
        {/* Thread for reading/writing serial port IO8*/

            string codeHigh_JuicerPin = cmdHigh3, codeLow_JuicerPin = cmdLow3;
            Stopwatch startpadReadWatch = new Stopwatch();
            long startpadReadInterval = 30;

            if (serialPort_IO8.IsOpen)
            {
                serialPort_IO8.WriteLine(codeLow_JuicerPin);
            }

            startpadReadWatch.Start();
            while (serialPort_IO8.IsOpen)
            {
                try
                {
                    // ----- Juicer Control
                    if (giveJuicerState == GiveJuicerState.CorrectGiven)
                    {
                        serialPort_IO8.WriteLine(codeHigh_JuicerPin);
                        Thread.Sleep(t_JuicerCorrectGiven);
                        if (serialPort_IO8.IsOpen)
                            serialPort_IO8.WriteLine(codeLow_JuicerPin);
                        giveJuicerState = GiveJuicerState.No;
                    }
                    //--- End of Juicer Control



                    //--- Startpad Read
                    if (startpadReadWatch.ElapsedMilliseconds >= startpadReadInterval)
                    {
                        if (serialPort_IO8.IsOpen)
                            serialPort_IO8.WriteLine(startpad_DigIn);

                        // Read the Startpad Voltage
                        string str_Read = "";
                        if (serialPort_IO8.IsOpen)
                            str_Read = serialPort_IO8.ReadExisting();

                        // Restart the startpadReadWatch
                        startpadReadWatch.Restart();


                        // parse the start pad status
                        string[] str_DigIn = str_Read.Split();

                        if (!String.IsNullOrEmpty(str_DigIn[0]))
                        {
                            int digIn = Int32.Parse(str_DigIn[0]);

                            if (digIn == startpad_DigIn_Pressed && pressedStartpad == PressedStartpad.No)
                            {/* time point from notouched state to touched state */

                                pressedStartpad = PressedStartpad.Yes;
                            }
                            else if (digIn == startpad_DigIn_Unpressed && pressedStartpad == PressedStartpad.Yes)
                            {/* time point from touched state to notouched state */

                                // the time point for leaving startpad
                                timePoint_StartpadLeft = globalWatch.ElapsedMilliseconds;
                                serialPort_IO8.WriteLine(TDTCmd_LeaveStartpad);
                                pressedStartpad = PressedStartpad.No;
                            }
                        }
                    }
                }
                catch (InvalidCastException e)
                { }
            }

            startpadReadWatch.Stop();
        }



        private Task Interface_WaitStartTrial()
        {
            /* task for WaitStart interface
             * 
             * Wait for Touching Startpad to trigger a new Trial
             */

            Remove_All();
            myGrid.Background = brush_BKWaitTrialStart;
            //myGridBorder.BorderBrush = brush_BDWaitTrialStart;

            Task task_WaitStart = Task.Run(() =>
            {
                while (PresentTrial && pressedStartpad == PressedStartpad.No) ;


                if (PresentTrial && pressedStartpad == PressedStartpad.Yes)
                {
                    // the time point for startpad touched
                    if (serialPort_IO8.IsOpen)
                        serialPort_IO8.WriteLine(TDTCmd_TouchTriggerTrial);
                    timePoint_StartpadTouched = globalWatch.ElapsedMilliseconds;
                }

            });

            return task_WaitStart;
        }

        private Task Wait_Reaction()
        {/* Wait for Reaction within tMax_ReactionTime */

            // start a task and return it
            return Task.Run(() =>
            {
                Stopwatch waitWatch = new Stopwatch();
                waitWatch.Start();
                while (PresentTrial && pressedStartpad == PressedStartpad.Yes)
                {
                    if (PresentTrial && waitWatch.ElapsedMilliseconds >= tMax_ReactionTimeMS)
                    {/* No release Startpad within tMax_ReactionTime */
                        if (serialPort_IO8.IsOpen)
                            serialPort_IO8.WriteLine(TDTCmd_GoReactionTooLong);
                        waitWatch.Stop();

                        noreactionGoTrialNum++;

                        
                        trialExeResult = TrialExeResult.goReactionTimeToolong;
                        

                        throw new TaskCanceledException("No Reaction within the Max Reaction Time");
                    }
                }
                waitWatch.Stop();
            });
        }

        private Task Wait_Reach()
        {/* Wait for Reach within tMax_ReachTime*/

            return Task.Run(() =>
            {
                Stopwatch waitWatch = new Stopwatch();
                waitWatch.Start();
                while (PresentTrial && screenTouchstate == ScreenTouchState.Idle)
                {
                    if (PresentTrial && waitWatch.ElapsedMilliseconds >= tMax_ReachTimeMS)
                    {/*No Screen Touched within tMax_ReachTime*/
                        if (serialPort_IO8.IsOpen)
                            serialPort_IO8.WriteLine(TDTCmd_GoReachTooLong);
                        waitWatch.Stop();

                        noreachGoTrialNum++;

                        trialExeResult = TrialExeResult.goReachTimeToolong;
                        

                        throw new TaskCanceledException("No Reach within the Max Reach Time");
                    }
                }
                downPoints_Pos.Clear();
                touchPoints_PosTime.Clear();
                waitWatch.Restart();
                while (PresentTrial && waitWatch.ElapsedMilliseconds <= tMax_1Touch) ;
                waitWatch.Stop();
                calc_GoTargetTouchState();
            }); 
        }

        private void calc_GoTargetTouchState()
        {/* Calculate GoTargetTouchState  
            1. based on the Touch Down Positions in  List downPoints_Pos and circleGo_cPoint_Pixal
            2. Assign the calculated target touch state to the GoTargetTouchState variable gotargetTouchstate
            */

            double distance; 
            gotargetTouchstate = GoTargetTouchState.goMissed;
            while (PresentTrial && downPoints_Pos.Count > 0)
            {
                // always deal with the point at 0
                Point touchp = new Point(downPoints_Pos[0][0], downPoints_Pos[0][1]);

                // distance between the touchpoint and the center of the circleGo
                distance = Point.Subtract(circleGo_cPoint_Pixal, touchp).Length;


                if (distance <= circleGo_Radius_Pixal)
                {// Hit 

                    if (serialPort_IO8.IsOpen)
                        serialPort_IO8.WriteLine(TDTCmd_GoTouchedHit);
                    gotargetTouchstate = GoTargetTouchState.goHit;
                    
                    downPoints_Pos.Clear();
                    break;
                }

                // remove the downPoint at 0
                downPoints_Pos.RemoveAt(0);
            }

            if (PresentTrial && serialPort_IO8.IsOpen && gotargetTouchstate == GoTargetTouchState.goMissed )
            {
                serialPort_IO8.WriteLine(TDTCmd_GoTouchedMiss);
            }
            downPoints_Pos.Clear();
        }


        private Task Wait_EnoughTouch(int t_EnoughTouchMS)
        {
            /* 
             * Wait for Enough Touch Time (ms)
             * 
             * Input: 
             *    t_EnoughTouch: the required Touch time (ms)  
             */

            Task task = null;

            // start a task and return it
            return Task.Run(() =>
            {
                Stopwatch touchedWatch = new Stopwatch();
                touchedWatch.Restart();

                while (PresentTrial && pressedStartpad == PressedStartpad.Yes && startpadHoldstate != StartPadHoldState.HoldEnough)
                {
                    if (touchedWatch.ElapsedMilliseconds >= t_EnoughTouchMS)
                    {/* touched with enough time */
                        startpadHoldstate = StartPadHoldState.HoldEnough;
                    }
                }
                touchedWatch.Stop();
                if (PresentTrial && startpadHoldstate != StartPadHoldState.HoldEnough)
                {
                    throw new TaskCanceledException(task);
                }

            });
        }

        private async Task Interface_Ready(int t_ReadyMS)
        {/* task for Ready interface:
            Show the Ready Interface while Listen to the state of the startpad. 
            * 
            * Output:
            *   startPadHoldstate_Ready = 
            *       StartPadHoldState.HoldEnough (if startpad is touched lasting t_Ready)
            *       StartPadHoldState.HoldTooShort (if startpad is released before t_Ready) 
            */

            try
            {
                myGrid.Background = brush_BKReady;
                if (serialPort_IO8.IsOpen)
                    serialPort_IO8.WriteLine(TDTCmd_ReadyShown);
                timePoint_Interface_ReadyOnset = globalWatch.ElapsedMilliseconds;

                // Wait Startpad Hold Enough Time
                startpadHoldstate = StartPadHoldState.HoldTooShort;
                if (PresentTrial)
                    await Wait_EnoughTouch(t_ReadyMS);

            }
            catch (TaskCanceledException)
            {
                // trial execute result: waitReadyTooShort 
                if (serialPort_IO8.IsOpen)
                    serialPort_IO8.WriteLine(TDTCmd_ReadyWaitTooShort);
                trialExeResult = TrialExeResult.readyWaitTooShort;

                Task task = null;
                throw new TaskCanceledException(task);
            }
        }

        private async Task Interface_Go(int[] pos_Target)
        {/* task for Go Interface: Show the Go Interface while Listen to the state of the startpad.
            * 1. If Reaction time < Max Reaction Time or Reach Time < Max Reach Time, end up with long reaction or reach time ERROR Interface
            * 2. Within proper reaction time && reach time, detect the touch point and end up with hit, near and miss.
            
            * Args:
            *    pos_Target: the center position of the Go Target

            * Output:
            *   startPadHoldstate_Cue = 
            *       StartPadHoldState.HoldEnough (if startpad is touched lasting t_Cue)
            *       StartPadHoldState.HoldTooShort (if startpad is released before t_Cue) 
            */

            try
            {
                myGrid.Background = brush_BKTargetShown;

                // Add the Go Circle
                Show_GoCircle(pos_Target);

                // go target Onset Time Point
                timePoint_Interface_TargetOnset = globalWatch.ElapsedMilliseconds;
                if(serialPort_IO8.IsOpen)
                    serialPort_IO8.WriteLine(TDTCmd_GoTargetShown);

                totalGoTrialNum++;

                // Wait for Reaction within tMax_ReactionTime
                pressedStartpad = PressedStartpad.Yes;
                await Wait_Reaction();
                if (!PresentTrial)
                    return;

                // Wait for Touch within tMax_ReachTime and Calcuate the gotargetTouchstate
                screenTouchstate = ScreenTouchState.Idle;
                await Wait_Reach();
                if (!PresentTrial)
                    return;


                /*---- Go Target Touch States ----*/
                if (PresentTrial && gotargetTouchstate == GoTargetTouchState.goHit)
                {/*Hit */

                    Feedback_GoCorrect_Hit();

                    successGoTrialNum++;

                    // trial execute result: goHit 
                    trialExeResult = TrialExeResult.goHit;
                    
                }
                else if (PresentTrial && gotargetTouchstate == GoTargetTouchState.goMissed)
                {/* touch missed*/
                    
                    Feedback_GoERROR_Miss();

                    missGoTrialNum++;

                    // trial execute result: goMiss 
                    trialExeResult = TrialExeResult.goMiss;
                }
                if (!PresentTrial)
                    return;
                await Task.Delay(t_VisfeedbackShowMS);
            }
            catch(TaskCanceledException)
            {
                if(PresentTrial)
                    Interface_GoERROR_LongReactionReach();
                if (PresentTrial)
                    await Task.Delay(t_VisfeedbackShowMS);
                throw new TaskCanceledException("Not Reaction Within the Max Reaction Time.");
                
            }
            
        }


        private void Feedback_GoERROR()
        {
            // Visual Feedback
            //myGridBorder.BorderBrush = brush_ErrorFill;
            circleGo.Fill = brush_ErrorFill;
            circleGo.Stroke = brush_ErrorOutline;
            

            Show_OneCrossing(new int[] { (int)circleGo_cPoint_Pixal.X, (int)circleGo_cPoint_Pixal.Y});

            myGrid.UpdateLayout();


            //Juicer Feedback
            giveJuicerState = GiveJuicerState.No;

            // Audio Feedback
            player_Error.Play()
;            
        }
        private void Interface_GoERROR_LongReactionReach()
        {
            Feedback_GoERROR();
        }

        private void Feedback_GoERROR_Miss()
        {
            Feedback_GoERROR();
        }

        private void Feedback_GoCorrect_Hit()
        {
            // Visual Feedback
            //myGridBorder.BorderBrush = brush_CorrectFill;
            circleGo.Fill = brush_CorrectFill;
            circleGo.Stroke = brush_CorrOutline;
            myGrid.UpdateLayout();


            //Juicer Feedback
            giveJuicerState = GiveJuicerState.CorrectGiven;

            // Audio Feedback
            player_Correct.Play();
        }



        public void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            parent.presentation_Stop();
        }


        void Touch_FrameReported(object sender, TouchFrameEventArgs e)
        {/* Add the Id of New Touch Points into Hashset touchPoints_Id 
            and the Corresponding Touch Down Positions into List downPoints_Pos (no replicates)*/
            screenTouchstate = ScreenTouchState.Touched;
            TouchPointCollection touchPoints = e.GetTouchPoints(myGrid);
            bool addedNew;
            long time = tpoints1TouchWatch.ElapsedMilliseconds;
            long timestamp_now = (DateTime.Now.Ticks - timestamp_0) / TimeSpan.TicksPerMillisecond;
            for (int i = 0; i < touchPoints.Count; i++)
            {
                TouchPoint _touchPoint = touchPoints[i];
                if (_touchPoint.Action == TouchAction.Down)
                { /* TouchAction.Down */

                    if (touchPoints_Id.Count == 0)
                    {// the first touch point for one touch
                        tpoints1TouchWatch.Restart();
                        serialPort_IO8.WriteLine(TDTCmd_GoTouched);
                    }
                    lock (touchPoints_Id)
                    {
                        // Add the touchPoint to the Hashset touchPoints_Id, Return true if added, otherwise false.
                        addedNew = touchPoints_Id.Add(_touchPoint.TouchDevice.Id);
                    }
                    if (addedNew)
                    {/* deal with the New Added TouchPoint*/

                        // store the pos of the point with down action
                        lock (downPoints_Pos)
                        {
                            downPoints_Pos.Add(new double[2] { _touchPoint.Position.X, _touchPoint.Position.Y });
                        }

                        // store the pos and time of the point with down action, used for file writing
                        lock (touchPoints_PosTime)
                        {
                            touchPoints_PosTime.Add(new double[7] { _touchPoint.TouchDevice.Id, timestamp_now, _touchPoint.Position.X, _touchPoint.Position.Y, 0, 0, 0 });
                        }
                    }
                }
                else if (_touchPoint.Action == TouchAction.Up)
                {
                    // remove the id of the point with up action
                    lock (touchPoints_Id)
                    {
                        touchPoints_Id.Remove(_touchPoint.TouchDevice.Id);
                    }

                    // add the left points timepoint, and x,y positions of the current _touchPoint.TouchDevice.Id
                    lock (touchPoints_PosTime)
                    {
                        for (int pointi = 0; pointi < touchPoints_PosTime.Count; pointi++)
                        {
                            if (touchPoints_PosTime[pointi][0] == _touchPoint.TouchDevice.Id)
                            {
                                touchPoints_PosTime[pointi][4] = timestamp_now;
                                touchPoints_PosTime[pointi][5] = _touchPoint.Position.X;
                                touchPoints_PosTime[pointi][6] = _touchPoint.Position.Y;
                            }
                        }
                    }
                }
            }

        }
    }
}

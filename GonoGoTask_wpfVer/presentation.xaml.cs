﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO.Ports;
using System.IO;
using System.Windows.Controls;

namespace GonoGoTask_wpfVer
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
            Nogo,
            Go,
        }

        public enum InterfaceState
        {
            beforeStart,
            Ready,
            TargetCue,
            GoNogo,
            Reward,
        }

        public enum ScreenTouchState
        {
            Idle,
            Touched
        }

        private enum GoTargetTouchState
        {
            goHit, // at least one finger inside the circleGo
            goClose, // the distance between the closest touch point and the center of the circleGo is less than a threshold
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

        private enum StartPadHoldState
        {
            HoldEnough,
            HoldTooShort
        }



        /*****************parameters ************************/
        MainWindow parent;


        string file_saved;

        // diameter for crossing, circle, square and white points
        int objdiameter;
        int disFromCenter, disXFromCenter, disYFromCenter;
        // the threshold distance defining close
        int disThreshold_close; 

        TargetType targetType;
        
        // randomized Go noGo tag list, tag_gonogo ==1: go, ==0: nogo
        List<TargetType> targetType_List = new List<TargetType>();
        // randomized t_Ready list
        List<float> t_Ready_List = new List<float>();
        // randomized t_Cue list
        List<float> t_Cue_List = new List<float>();


        // objects of Go cirle, nogo Rectangle, lines of the crossing, and two white points
        Ellipse circleGo;
        SolidColorBrush brush_goCircle;
        Rectangle rectNogo;
        SolidColorBrush brush_nogoRect;
        Line vertLine, horiLine;
        Ellipse point1, point2;

        // Colors for Correct and Error
        SolidColorBrush brush_ErrorInterface, brush_CorrectInterface, brush_NearInterface;

        Point circleGo_centerPoint; // the center of circleGo 
        double circleGo_radius; // the radius of circleGO

        // background of ready and trial
        SolidColorBrush brush_bkwaitstart, brush_bdwaitstart, brush_bktrial;
        

        InterfaceState interfaceState;

        // name of all the objects
        string name_circleGo = "circleGo";
        string name_rectNogo = "rectNogo";
        string name_vLine = "vLine", name_hLine = "hLine";
        string name_point1 = "wpoint1", name_point2 = "wpoint2";
        
        // all the optional positions, in which one is for target, the remaining for white points
        List<int[]> optPostions_List = new List<int[]>();

        // randomized target Position index list for each trial
        List<int> targetPosInd_List = new List<int>();
        // the remaining Position indices (for two while points) list for each trial
        List<int[]> otherPosInds_List = new List<int[]>();



        // wait range for each event
        float waitt_goNogo, waitt_reward;
        float[] waittrange_ready, waittrange_cue;
        // Max Reaction and Reach Time
        float tMax_ReactionTime, tMax_ReachTime;


        bool PresentTask;


        // set storing the touch point id (no replicates)
        HashSet<int> touchPoints_Id = new HashSet<int>();
        // list storing the position of the touch points when touched down
        List<double[]> downPoints_Pos = new List<double[]>();
        // Stop Watch for recording the time interval between the first touchpoint and the last touchpoint
        Stopwatch touchPointsWatch;
        // the Max Duration for One Touch (ms)
        long tMax_1Touch = 10;
        GoTargetTouchState gotargetTouchstate;
        // list storing the touch time of the touch points when touched down
        List<long> downPoints_time = new List<long>(); 


        // tag of starting a trial
        bool startTrial = false;
        // tag of touching during go interface 
        bool touched_InterfaceGoNogo = false;
        //tag of interupting during interfaces except go interface
        bool interupt_InterfaceOthers = false;

        ScreenTouchState screenTouchstate;
        

        // hold states for Ready, Cue Interfaces
        StartPadHoldState startpadHoldstate;


        CancellationTokenSource cancellationTokenSourece = new CancellationTokenSource();


        // serial port for DLP-IO8-G
        SerialPort serialPort_IO8;
        int baudRate = 115200;

        /* startpad parameters */
        PressedStartpad pressedStartpad;
        public delegate void UpdateTextCallback(string message);


        // Global stopwatch
        Stopwatch globalWatch; 
        // variables for recording the startpad touched on and off time point
        long startpadOn_TimePoint, startpadOff_TimePoint;
        long startpadOn_StartTrial_TimePoint;


        Thread thread_readStartpad;

        /*****Methods*******/
        public presentation(MainWindow mainWindow)
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;

            Touch.FrameReported += new TouchFrameEventHandler(Touch_FrameReported);

            parent = mainWindow;


        }

        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // get the setup from the parent interface
            GetSetupParameters();

            disXFromCenter = disFromCenter;
            disYFromCenter = disFromCenter;
            optPostions_List.Add(new int[] { -disXFromCenter, 0 }); // left position
            optPostions_List.Add(new int[] { 0, -disYFromCenter }); // top position
            optPostions_List.Add(new int[] { disXFromCenter, 0 }); // right position

            //shuffle go and nogo trials
            Shuffle_GonogoTrials(Int32.Parse(parent.textBox_goTrialNum.Text), Int32.Parse(parent.textBox_nogoTrialNum.Text));

            
            // Create necessary elements: go circle, nogo rect, two white points and one crossing
            Create_GoCircle();
            Create_NogoRect();
            Create_TwoWhitePoints();
            Create_OneCrossing();




            // init a global stopwatch
            globalWatch = new Stopwatch();
            touchPointsWatch = new Stopwatch();
            // Thread for reading startpad continously
            thread_readStartpad = new Thread(new ThreadStart(Thread_ReadStartpad));


            // create a serial Port IO8 instance, and open it
            serialPort_IO8 = new SerialPort();
            try
            {
                serialPort_SetOpen(parent.serialPortIO8_name, baudRate);

                WriteHeaderInf();

                // present task trial by trial
                //Present_Task();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Message", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void serialPort_SetOpen (string portName, int baudRate)
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


        private void Shuffle_GonogoTrials(int gotrialnum, int nogotrialnum)
        {/* ---- 
            1. shuffle go and nogo trials, present in member variable taglist_gonogo 
            2. Generate the random t_Ready, and t_Cue for each trial, stored in t_Ready_List and t_Cue_List;
            3. Select the target position index of the optional positions, stored in targetPosInd_List and otherPosInds_List
             */

            // create orderred gonogo list
            List<TargetType> tmporder_go = new List<TargetType>(Enumerable.Repeat(TargetType.Go, gotrialnum));
            List<TargetType> tmporder_nogo = new List<TargetType>(Enumerable.Repeat(TargetType.Nogo, nogotrialnum));
            List<TargetType> tmporder_gonogo = tmporder_go.Concat(tmporder_nogo).ToList();


            // shuffle 
            Random r = new Random();
            int randomIndex;
            

            while (tmporder_gonogo.Count > 0)
            {
                // choose a random index in list tmporder_gonogo
                randomIndex = r.Next(0, tmporder_gonogo.Count);

                // add the selected value (go/nogo type) into tagarray_gonogo
                targetType_List.Add(tmporder_gonogo[randomIndex]);


                // add the corresponding go/noGo object and the two white points position
                int targetPosInd = r.Next(0, 3);
                int[] otherPosInds = new int[] { 0, 1, 2};
                otherPosInds = otherPosInds.Where(w => w != targetPosInd).ToArray();
                targetPosInd_List.Add(targetPosInd);
                otherPosInds_List.Add(otherPosInds);

                // generate a random t_Ready and t_Cue, and and them into t_Ready_List and t_Cue_List individually
                t_Cue_List.Add(TransferTo((float)r.NextDouble(), waittrange_cue[0], waittrange_cue[1]));
                t_Ready_List.Add(TransferTo((float)r.NextDouble(), waittrange_ready[0], waittrange_ready[1]));

                //remove this value
                tmporder_gonogo.RemoveAt(randomIndex);
            }
        }

        public int cm2pixal(float cmlen)
        {/* convert length with unit cm to unit pixal, 96 pixals = 1 inch = 2.54 cm

            args:   
                cmlen: to be converted length (unit: cm)

            return:
                pixalen: converted length with unit pixal
         */

            float ratio = (float)96 / (float)2.54;

            int pixalen = (int)(cmlen * ratio);

            return pixalen;
        }


        public int in2pixal(float inlen)
        {/* convert length with unit inch to unit pixal, 96 pixals = 1 inch = 2.54 cm

            args:   
                cmlen: to be converted length (unit: inch)

            return:
                pixalen: converted length with unit pixal
         */

            int ratio = 96;

            int pixalen = (int)(inlen * ratio);

            return pixalen;
        }


        private void GetSetupParameters()
        {/* get the setup from the parent interface */

            // object size and distance parameters
            objdiameter = in2pixal(float.Parse(parent.textBox_objdiameter.Text));
            disFromCenter = in2pixal(float.Parse(parent.textBox_disfromcenter.Text));
            disThreshold_close = (int)((objdiameter / 2) * float.Parse(parent.textBox_errorMargin.Text) / 100);


            // interfaces time related parameters
            waitt_goNogo = float.Parse(parent.textBox_tmaxGoNogoShow.Text);
            waitt_reward = float.Parse(parent.textBox_tRewardShow.Text);
            waittrange_ready = new float[] { float.Parse(parent.textBox_tReady_min.Text), float.Parse(parent.textBox_tReady_max.Text) };
            waittrange_cue = new float[] { float.Parse(parent.textBox_tCue_min.Text), float.Parse(parent.textBox_tCue_max.Text) };
            tMax_ReactionTime = float.Parse(parent.textBox_MaxReactionTime.Text);
            tMax_ReachTime = float.Parse(parent.textBox_MaxReachTime.Text);


            // Brush for background and border of WaitStart Interface
            brush_bkwaitstart = new SolidColorBrush();
            brush_bkwaitstart.Color = Colors.Gray;
            brush_bdwaitstart = new SolidColorBrush();
            brush_bdwaitstart.Color = Colors.Black;
            // Brush for background of the trial
            brush_bktrial = new SolidColorBrush();
            brush_bktrial.Color = Colors.Black;
            brush_ErrorInterface = new SolidColorBrush();
            brush_ErrorInterface.Color = Colors.Red;
            brush_CorrectInterface = new SolidColorBrush();
            brush_CorrectInterface.Color = Colors.Green;

            // get the file for saving 
            file_saved = parent.file_saved;
        }


        private void WriteHeaderInf()
        {
            // save
            using (StreamWriter file = File.AppendText(file_saved))
            {
                // Store all the optional positions
                foreach (int[] position in optPostions_List)
                {
                    file.WriteLine(String.Format("{0, -10}:{1}, {2}", "Optional Postion ", position[0], position[1]));
                }


                file.WriteLine("\n");

                file.WriteLine("Trial Information:");
                file.WriteLine(String.Format("{0, -10}  {1, -20} {2, -10} {3, -20}  " +
                    "{4, -20} {5, -20} {6, -20}  {7, -20} {8, -20} {9, -20}  {10, -20} {11, -20}",
                    "TrialNumber", "TrialStartTime",
                    "Go or Nogo", "Object Position",
                    "Ready Interface Onset Time", "Cue Interface Onset Time", "Go/Nogo Interface Onset Time",
                    "Startpad Onset Time", "Startpad Off Time", "Screen Touched Time",
                    "Trial Result", "Touched Coordinates"));



            }
        }

        private void Create_GoCircle()
        {/*
            Create the go circle: circleGo

            Arg:
                pos: the position ([left, top]) the go circle, vertical and horizontal aligned to center 


            */

            // Create an Ellipse  
            circleGo = new Ellipse();

            // Create a Brush    
            brush_goCircle = new SolidColorBrush();

            if(parent.cbo_goColor.Text == "Blue")
                brush_goCircle.Color = Colors.Blue;
            else if(parent.cbo_goColor.Text == "Red")
                brush_goCircle.Color = Colors.Red;
            else if (parent.cbo_goColor.Text == "Green")
                brush_goCircle.Color = Colors.Green;
            else if (parent.cbo_goColor.Text == "Yellow")
                brush_goCircle.Color = Colors.Yellow;


            circleGo.Fill = brush_goCircle;

            // set the size, position of circleGo
            circleGo.Height = objdiameter;
            circleGo.Width = objdiameter;
            circleGo.VerticalAlignment = VerticalAlignment.Center;
            circleGo.HorizontalAlignment = HorizontalAlignment.Center;

            circleGo.Name = name_circleGo;
            circleGo.Visibility = Visibility.Hidden;
            circleGo.IsEnabled = false;

            // add to myGrid
            myGrid.Children.Add(circleGo);
            myGrid.RegisterName(circleGo.Name, circleGo);
            myGrid.UpdateLayout();

            Grid parent_GoCircle = (Grid)circleGo.Parent;
            Grid parent_wholeGrid = (Grid)parent_GoCircle.Parent;
        }

        private void Add_GoCircle(int[] pos)
        {/*show the Go Circle at pos*/

            


            circleGo.Margin = new Thickness(pos[0], pos[1], 0, 0);
            circleGo.Fill = brush_goCircle;
            circleGo.Visibility = Visibility.Visible;
            circleGo.IsEnabled = true;
            //myGrid.UpdateLayout();


            // get the center point and the radius of circleGo
            Point leftTopPoint_circleGo = circleGo.TransformToAncestor(this).Transform(new Point(0, 0));
            circleGo_centerPoint = Point.Add(leftTopPoint_circleGo, new Vector(circleGo.Width / 2, circleGo.Height / 2));
            circleGo_radius = ((circleGo.Height + circleGo.Width) / 2) / 2;

            Ellipse originGo = new Ellipse { Width = 5, Height = 5};
            double left = circleGo_centerPoint.X - (1 / 2);
            double top = circleGo_centerPoint.Y - (1 / 2);
            originGo.Fill = brush_ErrorInterface;
            originGo.VerticalAlignment = VerticalAlignment.Top;
            originGo.HorizontalAlignment = HorizontalAlignment.Left;
            originGo.Margin = new Thickness(left, top, 0, 0);
            myGrid.Children.Add(originGo);
            myGrid.UpdateLayout();
        }
        private void Remove_GoCircle()
        {
            circleGo.Visibility = Visibility.Hidden;
            circleGo.IsEnabled = false;
            myGrid.UpdateLayout();
        }


        private void Create_NogoRect()
        {/*Create the red nogo rectangle: rectNogo*/

            // Create an Ellipse  
            rectNogo = new Rectangle();

            // Create a Brush for nogo Rectangle    
            brush_nogoRect = new SolidColorBrush();
            if (parent.cbo_nogoColor.Text == "Blue")
                brush_nogoRect.Color = Colors.Blue;
            else if (parent.cbo_nogoColor.Text == "Red")
                brush_nogoRect.Color = Colors.Red;
            else if (parent.cbo_nogoColor.Text == "Green")
                brush_nogoRect.Color = Colors.Green;
            else if (parent.cbo_nogoColor.Text == "Yellow")
                brush_nogoRect.Color = Colors.Yellow;

            rectNogo.Fill = brush_nogoRect;

            // set the size, position of circleGo
            int square_width = objdiameter;
            int square_height = objdiameter;
            rectNogo.Height = square_height;
            rectNogo.Width = square_width;
            rectNogo.VerticalAlignment = VerticalAlignment.Center;
            rectNogo.HorizontalAlignment = HorizontalAlignment.Center;

            // name
            rectNogo.Name = name_rectNogo;

            // hidden and not enabled at first
            rectNogo.Visibility = Visibility.Hidden;
            rectNogo.IsEnabled = false;

            // add to myGrid   
            myGrid.Children.Add(rectNogo);
            myGrid.RegisterName(rectNogo.Name, rectNogo);
            myGrid.UpdateLayout();
        }

        private void Add_NogoRect(int[] pos)
        {/*show the noGo Rect at pos*/

            rectNogo.Margin = new Thickness(pos[0], pos[1], 0, 0);
            rectNogo.Fill = brush_nogoRect;
            rectNogo.Visibility = Visibility.Visible;
            rectNogo.IsEnabled = true;
            myGrid.UpdateLayout();
        }

        private void Remove_NogoRect()
        {
            rectNogo.Visibility = Visibility.Hidden;
            rectNogo.IsEnabled = false;
            myGrid.UpdateLayout();
        }


        private void Create_TwoWhitePoints()
        {/* Create draw the two write points: point1, point2 */

            // Create a while Brush    
            SolidColorBrush whiteBrush = new SolidColorBrush();
            whiteBrush.Color = Colors.White;

            // the left white point
            point1 = new Ellipse();
            point1.Fill = whiteBrush;
            point1.Height = wpoints_radius;
            point1.Width = wpoints_radius;
            point1.HorizontalAlignment = HorizontalAlignment.Center;
            point1.VerticalAlignment = VerticalAlignment.Center;
            

            point1.Name = name_point1;

            point1.Visibility = Visibility.Hidden;
            point1.IsEnabled = false;
            myGrid.Children.Add(point1);
            myGrid.RegisterName(point1.Name, point1);


            // the top white point
            point2 = new Ellipse();
            point2.Fill = whiteBrush;
            point2.Height = wpoints_radius;
            point2.Width = wpoints_radius;
            point2.HorizontalAlignment = HorizontalAlignment.Center;
            point2.VerticalAlignment = VerticalAlignment.Center;
            

            point2.Name = name_point2;
            point2.Visibility = Visibility.Hidden;
            point2.IsEnabled = false;
            myGrid.Children.Add(point2);
            myGrid.RegisterName(point2.Name, point2);
            myGrid.UpdateLayout();



        }

        private void Add_TwoWhitePoints(int[] pos1, int[] pos2)
        {// show Two White Points at pos1 and pos2 separately rectangle to myGrid

            point1.Margin = new Thickness(pos1[0], pos1[1], 0, 0);
            point2.Margin = new Thickness(pos2[0], pos2[1], 0, 0);

            point1.Visibility = Visibility.Visible;
            point2.Visibility = Visibility.Visible;

            myGrid.UpdateLayout();
        }

        private void Remove_TwoWhitePoints()
        {// add nogo rectangle to myGrid

            point1.Visibility = Visibility.Hidden;
            point2.Visibility = Visibility.Hidden;

            myGrid.UpdateLayout();
        }

        private void Create_OneCrossing()
        {/*create the crossing cue*/

            // the line length of the crossing
            int len = objdiameter;
            int linethickness = 2;

            // Create a while Brush    
            SolidColorBrush whiteBrush = new SolidColorBrush();
            whiteBrush.Color = Colors.White;

            // Create the horizontal line
            horiLine = new Line();
            horiLine.X1 = 0;
            horiLine.Y1 = 0;
            horiLine.X2 = len;
            horiLine.Y2 = horiLine.Y1;        
            
            // horizontal line position
            horiLine.HorizontalAlignment = HorizontalAlignment.Center;
            horiLine.VerticalAlignment = VerticalAlignment.Center;
            
            // horizontal line color
            horiLine.Stroke = whiteBrush;
            // horizontal line stroke thickness
            horiLine.StrokeThickness = linethickness;
            // name
            horiLine.Name = name_hLine;
            horiLine.Visibility = Visibility.Hidden;
            horiLine.IsEnabled = false;
            myGrid.Children.Add(horiLine);
            myGrid.RegisterName(horiLine.Name, horiLine);


            // Create the vertical line
            vertLine = new Line();
            vertLine.X1 = 0;
            vertLine.Y1 = 0;
            vertLine.X2 = vertLine.X1;
            vertLine.Y2 = len;
            // vertical line position
            vertLine.HorizontalAlignment = HorizontalAlignment.Center;
            vertLine.VerticalAlignment = VerticalAlignment.Center;
            
            // vertical line color
            vertLine.Stroke = whiteBrush;
            // vertical line stroke thickness
            vertLine.StrokeThickness = linethickness;
            //name
            vertLine.Name = name_vLine;

            vertLine.Visibility = Visibility.Hidden;
            vertLine.IsEnabled = false;
            myGrid.Children.Add(vertLine);
            myGrid.RegisterName(vertLine.Name, vertLine);
            myGrid.UpdateLayout();
        }

        private void Add_OneCrossing(int[] pos)
        {//show One Crossing at the same position of object go/nogo

            horiLine.Margin = new Thickness(pos[0], pos[1], 0, 0);
            vertLine.Margin = new Thickness(pos[0], pos[1], 0, 0);

            horiLine.Visibility = Visibility.Visible;
            vertLine.Visibility = Visibility.Visible;
            myGrid.UpdateLayout();
        }

        private void Remove_OneCrossing()
        {
            horiLine.Visibility = Visibility.Hidden;
            vertLine.Visibility = Visibility.Hidden;
            myGrid.UpdateLayout();
        }

        private void Remove_All()
        {
            Remove_OneCrossing();
            Remove_TwoWhitePoints();
            Remove_GoCircle();
            Remove_NogoRect();

        }


        public float TransferTo(float value, float lower, float upper)
        {// transform value (0=<value<1) into a valueT (lower=<valueT<upper)

            float rndTime;
            rndTime = value * (upper - lower) + lower;

            return rndTime;
        }

        public async void Present_Task()
        {
            int triali = 0;
            float t_Cue, t_Ready;
            int[] pos_WPoint1, pos_WPoint2;
            int[] pos_Taget;
            int targetPosInd;
                        
            // start the globalWatch and readStartpad thread
            globalWatch.Restart();
            thread_readStartpad.Start();
            PresentTask = true;
            while (triali < targetType_List.Count && PresentTask)
            {
                // Extract Parameters for this trial
                targetType = targetType_List[triali];
                pos_WPoint1 = optPostions_List[otherPosInds_List[triali][0]];
                pos_WPoint2 = optPostions_List[otherPosInds_List[triali][1]];
                targetPosInd = targetPosInd_List[triali];
                pos_Taget = optPostions_List[targetPosInd];
                t_Cue = t_Cue_List[triali];
                t_Ready = t_Ready_List[triali];

                // Write trial related Information
                using (StreamWriter file = File.AppendText(file_saved))
                {
                    file.WriteLine("\n");

                    file.WriteLine(String.Format("{0, -20}: {1}", "TrialNum", (triali + 1).ToString()));
                    file.WriteLine(String.Format("{0, -20}: {1}", "TargetType", targetType.ToString()));
                    file.WriteLine(String.Format("{0, -20}: {1}", "TargetPositionIndex", targetPosInd.ToString()));
                    file.WriteLine(String.Format("{0, -20}: {1}", "Ready Interface Time", t_Ready.ToString()));
                    file.WriteLine(String.Format("{0, -20}: {1}", "Cue Interface Time", t_Cue.ToString()));
                }


                triali++;
                textbox_main.Text = "triali = " + (triali + 1).ToString();

                
                /*----- WaitStartTrial Interface ------*/
                pressedStartpad = PressedStartpad.No;
                await Interface_WaitStartTrial();
                startpadOn_StartTrial_TimePoint = startpadOn_TimePoint;
                using (StreamWriter file = File.AppendText(file_saved))
                {
                    file.WriteLine(String.Format("{0, -20}: {1}", "Startpad Touched On Time", startpadOn_TimePoint.ToString()));
                }

                // Cue Interface
                try {
                    await Interface_Ready(t_Ready);
                    await Interface_Cue(t_Cue, pos_Taget, pos_WPoint1, pos_WPoint2);
                    await Interface_Go(pos_Taget);
                }
                catch(TaskCanceledException)
                {
                    Remove_All();
                    textbox_main.Text = "Interruptted by Not Touched Enough for Interface";
                    continue;
                }
            Remove_All();
            }

            thread_readStartpad.Abort();
        }

        private void Thread_ReadStartpad()
        {/* Thread for reading startpad*/
            while (serialPort_IO8.IsOpen)
            {
                serialPort_IO8.WriteLine("Z");

                // extract and parse the start pad voltage 
                string str_Read = serialPort_IO8.ReadExisting();
                string[] str_vol = str_Read.Split(new Char[] { 'V' });

                if (!String.IsNullOrEmpty(str_vol[0]))
                {
                    float voltage = float.Parse(str_vol[0]);

                    if (voltage < 1 && pressedStartpad == PressedStartpad.No)
                    {/* time point from notouched state to touched state */

                        // the time point for startpad on
                        startpadOn_TimePoint = globalWatch.ElapsedMilliseconds;

                        pressedStartpad = PressedStartpad.Yes;
                    }
                    else if (voltage > 1 && pressedStartpad == PressedStartpad.Yes)
                    {/* time point from touched state to notouched state */

                        startpadOn_TimePoint = globalWatch.ElapsedMilliseconds;
                        pressedStartpad = PressedStartpad.No;
                    }
                }

                Thread.Sleep(30);
            }
        }


        private Task Interface_WaitStartTrial()
        {
            /* task for WaitStart interface
             * 
             * Wait for Startpad touch to trigger a new Trial
             */

            Remove_All();
            myGrid.Background = brush_bkwaitstart;
            myGridBorder.BorderBrush = brush_bdwaitstart;

            Task task_WaitStart = Task.Run(() =>
            {
                while (pressedStartpad == PressedStartpad.No) ;
            });

            return task_WaitStart;
        }

        private Task Wait_EnoughTouch(float t_EnoughTouch)
        {
            /* 
             * Wait for Enough Touch Time
             * 
             * Input: 
             *    t_EnoughTouch: the required Touch time (s)  
             */

            Task task = null;

            // start a task and return it
            return Task.Run(() =>
            {
                Stopwatch touchedWatch = new Stopwatch();
                touchedWatch.Restart();
                while (pressedStartpad == PressedStartpad.Yes && startpadHoldstate != StartPadHoldState.HoldEnough)
                {
                    if (touchedWatch.ElapsedMilliseconds >= t_EnoughTouch * 1000)
                    {/* touched with enough time */
                        startpadHoldstate = StartPadHoldState.HoldEnough;
                    }
                }

                if (startpadHoldstate != StartPadHoldState.HoldEnough)
                {
                    throw new TaskCanceledException(task);
                }

            });
        }

        private async Task Interface_Ready(float t_Ready)
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
                myGrid.Background = brush_bktrial;

                textbox_thread.Text = "Ready Interface Running......";
                interfaceState = InterfaceState.Ready;
               
                // Wait Startpad Hold Enough Time
                startpadHoldstate = StartPadHoldState.HoldTooShort;
                await Wait_EnoughTouch(t_Ready);
                textbox_thread.Text = "Ready Interface Finshed with Enough Startpad Hold Time";

            }
            catch (TaskCanceledException)
            {
                textbox_thread.Text = "Ready Interface: not Touched Enough";

                Task task = null;
                throw new TaskCanceledException(task);
            }
        }

 
        public async Task Interface_Cue(float t_Cue, int[] onecrossingPos, int[] wpointpos1, int[] wpointpos2)
        {/* task for Cue Interface 
            Show the Cue Interface while Listen to the state of the startpad. 
            
            Args:
                t_Cue: Cue interface showes duration(s)
                onecrossingPos: the center position of the one crossing
                wpoint1pos, wpoint2pos: the positions of the two white points

            * Output:
            *   startPadHoldstate_Cue = 
            *       StartPadHoldState.HoldEnough (if startpad is touched lasting t_Cue)
            *       StartPadHoldState.HoldTooShort (if startpad is released before t_Cue) 
            */

            try
            {
                //myGrid.Children.Clear();
                Remove_All();

                // add one crossing on the right middle
                Add_OneCrossing(onecrossingPos);
                // two white points on left middle and top middle
                //Add_TwoWhitePoints(wpointpos1, wpointpos2);

                textbox_thread.Text = "Targetcue running......";
                interfaceState = InterfaceState.TargetCue;
                // wait target cue for several seconds
                startpadHoldstate = StartPadHoldState.HoldTooShort;
                await Wait_EnoughTouch(t_Cue);
                textbox_thread.Text = "Targetcue run completely";

            }
            catch (TaskCanceledException)
            {
                textbox_thread.Text = "Cue Interface: not Touched Enough";

                Task task = null;
                throw new TaskCanceledException(task);
            }
            
        }


        private Task Wait_Reaction()
        {/* Wait for Reaction within tMax_ReactionTime */

            // start a task and return it
            return Task.Run(() =>
            {
                Stopwatch waitWatch = new Stopwatch();
                waitWatch.Start();
                while (pressedStartpad == PressedStartpad.Yes)
                {
                    if (waitWatch.ElapsedMilliseconds >= tMax_ReactionTime * 1000)
                    {/* No release Startpad within tMax_ReactionTime */
                        throw new TaskCanceledException("No Reaction within the Max Reaction Time");
                    }
                }
            });
        }

        private Task Wait_Reach()
        {/* Wait for Reach within tMax_ReachTime*/

            return Task.Run(() =>
            {
                Stopwatch waitWatch = new Stopwatch();
                waitWatch.Start();
                while (screenTouchstate == ScreenTouchState.Idle)
                {
                    if (waitWatch.ElapsedMilliseconds >= tMax_ReachTime * 1000)
                    {/*No Screen Touched within tMax_ReachTime*/
                        throw new TaskCanceledException("No Reach within the Max Reach Time");
                    }
                }
                waitWatch.Restart();
                while (waitWatch.ElapsedMilliseconds < tMax_1Touch) ;
                calc_GoTargetTouchState();
            });
        }

        private void calc_GoTargetTouchState()
        {/* Calculate GoTargetTouchState  
            1. based on the Touch Down Positions in  List downPoints_Pos and circleGo_centerPoint
            2. Assign the calculated target touch state to the GoTargetTouchState variable gotargetTouchstate
            */

            double distance;
           
            gotargetTouchstate = GoTargetTouchState.goMissed;
            while (downPoints_Pos.Count > 0)
            {
                Point touchp = new Point(downPoints_Pos[0][0], downPoints_Pos[0][1]);

                // distance between the touchpoint and the center of the circleGo
                distance = Point.Subtract(circleGo_centerPoint, touchp).Length;


                if (distance <= circleGo_radius)
                {// Hit 
                    gotargetTouchstate = GoTargetTouchState.goHit;
                    break;
                }
                else if (distance <= circleGo_radius + disThreshold_close && gotargetTouchstate != GoTargetTouchState.goHit)
                {
                    gotargetTouchstate = GoTargetTouchState.goClose;
                }

                downPoints_Pos.RemoveAt(0);
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
                // Remove the Crossing and Add the Go Circle
                Remove_OneCrossing();
                Add_GoCircle(pos_Target);
                textBox_State.Text = "X = " + circleGo_centerPoint.X.ToString() + ", Y = " + circleGo_centerPoint.Y.ToString();

                // Wait for Reaction within tMax_ReactionTime
                pressedStartpad = PressedStartpad.Yes;
                await Wait_Reaction();

                // Wait for Touch within tMax_ReachTime and Calcuate the gotargetTouchstate
                screenTouchstate = ScreenTouchState.Idle;
                await Wait_Reach();

                /*---- Go Target Touch States ----*/
                if (gotargetTouchstate == GoTargetTouchState.goHit)
                {/*Hit */
                    Interface_GoCorrect_Hit();
                    textbox_thread2.Text = "Hit";
                }
                else if(gotargetTouchstate == GoTargetTouchState.goClose)
                {/* touch close to the target*/
                    Interface_GoCorrect_Close();
                    textbox_thread2.Text = "Close";
                }
                else if(gotargetTouchstate == GoTargetTouchState.goMissed)
                {/* touch missed*/
                    Interface_GoERROR_Miss();
                    textbox_thread2.Text = "Miss";
                }
                await Task.Delay(1000);
            }
            catch(TaskCanceledException)
            {
                Interface_GoERROR_LongReactionReach();
                await Task.Delay(1000);
                throw new TaskCanceledException("Not Reaction Within the Max Reaction Time.");
            }
            
        }

        private void Interface_ERROR()
        {
            myGridBorder.BorderBrush = brush_ErrorInterface;
            circleGo.Fill = brush_ErrorInterface;
            myGrid.UpdateLayout();
            
        }
        private void Interface_GoERROR_LongReactionReach()
        {
            Interface_ERROR();
        }

        private void Interface_GoERROR_Miss()
        {
            Interface_ERROR();
        }

        private void Interface_GoCorrect_Hit()
        {
            myGridBorder.BorderBrush = brush_CorrectInterface;
            circleGo.Fill = brush_CorrectInterface;
            myGrid.UpdateLayout();
        }

        private void Interface_GoCorrect_Close()
        {
            myGridBorder.BorderBrush = brush_NearInterface;
            myGrid.UpdateLayout();
        }

        private static Task Wait_Interface(CancellationToken cancellationToken)
        {
            /* 
             * wait until cancellation
             * 
             * Input: 
             *       cancellationToken
             */

            Task task = null;

            // start a task and return it
            return Task.Run(() =>
            {

                while(true)
                {
                    // Check if a cancellation is required
                    if (cancellationToken.IsCancellationRequested)
                        throw new TaskCanceledException(task);

                    //Do something
                    Thread.Sleep(10);
                }
            });
        }

        private static Task Wait_Interface(float t_wait, CancellationToken cancellationToken)
        {
            /* 
             * wait for several seconds for one kind of interface
             * 
             * Input: 
             *    t_wait: the waited time (s)  
             */

            Task task = null;

            // start a task and return it
            return Task.Run(() =>
            {
                Stopwatch waitWatch = new Stopwatch();
                waitWatch.Restart();
                while (waitWatch.ElapsedMilliseconds < t_wait * 1000)
                {
                    // Check if a cancellation is required
                    if (cancellationToken.IsCancellationRequested)
                        throw new TaskCanceledException(task);
                }
            });
        }

        private void UpdateBackground(string message)
        {
            myGrid.Background = brush_bktrial;
            textbox_thread2.Text = message;
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (serialPort_IO8.IsOpen)
                serialPort_IO8.Close();

            PresentTask = false;
        }

        public void Interface_Reward()
        {
            myGridBorder.BorderBrush = Brushes.Yellow;

            if (targetType == TargetType.Go)
            {
                circleGo.Fill = Brushes.Yellow;
            }

            else
            {
                rectNogo.Fill = Brushes.Yellow;
            }
            
            myGrid.UpdateLayout();
        }


        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            // Create an Ellipse  
            Ellipse circleAdd = new Ellipse();

            // Create a Brush    
            SolidColorBrush brush = new SolidColorBrush();
            brush.Color = Colors.Yellow;


            circleAdd.Fill = brush;

            // set the size, position of circleGo
            circleAdd.Height = 100;
            circleAdd.Width = 100;
            circleAdd.VerticalAlignment = VerticalAlignment.Center;
            circleAdd.HorizontalAlignment = HorizontalAlignment.Center;

            circleAdd.Name = "CircleAdded";
            circleAdd.IsEnabled = false;

            // add to myGrid
            myGrid.Children.Add(circleAdd);
            myGrid.RegisterName(circleAdd.Name, circleAdd);
            myGrid.UpdateLayout();
        }


        void Touch_FrameReported(object sender, TouchFrameEventArgs e)
        {/* Add the Id of New Touch Points into Hashset touchPoints_Id 
            and the Corresponding Touch Down Positions into List downPoints_Pos (no replicates)*/
            screenTouchstate = ScreenTouchState.Touched;
            TouchPointCollection touchPoints = e.GetTouchPoints(myGrid);
            bool addedNew;
            long time = touchPointsWatch.ElapsedMilliseconds;

            for (int i = 0; i < touchPoints.Count; i++)
            {
                TouchPoint _touchPoint = touchPoints[i];
                if (_touchPoint.Action == TouchAction.Down)
                { /* TouchAction.Down */

                    textbox_thread.Text = _touchPoint.Position.X.ToString() + ", " + _touchPoint.Position.Y.ToString();

                    if (touchPoints_Id.Count == 0)
                    {// the first touch point for one touch
                        touchPointsWatch.Restart();
                    }
                    lock (touchPoints_Id)
                    {
                        // Add the touchPoint to the Hashset touchPoints_Id, Return true if added, otherwise false.
                        addedNew = touchPoints_Id.Add(_touchPoint.TouchDevice.Id);
                    }
                    if (addedNew)
                    {/* deal with the New Added TouchPoint*/

                        double[] pos = new double[2] { _touchPoint.Position.X, _touchPoint.Position.Y };
                        // store the pos of the point with down action
                        lock (downPoints_Pos)
                        {
                            downPoints_Pos.Add(pos);
                        }

                        downPoints_time.Add(time);
                    }
                }
                else if (_touchPoint.Action == TouchAction.Up)
                {
                    // remove the id of the point with up action
                    lock (touchPoints_Id)
                    {
                        touchPoints_Id.Remove(_touchPoint.TouchDevice.Id);
                    }
                }
            }

        }
    }
}

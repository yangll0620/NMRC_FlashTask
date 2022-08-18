using System.Windows;

namespace FlashTask_wpf
{
    /// <summary>
    /// Interaction logic for SetupTimeWin.xaml
    /// </summary>
    public partial class SetupTimeWin : Window
    {
        public MainWindow parent;

        private bool BtnStartState, BtnStopState;

        public SetupTimeWin(MainWindow mainWindow)
        {
            InitializeComponent();

            parent = mainWindow;
            DisableBtnStartStop();

            LoadInitTimeData();
        }

        private void DisableBtnStartStop()
        {
            BtnStartState = parent.btn_start.IsEnabled;
            BtnStopState = parent.btn_stop.IsEnabled;
            parent.btn_start.IsEnabled = false;
            parent.btn_stop.IsEnabled = false;
        }

        private void ResumeBtnStartStop()
        {
             parent.btn_start.IsEnabled = BtnStartState;
             parent.btn_stop.IsEnabled = BtnStopState;
        }

        private void LoadInitTimeData()
        {// Load Initial Time Data from MainWindow

            textBox_tReady_min.Text = parent.tRange_ReadyTimeS[0].ToString();
            textBox_tReady_max.Text = parent.tRange_ReadyTimeS[1].ToString();


            textBox_MaxReachTime.Text = parent.tMax_ReachTimeS.ToString();
            textBox_MaxReactionTime.Text = parent.tMax_ReactionTimeS.ToString();

            textBox_tVisFeedback.Text = parent.t_VisfeedbackShowS.ToString();

            textBox_tInterTrial.Text = parent.t_InterTrialS.ToString();
        }

        private void SaveTimeData()
        {/* ---- Save all the Set Time Information back to MainWindow Variables ----- */
            parent.tRange_ReadyTimeS[0] = float.Parse(textBox_tReady_min.Text);
            parent.tRange_ReadyTimeS[1] = float.Parse(textBox_tReady_max.Text);


            parent.tMax_ReachTimeS = float.Parse(textBox_MaxReachTime.Text);
            parent.tMax_ReactionTimeS = float.Parse(textBox_MaxReactionTime.Text);

            parent.t_VisfeedbackShowS = float.Parse(textBox_tVisFeedback.Text);
            parent.t_InterTrialS = float.Parse(textBox_tInterTrial.Text);
        }

        private void Btn_Cancle_Click(object sender, RoutedEventArgs e)
        {
            ResumeBtnStartStop();
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ResumeBtnStartStop();
        }

        private void Btn_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveTimeData();
            ResumeBtnStartStop();
            this.Close();
        }
    }
}

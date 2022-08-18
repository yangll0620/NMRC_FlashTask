using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace FlashTask_wpf
{
    /// <summary>
    /// Interaction logic for SetupColorsWin.xaml
    /// </summary>
    public partial class SetupColorsWin : Window
    {
        MainWindow parent;
        private bool BtnStartState, BtnStopState;

        public SetupColorsWin(MainWindow parentWindow)
        {
            InitializeComponent();

            parent = parentWindow;
            DisableBtnStartStop();

            BindingComboData();  
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

        private void BindingComboData()
        {
            //Data binding the Color ComboBoxes
            cbo_goFillColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_goOutlineColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_BKWaitTrialColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_BKReadyColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_BKTargetShownColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_CorrFillColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_CorrOutlineColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_ErrorFillColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_ErrorOutlineColor.ItemsSource = typeof(Colors).GetProperties();
            cbo_ErrorCrossingColor.ItemsSource = typeof(Colors).GetProperties();


            // Set Default Selected Item
            cbo_goFillColor.SelectedItem = typeof(Colors).GetProperty(parent.targetFillColorStr);
            cbo_goOutlineColor.SelectedItem = typeof(Colors).GetProperty(parent.targetFillColorStr);
            cbo_BKWaitTrialColor.SelectedItem = typeof(Colors).GetProperty(parent.BKWaitTrialColorStr);
            cbo_BKReadyColor.SelectedItem = typeof(Colors).GetProperty(parent.BKReadyColorStr);
            cbo_BKTargetShownColor.SelectedItem = typeof(Colors).GetProperty(parent.BKTargetShownColorStr);
            cbo_CorrFillColor.SelectedItem = typeof(Colors).GetProperty(parent.CorrFillColorStr);
            cbo_CorrOutlineColor.SelectedItem = typeof(Colors).GetProperty(parent.CorrOutlineColorStr);
            cbo_ErrorFillColor.SelectedItem = typeof(Colors).GetProperty(parent.ErrorFillColorStr);
            cbo_ErrorOutlineColor.SelectedItem = typeof(Colors).GetProperty(parent.ErrorOutlineColorStr);
            cbo_ErrorCrossingColor.SelectedItem = typeof(Colors).GetProperty(parent.ErrorCrossingColorStr);
        }

        private void SaveColorsData()
        { /* ---- Save all the Select Colors Information back to MainWindow Color Strings ----- */

            parent.targetFillColorStr = (cbo_goFillColor.SelectedItem as PropertyInfo).Name;
            parent.targetOutlineColorStr = (cbo_goOutlineColor.SelectedItem as PropertyInfo).Name;

            parent.BKWaitTrialColorStr = (cbo_BKWaitTrialColor.SelectedItem as PropertyInfo).Name;
            parent.BKReadyColorStr = (cbo_BKReadyColor.SelectedItem as PropertyInfo).Name;
            parent.BKTargetShownColorStr = (cbo_BKTargetShownColor.SelectedItem as PropertyInfo).Name;

            parent.CorrFillColorStr = (cbo_CorrFillColor.SelectedItem as PropertyInfo).Name;
            parent.CorrOutlineColorStr = (cbo_CorrOutlineColor.SelectedItem as PropertyInfo).Name;
            parent.ErrorFillColorStr = (cbo_ErrorFillColor.SelectedItem as PropertyInfo).Name;
            parent.ErrorOutlineColorStr = (cbo_ErrorOutlineColor.SelectedItem as PropertyInfo).Name;
            parent.ErrorCrossingColorStr = (cbo_ErrorCrossingColor.SelectedItem as PropertyInfo).Name;
        }

        private void Btn_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveColorsData();
            ResumeBtnStartStop();
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ResumeBtnStartStop();
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            ResumeBtnStartStop();
            this.Close();
        }
    }
}

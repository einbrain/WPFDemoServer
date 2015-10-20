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
using System.Windows.Shapes;
using Microsoft.Win32;

namespace WPFDemoServer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        private MainWindow mainWin;

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.FileName = "result.csv";
            dialog.Filter = "CSVファイル(*.csv)|*.csv|全てのファイル(*.*)|*.*";
            dialog.OverwritePrompt = true;
            dialog.CheckPathExists = true;
            bool? res = dialog.ShowDialog();
            if (res.HasValue && res.Value)
            {
                try
                {
                    if (mainWin.fileWriter != null) mainWin.fileWriter.Flush();
                    System.IO.Stream fstream = dialog.OpenFile();
                    System.IO.Stream fstreamAppend = new System.IO.FileStream(dialog.FileName+"-log.csv", System.IO.FileMode.OpenOrCreate);
                    if (fstream != null && fstreamAppend != null)
                    {
                        textBox1.Text = dialog.FileName;
                        mainWin.fileWriter = new System.IO.StreamWriter(fstream);
                        mainWin.logWriter = new System.IO.StreamWriter(fstreamAppend);
                    }
                }
                catch (Exception exp) 
                {
                    MessageBox.Show(exp.ToString());
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mainWin = (MainWindow)this.Owner;
            repeatBox.Text = Convert.ToString(mainWin.task_count);
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            mainWin.task_count = Convert.ToInt16(repeatBox.Text);

            mainWin.startTest();

            slider1.Value = mainWin.mouseScale;
            label2.Content = slider1.Value;

            slider2.Value = mainWin.frameScale;
            label3.Content = slider2.Value;

            slider3.Value = mainWin.shrinkIndex;
            label6.Content = slider3.Value;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                mainWin.fileWriter.BaseStream.Flush();
            }
            catch (Exception) 
            {
                //DO NOTHING
            }

            try
            {
                mainWin.Close();
            }
            catch (Exception)
            {
                //DO NOTHING
            }
        }

        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mainWin != null)
            {
                mainWin.mouseScale = slider1.Value;
                label2.Content = slider1.Value.ToString();
            }
        }

        private void slider2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mainWin != null)
            {
                mainWin.frameScale = slider2.Value;
                label3.Content = slider2.Value.ToString();
            }
        }

        private void slider3_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mainWin != null)
            {
                mainWin.shrinkIndex = slider3.Value;
                label6.Content = slider3.Value.ToString();
            }
        }

        private void checkBox1_Checked(object sender, RoutedEventArgs e)
        {
            if (mainWin != null)
            {
                mainWin.bGlobalSettingChangableFrame = true;
            }
        }

        private void checkBox1_Unchecked(object sender, RoutedEventArgs e)
        {
            if (mainWin != null)
            {
                mainWin.bGlobalSettingChangableFrame = false;
            }
        }

        private void checkBox2_Checked(object sender, RoutedEventArgs e)
        {
            if (mainWin != null)
            {
                mainWin.bGlobalSettingMouseControl = true;
            }
        }

        private void checkBox2_Unchecked(object sender, RoutedEventArgs e)
        {
            if (mainWin != null)
            {
                mainWin.bGlobalSettingMouseControl = false;
            }
        }

        private void checkBox3_Checked(object sender, RoutedEventArgs e)
        {
            if (mainWin != null)
            {
                mainWin.bGlobalSettingUseLine = true;
            }
        }

        private void checkBox3_Unchecked(object sender, RoutedEventArgs e)
        {
            if (mainWin != null)
            {
                mainWin.bGlobalSettingUseLine = false;
            }
        }

        private void checkBox4_Checked(object sender, RoutedEventArgs e)
        {
            if (mainWin != null)
            {
                mainWin.bGlobalSettingFramed = true;
            }
        }

        private void checkBox4_Unchecked(object sender, RoutedEventArgs e)
        {
            if (mainWin != null)
            {
                mainWin.bGlobalSettingFramed = false;
            }
        }
    }
}

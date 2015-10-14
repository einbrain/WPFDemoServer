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
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.IO;
using System.Timers;
using System.Collections.Concurrent;

namespace WPFDemoServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //--constants---------------------------
        private const Int32 CAM_WIDTH = 640;
        private const Int32 CAM_HEIGHT = 480;
        private const Int32 KINECT_WIDTH = 640;
        private const Int32 KINECT_HEIGHT = 480;
        private const Int16 cmdQueueLength = 10;
        private const Int16 frameDistQueueLength = 10;
        private const Int16 cFrameRemainTimeMS = 400;
        private const Int16 cGazePosQueueLength = 10;
        private const double cGazeCheckIntlMilliSec = 50;
        private const double cGazeActivityThresh = 225;
        private const int cGazeDwellSampleCount = 20;
        private const double cGazeDotDrawingIntlMilliSec = 200;

        //--system settings---------------------
        public Boolean bGlobalSettingFramed;
        public Boolean bGlobalSettingChangableFrame;
        public Boolean bGlobalSettingMouseControl;
        public Boolean bGlobalSettingUseLine;

        //--variables---------------------------
        public Double mouseScale;
        public Double frameScale;
        public Double shrinkIndex;
        private Double cursorHeight;
        private Double cursorWidth;
        private Double windowHeight;
        private Double windowWidth;

        //--processing queues-------------------
        private ConcurrentQueue<Point> boxPosQueue;
        private ConcurrentQueue<Point> boxPosOffsetQueue;
        private ConcurrentQueue<Point> cursorPosQueue;
        private ConcurrentQueue<Boolean> lmbQueue;
        private ConcurrentQueue<String> cmdQueue;
        private ConcurrentQueue<Double> frameDistQueue;
        private ConcurrentQueue<Point> qGazePosQueue;
        private ConcurrentQueue<Point> qGazeDwellQueue;
        private ConcurrentQueue<bool> qGazeDwellFlagQueue;

        //--system status-----------------------
        private System.Windows.Point realMouseRelativePos;
        private Boolean bExitPending;
        private Boolean bTestStarted;
        private Boolean bShowFrame;
        private Boolean bMovingMouse;
        private Boolean bFrameFirstMove;
        private Boolean bMouseFirstMove;
        private Boolean bFirstTarget;
        private Boolean bFirstEnter;
        private Boolean bCursorOnFrame;
        private System.Windows.Point frameLastPos;
        private System.Windows.Point frameCurrentPos;
        private System.Windows.Point mouseLastPos;
        private System.Windows.Point mouseCurrentPos;
        private System.Windows.Point targetLastPos;
        private Double frameCurrentOffsetLength;
        private Double mouseCurrentOffsetLength;
        private Int16 miss_count;
        private Int16 move_count;
        public Int16 task_count;
        private Int64 nLastTimeStamp;
        private Double dFramePerSec;

        private Boolean bGazeDwelling;
        private Double dGazeActivity;

        //--misc components---------------------
        //---time---------------
        private System.Diagnostics.Stopwatch stopwatch;
        private System.Diagnostics.Stopwatch dwellStopwatch;
        private System.Diagnostics.Stopwatch moveIntervalStopwatch;
        private System.Diagnostics.Stopwatch fpsStopwatch;
        private System.Diagnostics.Stopwatch frameAppearStopwatch;
        private DispatcherTimer timer;
        private Int16 nTickCount;
        private DispatcherTimer gazeTimer;
        private Int16 nGazeTickCount;
        private System.Timers.Timer movementTimer;
        //---file---------------
        public StreamWriter fileWriter;
        public StreamWriter logWriter;
        //---graphic------------
        private Ellipse[] highLight;
        private Ellipse[] aDwellDots;
        //---sub window---------
        private Window1 dlgWindow;


        /// <summary>
        /// window load event
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            bGlobalSettingFramed = true;
            bGlobalSettingChangableFrame = true;
            bGlobalSettingMouseControl = false;
            bGlobalSettingUseLine = false;

            mouseScale = 1.0;
            frameScale = 2.0;
            shrinkIndex = 1.6;

            if (this.ActualWidth > this.ActualHeight)
            {
                rectangle1.Height = this.ActualHeight / frameScale;
                rectangle1.Width = rectangle1.Height * 1.618;
            }
            else
            {
                rectangle1.Width = this.ActualWidth / frameScale;
                rectangle1.Height = rectangle1.Width / 1.618;
            }
            cursorHeight = ellipse1.Height;
            cursorWidth = ellipse1.Width;
            windowHeight = this.Height;
            windowWidth = this.Width;

            bExitPending = false;
            bTestStarted = false;
            bFrameFirstMove = true;
            bMouseFirstMove = true;
            bShowFrame = false;
            bMovingMouse = false;
            bCursorOnFrame = false;
            bGazeDwelling = true;
            dGazeActivity = 0.0f;

            bFirstEnter = true;
            bFirstTarget = true;
            miss_count = 0;
            move_count = 1;
            task_count = 12;

            nLastTimeStamp = 0;
            dFramePerSec = 0;

            boxPosQueue = new ConcurrentQueue<Point>();
            boxPosOffsetQueue = new ConcurrentQueue<Point>();
            cursorPosQueue = new ConcurrentQueue<Point>();
            lmbQueue = new ConcurrentQueue<Boolean>();
            cmdQueue = new ConcurrentQueue<String>();
            frameDistQueue = new ConcurrentQueue<Double>();
            qGazePosQueue = new ConcurrentQueue<Point>();
            qGazeDwellQueue = new ConcurrentQueue<Point>();
            qGazeDwellFlagQueue = new ConcurrentQueue<bool>();

            stopwatch = new System.Diagnostics.Stopwatch();
            dwellStopwatch = new System.Diagnostics.Stopwatch();
            moveIntervalStopwatch = new System.Diagnostics.Stopwatch();
            fpsStopwatch = new System.Diagnostics.Stopwatch();
            frameAppearStopwatch = new System.Diagnostics.Stopwatch();

            timer = new DispatcherTimer(DispatcherPriority.Background);
            timer.Interval = TimeSpan.FromTicks(10);
            timer.Tick += new EventHandler(timer_Tick);
            nTickCount = 0;

            gazeTimer = new DispatcherTimer(DispatcherPriority.Background);
            gazeTimer.Interval = TimeSpan.FromMilliseconds(cGazeCheckIntlMilliSec);
            gazeTimer.Tick += new EventHandler(gazeTimer_Tick);
            nGazeTickCount = 0;

            movementTimer = new System.Timers.Timer();
            movementTimer.Interval = 100;
            movementTimer.Elapsed += new ElapsedEventHandler(logger_Tick);


            highLight = new Ellipse[10];
            SolidColorBrush ellipseBrush = new SolidColorBrush();
            ellipseBrush.Color = Colors.White;
            for (int i = 0; i < highLight.Length; ++i)
            {
                highLight[i] = new Ellipse();
                highLight[i].Opacity = 0;
                highLight[i].Stroke = ellipseBrush;
                highLight[i].StrokeThickness = 5;
                highLight[i].Width = ellipseTarget.Width + 10.0 * i;
                highLight[i].Height = ellipseTarget.Height + 10.0 * i;

                canvas1.Children.Add(highLight[i]);
            }

            aDwellDots = new Ellipse[cGazeDwellSampleCount];
            SolidColorBrush dotsBrush = new SolidColorBrush();
            dotsBrush.Color = Colors.Yellow;
            for (int i = 0; i < aDwellDots.Length; ++i)
            {
                aDwellDots[i] = new Ellipse();
                aDwellDots[i].Stroke = dotsBrush;
                aDwellDots[i].StrokeThickness = 1;
                aDwellDots[i].Width = 8;
                aDwellDots[i].Height = 8;

                canvas1.Children.Add(aDwellDots[i]);
            }


            dlgWindow = new Window1();
            dlgWindow.Owner = this;

            Thread udpThread = new Thread(new ThreadStart(udpLoop));
            udpThread.Start();
            timer.Start();
            gazeTimer.Start();
            dlgWindow.Show();
            fpsStopwatch.Start();

        }//Window_Loaded

        private void gazeTimer_Tick(object sender, EventArgs e)
        {
            //Gaze point queue
            Point currentGaze = new Point(
                ellipseLaserPoint.Margin.Left + ellipseLaserPoint.ActualWidth / 2,
                ellipseLaserPoint.Margin.Top + ellipseLaserPoint.ActualHeight / 2
            );
            qGazePosQueue.Enqueue(currentGaze);

            if (qGazePosQueue.Count > cGazePosQueueLength)
            {
                Point trashPoint = new Point();
                while (qGazePosQueue.Count > cGazePosQueueLength)
                {
                    qGazePosQueue.TryDequeue(out trashPoint);
                }

                int id;
                double xAverage = 0.0f, yAverage = 0.0f, avgSqrtDelta = 0.0f;
                for (id = 0; id < cGazePosQueueLength; ++id)
                {
                    xAverage += qGazePosQueue.ElementAt(id).X;
                    yAverage += qGazePosQueue.ElementAt(id).Y;
                }
                xAverage /= cGazePosQueueLength;
                yAverage /= cGazePosQueueLength;

                for (id = 0; id < cGazePosQueueLength; ++id)
                {
                    avgSqrtDelta += System.Math.Pow(qGazePosQueue.ElementAt(id).X - xAverage, 2) + System.Math.Pow(qGazePosQueue.ElementAt(id).Y - yAverage, 2);
                }
                avgSqrtDelta /= cGazePosQueueLength;
                dGazeActivity = avgSqrtDelta;
                label5.Content = "avgSqrtDelta(gaze activity): " + avgSqrtDelta.ToString("00.00");

                if(dGazeActivity > cGazeActivityThresh)
                {
                    if(bGazeDwelling)
                    {
                        bGazeDwelling = false;
                        ellipseLaserPoint.Fill = new SolidColorBrush(Color.FromArgb(0xff, 0xfa, 0xf3, 0x1e));
                        rectangle1.Fill = new SolidColorBrush(Color.FromArgb(0x48, 0x8a, 0xef, 0xb8));
                    }
                }
                else
                {
                    if(!bGazeDwelling)
                    {
                        bGazeDwelling = true;
                        ellipseLaserPoint.Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x58, 0x1b, 0xa2));
                        rectangle1.Fill = new SolidColorBrush(Color.FromArgb(0x48, 0x1e, 0xad, 0xfa));
                    }
                }
            }//if (qGazePosQueue.Count > cGazePosQueueLength)

            ++nGazeTickCount;

            if(nGazeTickCount % (cGazeDotDrawingIntlMilliSec / cGazeCheckIntlMilliSec) == 0 )
            {
                nGazeTickCount = 0;

                if(bGazeDwelling)
                {
                    qGazeDwellQueue.Enqueue(currentGaze);
                    Point trashpoint = new Point();
                    while (qGazeDwellQueue.Count > cGazeDwellSampleCount)
                    {
                        qGazeDwellQueue.TryDequeue(out trashpoint);
                    }
                }
                else
                {
                    if(qGazeDwellQueue.Count != 0)
                    {
                        qGazeDwellQueue = new ConcurrentQueue<Point>();
                    }
                }
            }
        }


        /// <summary>
        /// closing window event
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bExitPending = true;
            dlgWindow.Close();
        }

        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            windowHeight = this.ActualHeight;
            windowWidth = this.ActualWidth;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                if(fileWriter != null)
                {
                    fileWriter.Flush();
                    fileWriter.Close();
                }

                if (logWriter != null)
                {
                    logWriter.Flush();
                    logWriter.Close();
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message.ToString());
                return;
            }
        }

        private void canvas1_MouseMove(object sender, MouseEventArgs e)
        {
            realMouseRelativePos = e.GetPosition(canvas1);
        }

        //-------Functions-------------------------------------------------------------------------------
        private void timer_Tick(object sender, EventArgs e)
        {
            moveFrame();
            moveMouse();

            mouseCurrentPos = new System.Windows.Point(ellipse1.Margin.Left + ellipse1.Width / 2, ellipse1.Margin.Top + ellipse1.Height / 2);
            frameCurrentPos = new System.Windows.Point(rectangle1.Margin.Left + rectangle1.Width / 2, rectangle1.Margin.Top + rectangle1.Height / 2);

            //
            // Move the cursor(shape)
            if (normalizePos(ref mouseCurrentPos)) //Normalize cursor position so that it is inside the bound of main window
            {
                ellipse1.Margin = new Thickness(mouseCurrentPos.X - ellipse1.Width / 2, mouseCurrentPos.Y - ellipse1.Height / 2, 0, 0);
            }

            //
            // Put the cursor inside the Frame
            if (bGlobalSettingFramed) 
            {
                if(ellipseLaserPoint.Visibility != Visibility.Hidden)
                {
                    ellipseLaserPoint.Visibility = Visibility.Hidden;
                }

                if (rectangle1.Visibility == Visibility.Visible)
                {
                    if (cursorInFrame(ref mouseCurrentPos)) //set cursor position within frame
                    {
                        bCursorOnFrame = true;
                        if (bGlobalSettingUseLine && bMovingMouse)
                        {
                            line1.X1 = ellipse1.Margin.Left + ellipse1.Width / 2;
                            line1.Y1 = ellipse1.Margin.Top + ellipse1.Height / 2;
                            line1.X2 = rectangle1.Margin.Left + rectangle1.Width / 2;
                            line1.Y2 = rectangle1.Margin.Top + rectangle1.Height / 2;
                            line1.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            ellipse1.Margin = new Thickness(mouseCurrentPos.X - ellipse1.Width / 2, mouseCurrentPos.Y - ellipse1.Height / 2, 0, 0);
                        }
                    }
                    else //cursor already in frame
                    {
                        bCursorOnFrame = false;
                        line1.Visibility = Visibility.Hidden;
                    }
                }
                else //No frame
                {
                    line1.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                if (rectangle1.Visibility != Visibility.Hidden)
                {
                    rectangle1.Visibility = Visibility.Hidden;
                }
                if (ellipseLaserPoint.Visibility != Visibility.Visible)
                {
                    ellipseLaserPoint.Visibility = Visibility.Visible;
                }
            }

            //
            // Move mouse-moving indicator and set its visibility
            ellipseMouseRing.Margin = new Thickness(ellipse1.Margin.Left + (ellipse1.Width - ellipseMouseRing.Width) / 2,
                                                    ellipse1.Margin.Top + (ellipse1.Height - ellipseMouseRing.Height) / 2,
                                                    0, 0);
            if (bMovingMouse)
            {
                ellipseMouseRing.Visibility = Visibility.Visible;
            }
            else 
            {
                ellipseMouseRing.Visibility = Visibility.Hidden;
            }

            //
            // start dwelling clicking mechanism 
            if (bTestStarted) performDwellClick();

            //
            // Remove frame if gaze not present
            if (bShowFrame)
            {
                if (!frameAppearStopwatch.IsRunning)
                {
                    frameAppearStopwatch.Start();
                }
                else
                {
                    if (frameAppearStopwatch.ElapsedMilliseconds > cFrameRemainTimeMS)
                    {
                        bShowFrame = false;
                        bFrameFirstMove = true;
                        frameAppearStopwatch.Stop();
                        frameAppearStopwatch.Reset();
                    }
                }
            }

            //
            // Draw Gaze Points while gaze-dwelling & frame presenting
            if (!bShowFrame)
            {
                for (int i = 0; i < cGazeDwellSampleCount; ++i)
                {
                    aDwellDots[i].Visibility = Visibility.Hidden;
                }
            }
            else // when the frame is showing up
            {
                if (bGazeDwelling)
                {
                    for (int i = 0; i < qGazeDwellQueue.Count; ++i)
                    {
                        aDwellDots[i].Margin = new Thickness(
                            qGazeDwellQueue.ElementAt(i).X - aDwellDots[i].Width / 2,
                            qGazeDwellQueue.ElementAt(i).Y - aDwellDots[i].Width / 2, 0, 0);
                        aDwellDots[i].Visibility = Visibility.Visible;
                    }
                    for (int i = qGazeDwellQueue.Count; i < cGazeDwellSampleCount; ++i)
                    {
                        aDwellDots[i].Visibility = Visibility.Hidden;
                    }
                }
                else
                {
                    for (int i = 0; i < cGazeDwellSampleCount; ++i)
                    {
                        aDwellDots[i].Visibility = Visibility.Hidden;
                    }
                }
            }

            // ====== less frequently refreshed contents ==========
            ++nTickCount;

            if (nTickCount % 20 == 0) 
            {
                //UI: command list, OSD
                String[] commands = new String[cmdQueueLength];
                if(cmdQueue.Count > 0){
                    commands = cmdQueue.ToArray();
                    String cmdSingleLine = "";
                    for (int i = 0; i < commands.Length; i++)
                    {
                        cmdSingleLine += commands[i];
                    }
                    dlgWindow.textBlock1.Text = cmdSingleLine;
                }
                label1.Content = Convert.ToString(stopwatch.ElapsedMilliseconds / 1000.0) + "s";
                label3.Content = Convert.ToString(task_count) + " task(s) left";
                if (bTestStarted)
                {
                    label4.Content = "Test running...";
                }
                else
                {
                    label4.Content = "Test stopped.";
                }
            }

            if (nTickCount >= 100)
            {
                long nNow = fpsStopwatch.ElapsedMilliseconds;
                dFramePerSec = nTickCount * 1000 / (nNow - nLastTimeStamp);
                if (nNow != nLastTimeStamp) label2.Content = Convert.ToString(Convert.ToInt32(dFramePerSec)) + " ticks/sec";
                nTickCount = 0;
                nLastTimeStamp = nNow;
            }
        }//timer_Tick


        private void logger_Tick(object sender, EventArgs e)
        {
            try
            {
                if (logWriter != null)
                {
                    logWriter.WriteLine("{0},{1},{2}", frameCurrentOffsetLength, mouseCurrentOffsetLength, task_count*30);
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message.ToString());
                return;
            }

            mouseCurrentOffsetLength = 0;
            frameCurrentOffsetLength = 0;
        }

        
        /// <summary>
        /// udpLoop
        /// </summary>
        private void udpLoop()
        {
            UdpClient udp = new UdpClient(8888);
//             UdpClient udp;
//             try
//             {
//                 udp = new UdpClient(8888);
//             }
//             catch (Exception e)
//             {
//                 MessageBox.Show(e.Message.ToString());
//                 return;
//             }

            IPEndPoint remoteEP = null;
            byte[] recvBytes;

            udp.Client.ReceiveTimeout = 700;

            while (!bExitPending)
            {
                try
                {
                    recvBytes = udp.Receive(ref remoteEP);

                    switch (recvBytes[0])
                    {
                        case 0x0:
                            if(Convert.ToBoolean(recvBytes[1]))
                            {
                                Int32 frameX = recvBytes[2] + (recvBytes[3] << 8) + (recvBytes[4] << 16) + (recvBytes[5] << 24);
                                Int32 frameY = recvBytes[6] + (recvBytes[7] << 8) + (recvBytes[8] << 16) + (recvBytes[9] << 24);
                                changeFrame((Double)frameX, (Double)frameY, true);
                                Console.WriteLine("Frame: " + Convert.ToString(frameX) + "," + Convert.ToString(frameY));
                                string trashString = "";
                                while(cmdQueue.Count > cmdQueueLength) cmdQueue.TryDequeue(out trashString);
                                cmdQueue.Enqueue("Frame: " + Convert.ToString(frameX) + "," + Convert.ToString(frameY) + "\n"); 
                            }
                            else
                            {
                                changeFrame(0, 0, false);
                            }
                            break;
                        case 0x1:
                            if (Convert.ToBoolean(recvBytes[1]))
                            {
                                Int32 mouseX = recvBytes[2] + (recvBytes[3] << 8) + (recvBytes[4] << 16) + (recvBytes[5] << 24);
                                Int32 mouseY = recvBytes[6] + (recvBytes[7] << 8) + (recvBytes[8] << 16) + (recvBytes[9] << 24);
                                changeMouse((Double)mouseX, (Double)mouseY, true);
                                Console.WriteLine("Mouse: " + Convert.ToString(mouseX) + "," + Convert.ToString(mouseY));
                                string trashString2 = "";
                                while(cmdQueue.Count > cmdQueueLength) cmdQueue.TryDequeue(out trashString2); 
                                cmdQueue.Enqueue("Mouse: " + Convert.ToString(mouseX) + "," + Convert.ToString(mouseY) + "\n");
                            }
                            else
                            {
                                changeMouse(0, 0, false);
                            }
                            break;
                        case 0x2:
                            Boolean bLMB = Convert.ToBoolean(recvBytes[1]);
                            changeLMB(bLMB);
                            Console.WriteLine("LMB: " + Convert.ToString(bLMB));
                            string trashString3 = "";
                            while(cmdQueue.Count > cmdQueueLength) cmdQueue.TryDequeue(out trashString3);
                            cmdQueue.Enqueue("LMB: " + Convert.ToString(bLMB) + "\n");
                            break;
                        default:
                            break;
                    }
                }
                catch (SocketException e)
                {
                    if (e.ErrorCode == 10060) continue;
                    MessageBox.Show(e.Message.ToString());
                }
            }//while

            udp.Close();
        }//udpLoop

        private void changeFrame(Double x, Double y, Boolean bPresent)
        {
            if(bPresent)
            {
                frameAppearStopwatch.Stop();
                frameAppearStopwatch.Reset();
                bShowFrame = true;
                Double relX = x / CAM_WIDTH * windowWidth;
                Double relY = y / CAM_HEIGHT * windowHeight;
                boxPosQueue.Enqueue(new Point(relX, relY));   //Enqueue absolute position

                if (bFrameFirstMove)
                {
                    bFrameFirstMove = false;
                    boxPosOffsetQueue.Enqueue(new Point(0, 0)); 
                    frameLastPos = new Point(relX, relY);
                }
                else
                {
                    boxPosOffsetQueue.Enqueue(new Point(relX - frameLastPos.X, relY - frameLastPos.Y));  //Enqueue moving distance
                    frameLastPos = new Point(relX, relY);
                }
            }
            else
            {
                frameAppearStopwatch.Start();
            }
        }

        private void changeMouse(Double x, Double y, Boolean bPresent)
        {
            if (bPresent)
            {
                if (moveIntervalStopwatch.IsRunning && moveIntervalStopwatch.ElapsedMilliseconds > 100)
                {
                    move_count++;
                    moveIntervalStopwatch.Stop();
                    moveIntervalStopwatch.Reset();
                }

                //Absolute move
//                 Double relX = ((Double)x - (cursorWidth / 2)) / KINECT_WIDTH * windowWidth;
//                 Double relY = ((Double)y - (cursorHeight / 2)) / KINECT_HEIGHT * windowHeight;
//                 cursorPosQueue.Enqueue(new Thickness(relX, relY, 0, 0));

                //Relative move
                if (bMouseFirstMove)
                {
                    if (!bMovingMouse)
                    {
                        bMovingMouse = true;
                        System.Media.SystemSounds.Asterisk.Play();
                    }
                    mouseLastPos = new System.Windows.Point(x, y);
                    bMouseFirstMove = false;
                }
                else
                {
                    cursorPosQueue.Enqueue(new Point(x - mouseLastPos.X, y - mouseLastPos.Y));
                    mouseLastPos.X = x;
                    mouseLastPos.Y = y;
                }
            }
            else
            {
                if (bMovingMouse)
                {
                    bMovingMouse = false;
                    System.Media.SystemSounds.Beep.Play();
                }
                bMouseFirstMove = true;
                moveIntervalStopwatch.Start();
            }
        }

        private void changeLMB(Boolean lmbFlag){
            lmbQueue.Enqueue(lmbFlag);
        }

        private void moveFrame()
        {
            while (boxPosQueue.Count > 0 && boxPosOffsetQueue.Count > 0)
            {
                Point popup = new Point();
                while(!boxPosOffsetQueue.TryDequeue(out popup));
                Double frameDist = ((Vector)popup).Length;
                frameDistQueue.Enqueue(frameDist);
                if (frameDistQueue.Count > frameDistQueueLength)
                {
                    double trashDouble;
                    frameDistQueue.TryDequeue(out trashDouble);
                }
                frameCurrentOffsetLength = frameDistQueue.Average();

                //Change frame size
                if (bGlobalSettingChangableFrame && frameCurrentOffsetLength > 10)
                {
                    if (this.ActualWidth > this.ActualHeight)
                    {
                        rectangle1.Height = Math.Max(this.ActualHeight / frameScale - Math.Pow(frameCurrentOffsetLength, shrinkIndex), 0);
                        rectangle1.Width = rectangle1.Height * 1.618;
                    }
                    else
                    {
                        rectangle1.Width = Math.Max(this.ActualWidth / frameScale - Math.Pow(frameCurrentOffsetLength, shrinkIndex), 0);
                        rectangle1.Height = rectangle1.Width / 1.618;
                    }
                }
                else
                {
                    if (this.ActualWidth > this.ActualHeight)
                    {
                        rectangle1.Height = this.ActualHeight / frameScale;
                        rectangle1.Width = rectangle1.Height * 1.618;
                    }
                    else
                    {
                        rectangle1.Width = this.ActualWidth / frameScale;
                        rectangle1.Height = rectangle1.Width / 1.618;
                    }
                }

                //Absolute move
                while(!boxPosQueue.TryDequeue(out popup));
                rectangle1.Margin = new Thickness(  popup.X - rectangle1.Width / 2,
                                                    popup.Y - rectangle1.Height / 2,
                                                    0, 0);
                ellipseLaserPoint.Margin = new Thickness(   popup.X - ellipseLaserPoint.Width / 2,
                                                            popup.Y - ellipseLaserPoint.Height / 2,
                                                            0, 0);
            }

            if (bShowFrame)
            {
                if (rectangle1.Visibility != Visibility.Visible)
                {
                    rectangle1.Visibility = Visibility.Visible;
                }
            }
            else
            {

                if (rectangle1.Visibility != Visibility.Hidden)
                {
                    rectangle1.Visibility = Visibility.Hidden;
                }
            }
        }

        private void moveMouse()
        {
            if (bGlobalSettingMouseControl)
            {
                ellipse1.Margin = new Thickness(realMouseRelativePos.X - ellipse1.Width / 2,
                                                realMouseRelativePos.Y - ellipse1.Height / 2,
                                                0, 0);
            }
            else{
                int lowPassFilterLength = 1;
                if (cursorPosQueue.Count >= 3)
                {
                    lowPassFilterLength = 3;
                }
                else lowPassFilterLength = cursorPosQueue.Count;

                Point popup = new Point(0, 0);
                while (cursorPosQueue.Count > 0)
                {
                    while(!cursorPosQueue.TryDequeue(out popup));
                    //popup.X = 0;
                    //popup.Y = 0;
                    //for(int i=1; i <= lowPassFilterLength; ++i)
                    //{
                    //    popup.X += cursorPosQueue.ElementAt(cursorPosQueue.Count - i).X;
                    //    popup.X /= lowPassFilterLength;
                    //    popup.Y += cursorPosQueue.ElementAt(cursorPosQueue.Count - i).Y;
                    //    popup.Y /= lowPassFilterLength;
                    //}

                    ellipse1.Margin = new Thickness(ellipse1.Margin.Left + (popup.X * mouseScale),
                                                    ellipse1.Margin.Top + (popup.Y * mouseScale),
                                                    0, 0);
                    mouseCurrentOffsetLength = ((Vector)popup).Length;
                    //cursorPosQueue.Dequeue();
                }
            }
        }

        /// <summary>
        /// NOT IN USE
        /// </summary>
        private void performClick()
        {
            while (lmbQueue.Count > 0)
            {
                bool bLmb;
                while (!lmbQueue.TryDequeue(out bLmb)) ;
                if(bLmb)
                {
                    ellipse1.Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x3C, 0x57, 0x7F));
                    if(ellipse1.Margin.Left + ellipse1.Width / 2 > ellipseTarget.Margin.Left
                        && ellipse1.Margin.Left + ellipse1.Width / 2 < ellipseTarget.Margin.Left + ellipseTarget.Width
                        && ellipse1.Margin.Top + ellipse1.Height / 2 > ellipseTarget.Margin.Top
                        && ellipse1.Margin.Top + ellipse1.Height / 2 < ellipseTarget.Margin.Top + ellipseTarget.Height)
                    {
                        gotTarget();
                        System.Media.SystemSounds.Asterisk.Play();
                    }
                    else
                    {
                        missedTarget();
                        System.Media.SystemSounds.Exclamation.Play();
                    }
                }
                else
                {
                    ellipse1.Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x1E, 0xC4, 0xAA));
                }
            }
        }

        private void performDwellClick() 
        {
            if (ellipse1.Margin.Left + ellipse1.Width / 2 > ellipseTarget.Margin.Left
                        && ellipse1.Margin.Left + ellipse1.Width / 2 < ellipseTarget.Margin.Left + ellipseTarget.Width
                        && ellipse1.Margin.Top + ellipse1.Height / 2 > ellipseTarget.Margin.Top
                        && ellipse1.Margin.Top + ellipse1.Height / 2 < ellipseTarget.Margin.Top + ellipseTarget.Height)
            {
                if (bFirstEnter) 
                {
                    bFirstEnter=false;
                    dwellStopwatch.Reset();
                    dwellStopwatch.Start();
                }
                else if(dwellStopwatch.ElapsedMilliseconds >= 700)
                {
                    dwellStopwatch.Stop();
                    dwellStopwatch.Reset();

                    gotTarget();
                    System.Media.SystemSounds.Asterisk.Play();
                }
            }
            else 
            {
                if(!bFirstEnter)
                {
                    if (dwellStopwatch.ElapsedMilliseconds > 100 && dwellStopwatch.ElapsedMilliseconds < 700) 
                    {
                        missedTarget();
                    }
                    dwellStopwatch.Stop();
                    dwellStopwatch.Reset();

                    bFirstEnter = true;
                }
            }
        }

        private Boolean normalizePos(ref System.Windows.Point point)
        {
            Boolean bNormalized = false;
            if (point.X < 1)
            {
                point.X = 1;
                bNormalized = true;
            }
            if (point.Y < 1)
            {
                point.Y = 1;
                bNormalized = true;
            }
            if (point.X > this.ActualWidth) 
            {
                point.X = this.ActualWidth;
                bNormalized = true;
            }
            if (point.Y > this.ActualHeight) 
            {
                point.Y = this.ActualHeight;
                bNormalized = true;
            }

            return bNormalized;
        }

        private Boolean cursorInFrame(ref System.Windows.Point point)
        {
            Boolean bMovedToFrame = false;
            if (point.X < rectangle1.Margin.Left)
            {
                point.X = rectangle1.Margin.Left;
                bMovedToFrame = true;
            }
            if (point.Y < rectangle1.Margin.Top)
            {
                point.Y = rectangle1.Margin.Top;
                bMovedToFrame = true;
            }
            if (point.X > rectangle1.Margin.Left + rectangle1.Width)
            {
                point.X = rectangle1.Margin.Left + rectangle1.Width;
                bMovedToFrame = true;
            }
            if (point.Y > rectangle1.Margin.Top + rectangle1.Height)
            {
                point.Y = rectangle1.Margin.Top + rectangle1.Height;
                bMovedToFrame = true;
            }

            return bMovedToFrame;
        }


        private void targetBlink(double x, double y)
        {
            for(int i = 0; i < highLight.Length; i++)
            {
                highLight[i].Margin = new Thickness(x - highLight[i].Width / 2, y - highLight[i].Height / 2, 0, 0);
            }

            var storyboard = new Storyboard();
            {
                var animation1 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(100),
                };
                Storyboard.SetTargetProperty(animation1, new PropertyPath("Opacity"));
                storyboard.Children.Add(animation1);

                var animation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    BeginTime = TimeSpan.FromMilliseconds(100),
                    Duration = TimeSpan.FromMilliseconds(100)
                };
                Storyboard.SetTargetProperty(animation2, new PropertyPath("Opacity"));
                storyboard.Children.Add(animation2);
            }

            for (int i = 0; i < highLight.Length; i++)
            {
                storyboard.BeginTime = TimeSpan.FromMilliseconds(100 * i);
                highLight[i].BeginStoryboard(storyboard);
            }
        }

        //----------setting tests--------------------------------------------------------------------------------------

        public void startTest()
        {
            movementTimer.Start();
            
            setTarget();

            bTestStarted = true;
        }

        private void setTarget()
        {
            System.Windows.Point randomP = new Point();
            Random rnd = new Random();
            Boolean bNotInBound = true;

            double h = canvas1.ActualHeight;
            double w = canvas1.ActualWidth;
            double margin = 20;

            if (bFirstTarget)
            {
                Int32 x = rnd.Next(Convert.ToInt32(1 + margin), Convert.ToInt32(w - margin));
                Int32 y = rnd.Next(Convert.ToInt32(1 + margin), Convert.ToInt32(h - margin));

                randomP = new Point(x, y);
                targetLastPos = randomP;
                bFirstTarget = false;
            }
            else
            {
                do
                {
                    randomP.X = rnd.Next(Convert.ToInt32(Math.Max(1 + margin, targetLastPos.X - w / 2.0)),
                                            Convert.ToInt32(Math.Min(w - margin, targetLastPos.X + w / 2.0)));
                    Double y = Math.Sqrt(Math.Pow(w / 2.0, 2.0) - Math.Pow(randomP.X - targetLastPos.X, 2.0));
                    if (y + targetLastPos.Y > 1 + margin && y + targetLastPos.Y < h - margin)
                    {
                        randomP.Y = y + targetLastPos.Y;
                        bNotInBound = false;
                    }
                    if (-y + targetLastPos.Y > 1 + margin && -y + targetLastPos.Y < h - margin)
                    {
                        randomP.Y = -y + targetLastPos.Y;
                        bNotInBound = false;
                    }
                }
                while (bNotInBound);
                targetLastPos = randomP;
            }

            stopwatch.Stop();
            stopwatch.Reset();
            ellipseTarget.Visibility = Visibility.Visible;
            ellipseTarget.Margin = new Thickness(randomP.X - ellipseTarget.Width / 2, randomP.Y - ellipseTarget.Height / 2, 0, 0);
            targetBlink(randomP.X, randomP.Y);
            stopwatch.Start();

            task_count--;
        }

        private void gotTarget()
        {
            stopwatch.Stop();
            long millisec = stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();
            

            try
            {
                if (task_count > 0)
                {
                    if (fileWriter != null)
                    {
                        fileWriter.WriteLine("{0},{1},{2}", millisec, miss_count, move_count);
                    }
                    setTarget();
                }
                else
                {
                    movementTimer.Stop();

                    if (fileWriter != null)
                    {
                        fileWriter.WriteLine("{0},{1},{2}", millisec, miss_count, move_count);
                        fileWriter.Flush();
                    }
                    if (logWriter != null)
                    {
                        logWriter.Flush();
                    }

                    ellipseTarget.Visibility = Visibility.Hidden;
                    bFirstTarget = true;
                    bTestStarted = false;
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message.ToString());
                return;
            }

            miss_count = 0;
            move_count = 1;
        }

        private void missedTarget()
        {
            miss_count++;
        }
    }
}

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
            //-for gaze test--------------------
        private const Int16 cDwellClickingThreshMS = 700;
        private const Int16 cGazePosQueueLength = 10;
        private const double cGazeCheckIntlMilliSec = 50;
        private const double cGazeActivityThresh = 225;
        private const int cGazeDwellSampleCount = 20;
        private const double cGazeDotDrawingIntlMilliSec = 200;
        private const int cGridColumn = 6;
        private const int cGridRow = 4;
        private const int cGridMarginX = 20;
        private const int cGirdMarginY = 20; 
        private const double cGridDiameter = 20;
        private const Int16 cGazeDwellingThreshMS = 1000;
        private const Int16 cGazeHomeDwellingThreshMS = 1000;
        private const double cHomingAreaWidth = 150;
        private const double cHomingAreaHeight = 150;
        private const int cTaskRepeatCnt = 2;


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
        private ConcurrentQueue<Byte> lmbQueue;
        private ConcurrentQueue<String> cmdQueue;
        private ConcurrentQueue<Double> frameDistQueue;
        private ConcurrentQueue<Point> qGazePosQueue;
        private ConcurrentQueue<Point> qGazeDwellQueue;
        private ConcurrentQueue<bool> qGazeDwellFlagQueue;

        //--system status-----------------------
        private System.Windows.Point realMouseRelativePos;
        private System.Windows.Point currentGazeAvgPos;
        private Boolean bExitPending;
        private Boolean bTestStarted;
        private Boolean bShowFrame;
        private Boolean bMovingMouse;
        private Boolean bFrameFirstMove;
        private Boolean bMouseFirstMove;
        private Boolean bFirstEnter;
        private Boolean bCursorOnFrame;
        private System.Windows.Point frameLastPos;
        private System.Windows.Point frameCurrentPos;
        private System.Windows.Point mouseLastPos;
        private System.Windows.Point mouseCurrentPos;
        private Double frameCurrentOffsetLength;
        private Double mouseCurrentOffsetLength;
        private Int16 miss_count;
        private Int16 move_count;
        public int task_count;
        private Int64 nLastTimeStamp;
        private Double dFramePerSec;

        private bool bGazeDwelling;
        private Double dGazeActivity;
        private bool bGazeHoming;
        private int nTargetHandState;

        //--misc components---------------------
        //---time---------------
        private System.Diagnostics.Stopwatch stopwatch;
        private System.Diagnostics.Stopwatch dwellStopwatch;
        private System.Diagnostics.Stopwatch moveIntervalStopwatch;
        private System.Diagnostics.Stopwatch fpsStopwatch;
        private System.Diagnostics.Stopwatch frameAppearStopwatch;      // frame remain visible for a while after dead control signal
        private System.Diagnostics.Stopwatch gazingStopwatch;
        private System.Diagnostics.Stopwatch gazeHomeDwellStopwatch;

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
        private Ellipse homingArea;
        //---GridDots-----------
        public struct GridPos
        {
            public int i, j;

            public GridPos(int p1, int p2)
            {
                i = p1;
                j = p2;
            }
        }
        private GridPos[] gridMask;

        public struct GridDot
        {
            public Ellipse dot;
            public double x_percent, y_percent;

            public GridDot(Ellipse dot_in, double x_in, double y_in)
            {
                dot = dot_in;
                x_percent = x_in;
                y_percent = y_in;
            }
        }
        private List<GridDot> aGridDots;

        private int[] gTaskOrder;


        //---sub window---------
        private Window1 dlgWindow;


        /// <summary>
        /// window load event
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //gridMask = new GridPos[] { new GridPos(1,2) };
            gridMask = new GridPos[] { };

            currentGazeAvgPos = new Point(0, 0);
            bGlobalSettingFramed = false;
            bGlobalSettingChangableFrame = true;
            bGlobalSettingMouseControl = false;
            bGlobalSettingUseLine = false;

            mouseScale = 1.0;
            frameScale = 2.0;
            shrinkIndex = 1.6;

            if (this.ActualWidth > this.ActualHeight)
            {
                rectangle1.Height = this.ActualHeight / frameScale;
                rectangle1.Width = rectangle1.ActualHeight * 1.618;
            }
            else
            {
                rectangle1.Width = this.ActualWidth / frameScale;
                rectangle1.Height = rectangle1.ActualWidth / 1.618;
            }
            cursorHeight = ellipse1.ActualHeight;
            cursorWidth = ellipse1.ActualWidth;
            windowHeight = canvas1.ActualHeight;
            windowWidth = canvas1.ActualWidth;

            bExitPending = false;
            bTestStarted = false;
            bFrameFirstMove = true;
            bMouseFirstMove = true;
            bShowFrame = false;
            bMovingMouse = false;
            bCursorOnFrame = false;

            bGazeDwelling = true;
            dGazeActivity = 0.0f;
            bGazeHoming = false;
            nTargetHandState = 0;

            bFirstEnter = true;
            //bFirstTarget = true;
            miss_count = 0;
            move_count = 1;

            nLastTimeStamp = 0;
            dFramePerSec = 0;

            boxPosQueue = new ConcurrentQueue<Point>();
            boxPosOffsetQueue = new ConcurrentQueue<Point>();
            cursorPosQueue = new ConcurrentQueue<Point>();
            lmbQueue = new ConcurrentQueue<Byte>();
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
            gazingStopwatch = new System.Diagnostics.Stopwatch();
            gazeHomeDwellStopwatch = new System.Diagnostics.Stopwatch();

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
                highLight[i].Width = ellipseTarget.ActualWidth + 10.0 * i;
                highLight[i].Height = ellipseTarget.ActualHeight + 10.0 * i;

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

            // TODO : reposition the grid dots
            aGridDots = new List<GridDot>();
            SolidColorBrush gridBrush = new SolidColorBrush();
            gridBrush.Color = Colors.DarkCyan;
            for(int i = 0; i < cGridRow; ++i)
            {
                for(int j = 0; j < cGridColumn; ++j)
                {
                    if (Array.IndexOf(gridMask, new GridPos(i, j)) == -1)  //the dot is not masked out
                    {
                        Ellipse newDot = new Ellipse();
                        newDot = new Ellipse();
                        newDot.Fill = gridBrush;
                        newDot.Width = cGridDiameter;
                        newDot.Height = cGridDiameter;
                        newDot.Visibility = Visibility.Hidden;

                        canvas1.Children.Add(newDot);
                        Canvas.SetLeft(newDot, (canvas1.ActualWidth - cGridMarginX * 2) / (cGridColumn - 1) * j + cGridMarginX - (cGridDiameter / 2));
                        Canvas.SetTop(newDot, (canvas1.ActualHeight - cGirdMarginY * 2) / (cGridRow - 1) * i + cGirdMarginY - (cGridDiameter / 2));
                        Canvas.SetZIndex(newDot, -3);

                        // memorize the dot object and its proportional position
                        aGridDots.Add(
                            new GridDot(
                                newDot,
                                (Canvas.GetLeft(newDot) + (cGridDiameter / 2)) / canvas1.ActualWidth,
                                (Canvas.GetTop(newDot) + (cGridDiameter / 2)) / canvas1.ActualHeight
                            )
                        );
                    }
                }
            }

            homingArea = new Ellipse();
            homingArea.Width = cHomingAreaWidth;
            homingArea.Height = cHomingAreaHeight;
            Canvas.SetLeft(homingArea, windowWidth / 2 - cHomingAreaWidth / 2);
            Canvas.SetTop(homingArea, windowHeight / 2 - cHomingAreaHeight / 2);
            homingArea.Fill = new SolidColorBrush(Color.FromArgb(0x80, 0x16, 0xc1, 0xfa));
            canvas1.Children.Add(homingArea);
            Canvas.SetZIndex(homingArea, -2);

            rock_png.Visibility = Visibility.Hidden;
            scissors_png.Visibility = Visibility.Hidden;
            paper_png.Visibility = Visibility.Hidden;

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
                Canvas.GetLeft(ellipseLaserPoint) + ellipseLaserPoint.ActualWidth / 2,
                Canvas.GetTop(ellipseLaserPoint) + ellipseLaserPoint.ActualHeight / 2
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
                currentGazeAvgPos = new Point(xAverage, yAverage);

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
                        gazingStopwatch.Stop();
                        gazingStopwatch.Reset();

                        ellipseLaserPoint.Fill = new SolidColorBrush(Color.FromArgb(0xff, 0xfa, 0xf3, 0x1e));
                        rectangle1.Fill = new SolidColorBrush(Color.FromArgb(0x48, 0x8a, 0xef, 0xb8));
                    }
                }
                else
                {
                    if(!bGazeDwelling)
                    {
                        bGazeDwelling = true;
                        gazingStopwatch.Start();

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
            windowHeight = canvas1.ActualHeight;
            windowWidth = canvas1.ActualWidth;

            if (aGridDots != null)
            {
                foreach (GridDot gridDot in aGridDots)
                {
                    Canvas.SetLeft(gridDot.dot, gridDot.x_percent * canvas1.ActualWidth - (cGridDiameter / 2));
                    Canvas.SetTop(gridDot.dot, gridDot.y_percent * canvas1.ActualHeight - (cGridDiameter / 2));
                }
            }

            if (homingArea != null)
            {
                Canvas.SetLeft(homingArea, windowWidth / 2 - cHomingAreaWidth / 2);
                Canvas.SetTop(homingArea, windowHeight / 2 - cHomingAreaHeight / 2);
            }
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

            mouseCurrentPos = new System.Windows.Point(Canvas.GetLeft(ellipse1) + ellipse1.ActualWidth / 2, Canvas.GetTop(ellipse1) + ellipse1.ActualHeight / 2);
            frameCurrentPos = new System.Windows.Point(Canvas.GetLeft(rectangle1) + rectangle1.ActualWidth / 2, Canvas.GetTop(rectangle1) + rectangle1.ActualHeight / 2);

            //
            // Move the cursor(shape)
            if (normalizePos(ref mouseCurrentPos)) //Normalize cursor position so that it is inside the bound of main window
            {
                Canvas.SetLeft(ellipse1, mouseCurrentPos.X - ellipse1.ActualWidth / 2);
                Canvas.SetTop(ellipse1, mouseCurrentPos.Y - ellipse1.ActualHeight / 2);
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
                            line1.X1 = Canvas.GetLeft(ellipse1) + ellipse1.ActualWidth / 2;
                            line1.Y1 = Canvas.GetTop(ellipse1) + ellipse1.ActualHeight / 2;
                            line1.X2 = Canvas.GetLeft(rectangle1) + rectangle1.ActualWidth / 2;
                            line1.Y2 = Canvas.GetTop(rectangle1) + rectangle1.ActualHeight / 2;
                            line1.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            Canvas.SetLeft(ellipse1, mouseCurrentPos.X - ellipse1.ActualWidth / 2);
                            Canvas.SetTop(ellipse1, mouseCurrentPos.Y - ellipse1.ActualHeight / 2);
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
            Canvas.SetLeft(ellipseMouseRing, Canvas.GetLeft(ellipse1) + (ellipse1.ActualWidth - ellipseMouseRing.ActualWidth) / 2);
            Canvas.SetTop(ellipseMouseRing, Canvas.GetTop(ellipse1) + (ellipse1.ActualHeight - ellipseMouseRing.ActualHeight) / 2);

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
            if (bTestStarted) performGaze();

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
                        Canvas.SetLeft(aDwellDots[i], qGazeDwellQueue.ElementAt(i).X - aDwellDots[i].ActualWidth / 2);
                        Canvas.SetTop(aDwellDots[i], qGazeDwellQueue.ElementAt(i).Y - aDwellDots[i].ActualHeight / 2);
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
                
                if (bGazeHoming)
                    // change homing area color
                {
                    Point homingCenter = new Point(
                        Canvas.GetLeft(homingArea) + cHomingAreaWidth / 2,
                        Canvas.GetTop(homingArea) + cHomingAreaHeight / 2
                        );

                    if (
                        Point.Subtract(currentGazeAvgPos, homingCenter).Length < cHomingAreaWidth / 2
                        && bGazeDwelling
                        )
                    {
                        homingArea.Fill = new SolidColorBrush(Color.FromArgb(0x80, 0xf4, 0xff, 0x21));
                    }
                    else
                    {
                        homingArea.Fill = new SolidColorBrush(Color.FromArgb(0x80, 0x16, 0xc1, 0xfa));
                    }
                }
            }

            if (nTickCount >= 100)
            {
                label_resolution.Content = "CanvasRes: "+canvas1.ActualWidth + " × " + canvas1.ActualHeight;

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
                            Byte cLMB = Convert.ToByte(recvBytes[1]);
                            changeLMB(cLMB);
                            Console.WriteLine("LMB: " + Convert.ToString(cLMB));
                            string trashString3 = "";
                            while(cmdQueue.Count > cmdQueueLength) cmdQueue.TryDequeue(out trashString3);
                            cmdQueue.Enqueue("LMB: " + Convert.ToString(cLMB) + "\n");
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

        private void changeLMB(Byte lmbFlag){
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
                        rectangle1.Width = rectangle1.ActualHeight * 1.618;
                    }
                    else
                    {
                        rectangle1.Width = Math.Max(this.ActualWidth / frameScale - Math.Pow(frameCurrentOffsetLength, shrinkIndex), 0);
                        rectangle1.Height = rectangle1.ActualWidth / 1.618;
                    }
                }
                else
                {
                    if (this.ActualWidth > this.ActualHeight)
                    {
                        rectangle1.Height = this.ActualHeight / frameScale;
                        rectangle1.Width = rectangle1.ActualHeight * 1.618;
                    }
                    else
                    {
                        rectangle1.Width = this.ActualWidth / frameScale;
                        rectangle1.Height = rectangle1.ActualWidth / 1.618;
                    }
                }

                //Absolute move
                while(!boxPosQueue.TryDequeue(out popup));
                Canvas.SetLeft(rectangle1, popup.X - rectangle1.ActualWidth / 2);
                Canvas.SetTop(rectangle1, popup.Y - rectangle1.ActualHeight / 2);
                Canvas.SetLeft(ellipseLaserPoint, popup.X - ellipseLaserPoint.ActualWidth / 2);
                Canvas.SetTop(ellipseLaserPoint, popup.Y - ellipseLaserPoint.ActualHeight / 2);
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
                Canvas.SetLeft(ellipse1, realMouseRelativePos.X - ellipse1.ActualWidth / 2);
                Canvas.SetTop(ellipse1, realMouseRelativePos.Y - ellipse1.ActualHeight / 2);
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

                    Canvas.SetLeft(ellipse1, Canvas.GetLeft(ellipse1) + (popup.X * mouseScale));
                    Canvas.SetTop(ellipse1, Canvas.GetTop(ellipse1) + (popup.Y * mouseScale));
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
                Byte bLmb;
                while (!lmbQueue.TryDequeue(out bLmb)) ;
                if(bLmb != 0)
                {
                    ellipse1.Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x3C, 0x57, 0x7F));
                    if(Canvas.GetLeft(ellipse1) + ellipse1.ActualWidth / 2 > Canvas.GetLeft(ellipseTarget)
                        && Canvas.GetLeft(ellipse1) + ellipse1.ActualWidth / 2 < Canvas.GetLeft(ellipseTarget) + ellipseTarget.ActualWidth
                        && Canvas.GetTop(ellipse1) + ellipse1.ActualHeight / 2 > Canvas.GetTop(ellipseTarget)
                        && Canvas.GetTop(ellipse1) + ellipse1.ActualHeight / 2 < Canvas.GetTop(ellipseTarget) + ellipseTarget.ActualHeight)
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
        
        /// <summary>
        /// NOT IN USE
        /// </summary>
        private void performDwellClick() 
        {
            if (
                Canvas.GetLeft(ellipse1) + ellipse1.ActualWidth / 2 > Canvas.GetLeft(ellipseTarget) &&
                Canvas.GetLeft(ellipse1) + ellipse1.ActualWidth / 2 < Canvas.GetLeft(ellipseTarget) + ellipseTarget.ActualWidth &&
                Canvas.GetTop(ellipse1) + ellipse1.ActualHeight / 2 > Canvas.GetTop(ellipseTarget) &&
                Canvas.GetTop(ellipse1) + ellipse1.ActualHeight / 2 < Canvas.GetTop(ellipseTarget) + ellipseTarget.ActualHeight)
            {
                if (bFirstEnter) 
                {
                    bFirstEnter=false;
                    dwellStopwatch.Reset();
                    dwellStopwatch.Start();
                }
                else if(dwellStopwatch.ElapsedMilliseconds >= cDwellClickingThreshMS)
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
                    if (dwellStopwatch.ElapsedMilliseconds > 100 && dwellStopwatch.ElapsedMilliseconds < cDwellClickingThreshMS) 
                    {
                        missedTarget();
                    }
                    dwellStopwatch.Stop();
                    dwellStopwatch.Reset();

                    bFirstEnter = true;
                }
            }
        }

        private void performGaze()
        {
            if(!bGazeHoming)
            {
                if (gazingStopwatch.ElapsedMilliseconds >= cGazeDwellingThreshMS)
                {
                    Byte lmbOut = 99;
                    while (lmbQueue.Count > 0) lmbQueue.TryDequeue(out lmbOut) ;

                    bool winner = false;
                    switch(nTargetHandState)
                    {
                        case 0: //open - paper
                            if(lmbOut == 2)
                            {
                                winner = true;
                            }
                            break;
                        case 1: //close - rock
                            if(lmbOut == 0)
                            {
                                winner = true;
                            }
                            break;
                        case 2: //lasso - scissors
                            if(lmbOut == 1)
                            {
                                winner = true;
                            }
                            break;
                        default:
                            break;
                    }

                    if(winner)
                    {
                        bGazeHoming = true;
                        gotTarget();
                    }
                }
            }//if Homing == false
            else
            {
                homingArea.Visibility = Visibility.Visible;

                Point homingCenter = new Point(
                    Canvas.GetLeft(homingArea) + cHomingAreaWidth / 2, 
                    Canvas.GetTop(homingArea) + cHomingAreaHeight / 2
                    );

                if (Point.Subtract(currentGazeAvgPos, homingCenter).Length < cHomingAreaWidth / 2)
                    //if inside homing area
                {
                    if(bGazeDwelling)
                    {
                        if (!gazeHomeDwellStopwatch.IsRunning)
                        {
                            gazeHomeDwellStopwatch.Start();
                        }
                        else
                        {
                            if (gazeHomeDwellStopwatch.ElapsedMilliseconds >= cGazeHomeDwellingThreshMS)
                            {
                                bGazeHoming = false;
                                gazeHomeDwellStopwatch.Stop();
                                gazeHomeDwellStopwatch.Reset();
                                gazingStopwatch.Reset();

                                homingArea.Visibility = Visibility.Hidden;
                                setTarget();
                            }
                        }
                    }
                    else
                    {
                        if(gazeHomeDwellStopwatch.IsRunning)
                        {
                            gazeHomeDwellStopwatch.Stop();
                            gazeHomeDwellStopwatch.Reset();
                        }
                    }
                }
            }//if Homing == true
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
            if (point.X < Canvas.GetLeft(rectangle1))
            {
                point.X = Canvas.GetLeft(rectangle1);
                bMovedToFrame = true;
            }
            if (point.Y < Canvas.GetTop(rectangle1))
            {
                point.Y = Canvas.GetTop(rectangle1);
                bMovedToFrame = true;
            }
            if (point.X > Canvas.GetLeft(rectangle1) + rectangle1.ActualWidth)
            {
                point.X = Canvas.GetLeft(rectangle1) + rectangle1.ActualWidth;
                bMovedToFrame = true;
            }
            if (point.Y > Canvas.GetTop(rectangle1) + rectangle1.ActualHeight)
            {
                point.Y = Canvas.GetTop(rectangle1) + rectangle1.ActualHeight;
                bMovedToFrame = true;
            }

            return bMovedToFrame;
        }


        private void targetBlink(double x, double y)
        {
            for(int i = 0; i < highLight.Length; i++)
            {
                Canvas.SetLeft(highLight[i], x - highLight[i].ActualWidth / 2);
                Canvas.SetTop(highLight[i], y - highLight[i].ActualHeight / 2);
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

        private void setTaskOrder(ref int[] vancant_order_list, int task_cnt, int repeat_cnt)
        {
            for (int i=0; i< task_cnt; ++i)
            {
                for (int j=0; j< repeat_cnt; ++j)
                {
                    vancant_order_list[repeat_cnt * i + j]= i;
                }
            }

            Random rng = new Random();
            int total_cnt = task_cnt * repeat_cnt;
            while (total_cnt > 1)
            {
                total_cnt--;
                int k = rng.Next(total_cnt + 1);
                int temp = vancant_order_list[k];
                vancant_order_list[k] = vancant_order_list[total_cnt];
                vancant_order_list[total_cnt] = temp;
            }
        }

        //----------setting tests--------------------------------------------------------------------------------------

        public void startTest()
        {
            task_count = aGridDots.Count * cTaskRepeatCnt;
            gTaskOrder = new int[task_count];
            setTaskOrder(ref gTaskOrder, aGridDots.Count, cTaskRepeatCnt);

            paper_png.Visibility = Visibility.Hidden;
            rock_png.Visibility = Visibility.Hidden;
            scissors_png.Visibility = Visibility.Hidden;

            movementTimer.Start();
            
            setTarget();
            homingArea.Visibility = Visibility.Hidden;

            bTestStarted = true;
        }

        private void setTarget()
        {
            System.Windows.Point targetPos = new Point();
            double h = canvas1.ActualHeight;
            double w = canvas1.ActualWidth;

            // grid positions
            int orderIdx = gTaskOrder.Count() - task_count ;
            targetPos.X = Canvas.GetLeft(aGridDots[gTaskOrder[orderIdx]].dot) + (cGridDiameter / 2);
            targetPos.Y = Canvas.GetTop(aGridDots[gTaskOrder[orderIdx]].dot) + (cGridDiameter / 2);

            stopwatch.Stop();
            stopwatch.Reset();

            ellipseTarget.Visibility = Visibility.Visible;
            Canvas.SetLeft(ellipseTarget, targetPos.X - ellipseTarget.ActualWidth / 2);
            Canvas.SetTop(ellipseTarget, targetPos.Y - ellipseTarget.ActualHeight / 2);

            Random rng = new Random();
            nTargetHandState = rng.Next(3);
            switch(nTargetHandState) //set rock-scissors-paper images
            {
                case 0: //open - paper
                    paper_png.Visibility = Visibility.Visible;
                    Canvas.SetLeft(paper_png, targetPos.X - paper_png.ActualWidth / 2);
                    Canvas.SetTop(paper_png, targetPos.Y - paper_png.ActualHeight / 2);
                    break;
                case 1: //close - rock
                    rock_png.Visibility = Visibility.Visible;
                    Canvas.SetLeft(rock_png, targetPos.X - rock_png.ActualWidth / 2);
                    Canvas.SetTop(rock_png, targetPos.Y - rock_png.ActualHeight / 2);
                    break;
                case 2: //lasso - scissors
                    scissors_png.Visibility = Visibility.Visible;
                    Canvas.SetLeft(scissors_png, targetPos.X - scissors_png.ActualWidth / 2);
                    Canvas.SetTop(scissors_png, targetPos.Y - scissors_png.ActualHeight / 2);
                    break;
                default:
                    break;
            }

            targetBlink(targetPos.X, targetPos.Y);
            stopwatch.Start();
        }

        private void gotTarget()
        {
            stopwatch.Stop();
            long millisec = stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();
            ellipseTarget.Visibility = Visibility.Hidden;
            paper_png.Visibility = Visibility.Hidden;
            rock_png.Visibility = Visibility.Hidden;
            scissors_png.Visibility = Visibility.Hidden;

            Point currentGazeAvgPosRelevant2Target = new Point(
                 (Canvas.GetLeft(ellipseTarget) + ellipseTarget.ActualWidth / 2) - currentGazeAvgPos.X,
                 (Canvas.GetTop(ellipseTarget) + ellipseTarget.ActualHeight / 2) - currentGazeAvgPos.Y
            );

            task_count--;
            try
            {
                if (fileWriter != null)
                {
                    fileWriter.WriteLine("{0},{1},{2},{3}",
                        currentGazeAvgPosRelevant2Target.X,
                        currentGazeAvgPosRelevant2Target.Y,
                        Canvas.GetLeft(ellipseTarget) + ellipseTarget.ActualWidth / 2,
                        Canvas.GetTop(ellipseTarget) + ellipseTarget.ActualHeight / 2
                    );
                }

                if (task_count <= 0)
                {
                    movementTimer.Stop();

                    if (fileWriter != null)
                    {
                        fileWriter.Flush();
                    }
                    if (logWriter != null)
                    {
                        logWriter.Flush();
                    }

                    //bFirstTarget = true;
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

        /// <summary>
        /// NOT IN USE
        /// </summary>
        private void missedTarget()
        {
            miss_count++;
        }
    }
}

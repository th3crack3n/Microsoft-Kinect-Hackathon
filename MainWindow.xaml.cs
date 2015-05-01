//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    #region "Includes"
        //vjoy addition
        using System; //(necessary for math)
        using System.Collections.Generic;
        using System.ComponentModel;
        using System.Data;
        using System.Linq;
        using System.Text;
        using System.Runtime.InteropServices;
        using System.Deployment;
        using System.Windows.Threading;
        //kinnect original
        using System.IO;
        using System.Windows;
        using System.Windows.Media;
        using Microsoft.Kinect;
    #endregion
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region "Dimensions and Declarations"
            #region "Constants"
                /// <summary>
                /// Width of output drawing
                /// </summary>
                private const float RenderWidth = 640.0f;
                /// <summary>
                /// Height of our output drawing
                /// </summary>
                private const float RenderHeight = 480.0f;
                /// <summary>
                /// Thickness of drawn joint lines
                /// </summary>
                private const double JointThickness = 3;
                /// <summary>
                /// Thickness of body center ellipse
                /// </summary>
                private const double BodyCenterThickness = 10;
                /// <summary>
                /// Thickness of clip edge rectangles
                /// </summary>
                private const double ClipBoundsThickness = 10;
            #endregion
            #region "Readonly Reference"
                /// <summary>
                /// Brush used to draw skeleton center point
                /// </summary>
                private readonly Brush centerPointBrush = Brushes.Blue;
                /// <summary>
                /// Brush used for drawing joints that are currently tracked
                /// </summary>
                private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
                /// <summary>
                /// Brush used for drawing joints that are currently inferred
                /// </summary>        
                private readonly Brush inferredJointBrush = Brushes.Yellow;
                /// <summary>
                /// Pen used for drawing bones that are currently tracked
                /// </summary>
                private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);
                /// <summary>
                /// Pen used for drawing bones that are currently inferred
                /// </summary>        
                private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);
                //Custom
                private readonly Brush CalibrationBrush = Brushes.Purple;
                private readonly Brush BoundBrush = Brushes.Red;
                private readonly Pen BoundPen = new Pen(Brushes.Red, 6);
            #endregion
            #region "Variables"
                #region "Default"
                    /// <summary>
                /// Active Kinect sensor
                /// </summary> 
                    private KinectSensor sensor;
                    /// <summary>
                    /// Drawing group for skeleton rendering output
                    /// </summary>
                    private DrawingGroup drawingGroup;
                    /// <summary>
                    /// Drawing image that we will display
                    /// </summary>
                    private DrawingImage imageSource;
                #endregion
                #region "Custom"
                    public int heartbeat=0;
                    public KinectSensor myKinect;
                    int closestID = 0;
                #endregion
            #endregion
        #endregion

        #region "Form Handlers and Initialization"
            // Initialize a new instance of the MainWindow class.
            public MainWindow()
            {
                InitializeComponent();
            }
            // Execute startup tasks
            private void WindowLoaded(object sender, RoutedEventArgs e)
            {
                checkBoxSeatedMode.IsChecked = true;
                #region "Start Kinect"
                this.drawingGroup = new DrawingGroup();                         // Create the drawing group we'll use for drawing
                this.imageSource = new DrawingImage(this.drawingGroup);         // Create an image source that we can use in our image control
                Image.Source = this.imageSource;                                // Display the drawing using our image control
            
                foreach (var potentialSensor in KinectSensor.KinectSensors)     // Look through all sensors and start the first connected one.
                {                                                               // This requires that a Kinect is connected at the time of app startup.
                    if (potentialSensor.Status == KinectStatus.Connected)       // To make your app robust against plug/unplug, 
                    {                                                           // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit
                        this.sensor = potentialSensor;
                        break;
                    }
                }
            
                if (null != this.sensor)                                        //If there is a sensor
                {
                    myKinect = this.sensor;
                    myKinect.SkeletonStream.Enable();                                // Turn on the skeleton stream to receive skeleton frames
                    myKinect.SkeletonFrameReady += this.SensorSkeletonFrameReady;    // Add an event handler to be called whenever there is new color frame data
                    myKinect.DepthStream.Range = DepthRange.Near;                    //Attempt to set depth range...     
                    myKinect.SkeletonStream.EnableTrackingInNearRange = true;
                    myKinect.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                    try
                    {
                        myKinect.Start();                                    // Start the sensor!
                    }
                    catch (IOException)
                    {
                        myKinect = null;                                     //Error!
                    }
                }
                if (null == this.sensor)
                {
                    this.statusBarText.Text = Properties.Resources.NoKinectReady;  //Throw Flag
                }
            
                #endregion
            }
            /// Execute shutdown tasks
            private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
            {
                //ADDED!!!
                if (null != this.sensor)
                    {this.sensor.Stop();}
            }
        #endregion             
        int identified = 0;
        List<Joint> LeftHand = new List<Joint>();
        List<Joint> LeftElbow = new List<Joint>();
        List<Joint> LeftShoulder = new List<Joint>();
        List<Joint> RightHand = new List<Joint>();
        List<Joint> RightElbow = new List<Joint>();
        List<Joint> RightShoulder = new List<Joint>();
        List<Joint> CenterShoulder = new List<Joint>();
        double LReach = 0;
        double LTravel = 0;
        double LDirection = 0;
        double RReach = 0;
        double RTravel = 0;
        double RDirection = 0;
        double OffsetX;
        double OffsetY;
        int RPositionX;
        int RPositionY;
        #region "Skeleton Data Collection Smoothing"
            /// Event handler for Kinect sensor's SkeletonFrameReady event
            private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
            {
                Skeleton[] skeletons = new Skeleton[0];
                using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
                {
                    if (skeletonFrame != null)
                        {skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                         skeletonFrame.CopySkeletonDataTo(skeletons);}
                }
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                    #region "Get Skeleton Data and Display"
                    if (skeletons.Length != 0)
                    {
                        //Has the skeleton Disapeared
                        identified -= 1;
                        foreach(Skeleton skel in skeletons){
                            txtTracking.Text = skel.TrackingId.ToString();
                            if (skel.TrackingId == closestID && skel.TrackingId !=0)
                                identified += 1;
                        }
                        //Grab the closest person
                        txtIdentified.Text = identified.ToString();
                        if (identified < -30)
                        {
                            LeftHand.Clear();
                            RightHand.Clear();
                            LeftElbow.Clear();
                            RightElbow.Clear();
                            LeftShoulder.Clear();
                            RightShoulder.Clear();
                            RReach = 0;
                            identified = 0;
                            if (myKinect.SkeletonStream.AppChoosesSkeletons == false)                   // Ensure AppChoosesSkeletons is set
                                myKinect.SkeletonStream.AppChoosesSkeletons = true;             
                            float closestDistance = 10000f;                                             // Start with a far enough distance
                            closestID = 0;
                            foreach (Skeleton skeleton in skeletons.Where(s => s.TrackingState != SkeletonTrackingState.NotTracked))
                            {
                                if (skeleton.Position.Z < closestDistance)
                                {
                                    closestID = skeleton.TrackingId;
                                    closestDistance = skeleton.Position.Z;
                                }
                            }
                            if (closestID > 0)
                                myKinect.SkeletonStream.ChooseSkeletons(closestID);                     // Track this skeleton
                        }
                        foreach (Skeleton skel in skeletons)
                        {
                            if (skel.TrackingId != 0)                                                   //Write Skeleton ID
                                skelId.Text = skel.TrackingId.ToString();
                            RenderClippedEdges(skel, dc);                                               //Render Clipped Edges
                            if (skel.TrackingState == SkeletonTrackingState.Tracked)                    //If skeleton is tracked, show on screen
                                this.DrawBonesAndJoints(skel, dc);
                            else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)          //If only position of person not full skeleton
                            {
                                dc.DrawEllipse(this.centerPointBrush, null, this.SkeletonPointToScreen(skel.Position),
                                BodyCenterThickness, BodyCenterThickness);
                            }
                            AddJoints(LeftHand,skel.Joints,JointType.HandLeft);
                            AddJoints(LeftElbow, skel.Joints, JointType.ElbowLeft);
                            AddJoints(LeftShoulder, skel.Joints, JointType.ShoulderLeft);
                            AddJoints(RightHand, skel.Joints, JointType.HandRight);
                            AddJoints(RightElbow, skel.Joints, JointType.ElbowRight);
                            AddJoints(RightShoulder, skel.Joints, JointType.ShoulderRight);
                            AddJoints(CenterShoulder, skel.Joints, JointType.ShoulderCenter);
                            if (LeftElbow.Count != 0 && LeftHand.Count != 0)
                            {
                                LReach = Math.Sqrt(Math.Pow(LeftHand.Last().Position.X - LeftElbow.Last().Position.X, 2) + Math.Pow(LeftHand.Last().Position.Y - LeftElbow.Last().Position.Y, 2));
                                LTravel = Math.Sqrt(Math.Pow(LeftHand.Last().Position.X - LeftHand.First().Position.X, 2) + Math.Pow(LeftHand.Last().Position.Y - LeftHand.First().Position.Y, 2));
                                //txtLeftHand.Text = LeftHand.Last().Position.X.ToString() + " , " + LeftHand.Last().Position.Y.ToString();
                            }
                            if (RightElbow.Count != 0 && RightHand.Count != 0 && RightShoulder.Count !=0)
                            {
                                if (RReach == 0)
                                {
                                    RReach = Math.Sqrt(Math.Pow(RightHand.Last().Position.X - RightElbow.Last().Position.X, 2) + Math.Pow(RightHand.Last().Position.Y - RightElbow.Last().Position.Y, 2));
                                    RTravel = Math.Sqrt(Math.Pow(RightHand.Last().Position.X - RightHand.First().Position.X, 2) + Math.Pow(RightHand.Last().Position.Y - RightHand.First().Position.Y, 2));
                                    OffsetX = RightShoulder.Last().Position.X - CenterShoulder.Last().Position.X;
                                    OffsetY = RightShoulder.Last().Position.Y - CenterShoulder.Last().Position.Y;
                                }
                                RPositionX = (int)Math.Max(Math.Min((RightHand.Last().Position.X - (CenterShoulder.Last().Position.X + OffsetX)) / ((4.0 / 3.0) * RReach) * System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width),0);
                                RPositionY = (int)Math.Max(Math.Min(-((RightHand.Last().Position.Y - (CenterShoulder.Last().Position.Y + OffsetY)) - RReach) / ((8.0 / 5.0) * RReach) * System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height,System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height),0);
                                if(RightHand.Last().Position.Z < (RightShoulder.Last().Position.Z-((1.0/2.0)*RReach)))
                                    WinAPIWrapper.WinAPI.MouseMove(RPositionX, RPositionY);
                                txtPositionX.Text = RPositionX.ToString();
                                txtPositionY.Text = RPositionY.ToString();
                                //txtRightHand.Text = RightHand.Last().Position.X.ToString() + " , " + RightHand.Last().Position.Y.ToString();
                            }
                            txtLReach.Text = LReach.ToString();
                            txtLTravel.Text = LTravel.ToString();                            
                            txtRReach.Text = RReach.ToString();
                            txtRTravel.Text = RTravel.ToString();
                        }
                    }
                    #endregion
                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                    heartbeat += 1;
                    lblClock.Text = "Clock: " + heartbeat.ToString();
                    if (heartbeat > 1000)
                    {
                        heartbeat = 0;
                    }
                }
            }
            private void AddJoints(List<Joint> JointData,JointCollection JointRaw, JointType JointDef)
            {
                IEnumerable<Joint> Results = JointRaw.Where(s => s.JointType == JointDef).Where(s => s.TrackingState == JointTrackingState.Tracked);
                if (Results.Count() > 0)
                    JointData.Add(Results.First());
                while(JointData.Count>10)
                {
                    JointData.Remove(JointData.First());
                }
            }
        #endregion

        #region "Drawing"
                // Maps a SkeletonPoint to lie within our render space and converts to Point(point to map) Returns:mapped point
                private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
                {
                    // Convert point to depth space.  
                    // We are not using depth directly, but we do want the points in our 640x480 output resolution.
                    DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(
                                                                                     skelpoint,
                                                                                     DepthImageFormat.Resolution640x480Fps30);
                    return new Point(depthPoint.X, depthPoint.Y);
                }
                // Draws a skeleton's bones and joints(skeleton to draw, drawing context to draw to)
                private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
                {
                    // Render Torso
                    this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
                    this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
                    this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
                    this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
                    this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
                    this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
                    this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

                    // Left Arm
                    this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
                    this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
                    this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

                    // Right Arm
                    this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
                    this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
                    this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

                    // Left Leg
                    this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
                    this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
                    this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

                    // Right Leg
                    this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
                    this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
                    this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

                    // Render Joints
                    foreach (Joint joint in skeleton.Joints)
                    {
                        Brush drawBrush = null;

                        if (joint.TrackingState == JointTrackingState.Tracked)
                        {
                            drawBrush = this.trackedJointBrush;
                        }
                        else if (joint.TrackingState == JointTrackingState.Inferred)
                        {
                            drawBrush = this.inferredJointBrush;
                        }

                        if (drawBrush != null)
                        {
                            drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                        }
                    }
                }
                // Draws a bone line between two joints (skeleton to draw bones from, drawing context to draw to, joint to start drawing from, joint to end drawing at)
                private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
                {
                    Joint joint0 = skeleton.Joints[jointType0];
                    Joint joint1 = skeleton.Joints[jointType1];

                    // If we can't find either of these joints, exit
                    if (joint0.TrackingState == JointTrackingState.NotTracked ||
                        joint1.TrackingState == JointTrackingState.NotTracked)
                    {
                        return;
                    }

                    // Don't draw if both points are inferred
                    if (joint0.TrackingState == JointTrackingState.Inferred &&
                        joint1.TrackingState == JointTrackingState.Inferred)
                    {
                        return;
                    }

                    // We assume all drawn bones are inferred unless BOTH joints are tracked
                    Pen drawPen = this.inferredBonePen;
                    if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
                    {
                        drawPen = this.trackedBonePen;
                    }

                    drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
                }
                // Draws indicators to show which edges are clipping skeleton data (skeleton to draw clipping information for, drawing context to draw to)
                private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
                {
                    if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
                    {
                        drawingContext.DrawRectangle(
                            Brushes.Red,
                            null,
                            new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
                    }

                    if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
                    {
                        drawingContext.DrawRectangle(
                            Brushes.Red,
                            null,
                            new Rect(0, 0, RenderWidth, ClipBoundsThickness));
                    }

                    if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
                    {
                        drawingContext.DrawRectangle(
                            Brushes.Red,
                            null,
                            new Rect(0, 0, ClipBoundsThickness, RenderHeight));
                    }

                    if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
                    {
                        drawingContext.DrawRectangle(
                            Brushes.Red,
                            null,
                            new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
                    }
                }
        #endregion

        #region "Form Control handlers"
            /// <summary>
            /// Handles the checking or unchecking of the seated mode combo box
            /// </summary>
            /// <param name="sender">object sending the event</param>
            /// <param name="e">event arguments</param>
            private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
            {
                if (null != this.sensor)
                {
                    if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                        {myKinect.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;}
                    else
                        {myKinect.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;}
                }
            }
        #endregion
    }
}
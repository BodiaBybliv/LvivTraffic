using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.Structure;
using Emgu.CV.VideoSurveillance;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using Emgu.CV.CvEnum;
//using Accord.Video.FFMPEG;
using Newtonsoft.Json;
using System.IO;

namespace LvivTraffic.Models
{
    public class VideoProcessing
    {
        ApplicationDbContext db = new ApplicationDbContext();
        public string directory { get; set; }
        public string Name{ get; set; }
        Capture cap;
        Mat frame;
        BackgroundSubtractorMOG2 sub;
        CvBlobDetector detect;
        CvBlobs blobs;
        CvTracks tracks;
        List<String> carid;
        int framecount = 0;
        double frames = 0;
        double framerate = 0;
        double timecount = 0;
        int cameraId;
        int numberOfLines;
        public int px1 { get; set; }
        public int px2 { get; set; }
        public int py1 { get; set; }
        public int py2 { get; set; }

        int carcount;
        Image<Bgr, byte> currentframe = null;
        public VideoProcessing()
        {
            sub = new BackgroundSubtractorMOG2();
            detect = new CvBlobDetector();
            blobs = new CvBlobs();
            tracks = new CvTracks();
            carid = new List<string>();
        }
        public VideoProcessing(string dir, string name, int id, int number)
        {
            directory = dir;
            Name = name;
            cap = new Capture(dir + name);
            frame = new Mat();
            carcount = 0;
            sub = new BackgroundSubtractorMOG2();
            detect = new CvBlobDetector();
            blobs = new CvBlobs();
            tracks = new CvTracks();
            carid = new List<string>();
            cameraId = id;
            numberOfLines = number;
        }
        public Image<Bgr,byte> GetImage()
        {
            cap = new Capture(directory + Name);
            currentframe = cap.QueryFrame().ToImage<Bgr, byte>();
            return currentframe;
        }
        public int GetCarCount()
        {
            return carcount;
        }
        public void Process()
        {
            cap = new Capture(directory + Name);
            cap.QueryFrame();
            frames = cap.GetCaptureProperty(CapProp.FrameCount);
            framerate = Math.Round(cap.GetCaptureProperty(CapProp.Fps));
            //using (StreamWriter streamWriter = new StreamWriter("D:/Games/Frames.txt", false))
            //{
            //    streamWriter.WriteLine(framerate);
            //}
            cap.ImageGrabbed += ProcessFrame;
            cap.Start();
            
        }
        private void ProcessFrame(object sender, EventArgs e)
        {

            Point p1 = new Point(px1, py1);
            Point p2 = new Point(px2, py2);


            if (cap != null)
            {

                cap.Retrieve(frame, 0);
                currentframe = frame.ToImage<Bgr, byte>();


                Mat mask = new Mat();
                sub.Apply(currentframe, mask);

                Mat kernelOp = new Mat();
                Mat kernelCl = new Mat();
                Mat kernelEl = new Mat();
                Mat Dilate = new Mat();
                kernelOp = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
                kernelCl = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(11, 11), new Point(-1, -1));
                var element = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(3, 3), new Point(-1, -1));

                CvInvoke.GaussianBlur(mask, mask, new Size(13, 13), 1.5);
                CvInvoke.MorphologyEx(mask, mask, MorphOp.Open, kernelOp, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
                CvInvoke.MorphologyEx(mask, mask, MorphOp.Close, kernelCl, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
                CvInvoke.Dilate(mask, mask, element, new Point(-1, -1), 1, BorderType.Reflect, default(MCvScalar));
                CvInvoke.Threshold(mask, mask, 127, 255, ThresholdType.Binary);

                detect.Detect(mask.ToImage<Gray, byte>(), blobs);
                blobs.FilterByArea(500, 20000);
                tracks.Update(blobs, 20.0, 1, 10);

                Image<Bgr, byte> result = new Image<Bgr, byte>(currentframe.Size);
                using (Image<Gray, Byte> blobMask = detect.DrawBlobsMask(blobs))
                {
                    frame.CopyTo(result, blobMask);
                }
                CvInvoke.Line(currentframe, p1, p2, new MCvScalar(0, 0, 255), 2);

                foreach (KeyValuePair<uint, CvTrack> pair in tracks)
                {
                    
                    if (pair.Value.Inactive == 0) //only draw the active tracks.
                    {
                        
                        int cx = Convert.ToInt32(pair.Value.Centroid.X);
                        int cy = Convert.ToInt32(pair.Value.Centroid.Y);

                        CvBlob b = blobs[pair.Value.BlobLabel];
                        Bgr color = detect.MeanColor(b, frame.ToImage<Bgr, Byte>());
                        result.Draw(pair.Key.ToString(), pair.Value.BoundingBox.Location, FontFace.HersheySimplex, 0.5, color);
                        currentframe.Draw(pair.Value.BoundingBox, new Bgr(0, 0, 255), 1);
                        Point[] contour = b.GetContour();
                        //result.Draw(contour, new Bgr(0, 0, 255), 1);

                        Point center = new Point(cx, cy);
                        CvInvoke.Circle(currentframe, center, 1, new MCvScalar(255, 0, 0), 2);

                        if (p1.Y == p2.Y)
                        {
                            if (p1.X < p2.X)
                            {
                                if (center.X <= p2.X && center.X > p1.X && Math.Abs(center.Y - p1.Y) <= 10)
                                {
                                    if (pair.Key.ToString() != "")
                                    {
                                        if (!carid.Contains(pair.Key.ToString()))
                                        {
                                            carid.Add(pair.Key.ToString());
                                            if (carid.Count == 10)
                                            {
                                                carid.Clear();
                                            }

                                            carcount++;

                                        }
                                    }
                                    //carcount++;
                                    CvInvoke.Line(currentframe, p1, p2, new MCvScalar(0, 255, 0), 2);

                                }
                            }
                            else
                            {
                                if (center.X <= p1.X && center.X > p2.X && Math.Abs(center.Y - p1.Y) <= 10)
                                {
                                    if (pair.Key.ToString() != "")
                                    {
                                        if (!carid.Contains(pair.Key.ToString()))
                                        {
                                            carid.Add(pair.Key.ToString());
                                            if (carid.Count == 10)
                                            {
                                                carid.Clear();
                                            }

                                            carcount++;

                                        }
                                    }
                                    //carcount++;
                                    CvInvoke.Line(currentframe, p1, p2, new MCvScalar(0, 255, 0), 2);

                                }
                            }
                        }
                        else
                        {
                            if (p1.X == p2.X)
                            {
                                if (p1.Y < p2.Y)
                                {
                                    if (center.Y >= p1.Y && center.Y < p2.Y && Math.Abs(center.X - p1.X) <= 10)
                                    {
                                        if (pair.Key.ToString() != "")
                                        {
                                            if (!carid.Contains(pair.Key.ToString()))
                                            {
                                                carid.Add(pair.Key.ToString());
                                                if (carid.Count == 10)
                                                {
                                                    carid.Clear();
                                                }

                                                carcount++;

                                            }
                                        }
                                        //carcount++;
                                        CvInvoke.Line(currentframe, p1, p2, new MCvScalar(0, 255, 0), 2);

                                    }
                                }
                                else
                                {
                                    if (center.Y >= p2.Y && center.Y < p1.Y && Math.Abs(center.X - p1.X) <= 10)
                                    {
                                        if (pair.Key.ToString() != "")
                                        {
                                            if (!carid.Contains(pair.Key.ToString()))
                                            {
                                                carid.Add(pair.Key.ToString());
                                                if (carid.Count == 10)
                                                {
                                                    carid.Clear();
                                                }

                                                carcount++;

                                            }
                                        }
                                        //carcount++;
                                        CvInvoke.Line(currentframe, p1, p2, new MCvScalar(0, 255, 0), 2);

                                    }
                                }
                            }
                            else
                            {
                                if (p1.X < p2.X)
                                {
                                    if (p1.Y < p2.Y)
                                    {
                                        if (center.Y > p1.Y && center.Y < p2.Y && center.X < p2.X && center.X > p1.X)
                                        {
                                            if (pair.Key.ToString() != "")
                                            {
                                                if (!carid.Contains(pair.Key.ToString()))
                                                {
                                                    carid.Add(pair.Key.ToString());
                                                    if (carid.Count == 10)
                                                    {
                                                        carid.Clear();
                                                    }

                                                    carcount++;
                                                    

                                                }

                                            }
                                            //carcount++;
                                            CvInvoke.Line(currentframe, p1, p2, new MCvScalar(0, 255, 0), 2);


                                        }
                                    }
                                    else
                                    {
                                        if (center.Y <= p1.Y && center.Y > p2.Y && center.X <= p2.X && center.X > p1.X)
                                        {
                                            if (pair.Key.ToString() != "")
                                            {
                                                if (!carid.Contains(pair.Key.ToString()))
                                                {
                                                    carid.Add(pair.Key.ToString());
                                                    if (carid.Count == 10)
                                                    {
                                                        carid.Clear();
                                                    }

                                                    carcount++;


                                                }

                                            }
                                            //carcount++;
                                            CvInvoke.Line(currentframe, p1, p2, new MCvScalar(0, 255, 0), 2);


                                        }
                                    }
                                }
                                else
                                {
                                    if (p1.Y > p2.Y)
                                    {
                                        if (center.Y <= p1.Y && center.Y > p2.Y && center.X <= p1.X && center.X > p2.X)
                                        {
                                            if (pair.Key.ToString() != "")
                                            {
                                                if (!carid.Contains(pair.Key.ToString()))
                                                {
                                                    carid.Add(pair.Key.ToString());
                                                    if (carid.Count == 10)
                                                    {
                                                        carid.Clear();
                                                    }

                                                    carcount++;


                                                }

                                            }
                                            //carcount++;
                                            CvInvoke.Line(currentframe, p1, p2, new MCvScalar(0, 255, 0), 2);


                                        }
                                    }
                                    else
                                    {
                                        if (center.Y >= p1.Y && center.Y < p2.Y && center.X <= p1.X && center.X > p2.X)
                                        {
                                            if (pair.Key.ToString() != "")
                                            {
                                                if (!carid.Contains(pair.Key.ToString()))
                                                {
                                                    carid.Add(pair.Key.ToString());
                                                    if (carid.Count == 10)
                                                    {
                                                        carid.Clear();
                                                    }

                                                    carcount++;


                                                }

                                            }
                                            //carcount++;
                                            CvInvoke.Line(currentframe, p1, p2, new MCvScalar(0, 255, 0), 2);


                                        }
                                    }
                                }
                            }
                        }


                    }

                }


                CvInvoke.PutText(currentframe, "Count :" + carcount.ToString(), new Point(10, 25), FontFace.HersheySimplex, 1, new MCvScalar(255, 0, 255), 2, LineType.AntiAlias);
                //try
                //{
                //    timecount++;
                //    if (timecount == framerate * 10)
                //    {
                //        var processingCamera = db.ProcessingCameras.Find(cameraId);
                //        var camera = db.Cameras.Find(processingCamera.CameraId);
                //        var marker = db.Markers.Find(camera.MarkerId);
                //        double congestion= Math.Round(carcount / (double)numberOfLines / 5.0, 1);
                //        if (marker.Congestion < congestion)
                //        {
                //            double diff = congestion - marker.Congestion;
                //            double percent = diff * 100 / (double)marker.Congestion;
                //            if (percent > 10)
                //            {
                //                marker.Message = "Кількість автомобілів збільшується";
                //            }
                //            else
                //            {
                //                marker.Message = "Кількість автомобілів не змінюється";
                //            }
                //            marker.CarCount += carcount;
                //            marker.Congestion = Math.Round(carcount / (double)numberOfLines / 5.0, 1);
                //            //marker.Percent = percent;
                //            Description description = new Description();
                //            marker.Description = description.descriptions[marker.Congestion];
                //            Colors colors = new Colors();
                //            marker.Color = colors.colors[marker.Congestion];
                //        }
                //        else
                //        {
                //            if (marker.Congestion == congestion)
                //            {
                //                marker.Message = "Кількість автомобілів не змінюється";
                //                marker.CarCount += carcount;
                //            }
                //            else
                //            {
                //                double diff = marker.Congestion - congestion;
                //                double percent = diff * 100 / (double)marker.Congestion;
                //                if (percent > 10)
                //                {
                //                    marker.Message = "Кількість автомобілів зменшується";
                //                }
                //                else
                //                {
                //                    marker.Message = "Кількість автомобілів не змінюється";
                //                }
                //                marker.CarCount += carcount;
                //                marker.Congestion = Math.Round(carcount / (double)numberOfLines / 5.0, 1);
                //                //marker.Percent = percent;
                //                Description description = new Description();
                //                marker.Description = description.descriptions[marker.Congestion];
                //                Colors colors = new Colors();
                //                marker.Color = colors.colors[marker.Congestion];
                //            }
                //        }
                //        //processingCamera.CarCount += carcount / numberOfLines;
                //        //marker.CarCount += carcount / numberOfLines;
                //        db.SaveChanges();
                //        timecount = 0;
                //        //using (StreamWriter streamWriter = new StreamWriter($"D:/Games/{Name}.txt", false))
                //        //{
                //        //    streamWriter.WriteLine(carcount);
                //        //}
                //        carcount = 0;
                //    }
                //    framecount++;
                //    if (frames - 1 == framecount)
                //    {
                //        var processingCamera = db.ProcessingCameras.Find(cameraId);
                //        var camera = db.Cameras.Find(processingCamera.CameraId);
                //        var marker = db.Markers.Find(camera.MarkerId);
                //        marker.CarCount += carcount;

                //        timecount = 0;

                //        carcount = 0;


                //        //----!!!!!!!!!!!!!!!-----
                //        db.ProcessingCameras.Remove(processingCamera);
                //        //----!!!!!!!!!!!!!!!-----
                //        db.SaveChanges();

                //    }
                //}
                //catch (Exception ex)
                //{
                //    cap.Stop();
                //}


                try
                {
                    timecount++;
                    if (timecount == framerate * 10)
                    {
                        var processingCamera = db.ProcessingCameras.Find(cameraId);
                        var camera = db.Cameras.Find(processingCamera.CameraId);
                        var marker = db.Markers.Find(camera.MarkerId);
                        if (marker.CarCount < carcount)
                        {
                            int diff = carcount - marker.CarCount;
                            double percent = diff * 100 / (double)marker.CarCount;
                            if (percent > 10)
                            {
                                marker.Message = "Кількість автомобілів збільшується";
                            }
                            else
                            {
                                marker.Message = "Кількість автомобілів не змінюється";
                            }
                            marker.CarCount += carcount;
                            marker.Congestion = Math.Round(carcount / (double)numberOfLines / 5.0, 1);
                            //marker.Percent = percent;
                            Description description = new Description();
                            marker.Description = description.descriptions[marker.Congestion];
                            Colors colors = new Colors();
                            marker.Color = colors.colors[marker.Congestion];
                            //marker.Image = currentframe.Bytes;
                            currentframe.Save(directory + $"LvivTraffic/Images/{marker.Name}line.bmp");
                        }
                        else
                        {
                            if (marker.CarCount == carcount)
                            {
                                //marker.Image = currentframe.Bytes;
                                marker.Message = "Кількість автомобілів не змінюється";
                                currentframe.Save(directory + $"LvivTraffic/Images/{marker.Name}line.bmp");
                            }
                            else
                            {
                                int diff = marker.CarCount - carcount;
                                double percent = diff * 100 / (double)marker.CarCount;
                                if (percent > 10)
                                {
                                    marker.Message = "Кількість автомобілів зменшується";
                                }
                                else
                                {
                                    marker.Message = "Кількість автомобілів не змінюється";
                                }
                                currentframe.Save(directory + $"LvivTraffic/Images/{marker.Name}line.bmp");
                                marker.CarCount = carcount;
                                marker.Congestion = Math.Round(carcount / (double)numberOfLines / 5.0, 1);
                                //marker.Percent = percent;
                                Description description = new Description();
                                marker.Description = description.descriptions[marker.Congestion];
                                Colors colors = new Colors();
                                marker.Color = colors.colors[marker.Congestion];
                                //marker.Image = currentframe.Bytes;
                            }
                            if (marker.Congestion>1)
                            {
                                int diff = carcount - marker.CarCount;
                                double percent = diff * 100 / (double)marker.CarCount;
                                if (percent > 10)
                                {
                                    marker.Message = "Кількість автомобілів збільшується";
                                }
                                else
                                {
                                    marker.Message = "Кількість автомобілів не змінюється";
                                }
                                marker.CarCount += carcount;
                                marker.Congestion = Math.Round(carcount / (double)numberOfLines / 5.0, 1);
                                //marker.Percent = percent;
                                Description description = new Description();
                                marker.Description = description.descriptions[marker.Congestion];
                                Colors colors = new Colors();
                                marker.Color = colors.colors[marker.Congestion];
                                marker.Image = currentframe.Bytes;
                            }
                        }
                        db.SaveChanges();
                        timecount = 0;
                        carcount = 0;
                    }
                    framecount++;
                    if (frames - 1 == framecount)
                    {
                        var processingCamera = db.ProcessingCameras.Find(cameraId);
                        var camera = db.Cameras.Find(processingCamera.CameraId);
                        var marker = db.Markers.Find(camera.MarkerId);
                        marker.CarCount += carcount / numberOfLines;
                        timecount = 0;

                        carcount = 0;


                        //----!!!!!!!!!!!!!!!-----
                        db.ProcessingCameras.Remove(processingCamera);
                        //----!!!!!!!!!!!!!!!-----
                        db.SaveChanges();
                        currentframe.Save(directory + $"LvivTraffic/Images/{marker.Name}line.bmp");
                    }
                }
                catch (Exception ex)
                {
                    cap.Stop();
                }

                //try
                //{
                //    timecount++;
                //    if (timecount == framerate * 10)
                //    {
                //        var processingCamera = db.ProcessingCameras.Find(cameraId);
                //        var camera = db.Cameras.Find(processingCamera.CameraId);
                //        var marker = db.Markers.Find(camera.MarkerId);
                //        processingCamera.CarCount += carcount;
                //        marker.CarCount += carcount;
                //        marker.Image = currentframe.Bytes;
                //        db.SaveChanges();
                //        timecount = 0;
                //        carcount = 0;
                //    }
                //    framecount++;
                //    if (frames - 1 == framecount)
                //    {
                //        var processingCamera = db.ProcessingCameras.Find(cameraId);
                //        var camera = db.Cameras.Find(processingCamera.CameraId);
                //        var marker = db.Markers.Find(camera.MarkerId);
                //        marker.CarCount += carcount;
                //        marker.Image = currentframe.Bytes;
                //        timecount = 0;

                //        carcount = 0;


                //        //----!!!!!!!!!!!!!!!-----
                //        db.ProcessingCameras.Remove(processingCamera);
                //        //----!!!!!!!!!!!!!!!-----
                //        db.SaveChanges();

                //    }
                //}
                //catch (Exception ex)
                //{
                //    cap.Stop();
                //}

            }

        }
    }
}
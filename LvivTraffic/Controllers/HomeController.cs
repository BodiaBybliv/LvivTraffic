using Emgu.CV;
using Emgu.CV.Structure;
using LvivTraffic.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Windows.Forms;
using System.Text.Json;

namespace LvivTraffic.Controllers
{
    public class HomeController : Controller
    {
        ApplicationDbContext db = new ApplicationDbContext();
        VideoProcessing videoProcessing = new VideoProcessing();
        public ActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public ActionResult SelectCamera(int id)
        {
            var camera = db.Cameras.Find(id);
            videoProcessing.directory = camera.DirectoryName + "Cameras/";
            videoProcessing.Name = camera.Name;
            var image = videoProcessing.GetImage();
            image.Save(camera.DirectoryName + "LvivTraffic/TempImages/1.bmp");
            image.Save(camera.DirectoryName + $"LvivTraffic/Images/{camera.Name}.bmp");
            ProcessingCamera processingCamera = new ProcessingCamera()
            {
                CameraId = camera.Id
            };
            db.ProcessingCameras.Add(processingCamera);
            var marker = db.Markers.ToList().Last();
            camera.MarkerId = marker.Id;
            marker.Name = camera.Name;
            db.SaveChanges();
            return RedirectToAction("About");
        }
        [HttpPost]
        public ActionResult AddLine(int x1, int y1, int x2, int y2)
        {
            var processingCamera = db.ProcessingCameras.ToList().Last();
            var camera = db.Cameras.Find(processingCamera.CameraId);
            var directory = camera.DirectoryName;
            Image<Bgr, byte> image = new Image<Bgr, byte>(directory + "LvivTraffic/TempImages/1.bmp");
            int height = image.Height;
            double scale = height / 350.0;
            
            processingCamera.Px1 = (int)(x1 * scale);
            processingCamera.Py1 = (int)(y1 * scale);
            processingCamera.Px2 = (int)(x2 * scale);
            processingCamera.Py2 = (int)(y2 * scale);
            db.SaveChanges();
            Point p1 = new Point((int)(x1 * scale), (int)(y1 * scale));
            Point p2 = new Point((int)(x2 * scale), (int)(y2 * scale));
            CvInvoke.Line(image, p1, p2, new MCvScalar(0, 0, 255), 2);
            image.Save(directory + "LvivTraffic/TempImages/line.bmp");
            image.Save(directory + $"LvivTraffic/Images/{camera.Name}line.bmp");
            return RedirectToAction("Confirm");
        }
        [HttpPost]
        public ActionResult AddCameraToMap(string Latitude, string Longitude, string Street)
        {
            Marker marker = new Marker();
            marker.Latitude = Latitude;
            marker.Longitude = Longitude;
            marker.Street = Street;
            marker.Message = "";
            marker.Percent = 0;
            marker.Description = "";
            marker.Color = "green";
            db.Markers.Add(marker);
            db.SaveChanges();
            return RedirectToAction("ListCameras");
        }
        public ActionResult ListCameras()
        {
            CreateImages();
            var cameras = (from c in db.Cameras where c.IsAdded == false select c).OrderBy(c => c.Name);
            ViewBag.Cameras = cameras;
            return View();
        }
        public void CreateImages()
        {
            var cameras = db.Cameras.ToList();
            foreach (var camera in cameras)
            {
                VideoProcessing processing = new VideoProcessing();
                processing.directory = camera.DirectoryName + "Cameras/";
                processing.Name = camera.Name;
                var image = processing.GetImage();
                image.Save(camera.DirectoryName + $"LvivTraffic/CameraImages/{camera.Name}.bmp");
            }
        }
        public ActionResult Confirm()
        {
            return View();
        }
        public ActionResult CameraManage()
        {
            var processingCameras = db.ProcessingCameras.ToList();
            List<Camera> cameras = new List<Camera>();
            foreach (var processingCamera in processingCameras)
            {
                var camera = db.Cameras.Find(processingCamera.CameraId);
                cameras.Add(camera);
            }
            List<Marker> markers = new List<Marker>();
            foreach (var camera in cameras)
            {
                var marker = db.Markers.Find(camera.MarkerId);
                markers.Add(marker);
            }
            ViewBag.Cameras = markers;
            return View();
        }
        [HttpGet]
        public ActionResult Delete(int id)
        {
            var camera = db.Cameras.First(c => c.MarkerId == id);
            var processingCamera = db.ProcessingCameras.First(p => p.CameraId == camera.Id);
            var marker = db.Markers.Find(id);
            db.Markers.Remove(marker);
            camera.IsAdded = false;
            db.ProcessingCameras.Remove(processingCamera);
            db.SaveChanges();
            return RedirectToAction("CameraManage");
        }
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var camera = db.Cameras.Find(id);
            var processingCamera = db.ProcessingCameras.First(p => p.CameraId == id);
            var marker = db.Markers.Find(camera.MarkerId);

            db.Markers.Remove(marker);
            camera.IsAdded = false;
            db.ProcessingCameras.Remove(processingCamera);
            db.SaveChanges();
            return RedirectToAction("CameraManage");
        }
        [HttpGet]
        public ActionResult ShowMarker(int id)
        {
            var marker = db.Markers.Find(id);
            ViewBag.Latitude = marker.Latitude;
            ViewBag.Longitude = marker.Longitude;
            return View();
        }
        [HttpPost]
        public ActionResult AddNumber(int numberOfLines)
        {
            var processingCamera = db.ProcessingCameras.ToList().Last();
            var camera = db.Cameras.Find(processingCamera.CameraId);
            camera.IsAdded = true;
            videoProcessing = new VideoProcessing(camera.DirectoryName + "Cameras/", camera.Name, processingCamera.Id, numberOfLines);
            videoProcessing.px1 = processingCamera.Px1;
            videoProcessing.py1 = processingCamera.Py1;
            videoProcessing.px2 = processingCamera.Px2;
            videoProcessing.py2 = processingCamera.Py2;
            
            videoProcessing.Process();
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        public FileContentResult ShowImage(int id)
        {
            var marker = db.Markers.Find(id);
            return new FileContentResult(marker.Image, "image/bmp");
        }
        [Route("GetData")]
        public JsonResult GetData()
        {
            var markers = db.Markers.ToList();
            return Json(markers, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Statistic()
        {
            var processingCameras = db.ProcessingCameras.ToList();
            List<Camera> cameras = new List<Camera>();
            foreach (var processingCamera in processingCameras)
            {
                var camera = db.Cameras.Find(processingCamera.CameraId);
                cameras.Add(camera);
            }
            List<Marker> markers = new List<Marker>();
            foreach (var camera in cameras)
            {
                var marker = db.Markers.Find(camera.MarkerId);
                markers.Add(marker);
            }
            ViewBag.Cameras = markers;
            ViewBag.Count = markers.Count();
            ViewBag.Clean = markers.Count(m => m.Congestion >= 0.0 && m.Congestion < 0.3);
            ViewBag.Traction = markers.Count(m => m.Congestion >= 0.3 && m.Congestion < 0.7);
            ViewBag.Congestion = markers.Count(m => m.Congestion >= 0.7 && m.Congestion <= 1.0);
            return View();
        }
        public ActionResult Map()
        {
            return View();
        }
        public ActionResult AddCamera()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase upload)
        {
            if (upload != null)
            {
                string fileName = System.IO.Path.GetFileName(upload.FileName);
                using (StreamWriter streamWriter=new StreamWriter("Fnk.txt",false))
                {
                    streamWriter.WriteLine(fileName);
                }
                Capture cap = new Capture(fileName);
                Mat frame = new Mat();
                cap.QueryFrame();
                cap.Retrieve(frame, 0);
                Image<Bgr, byte> currentframe = frame.ToImage<Bgr, byte>();
                currentframe.Save(Server.MapPath("~/Files/" + fileName + ".bmp"));
                //Image<Bgr, byte> result = new Image<Bgr, byte>(currentframe.Size);
            }
            return RedirectToAction("Index");
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
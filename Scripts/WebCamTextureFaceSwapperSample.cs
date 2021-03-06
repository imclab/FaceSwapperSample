﻿using DlibFaceLandmarkDetector;
using OpenCVForUnity;
using OpenCVForUnity.RectangleTrack;
using OpenCVForUnity.FaceSwap;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_5_3
using UnityEngine.SceneManagement;
#endif

namespace FaceSwapperSample
{
	/// <summary>
	/// WebCamTexture face swapper sample.
	/// </summary>
	[RequireComponent(typeof(WebCamTextureToMatHelper))]
	public class WebCamTextureFaceSwapperSample : MonoBehaviour
	{

		/// <summary>
		/// The colors.
		/// </summary>
		Color32[] colors;

		/// <summary>
		/// The gray mat.
		/// </summary>
		Mat grayMat;

		/// <summary>
		/// The texture.
		/// </summary>
		Texture2D texture;

		/// <summary>
		/// The cascade.
		/// </summary>
		CascadeClassifier cascade;

		/// <summary>
		/// The web cam texture to mat helper.
		/// </summary>
		WebCamTextureToMatHelper webCamTextureToMatHelper;

		/// <summary>
		/// The face landmark detector.
		/// </summary>
		FaceLandmarkDetector faceLandmarkDetector;
		/// <summary>
		/// The face Swaper.
		/// </summary>
		DlibFaceSwapper faceSwapper;

		/// <summary>
		/// The detection based tracker.
		/// </summary>
		RectangleTracker rectangleTracker;

		/// <summary>
		/// The frontal face parameter.
		/// </summary>
		FrontalFaceParam frontalFaceParam;

		/// <summary>
		/// The is showing face rects.
		/// </summary>
		public bool isShowingFaceRects = false;
		
		/// <summary>
		/// The is showing face rects toggle.
		/// </summary>
		public Toggle isShowingFaceRectsToggle;

		/// <summary>
		/// The use Dlib face detector flag.
		/// </summary>
		public bool useDlibFaceDetecter = false;

		/// <summary>
		/// The use dlib face detecter toggle.
		/// </summary>
		public Toggle useDlibFaceDetecterToggle;

		/// <summary>
		/// The is filtering non frontal faces.
		/// </summary>
		public bool isFilteringNonFrontalFaces;
		
		/// <summary>
		/// The is filtering non frontal faces toggle.
		/// </summary>
		public Toggle isFilteringNonFrontalFacesToggle;
		
		/// <summary>
		/// The frontal face rate lower limit.
		/// </summary>
		[Range(0.0f, 1.0f)]
		public float
			frontalFaceRateLowerLimit;

		/// <summary>
		/// The use seamless clone for paste faces.
		/// </summary>
		public bool useSeamlessCloneForPasteFaces = false;
		
		/// <summary>
		/// The use seamless clone for paste faces toggle.
		/// </summary>
		public Toggle useSeamlessCloneForPasteFacesToggle;

		/// <summary>
		/// The enable tracker flag.
		/// </summary>
		public bool enableTracking = true;

		/// <summary>
		/// The enable tracking toggle.
		/// </summary>
		public Toggle enableTrackingToggle;

		/// <summary>
		/// The is showing debug face points.
		/// </summary>
		public bool isShowingDebugFacePoints = false;
		
		/// <summary>
		/// The is showing debug face points toggle.
		/// </summary>
		public Toggle isShowingDebugFacePointsToggle;


		// Use this for initialization
		void Start ()
		{
			rectangleTracker = new RectangleTracker ();

			faceLandmarkDetector = new FaceLandmarkDetector (DlibFaceLandmarkDetector.Utils.getFilePath ("shape_predictor_68_face_landmarks.dat"));

			faceSwapper = new DlibFaceSwapper ();
			faceSwapper.useSeamlessCloneForPasteFaces = useSeamlessCloneForPasteFaces;
			faceSwapper.isShowingDebugFacePoints = isShowingDebugFacePoints;

			webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();
			webCamTextureToMatHelper.Init ();

			isShowingFaceRectsToggle.isOn = isShowingFaceRects;
			useDlibFaceDetecterToggle.isOn = useDlibFaceDetecter;
			isFilteringNonFrontalFacesToggle.isOn = isFilteringNonFrontalFaces;
			useSeamlessCloneForPasteFacesToggle.isOn = useSeamlessCloneForPasteFaces;
			enableTrackingToggle.isOn = enableTracking;
			isShowingDebugFacePointsToggle.isOn = isShowingDebugFacePoints;
		}

		/// <summary>
		/// Raises the web cam texture to mat helper inited event.
		/// </summary>
		public void OnWebCamTextureToMatHelperInited ()
		{
			Debug.Log ("OnWebCamTextureToMatHelperInited");

			Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();

			colors = new Color32[webCamTextureMat.cols () * webCamTextureMat.rows ()];
			texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);


			gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
			Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);
			
			float width = gameObject.transform.localScale.x;
			float height = gameObject.transform.localScale.y;
			
			float widthScale = (float)Screen.width / width;
			float heightScale = (float)Screen.height / height;
			if (widthScale < heightScale) {
				Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
			} else {
				Camera.main.orthographicSize = height / 2;
			}
			
			gameObject.GetComponent<Renderer> ().material.mainTexture = texture;



			grayMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC1);
			cascade = new CascadeClassifier (OpenCVForUnity.Utils.getFilePath ("haarcascade_frontalface_alt.xml"));
			if (cascade.empty ()) {
				Debug.LogError ("cascade file is not loaded.Please copy from “FaceTrackerSample/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
			}

			frontalFaceParam = new FrontalFaceParam ();
		}

		/// <summary>
		/// Raises the web cam texture to mat helper disposed event.
		/// </summary>
		public void OnWebCamTextureToMatHelperDisposed ()
		{
			Debug.Log ("OnWebCamTextureToMatHelperDisposed");

			grayMat.Dispose ();

			rectangleTracker.Reset ();
		}

		// Update is called once per frame
		void Update ()
		{

			if (webCamTextureToMatHelper.isPlaying () && webCamTextureToMatHelper.didUpdateThisFrame ()) {

				Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

				//face detection
				List<OpenCVForUnity.Rect> detectResult = new List<OpenCVForUnity.Rect> ();
				if (useDlibFaceDetecter) {
					OpenCVForUnityUtils.SetImage (faceLandmarkDetector, rgbaMat);
					List<UnityEngine.Rect> result = faceLandmarkDetector.Detect ();

					foreach (var unityRect in result) {
						detectResult.Add (new OpenCVForUnity.Rect ((int)unityRect.x, (int)unityRect.y, (int)unityRect.width, (int)unityRect.height));
					}
				} else {
					//convert image to greyscale
					Imgproc.cvtColor (rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

					//convert image to greyscale
					using (Mat equalizeHistMat = new Mat())
					using (MatOfRect faces = new MatOfRect()) {
						Imgproc.equalizeHist (grayMat, equalizeHistMat);

						cascade.detectMultiScale (equalizeHistMat, faces, 1.1f, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE, new OpenCVForUnity.Size (equalizeHistMat.cols () * 0.15, equalizeHistMat.cols () * 0.15), new Size ());

						detectResult = faces.toList ();
					}
				}

				//face traking
				if (enableTracking) {
					rectangleTracker.UpdateTrackedObjects (detectResult);
					detectResult = new List<OpenCVForUnity.Rect> ();
					rectangleTracker.GetObjects (detectResult, true);
				}

				//face landmark detection
				OpenCVForUnityUtils.SetImage (faceLandmarkDetector, rgbaMat);
				List<List<Vector2>> landmarkPoints = new List<List<Vector2>> ();
				foreach (var openCVRect in detectResult) {
					UnityEngine.Rect rect = new UnityEngine.Rect (openCVRect.x, openCVRect.y, openCVRect.width, openCVRect.height);

					List<Vector2> points = faceLandmarkDetector.DetectLandmark (rect);
					if (points.Count > 0) {
						//OpenCVForUnityUtils.DrawFaceLandmark(rgbaMat, points, new Scalar(0, 255, 0, 255), 2);
						landmarkPoints.Add (points);
					}
				}

				//filter nonfrontalface
				if (isFilteringNonFrontalFaces) {
					for (int i = 0; i < landmarkPoints.Count; i++) {
						if (frontalFaceParam.getFrontalFaceRate (landmarkPoints [i]) < frontalFaceRateLowerLimit) {
							detectResult.RemoveAt (i);
							landmarkPoints.RemoveAt (i);
							i--;
						}
					}
				}

				//face swapping
				if (landmarkPoints.Count >= 2) {
					int ann = 0, bob = 1;
					for (int i = 0; i < landmarkPoints.Count-1; i += 2) {
						ann = i;
						bob = i + 1;
						
						faceSwapper.SwapFaces (rgbaMat, landmarkPoints [ann], landmarkPoints [bob], 1);
					}
				}

				//draw face rects
				if (isShowingFaceRects) {
					for (int i = 0; i < detectResult.Count; i++) {
						UnityEngine.Rect rect = new UnityEngine.Rect (detectResult [i].x, detectResult [i].y, detectResult [i].width, detectResult [i].height);
						OpenCVForUnityUtils.DrawFaceRect (rgbaMat, rect, new Scalar (255, 0, 0, 255), 2);
//						Imgproc.putText (rgbaMat, " " + frontalFaceParam.getAngleOfFrontalFace (landmarkPoints [i]), new Point (rect.xMin, rect.yMin - 10), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
//						Imgproc.putText (rgbaMat, " " + frontalFaceParam.getFrontalFaceRate (landmarkPoints [i]), new Point (rect.xMin, rect.yMin - 10), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
					}
				}
                       


				Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);

				OpenCVForUnity.Utils.matToTexture2D (rgbaMat, texture, colors);

			}
		}


		/// <summary>
		/// Raises the disable event.
		/// </summary>
		void OnDisable ()
		{
			webCamTextureToMatHelper.Dispose ();

			rectangleTracker.Dispose ();
			faceLandmarkDetector.Dispose ();
			faceSwapper.Dispose ();
			frontalFaceParam.Dispose ();
		}

		/// <summary>
		/// Raises the back button event.
		/// </summary>
		public void OnBackButton ()
		{
#if UNITY_5_3
            SceneManager.LoadScene ("FaceSwapperSample");
#else
			Application.LoadLevel ("FaceSwapperSample");
#endif
		}

		/// <summary>
		/// Raises the play button event.
		/// </summary>
		public void OnPlayButton ()
		{
			webCamTextureToMatHelper.Play ();
		}

		/// <summary>
		/// Raises the pause button event.
		/// </summary>
		public void OnPauseButton ()
		{
			webCamTextureToMatHelper.Pause ();
		}

		/// <summary>
		/// Raises the stop button event.
		/// </summary>
		public void OnStopButton ()
		{
			webCamTextureToMatHelper.Stop ();
		}

		/// <summary>
		/// Raises the change camera button event.
		/// </summary>
		public void OnChangeCameraButton ()
		{
			webCamTextureToMatHelper.Init (null, webCamTextureToMatHelper.requestWidth, webCamTextureToMatHelper.requestHeight, !webCamTextureToMatHelper.requestIsFrontFacing);
		}

		/// <summary>
		/// Raises the is showing face rects toggle event.
		/// </summary>
		public void OnIsShowingFaceRectsToggle ()
		{
			if (isShowingFaceRectsToggle.isOn) {
				isShowingFaceRects = true;
			} else {
				isShowingFaceRects = false;
			}
		}

		/// <summary>
		/// Raises the use Dlib face detector toggle event.
		/// </summary>
		public void OnUseDlibFaceDetecterToggle ()
		{
			if (useDlibFaceDetecterToggle.isOn) {
				useDlibFaceDetecter = true;
			} else {
				useDlibFaceDetecter = false;
			}
		}

		/// <summary>
		/// Raises the is filtering non frontal faces toggle event.
		/// </summary>
		public void OnIsFilteringNonFrontalFacesToggle ()
		{
			if (isFilteringNonFrontalFacesToggle.isOn) {
				isFilteringNonFrontalFaces = true;
			} else {
				isFilteringNonFrontalFaces = false;
			}
		}

		/// <summary>
		/// Raises the use seamless clone for paste faces toggle event.
		/// </summary>
		public void OnUseSeamlessCloneForPasteFacesToggle ()
		{
			if (useSeamlessCloneForPasteFacesToggle.isOn) {
				useSeamlessCloneForPasteFaces = true;
			} else {
				useSeamlessCloneForPasteFaces = false;
			}
			faceSwapper.useSeamlessCloneForPasteFaces = useSeamlessCloneForPasteFaces;
		}

		/// <summary>
		/// Raises the enable tracking toggle event.
		/// </summary>
		public void OnEnableTrackingToggle ()
		{
			if (enableTrackingToggle.isOn) {
				enableTracking = true;
			} else {
				enableTracking = false;
			}
		}

		/// <summary>
		/// Raises the is showing debug face points toggle event.
		/// </summary>
		public void OnIsShowingDebugFacePointsToggle ()
		{
			if (isShowingDebugFacePointsToggle.isOn) {
				isShowingDebugFacePoints = true;
			} else {
				isShowingDebugFacePoints = false;
			}
			faceSwapper.isShowingDebugFacePoints = isShowingDebugFacePoints;
		}
	}
}
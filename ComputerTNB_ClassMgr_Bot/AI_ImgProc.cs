using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputerTNB_ClassMgr_Bot.Models;
using Mysqlx.Notice;
using OpenCvSharp;
using OpenCvSharp.Face;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace ComputerTNB_ClassMgr_Bot
{
    /// <summary>
    /// Provides functionalities used for AI Image Processing.
    /// </summary>
    public class AI_ImgProc
    {
        #region AI_ImgProc_CONSTANTS

        public const string AI_IMG_MODEL_XML = @"haarcascade_frontalface_default.xml";
        public const string AI_IMG_DEFAULT_DATASET_FOLDER = @"dataset";
        public const string AI_IMG_DEFAULT_TEMP_FOLDER = @"temp";

        public const uint AI_LIMIT_IMAGES_LOAD = 256;

        #endregion

        #region AI_ImgProc_Variables

        public LBPHFaceRecognizer model;
        public CascadeClassifier faceCascade;

        public List<Mat> mats_Faces;
        public List<int> mats_Labels;

        #endregion

        #region AI_ImgProc_Methods

        /// <summary>
        /// Default ctor. Initializes AI models (Doesn't load images!).
        /// For training, call <see cref="AI_ImgProc.BeginTrain()"/> method.
        /// </summary>
        public AI_ImgProc()
        {
            model = LBPHFaceRecognizer.Create();
            faceCascade = new CascadeClassifier(AI_IMG_MODEL_XML);

            mats_Faces = new List<Mat>();
            mats_Labels = new List<int>();
        }

        /// <summary>
        /// Begins training process.
        /// </summary>
        /// <returns>This task returns nothing.</returns>
        public async Task BeginTrain()
        {
            if (Program.db == null)
                throw new NullReferenceException();

            // Clear MATs and labels.
            mats_Faces = new List<Mat>();
            mats_Labels = new List<int>();

            // Load default dataset.
            Logging.Log_Information("BEGIN -> Load default faces dataset...", "AI_IMG_PROC -> DEFAULT DATASET");
            var imageFiles = Directory.GetFiles(AI_IMG_DEFAULT_DATASET_FOLDER,
                "*.*", SearchOption.AllDirectories);
            uint loadsRemaining = AI_LIMIT_IMAGES_LOAD;
            foreach (var imageFile in imageFiles)
            {
                if (loadsRemaining == 0)
                    break;

                Mat matInfo = new Mat(imageFile, ImreadModes.Grayscale);
                if (matInfo.Rows <= 0 || matInfo.Cols <= 0)
                    continue;

                mats_Faces.Add(
                    matInfo
                    );
                mats_Labels.Add(0);

                Logging.Log_Information($"Loaded:  {imageFile}.", "AI_IMG_PROC -> DEFAULT DATASET");

                loadsRemaining--;
            }
            Logging.Log_Information("END -> Load default faces dataset.", "AI_IMG_PROC -> DEFAULT DATASET");

            Logging.Log_Information("Fetching image indices for AI model training...", "AI_IMGPROC -> BeginTrain()");
            var imagePaths_Query = await Program.db.SQL_Get_ListOfImagesPaths();
            if (imagePaths_Query.exception != null)
                throw imagePaths_Query.exception;
            else if (imagePaths_Query.result == null)
                throw new NullReferenceException();
            Logging.Log_Information($"Finished fetching {((List<AI_ImageIndex>)imagePaths_Query.result).Count} image indices from dataset.", "AI_IMGPROC -> BeginTrain()");

            foreach (var imageIndex in (List<AI_ImageIndex>)imagePaths_Query.result)
            {
                try
                {
                    mats_Faces.Add(
                    new Mat(imageIndex.imagePath, ImreadModes.Grayscale)
                    );
                    mats_Labels.Add(
                        imageIndex.ai_ModelIndex
                        );

                    Logging.Log_Information($"Loaded image:  {imageIndex.imagePath} with label index:  {imageIndex.ai_ModelIndex}.", "AI_IMGPROC -> BeginTrain()");
                }
                catch (Exception ex)
                {
                    Logging.Log_Error($"Error opening image for training set: {imageIndex.imagePath} - {ex.Message}", "AI_IMGPROC -> BeginTrain()");
                }
            }
            Logging.Log_Information($"Finished loading MATs", "AI_IMGPROC -> BeginTrain()");

            // Train AI model for the first time.
            try
            {
                Logging.Log_Information($"TRAINING DATASET", "AI_IMGPROC -> BeginTrain()");
                model.Train(mats_Faces, mats_Labels);
                Logging.Log_Information($"SUCCESSFULLY trained AI Image dataset.", "AI_IMGPROC -> BeginTrain()");
            }
            catch (Exception ex)
            {
                Logging.Log_Error($"ERROR Training AI Image dataset: {ex}", "AI_IMGPROC -> BeginTrain()");
            }
        }

        public List<KeyValuePair<MemoryStream, int>> AI_DetectAndTagFaces(
            string pathToPhoto, out Mat renderResult
            )
        {
            List<KeyValuePair<MemoryStream, int>> result = new();

            Mat frame = Cv2.ImRead(pathToPhoto);
            Mat grayFrame = new Mat();
            Cv2.CvtColor(frame, grayFrame, ColorConversionCodes.BGR2GRAY);

            // Extract faces.
            Rect[] faces = faceCascade.DetectMultiScale(grayFrame, 1.1, 4);

            // Enumerate detected faces.
            foreach (var face in faces)
            {
                // Recognize face.
                using (Mat faceROI = new Mat(grayFrame, face))
                {
                    model.Predict(faceROI, out int label, out double confidence);

                    // Generate cropped image.
                    Mat faceImg = new Mat(frame, face);
                    MemoryStream memoryStream = new MemoryStream();
                    var bmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(faceImg);
                    bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

                    result.Add(
                        new KeyValuePair<MemoryStream, int>(memoryStream, label)
                    );

                    Cv2.Rectangle(frame, face, Scalar.Red, 4);

                    // Put the label near the rectangle
                    Cv2.PutText(frame, label.ToString(), new Point(face.X, face.Y - 10), HersheyFonts.HersheyPlain, 1.2, Scalar.Green, 4);
                }
            }

            renderResult = frame;
            return result;
        }

        /// <summary>
        /// Saves a MAT object to <see cref="AI_IMG_DEFAULT_TEMP_FOLDER"/> using GUID to generate unique filename and the provided image extension.
        /// </summary>
        /// <param name="mat">The OpenCV MAT object to be saved.</param>
        /// <param name="fileExtension">Original MAT Image extension.</param>
        /// <returns>This method returns the string to final saved image path.</returns>
        /// <exception cref="OpenCVException">Occurs when ImWrite() function fails.</exception>
        public static string Mat_Save_Temp(Mat mat, string fileExtension)
        {
            Directory.CreateDirectory(AI_IMG_DEFAULT_TEMP_FOLDER);

            var uniqueFileName = DBMgr._GET_GUID();
            string finalFilePath = Path.Combine(new string[] { AI_IMG_DEFAULT_TEMP_FOLDER, uniqueFileName });

            var bmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);
            bmp.Save(finalFilePath + fileExtension, System.Drawing.Imaging.ImageFormat.Png);

            return finalFilePath;
        }

        /// <summary>
        /// Updates AI Image Processor model with a new <see cref="Mat"/> and the tag associated with it.
        /// </summary>
        /// <param name="faceImgFilePath">The path to face image file.</param>
        /// <param name="label">The label of face. (AI_ModelIndex)</param>
        /// <returns>This task returns nothing.</returns>
        public async Task AI_UpdateDataset(string faceImgFilePath, int label)
        {
            await Task.Run(() =>
            {
                model.Update(new List<Mat> { new Mat(faceImgFilePath, ImreadModes.Grayscale) },
                new List<int> { label });
            });
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputerTNB_ClassMgr_Bot.Models;
using OpenCvSharp;
using OpenCvSharp.Face;

namespace ComputerTNB_ClassMgr_Bot
{
    /// <summary>
    /// Provides functionalities used for AI Image Processing.
    /// </summary>
    public class AI_ImgProc
    {
        #region AI_ImgProc_CONSTANTS

        public const string AI_IMG_MODEL_XML = @"haarcascade_frontalface_default.xml";

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
                catch(Exception ex)
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
            catch(Exception ex)
            {
                Logging.Log_Error($"ERROR Training AI Image dataset: {ex}", "AI_IMGPROC -> BeginTrain()");
            }
        }

        

        #endregion
    }
}

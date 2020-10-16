using System;
using System.Linq;
using Microsoft.Psi;
using Microsoft.Psi.CognitiveServices.Vision;
using System.Configuration;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace RealSenseDemo
{
    class Program
    {
#pragma warning disable IDE0060 // Remove unused parameter
        static void Main(string[] args)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            var storeName = ConfigurationManager.AppSettings["PsiStoreName"];
            var storePath = ConfigurationManager.AppSettings["PsiStorePath"];

            using var msPipe = Pipeline.Create(deliveryPolicy:DeliveryPolicy.LatestMessage);
            var storeRSG = PsiStore.Create(msPipe, storeName, storePath);

            RealSenseGenerator rsg = new RealSenseGenerator(msPipe);
            rsg.OutDepthImage.Write(nameof(rsg.OutDepthImage), storeRSG);
            rsg.OutDepthImageColorized.Write(nameof(rsg.OutDepthImageColorized), storeRSG);
            rsg.OutRBGImage.Write(nameof(rsg.OutRBGImage), storeRSG);

            var subKey = ConfigurationManager.AppSettings["CognitiveSubKey"];
            var region = ConfigurationManager.AppSettings["CognitiveRegion"];
            VisualFeatureTypes[] featViz = {
                VisualFeatureTypes.Objects
            };
            ImageAnalyzerConfiguration zerConfig = new ImageAnalyzerConfiguration(
                subKey,
                region,
                featViz
                );
            ImageAnalyzer zer = new ImageAnalyzer(msPipe, zerConfig);

            rsg.OutRBGImage
                .Where(shImg => shImg != null && shImg.Resource !=null)
                .PipeTo(zer.In);
            zer.Out
                .ExtractDetectedObjects()
                .Write("DetectedObjects.AllFeatures", storeRSG)
                .Select(tupleList => tupleList.Count)
                .Write("DetectedObjects.Count", storeRSG);

            msPipe.RunAsync();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
    
}

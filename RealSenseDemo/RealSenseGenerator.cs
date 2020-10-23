using Intel.RealSense;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Components;
using System;
using Microsoft.Psi;
using System.Threading.Tasks;
using Microsoft.Psi.Data;

namespace RealSenseDemo
{
    //Using Generator Pattern Described at: https://github.com/microsoft/psi/wiki/Writing-Components#SourceComponents
    class RealSenseGenerator : Generator
    {
        const int Q_SIZE = 5;
        private readonly FrameQueue qDepth = new FrameQueue(Q_SIZE);
        private readonly FrameQueue qRBG = new FrameQueue(Q_SIZE);
        private Intel.RealSense.Pipeline intelPipe;
        private readonly Microsoft.Psi.Pipeline msPipe;
        private Intrinsics sicsDepth;
        private Intrinsics sicsRBG;

        public RealSenseGenerator(Microsoft.Psi.Pipeline msPipe) : base(msPipe)
        {
            this.msPipe = msPipe;
            OutDepthImage = msPipe.CreateEmitter<Shared<DepthImage>>(this, nameof(OutDepthImage));
            OutDepthImageColorized = msPipe.CreateEmitter<Shared<Image>>(this, nameof(OutDepthImageColorized));
            OutRBGImage = msPipe.CreateEmitter<Shared<Image>>(this, nameof(OutRBGImage));
            Init();

        }

        protected override DateTime GenerateNext(DateTime currentTime)
        {

            Shared<DepthImage> imgDepth = DepthImagePool.GetOrCreate(sicsDepth.width, sicsDepth.height);
            Shared<Image> imgDepthColor = ImagePool.GetOrCreate(sicsDepth.width, sicsDepth.height, PixelFormat.BGR_24bpp);
            Shared<Image> imgRBG = ImagePool.GetOrCreate(sicsRBG.width, sicsRBG.height, PixelFormat.BGR_24bpp);
            try
            {
                if (qDepth.PollForFrame(out Frame frDepth))
                {
                    imgDepth.Resource.CopyFrom(frDepth.Data);
                    imgDepthColor.Resource.CopyFrom(imgDepth.Resource.PseudoColorize((0, 3072)));
                    OutDepthImage.Post(imgDepth, msPipe.GetCurrentTime());
                    OutDepthImageColorized.Post(imgDepthColor, msPipe.GetCurrentTime());
                }

                if (qRBG.PollForFrame(out Frame frRBG))
                {
                    imgRBG.Resource.CopyFrom(frRBG.Data);
                    OutRBGImage.Post(imgRBG, msPipe.GetCurrentTime());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return msPipe.GetCurrentTime();
        }

        private void Init()
        {

            using Context ctx = new Context();
            var devices = ctx.QueryDevices();
            Console.WriteLine($"Found {devices.Count} RealSense devices connected.");
            if (devices.Count == 0)
            {
                throw new Exception("No RealSense device detected!");
            }

            Device dev = devices[0];

            Console.WriteLine($"Using device 0: {dev.Info[CameraInfo.Name]}");
            Console.WriteLine("Device Sources:");

            foreach (Sensor sensor in dev.Sensors)
            {
                Console.WriteLine($"Sensor found: {sensor.Info[CameraInfo.Name]}");
            }

            intelPipe = new Intel.RealSense.Pipeline();
            PipelineProfile profileIntelPipe = intelPipe.Start();
            var streamDepth = profileIntelPipe.GetStream<VideoStreamProfile>(Stream.Depth);
            sicsDepth = streamDepth.GetIntrinsics();
            Console.WriteLine($"Depth Stream: {sicsDepth.width}X{sicsDepth.height}");

            var streamRBG = profileIntelPipe.GetStream<VideoStreamProfile>(Stream.Color);
            sicsRBG = streamRBG.GetIntrinsics();
            Console.WriteLine($"RBG Stream: {sicsRBG.width}X{sicsRBG.height}");

            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        using FrameSet frames = intelPipe.WaitForFrames();
                        using Frame frDepth = frames.FirstOrDefault(Stream.Depth);
                        qDepth.Enqueue(frDepth);
                        using Frame frRBG = frames.FirstOrDefault(Stream.Color);
                        qRBG.Enqueue(frRBG);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }


                }
            });


        }
        public Emitter<Shared<DepthImage>> OutDepthImage { get; private set; }
        public Emitter<Shared<Image>> OutDepthImageColorized { get; private set; }

        public Emitter<Shared<Image>> OutRBGImage { get; private set; }


    }
}

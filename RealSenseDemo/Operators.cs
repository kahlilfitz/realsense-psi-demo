using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Psi;
using Microsoft.Psi.Imaging;

namespace RealSenseDemo
{
    internal static class Operators
    {
        /// <summary>
        /// Computes a stream containing the labeled rectangles for the detected objects.
        /// </summary>
        /// <param name="source">The stream of image analysis results.</param>
        /// <returns>A stream with the labeled rectangles.</returns>
        /// Copied from: https://github.com/microsoft/psi-samples/blob/main/Samples/WhatIsThat/Operators.cs
        internal static IProducer<List<Tuple<System.Drawing.Rectangle, string>>> ExtractDetectedObjects(this IProducer<ImageAnalysis> source)
        {
            return source.Select(
                r => r.Objects.Select(
                    o => Tuple.Create(
                        new System.Drawing.Rectangle(
                            o.Rectangle.X,
                            o.Rectangle.Y,
                            o.Rectangle.W,
                            o.Rectangle.H), o.ObjectProperty)).ToList());
        }

    }
}

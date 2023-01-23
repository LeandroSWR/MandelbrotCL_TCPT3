using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T3_LeandroBras_22100770
{
    internal class MandelbrotCalcParallel : MandelbrotCalc
    {
        /// <summary>
        /// Constructor for the MandelbrotCalcParallel class
        /// </summary>
        /// <param name="width">The image width</param>
        /// <param name="height">The image height</param>
        /// <param name="maxIterations">The max number of iterations</param>
        /// <param name="scale">The scale</param>
        public MandelbrotCalcParallel(int width, int height, int maxIterations, float scale) :
            base(width, height, maxIterations, scale)
        {
            this.width = width;
            this.height = height;
            this.maxIterations = maxIterations;
            this.scale = scale;
        }

        /// <summary>
        /// Calculate the mandelbrot set
        /// </summary>
        /// <returns>A formated value to calculate the color based of the number of iterations</returns>
        internal override float[,] Calculate()
        {
            // Create a 2D array to hold the number of iterations it took for each point in the set to diverge,
            // or 0 if it did not diverge within the given number of maxIterations
            float[,] iterations = new float[width, height];

            // Makes sure the set is centered
            xMax -= scale - 1.3f;
            xMin -= scale - 1.3f;
            yMax += scale - 1f;
            yMin += scale - 1f;

            // Calculate the width and height of each pixel in the complex plane
            float pixelWidth = (xMax - xMin) / width;
            float pixelHeight = (yMax - yMin) / height;

            var options = new ParallelOptions();
            int maxCore = Environment.ProcessorCount - 1;
            options.MaxDegreeOfParallelism = maxCore > 0 ? maxCore : 1;

            // Iterate over each pixel in the image
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    // Calculate the complex number c corresponding to the pixel's position,
                    // using the width and height of each pixel and the range of complex numbers to be plotted
                    float cReal = xMin + x * pixelWidth * scale;
                    float cImaginary = yMax - y * pixelHeight * scale;

                    // Initialize the real and imaginary parts of the z complex number to 0
                    float zReal = 0f;
                    float zImaginary = 0f;

                    // Initialize a counter to keep track of the number of iterations
                    int iteration = 0;

                    // Initialize a holder for the new values of z
                    float zRealNew = 0f;
                    float zImaginaryNew = 0f;

                    // Loop until either the magnitude of z becomes greater than 2 (which means the function has diverged)
                    // or the number of iterations exceeds maxIterations
                    while (zReal * zReal + zImaginary * zImaginary < 4f && iteration < maxIterations)
                    {
                        // Calculate the new value of z using the formula z = z^2 + c
                        zRealNew = zReal * zReal - zImaginary * zImaginary + cReal;
                        zImaginaryNew = 2f * zReal * zImaginary + cImaginary;

                        // Update the values of zReal and zImaginary with the new values
                        zReal = zRealNew;
                        zImaginary = zImaginaryNew;

                        // Increment the iteration counter
                        iteration++;
                    }

                    // Calculate the square magnitude of z
                    float sMag = zRealNew * zRealNew + zImaginaryNew * zImaginaryNew;

                    // If the square magnitude of z is greater than 4.
                    if (sMag > 4f)
                    {
                        // Calculate the number of iterations it took for the function to diverge
                        // based of the squared magnitude of z times a constant
                        iterations[x, y] = (iteration - (float)Math.Log(Math.Log(sMag, 2), 2)) * 0.002f;
                    }
                    // If the magnitude of z is not greater than 4.
                    else
                    {
                        // The function did not diverge within the given number of iterations and
                        // the value of iterations[x, y] is set to 0
                        iterations[x, y] = 0f;
                    }
                }
            });

            // Return the iterations array
            return iterations;
        }
    }
}

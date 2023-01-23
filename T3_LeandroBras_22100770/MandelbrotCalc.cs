using System;
using System.Diagnostics;
using System.Drawing;

namespace T3_LeandroBras_22100770
{
    internal class MandelbrotCalc
    {
        internal int width;
        internal int height;
        internal int maxIterations;
        internal float scale;

        internal float xMin = -2;
        internal float xMax = 0;
        internal float yMin = -1;
        internal float yMax = 1;

        /// <summary>
        /// Constructor for the MandelbrotCalc class
        /// </summary>
        /// <param name="width">The image width</param>
        /// <param name="height">The image height</param>
        /// <param name="maxIterations">The max number of iterations</param>
        /// <param name="scale">The scale</param>
        public MandelbrotCalc(int width, int height, int maxIterations, float scale)
        {
            this.width = width;
            this.height = height;
            this.maxIterations = maxIterations;
            this.scale = scale;
        }

        /// <summary>
        /// Renders the mandelbrot set to an end image
        /// </summary>
        /// <param name="time">The time it takes to calculate the set</param>
        /// <param name="bmp">The final image</param>
        public void RenderMandelbrotSet(ref long time, ref Bitmap bmp)
        {
            // Start a new Stopwatch to get the time it takes for the calculations
            Stopwatch sw = Stopwatch.StartNew();

            // Get the iter values by calculating the Mandelbrot set
            float[,] iterations = Calculate();

            // Save the current time
            time = sw.ElapsedMilliseconds;

            // Stop the StopWatch
            sw.Stop();

            // Iterate over each pixel in the image
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Retrieve the corresponding value from the iterations array
                    float iteration = iterations[x, y];

                    // Map the iteration value to a color using MapColor()
                    Color color = MapColor(iteration);
                    bmp.SetPixel(x, y, color);
                }
            }
        }

        /// <summary>
        /// Map the iteration value to a color
        /// </summary>
        /// <param name="iter">The iteration value</param>
        /// <returns>A Color</returns>
        Color MapColor(double iter)
        {
            // Use the cos function and constants to create a color gradient
            float R = (float)(0.5d + 0.5d * Math.Cos((2.5d + iter * 30.0d) + 1.0d));
            float G = (float)(0.5d + 0.5d * Math.Cos((2.5d + iter * 30.0d) + 0.5d));
            float B = (float)(0.5d + 0.5d * Math.Cos((2.5d + iter * 30.0d) + 0.0d));

            return Color.FromArgb(
                255,
                (int)(R * 255),
                (int)(G * 255),
                (int)(B * 255));
        }

        /// <summary>
        /// Calculate the mandelbrot set
        /// </summary>
        /// <returns>A formated value to calculate the color based of the number of iterations</returns>
        internal virtual float[,] Calculate()
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

            // Iterate over each pixel in the image
            for (int x = 0; x < width; x++)
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
                    float sMag = zReal * zReal + zImaginary * zImaginary;

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
            }

            // Return the iterations array
            return iterations;
        }
    }
}

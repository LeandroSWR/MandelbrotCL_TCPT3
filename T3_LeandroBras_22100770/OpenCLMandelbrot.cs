using OpenCL.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace T3_LeandroBras_22100770
{
    internal class OpenCLMandelbrot
    {
        Platform[] platforms;
        List<Device> devices = new List<Device>();
        ErrorCode errorCode;
        Context context;
        Device gpu;
        CommandQueue commandQueue;
        Event event0;
        ErrorCode err;
        Kernel kernel;
        OpenCL.Net.Program program;
        string functionName = "mandelbrot";

        // Fixed size buffers that determine the maximum display size of the image without interpolation
        const int maxCountX = 4096;
        const int maxCountY = 4096;
        int lineSize = 1024;

        private int maxIterations = 1000;

        public int[] dataX = new int[maxCountX];
        public int[] dataY = new int[maxCountY];
        Mem aspectMem, scaleMem, iterationsMem;
        Mem centerXMem, centerYMem;
        string kernelPath = @"..\..\compute\compute_gpu.cl";
        string compileOption = string.Empty;

        Mem outputMem;
        Mem lineSizeMem;
        IntPtr[] workGroupSizePtr;
        InfoBuffer local;

        /// <summary>
        /// Initialize OpenCL and compile the code
        /// </summary>
        public void Init(int gpuNum = 0)
        {
            // Detect video cards
            platforms = Cl.GetPlatformIDs(out errorCode);

            foreach (Platform platform in platforms)
            {
                // We only want GPUs
                foreach (Device device in Cl.GetDeviceIDs(platform, DeviceType.Gpu, out errorCode))
                {
                    devices.Add(device);
                }
            }

            // If no equipment was detected
            if (devices.Count == 0)
            {
                throw new Exception("No GPU Detected");
            }

            // Chose GPU
            gpu = devices[gpuNum];

            // Create context
            context = Cl.CreateContext(null, 1, new Device[] { gpu }, null, IntPtr.Zero, out errorCode);
            if (errorCode != ErrorCode.Success)
            {
                throw new Exception("Unable to create context");
            }

            // Initialize the command list
            commandQueue = Cl.CreateCommandQueue(context, gpu, CommandQueueProperties.OutOfOrderExecModeEnable, out errorCode);
            if (errorCode != ErrorCode.Success)
            {
                throw new Exception("Unable to create command list");
            }

            // Read program
            string programSource = string.Join(System.Environment.NewLine, File.ReadLines(kernelPath));
            // Create program
            program = Cl.CreateProgramWithSource(context, 1, new string[] { programSource }, null, out err);
            errorCode = Cl.BuildProgram(program, 0, null, compileOption, null, IntPtr.Zero);
            if (errorCode != ErrorCode.Success)
            {
                throw new Exception("Unable to compile program: " + Cl.GetProgramBuildInfo(program, gpu, ProgramBuildInfo.Log, out errorCode));
            }

            // Retrieve build information
            if (Cl.GetProgramBuildInfo(program, gpu, ProgramBuildInfo.Status, out err).CastTo<BuildStatus>() != BuildStatus.Success)
            {
                // Display any errors that occurred during the build
                string message = string.Format("ERROR: Cl.GetProgramBuildInfo(" + err.ToString() + ")")
                    + string.Format("Cl.GetProgramBuildInfo != Success")
                    + Cl.GetProgramBuildInfo(program, gpu, ProgramBuildInfo.Log, out err);
                string caption = "OpenCL Compilation Error";
                Console.WriteLine(message, caption);
                // MessageBox.Show(message, caption);
            }
        }

        /// <summary>
        /// Starts the calculation / retrieve and copies the data without reallocation the buffers
        /// </summary>
        /// <param name="ptr">Pointer to the image buffer that the processed image data will be copied to.</param>
        /// <param name="countX">Number of columns of the Image</param>
        /// <param name="countY">Number of rows of the Image</param>
        /// <param name="xMin">The Minimum value of the x-coordinate of the image. Defaults to -2,3</param>
        /// <param name="yMin">The Minimum value of the y-coordinate of the image. Defaults to -1,2</param>
        /// <param name="scale">The scale factor used to generate the image data. Defaults to 3</param>
        public void ReComputeGPU(IntPtr ptr, int countX, int countY, double centerX = -2.3, double centerY = -1.2, double scale = 3, int maxIterations = 1000)
        {
            double windowAspect = (double)countX / (double)countY;
            lineSize = countX;

            // Copy host buffer into input device buffer
            Cl.EnqueueWriteBuffer(commandQueue, (IMem)aspectMem, Bool.True, IntPtr.Zero, new IntPtr(sizeof(double)), windowAspect, 0, null, out event0);
            Cl.EnqueueWriteBuffer(commandQueue, (IMem)scaleMem, Bool.True, IntPtr.Zero, new IntPtr(sizeof(double)), scale, 0, null, out event0);
            Cl.EnqueueWriteBuffer(commandQueue, (IMem)iterationsMem, Bool.True, IntPtr.Zero, new IntPtr(sizeof(int)), maxIterations, 0, null, out event0);
            Cl.EnqueueWriteBuffer(commandQueue, (IMem)centerXMem, Bool.True, IntPtr.Zero, new IntPtr(sizeof(double)), centerX, 0, null, out event0);
            Cl.EnqueueWriteBuffer(commandQueue, (IMem)centerYMem, Bool.True, IntPtr.Zero, new IntPtr(sizeof(double)), centerY, 0, null, out event0);
            Cl.EnqueueWriteBuffer(commandQueue, (IMem)lineSizeMem, Bool.True, IntPtr.Zero, new IntPtr(sizeof(int)), lineSize, 0, null, out event0);

            // Load Queue
            Cl.EnqueueNDRangeKernel(commandQueue, kernel, 2, null, workGroupSizePtr, null, 0, null, out event0);

            // Launch calculations in queue
            Cl.Finish(commandQueue);

            // Direct copy into image buffer
            Cl.EnqueueReadBuffer(commandQueue, (IMem)outputMem, Bool.True, IntPtr.Zero, new IntPtr(countX * countY * sizeof(int)), ptr, 0, null, out event0);
        }

        /// <summary>
        /// Allocates memory buffers, loads the command queue, launches the calculation, retrieves and copies the data
        /// </summary>
        /// <param name="ptr">Pointer to the image buffer that the processed image data will be copied to.</param>
        /// <param name="countX">Number of columns of the Image</param>
        /// <param name="countY">Number of rows of the Image</param>
        /// <param name="xMin">The Minimum value of the x-coordinate of the image. Defaults to -2,3</param>
        /// <param name="yMin">The Minimum value of the y-coordinate of the image. Defaults to -1,2</param>
        /// <param name="scale">The scale factor used to generate the image data. Defaults to 3</param>
        public void ComputeGPU(IntPtr ptr, int countX, int countY, double centerX = -3.0d / 4.0d, double centerY = 0.0d, double scale = 3)
        {
            double windowAspect = (double)countX / (double)countY;

            lineSize = countX;
            string errors = "";
            // Creating a kernel for the program
            kernel = Cl.CreateKernel(program, functionName, out err);
            if (err != ErrorCode.Success)
            {
                errors += err;
            }

            // Allocate input and output buffers and fill the input with data
            aspectMem = (Mem)Cl.CreateBuffer(context, MemFlags.ReadOnly, sizeof(double), out err);
            if (err != ErrorCode.Success) { errors += err; }
            scaleMem = (Mem)Cl.CreateBuffer(context, MemFlags.ReadOnly, sizeof(double), out err);
            if (err != ErrorCode.Success) { errors += err; }
            iterationsMem = (Mem)Cl.CreateBuffer(context, MemFlags.ReadOnly, sizeof(int), out err);
            if (err != ErrorCode.Success) { errors += err; }
            centerXMem = (Mem)Cl.CreateBuffer(context, MemFlags.ReadOnly, sizeof(double), out err);
            if (err != ErrorCode.Success) { errors += err; }
            centerYMem = (Mem)Cl.CreateBuffer(context, MemFlags.ReadOnly, sizeof(double), out err);
            if (err != ErrorCode.Success) { errors += err; }
            lineSizeMem = (Mem)Cl.CreateBuffer(context, MemFlags.ReadOnly, sizeof(int), out err);
            if (err != ErrorCode.Success) { errors += err; }
            // Create an output buffer for the results
            //outputMem = (Mem)Cl.CreateBuffer(context, MemFlags.ReadWrite, new IntPtr(sizeof(int) * countX * countY), ptr, out err);
            outputMem = (Mem)Cl.CreateBuffer(context, MemFlags.WriteOnly, sizeof(int) * countX * countY, out err);
            if (err != ErrorCode.Success) { errors += err; }

            // Copy the host buffer to the input device buffer
            Cl.EnqueueWriteBuffer(commandQueue, (IMem)aspectMem, Bool.True, IntPtr.Zero, new IntPtr(sizeof(double)), windowAspect, 0, null, out event0);
            Cl.EnqueueWriteBuffer(commandQueue, (IMem)scaleMem, Bool.True, IntPtr.Zero, new IntPtr(sizeof(double)), scale, 0, null, out event0);
            Cl.EnqueueWriteBuffer(commandQueue, (IMem)iterationsMem, Bool.True, IntPtr.Zero, new IntPtr(sizeof(int)), maxIterations, 0, null, out event0);
            Cl.EnqueueWriteBuffer(commandQueue, (IMem)centerXMem, Bool.True, IntPtr.Zero, new IntPtr(sizeof(double)), centerX, 0, null, out event0);
            Cl.EnqueueWriteBuffer(commandQueue, (IMem)centerYMem, Bool.True, IntPtr.Zero, new IntPtr(sizeof(double)), centerY, 0, null, out event0);
            Cl.EnqueueWriteBuffer(commandQueue, (IMem)lineSizeMem, Bool.True, IntPtr.Zero, new IntPtr(sizeof(int)), lineSize, 0, null, out event0);

            // Use the maximum number of work items supported for this kernel on this device
            IntPtr notUsed;
            local = new InfoBuffer(new IntPtr(4));
            Cl.GetKernelWorkGroupInfo(kernel, gpu, KernelWorkGroupInfo.WorkGroupSize, new IntPtr(sizeof(int)), local, out notUsed);

            // Set pointer size
            int intPtrSize = 0;
            intPtrSize = Marshal.SizeOf(typeof(IntPtr));

            // Setting kernel arguments and enqueue for execution
            Cl.SetKernelArg(kernel, 0, (IntPtr)intPtrSize, aspectMem);
            Cl.SetKernelArg(kernel, 1, (IntPtr)intPtrSize, scaleMem);
            Cl.SetKernelArg(kernel, 2, (IntPtr)intPtrSize, iterationsMem);
            Cl.SetKernelArg(kernel, 3, (IntPtr)intPtrSize, centerXMem);
            Cl.SetKernelArg(kernel, 4, (IntPtr)intPtrSize, centerYMem);
            Cl.SetKernelArg(kernel, 5, (IntPtr)intPtrSize, lineSizeMem);
            Cl.SetKernelArg(kernel, 6, (IntPtr)intPtrSize, outputMem);

            workGroupSizePtr = new IntPtr[] { (IntPtr)countX, (IntPtr)countY};
            Cl.EnqueueNDRangeKernel(commandQueue, kernel, 2, null, workGroupSizePtr, null, 0, null, out event0);

            // Launch the calculations in the queue
            Cl.Finish(commandQueue);

            // Direct copy to the image buffer
            Cl.EnqueueReadBuffer(commandQueue, (IMem)outputMem, Bool.True, IntPtr.Zero, new IntPtr(countX * countY * sizeof(uint)), ptr, 0, null, out event0);
        }

        public void ClearClData()
        {
            Cl.ReleaseMemObject((IMem)outputMem);
            Cl.ReleaseMemObject((IMem)aspectMem);
            Cl.ReleaseMemObject((IMem)scaleMem);
            Cl.ReleaseMemObject((IMem)iterationsMem);
            Cl.ReleaseMemObject((IMem)centerXMem);
            Cl.ReleaseMemObject((IMem)centerYMem);
            Cl.ReleaseMemObject((IMem)lineSizeMem);
            Cl.ReleaseKernel(kernel);
        }
    }
}

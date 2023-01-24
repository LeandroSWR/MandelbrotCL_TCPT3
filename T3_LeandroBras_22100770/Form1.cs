using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace T3_LeandroBras_22100770
{
    public partial class Form1 : Form
    {
        Stopwatch stopwatch;

        private OpenCLMandelbrot clCalculator;
        private Point previousPosition;

        private double centerX =  -2.07d;
        private double centerY =  -1.40d;

        private double scale = 2d;
        private float lockedAspectRatio;

        private int width = 1024;
        private int height = 1024;

        public List<MandelbrotStats> MStats { get; set; }
        private List<MandelbrotStats> benchStats;
        private AverageStats avgStatsTask;
        private AverageStats avgStatsLinear;
        private AverageStats avgStatsOpenCL;

        public Form1()
        {
            InitializeComponent();
            clCalculator = new OpenCLMandelbrot();
            stopwatch = new Stopwatch();

            this.MouseWheel += new MouseEventHandler(openCLTab_MouseWheel);
            previousPosition = MousePosition;

            MStats = new List<MandelbrotStats>();
            benchStats = new List<MandelbrotStats>();

            CheckForPreviousStats();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            clCalculator.Init();

            scale = 2 * (860d / (double)glPicture.Width);

            glPicture.Width = glPicture.Height;
            lockedAspectRatio = (float)this.Width / (float)this.Height;

            bmSize.Text = $"{width}x{height}";

            Calculate();
        }

        private void CheckForPreviousStats()
        {
            // Check to update stats
            if (File.Exists("mStats.json"))
            {
                UpdateMandelbrotStats();
            }
        }

        private void UpdateMandelbrotStats()
        {
            using (StreamReader f = new StreamReader("mStats.json"))
            {
                string jsonString = f.ReadToEnd();
                MStats = JsonConvert.DeserializeObject<List<MandelbrotStats>>(jsonString);
            }
        }

        private double Calculate(bool recalculate = false, bool test = false)
        {
            int width = test ? this.width : glPicture.Width;
            int height = test ? this.height : glPicture.Height;
            PixelFormat format = PixelFormat.Format32bppRgb;
            Bitmap myBitmap = new Bitmap(width, height, format);
            BitmapData bmpData;

            Rectangle rect = new Rectangle(0, 0, width, 1);
            bmpData = myBitmap.LockBits(rect, ImageLockMode.ReadWrite, format);
            int lineSize = bmpData.Stride;
            IntPtr ptr = bmpData.Scan0; // Pointer to the RGB part of the image
            
            stopwatch.Reset();
            stopwatch.Start();
            if (recalculate)
            {
                clCalculator.ReComputeGPU(ptr, width, height, centerX, centerY, scale);
            }
            else
            {
                clCalculator.ComputeGPU(ptr, width, height, centerX, centerY, scale);
            }
            stopwatch.Stop();
            
            myBitmap.UnlockBits(bmpData);
            if (glPicture.Image != null) { glPicture.Image.Dispose(); }
            glPicture.Image = myBitmap;
            

            centerXBox.Text = centerX.ToString();
            centerYBox.Text = centerY.ToString();
            scaleBox.Text = scale.ToString();
            timeBox.Text = stopwatch.Elapsed.TotalMilliseconds.ToString("0.00") + "ms";
            sizeBox.Text = $"{glPicture.Width}x{glPicture.Height}";

            if (test)
            {
                bmCLPicture.Image = myBitmap;
            }

            return stopwatch.Elapsed.TotalMilliseconds;
        }

        private void openCLTab_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0 && glPicture.ClientRectangle.Contains(e.Location))
            {
                double oldCenterX = centerX + scale * 0.5;
                double oldCenterY = centerY + scale * 0.5;
                double newCenterX = centerX + (double)e.Location.X / glPicture.Width * scale;
                double newCenterY = centerY + (double)e.Location.Y / glPicture.Height * scale;
                scale *= 0.97;

                double ratio = 0.05;
                centerX = (oldCenterX * (1 - ratio) + newCenterX * ratio) - scale * 0.5;
                centerY = (oldCenterY * (1 - ratio) + newCenterY * ratio) - scale * 0.5;

                Calculate(true);
            }
            if (e.Delta < 0 && glPicture.ClientRectangle.Contains(e.Location))
            {
                double oldCenterX = centerX + scale * 0.5;
                double oldCenterY = centerY + scale * 0.5;
                double newCenterX = centerX + (double)e.Location.X / glPicture.Width * scale;
                double newCenterY = centerY + (double)e.Location.Y / glPicture.Height * scale;
                scale *= 1.03;
                double ratio = 0.05;
                centerX = (oldCenterX * (1 - ratio) + newCenterX * ratio) - scale * 0.5;
                centerY = (oldCenterY * (1 - ratio) + newCenterY * ratio) - scale * 0.5;

                Calculate(true);
            }
        }

        private void glPicture_MouseMove(object sender, MouseEventArgs e)
        {
            if (MouseButtons.Equals(MouseButtons.Left) && glPicture.ClientRectangle.Contains(e.Location))
            {
                Point mousePos = MousePosition;

                if (mousePos == previousPosition)
                    return;

                double deltaX = (mousePos.X - previousPosition.X);
                double deltaY = (mousePos.Y - previousPosition.Y);

                var translationSpeed = 0.003d * scale;

                centerX -= deltaX * translationSpeed;
                centerY -= deltaY * translationSpeed;

                Calculate(true);
            }

            previousPosition = MousePosition;
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            glPicture.Width = glPicture.Height;
            clCalculator.ClearClData();
            Calculate();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            glPicture.Width = glPicture.Height;

            float currentAspectRatio = (float)this.Width / (float)this.Height;
            if (lockedAspectRatio != currentAspectRatio)
            {
                if (Width > Height)
                    Width = (int)(Height * lockedAspectRatio);
                else
                    Height = (int)(Width / lockedAspectRatio);
            }

            clCalculator.ClearClData();
            Calculate();
        }

        private void resetBtt_Click(object sender, EventArgs e)
        {
            centerX =  -2.07d;
            centerY = -1.40d;
            scale = 2 * (860d / (double)glPicture.Width);
            clCalculator.ClearClData();
            Calculate();
        }

        public void CalculateMandelLinear(bool isBench = false)
        {
            long time = 0;
            Bitmap bmp = new Bitmap(width, height);
            MandelbrotCalc linearCalc = new MandelbrotCalc(width, height, 1000, 1.35f);
            linearCalc.RenderMandelbrotSet(ref time, ref bmp);
            this.BeginInvoke((MethodInvoker)delegate
            {
                linearTime.Text = $"{time:0.00} ms";
                bmLPicture.Image = bmp;
            });

            HandleResult(time, CalcType.Linear, isBench);
        }

        public void CalculateMandelParallel(bool isBench = false)
        {
            long time = 0;
            Bitmap bmp = new Bitmap(width, height);
            MandelbrotCalcParallel parallelCalc = new MandelbrotCalcParallel(width, height, 1000, 1.35f);
            parallelCalc.RenderMandelbrotSet(ref time, ref bmp);
            
            this.BeginInvoke((MethodInvoker)delegate
            {
                parallelTime.Text = $"{time:0.00} ms";
                bmPPicture.Image = bmp;
            });

            HandleResult(time, CalcType.Parallel, isBench);
        }

        public void CalculateMandelOpenCL(bool isBench = false)
        {
            centerX = -2.07d;
            centerY = -1.40d;
            scale = 2 * (860d / (double)width);
            clCalculator.ClearClData();
            double time = (double)Calculate(false, true);
            clTime.Text = $"{time:0.00}ms";

            HandleResult(time, CalcType.OpenCL, isBench);
        }

        private void HandleResult(double time, CalcType type, bool isBench)
        {
            // Add the data to the stats list
            MandelbrotStats stat =
                new MandelbrotStats(time, string.Format($"{width}x{height}"), type);
            MStats.Add(stat);
            benchStats.Add(stat);

            // Save the new Data
            SaveNewData();
        }

        private void SaveNewData()
        {
            if (width != 4096 || height != 4096)
                return;

            string jsonString = JsonConvert.SerializeObject(MStats);
            File.WriteAllText("mStats.json", jsonString);
        }

        private void tabsControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            bmCLPicture.Image = null;

            if (tabsControl.SelectedTab == statsTab)
            {
                // Load stats
                CheckForPreviousStats();

                if (MStats == null || MStats.Count == 0)
                {
                    statsList.Text = "There are no stats to be displayed...";
                    return;
                }

                SortStats();
                DisplayStats();
            }
        }

        private void SortStats()
        {
            avgStatsTask = new AverageStats(CalcType.Parallel);
            avgStatsLinear = new AverageStats(CalcType.Linear);
            avgStatsOpenCL = new AverageStats(CalcType.OpenCL);

            foreach (MandelbrotStats stat in MStats)
            {
                switch (stat.Type)
                {
                    case CalcType.Linear:
                        avgStatsLinear.Num++;
                        avgStatsLinear.Size = stat.MRes;
                        avgStatsLinear.Avg += stat.ETime;
                        break;
                    case CalcType.Parallel:
                        avgStatsTask.Num++;
                        avgStatsTask.Size = stat.MRes;
                        avgStatsTask.Avg += stat.ETime;
                        break;
                    case CalcType.OpenCL:
                        avgStatsOpenCL.Num++;
                        avgStatsOpenCL.Size = stat.MRes;
                        avgStatsOpenCL.Avg += stat.ETime;
                        break;
                }
            }

            avgStatsTask.Avg /= avgStatsTask.Num;
            avgStatsLinear.Avg /= avgStatsLinear.Num;
            avgStatsOpenCL.Avg /= avgStatsOpenCL.Num;
        }

        private void DisplayStats()
        {
            rLinearTime.Text = $"{avgStatsLinear.Avg:0.00}";
            rParallelTime.Text = $"{avgStatsTask.Avg:0.00}";
            rOpenCLTime.Text = $"{avgStatsOpenCL.Avg:0.00}";

            string statsString = "All Sets calculated so far:\n\n";
            for (int i = 0; i < MStats.Count; i++)
            {
                statsString += $" - {MStats[i]}\n";
            }

            statsList.Text = statsString;
        }

        private void benchBtt_Click(object sender, EventArgs e)
        {
            var token = Task.Factory.CancellationToken;
            Task.Factory.StartNew(() => RunBenchMark(), token, TaskCreationOptions.None, TaskScheduler.Default);
        }

        private void RunBenchMark()
        {
            bmStatus.BeginInvoke((MethodInvoker)delegate { bmStatus.Text = "Status: Running..."; });

            for (int i = 0; i < 3; i++)
            {
                CalculateMandelLinear(true);
                CalculateMandelParallel(true);
                this.BeginInvoke((MethodInvoker)delegate
                {
                    CalculateMandelOpenCL(true);
                });
            }

            double avgLinearTime = 0;
            double avgParallelTime = 0;
            double avgOpenCLTime = 0;

            foreach (MandelbrotStats stats in benchStats)
            {
                switch (stats.Type)
                {
                    case CalcType.Linear:
                        avgLinearTime += stats.ETime / 3;
                        break;
                    case CalcType.Parallel:
                        avgParallelTime += stats.ETime / 3;
                        break;
                    case CalcType.OpenCL:
                        avgOpenCLTime += stats.ETime / 3;
                        break;
                }
            }

            this.BeginInvoke((MethodInvoker)delegate { 
                linearAvgTime.Text = $"{avgLinearTime:0.00}ms";
                parallelAvgTime.Text = $"{avgParallelTime:0.00}ms";
                clAvgTime.Text = $"{avgOpenCLTime:0.00}ms";
                bmStatus.BeginInvoke((MethodInvoker)delegate { bmStatus.Text = "Status: Complete!"; });
            });
            benchStats.Clear();
        }

        #region Extra

        private void openclBtt_Click(object sender, EventArgs e) => CalculateMandelOpenCL();
        private void parallelBtt_Click(object sender, EventArgs e) => CalculateMandelParallel();
        private void linearBtt_Click(object sender, EventArgs e) => CalculateMandelLinear();

        private void clBtt_Click(object sender, EventArgs e)
        {
            tabsControl.SelectedTab = clTab;
            clTab.Focus();
        }

        private void glBtt_Click(object sender, EventArgs e)
        {
            Process.Start(@"..\..\MandelbrotGL\MandelbrotGL.exe");
        }

        private void bmBtt_Click(object sender, EventArgs e)
        {
            tabsControl.SelectedTab = benchTab;
            benchTab.Focus();
        }

        private void statsBtt_Click(object sender, EventArgs e)
        {
            tabsControl.SelectedTab = statsTab;
            statsTab.Focus();
        }

        private void bonusBtt_Click(object sender, EventArgs e)
        {
            Process.Start(@"..\..\Bonus\Bonus.exe");
        }

        #endregion
    }
}

#pragma OPENCL EXTENSION cl_khr_fp64: enable

double2 complexMult(double2 a, double2 b)
{
	return (double2)(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
}

int convertColorToInt(double4 fragColor) {
	int r = (int)(fragColor.x * 255);
	int g = (int)(fragColor.y * 255);
	int b = (int)(fragColor.z * 255);
	return (r << 16) | (g << 8) | b;
}

int mapColor(double mcol)
{
	return convertColorToInt((double4)(0.5 + 0.5 * cos(2.5 + mcol * 30 + (double3)(1.0, 0.5, 0.0)), 1.0));
}

__kernel void mandelbrot(__global double* windowAspect,
	__global double* scale, __global int* maxIterations,
	__global double* centerX, __global double* centerY,
	__global int* lineSize, __global int* output)
{
	//Get index of this work item
	int xi = get_global_id(0);
	int yi = get_global_id(1);

	double ndot = 0;

	double2 fragment = (double2)(
		xi * (scale[0] * 0.00165) + centerX[0],
		yi * (scale[0] * 0.00165) + centerY[0]
		);

	double2 currentPoint = (double2)(0, 0);

	for (int i = 0; i < maxIterations[0]; i++)
	{
		currentPoint = complexMult(currentPoint, currentPoint) + fragment;

		ndot = dot(currentPoint, currentPoint);

		if (ndot > 7.0)
		{
			double sl = i - log2(log2(ndot)) + 4.0;

			output[(yi * lineSize[0]) + xi] = mapColor(sl * 0.002);
			break;
		}
	}
	if (ndot <= 7.0) {
		output[(yi * lineSize[0]) + xi] = 0;
	}
}
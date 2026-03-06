using System.IO;
using ImageProcessing;

namespace DotTraceExamplesTaskSolve.Programs;

public class MeanShiftProgram : DotTraceExamplesTaskSolve.Programs.IProgram
{
	public void Run()
	{
		var fileName = @"TestImages\TestImage.jpg";
		using var fileStream = File.OpenRead(fileName);
		var image = RGBImage.FromStream(fileStream);
		image.MeanShiftV1().SaveToFile(Path.Combine(Directory.GetCurrentDirectory(), "TestImages",
			$"{Path.GetFileNameWithoutExtension(fileName)}_Processed.jpg"));
	}
}
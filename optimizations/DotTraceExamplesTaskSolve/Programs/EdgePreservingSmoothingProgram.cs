using System.IO;
using ImageProcessing;

namespace DotTraceExamplesTaskSolve.Programs;

public class EdgePreservingSmoothingProgram : IProgram
{
	public void Run()
	{
		var fileName = @"TestImages\TestImage.jpg";
		using var fileStream = File.OpenRead(fileName);
		var image = RGBImage.FromStream(fileStream);
		image.EdgePreservingSmoothingV1().SaveToFile(Path.Combine(Directory.GetCurrentDirectory(), "TestImages",
			$"{Path.GetFileNameWithoutExtension(fileName)}_Processed.jpg"));
	}
}
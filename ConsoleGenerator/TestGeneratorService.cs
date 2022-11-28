using System.Threading.Tasks.Dataflow;

namespace ConsoleGenerator;

public class TestGeneratorService
{
    private string _filename;
    private int _sourceFileDegreeOfParallelism;
    private int _taskDegreeOfParallelism;
    private int _outFileDegreeOfParallelism;
    private string _outputDirectory;
    private TestGenerator.Core.TestGenerator _testGenerator;

    public TestGeneratorService(string filename, int sourceFileDegreeOfParallelism, int taskDegreeOfParallelism,
        int outFileDegreeOfParallelism, string outputDirectory)
    {
        _filename = filename;
        _taskDegreeOfParallelism = taskDegreeOfParallelism;
        _sourceFileDegreeOfParallelism = sourceFileDegreeOfParallelism;
        _outFileDegreeOfParallelism = outFileDegreeOfParallelism;
        _outputDirectory = outputDirectory;
        _testGenerator = new TestGenerator.Core.TestGenerator();
    }

    public void Generate()
    {
        var getProgramCode = new TransformBlock<string, string>(async filename =>
        {
            char[] buffer = new char[0x1000];
            using (StreamReader reader = File.OpenText(filename))
            {
                await reader.ReadAsync(buffer);
            }

            return buffer.ToString();
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _sourceFileDegreeOfParallelism });

        var generateTests = new TransformBlock<string, string>(async programCode =>
        {
            return _testGenerator.Generate();
        });

        var writeTests = new ActionBlock<string>(async info =>
        {
            using (StreamWriter writer = File.CreateText($"{_outputDirectory}\\{info.ClassName}.cs"))
            {
                writer.WriteAsync(info);
            }
        });

        getProgramCode.LinkTo(generateTests);
        generateTests.LinkTo(writeTests);

        getProgramCode.Post(_filename);
        getProgramCode.Complete();
        writeTests.Completion.Wait();
    }
}
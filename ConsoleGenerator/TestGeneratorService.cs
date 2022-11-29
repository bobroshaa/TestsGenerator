using System.Threading.Tasks.Dataflow;

namespace ConsoleGenerator;

public class TestGeneratorService
{
    private string[] _filenames;
    private int _sourceFileDegreeOfParallelism;
    private int _taskDegreeOfParallelism;
    private int _outFileDegreeOfParallelism;
    private string _outputDirectory;
    private TestGenerator.Core.TestGenerator _testGenerator;

    public TestGeneratorService(string[] filenames, int sourceFileDegreeOfParallelism, int taskDegreeOfParallelism,
        int outFileDegreeOfParallelism, string outputDirectory)
    {
        _filenames = filenames;
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

            return new string(buffer);
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _sourceFileDegreeOfParallelism });

        var generateTests = new TransformBlock<string, List<TestGenerator.Core.TestGenerator.TestInfo>>(programCode =>
        {
            return _testGenerator.Generate(programCode);
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _taskDegreeOfParallelism});

        var writeTests = new ActionBlock<List<TestGenerator.Core.TestGenerator.TestInfo>>(async testInfos =>
        {
            foreach (var testInfo in testInfos)
            {
                using (StreamWriter writer = File.CreateText($"{_outputDirectory}\\{testInfo.ClassName}.cs"))
                {
                    await writer.WriteAsync(testInfo.GeneratedCode);
                }
            }
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _outFileDegreeOfParallelism });
        
        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
        getProgramCode.LinkTo(generateTests, linkOptions);
        generateTests.LinkTo(writeTests,linkOptions);
        foreach (var file in _filenames)
        {
            getProgramCode.Post(file);
            getProgramCode.Complete();
            writeTests.Completion.Wait();
        }

    }
}
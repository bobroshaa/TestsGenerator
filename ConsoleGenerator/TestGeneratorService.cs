using System.Threading.Tasks.Dataflow;
using TestGenerator.Core;

namespace ConsoleGenerator;

public class TestGeneratorService
{
    private string[] _filenames;
    private int _sourceFileDegreeOfParallelism;
    private int _taskDegreeOfParallelism;
    private int _outFileDegreeOfParallelism;
    private string _outputDirectory;
    private UnitTestGenerator _testGenerator;

    private TransformBlock<string, string> _getProgramCode;
    private ActionBlock<UnitTestGenerator.TestInfo> _writeTests;

    public TestGeneratorService(string[] filenames, int sourceFileDegreeOfParallelism, int taskDegreeOfParallelism,
        int outFileDegreeOfParallelism, string outputDirectory)
    {
        _filenames = filenames;
        _taskDegreeOfParallelism = taskDegreeOfParallelism;
        _sourceFileDegreeOfParallelism = sourceFileDegreeOfParallelism;
        _outFileDegreeOfParallelism = outFileDegreeOfParallelism;
        _outputDirectory = outputDirectory;
        _testGenerator = new UnitTestGenerator();
        
        _getProgramCode = new TransformBlock<string, string>(async filename =>
        {
            using var reader = File.OpenText(filename);
            return await reader.ReadToEndAsync();
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _sourceFileDegreeOfParallelism });

        var generateTests = new TransformManyBlock<string, UnitTestGenerator.TestInfo>(programCode =>
        {
            return _testGenerator.Generate(programCode);
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _taskDegreeOfParallelism});

        _writeTests = new ActionBlock<UnitTestGenerator.TestInfo>(async testInfo =>
        {
            await using StreamWriter writer = File.CreateText($"{_outputDirectory}\\{testInfo.ClassName}.cs");
            await writer.WriteAsync(testInfo.GeneratedCode);
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _outFileDegreeOfParallelism });
        
        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
        _getProgramCode.LinkTo(generateTests, linkOptions);
        generateTests.LinkTo(_writeTests,linkOptions);
    }

    public async void Generate()
    {
        foreach (var file in _filenames)
        {
            _getProgramCode.Post(file);
        }
        _getProgramCode.Complete();
        await _writeTests.Completion;
    }
}
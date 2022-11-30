namespace ConsoleGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] filenames;
            Console.Write("Enter filenames through \"*\": ");
            var files = Console.ReadLine();
            if (files != "")
            {
                filenames = files.Split("*");
            }
            else
            {
                Console.WriteLine("Incorrect data!");
                return;
            }

            Console.Write("Enter path to output directory: ");
            string path = Console.ReadLine();
            if (path == "")
            {
                Console.WriteLine("Incorrect data!");
                return;
            }

            Console.Write("Enter degree of parallelism for loading source files: ");
            int sourceFileDegreeOfParallelism;
            try
            {
                sourceFileDegreeOfParallelism = int.Parse(Console.ReadLine());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Incorrect data!");
                return;
            }

            Console.Write("Enter degree of parallelism for : ");
            int taskDegreeOfParallelism;
            try
            {
                taskDegreeOfParallelism = int.Parse(Console.ReadLine());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Incorrect data!");
                return;
            }

            Console.Write("Enter degree of parallelism for loading source files: ");
            int outFileDegreeOfParallelism;
            try
            {
                outFileDegreeOfParallelism = int.Parse(Console.ReadLine());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Incorrect data!");
                return;
            }

            TestGeneratorService testGeneratorService = new TestGeneratorService(filenames,
                sourceFileDegreeOfParallelism, taskDegreeOfParallelism, outFileDegreeOfParallelism, path);
            testGeneratorService.Generate();

        }
    }
}

//C:\Users\bobro\RiderProjects\TestsGenerator\ExampleProject\ExampleProject\ExampleClass.cs*C:\Users\bobro\RiderProjects\TestsGenerator\ExampleProject\ExampleProject\ExampleClass2.cs
//C:\Users\bobro\RiderProjects\TestsGenerator\ExampleProject\Tests
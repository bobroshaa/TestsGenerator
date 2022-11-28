namespace ConsoleGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter filenames through \"*\": ");
            var files = Console.ReadLine();
            if (files != "")
            {
                var filenames = files.Split("*");
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
            try
            {
                int sourceFileDegreeOfParallelism = int.Parse(Console.ReadLine());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Incorrect data!");
                return;
            }

            Console.Write("Enter degree of parallelism for : ");
            try
            {
                int taskDegreeOfParallelism = int.Parse(Console.ReadLine());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Incorrect data!");
                return;
            }

            Console.Write("Enter degree of parallelism for loading source files: ");
            try
            {
                int outFileDegreeOfParallelism = int.Parse(Console.ReadLine());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Incorrect data!");
                return;
            }
            
            



        }
    }
}
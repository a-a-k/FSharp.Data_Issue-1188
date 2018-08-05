namespace FSharp.Data_Issue_1188
{
    using System;
    using Scrappers;
    using MinEnvironment;

    class Program
    {
        static void Main(string[] args)
        {
            var result = (new AptekaruAdapter() as IAdapter).SearchAndParse("30", "презервативы").Result;
            Console.WriteLine($"{result.zoneName} pages - {result.pages.Count}");
        }
    }
}
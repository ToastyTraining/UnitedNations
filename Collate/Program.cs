/////////////////////////////////////////////////////////////////////////////
// Collate
// Part of https://github.com/UNLangAI/Dataset-Tools
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;

namespace Collate
{
public class Program
{
    public static void Main(string[] args)
    {
        string inputDirectoryPath, inputFileNamePattern, outputFilePath;
        Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>\n" +
                          ">>> Collate\n>>> A collation tool\n>>>\n>>> A small tool that is a part of https://github.com/UNLangAI\n" +
                          "<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<\n");
        try {
            // First get directory which has the files
            {
                Console.WriteLine("What is the location of source files? (e.g. E:\\Whatever\\Source Files): ");
                inputDirectoryPath = Console.ReadLine();
                if (!Directory.Exists(inputDirectoryPath)) {
                    Console.WriteLine("Directory does not exist, illegal directory given, please close program and try again!");
                    string r = Console.ReadLine();
                    return;
                }
            }

            // Get extension of documents to collate
            {
                Console.WriteLine("Add file extension of source files (e.g. '*.txt'): ");
                inputFileNamePattern = Console.ReadLine();
                if ("*" + Path.GetExtension("123456abcd" + inputFileNamePattern) != inputFileNamePattern) {
                    Console.WriteLine("Extension is illegal, please close program and try again!");
                    string r = Console.ReadLine();
                    return;
                }
            }

            // Get final destination
            {
                Console.WriteLine("Add destination for final stream (e.g. E:\\Whatever\\Output\\trainingData.txt) - NOTE: CANNOT BE IN SAME DIRECTORY AS SOURCE FILES IF EXTENSION IS SAME! : ");
                outputFilePath  = Console.ReadLine();
                if (!Directory.Exists(Path.GetDirectoryName(outputFilePath))) {
                    Console.WriteLine("Directory for output does not exist, illegal destination given, please close program and try again!");
                    string r = Console.ReadLine();
                    return;
                }
                else if (Path.GetDirectoryName(outputFilePath) == inputDirectoryPath) {
                    Console.WriteLine("Directory for output file equals directory for input, please close program and try again!");
                    string r = Console.ReadLine();
                    return;
                }
            }

            Console.WriteLine("InputDirectoryPath: " + inputDirectoryPath + " inputFileNamePath: " + inputFileNamePattern);

            string[] inputFilePaths = Directory.GetFiles(inputDirectoryPath, inputFileNamePattern);
            Console.WriteLine("Number of files: {0}.", inputFilePaths.Length);
            using (var outputStream = File.Create(outputFilePath))
            {
                foreach (var inputFilePath in inputFilePaths)
                {
                    using (var inputStream = File.OpenRead(inputFilePath))
                    {
                        // Buffer size can be passed as the second argument.
                        inputStream.CopyTo(outputStream);
                    }
                    Console.WriteLine("The file {0} has been processed.", inputFilePath);
                }
            }

            // Reopen file and re-save to UTF8
            string readText = File.ReadAllText(outputFilePath);
            File.WriteAllText(outputFilePath, readText, Encoding.UTF8);
        } catch {
            Console.WriteLine("The application has been faced with an exception, press any key to shut program...");
            string r = Console.ReadLine();
            return;
        }
    }
}
}
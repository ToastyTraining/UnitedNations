/////////////////////////////////////////////////////////////////////////////
// BulkFilesToTXT
// Part of https://github.com/UNLangAI/Dataset-Tools
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Linq;

namespace CheckIfPresent
{
	class Program
	{
		public static void Main(string[] args)
		{
			string inputDirectoryPath;
			
	        Console.WriteLine	(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>\n" +
	                             ">>> CheckIfPresent\n>>> To check if corresponding text files are missing\n>>>\n>>> A small tool that is a part of https://github.com/UNLangAI\n" +
	                             "<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
	        
			// Get directory which has the files
	        {
	            Console.WriteLine("What is the location of source files? (e.g. E:\\Whatever\\Source Files): ");
	            inputDirectoryPath = Console.ReadLine();
	            if (!System.IO.Directory.Exists(inputDirectoryPath)) {
	                Console.WriteLine("Directory does not exist, illegal directory given, please close program and try again!");
	                string r = Console.ReadLine();
	                return;
	            }
	        }
			
			string[] listOfTemps = System.IO.Directory.GetFiles(inputDirectoryPath, @"*.doc").Concat(System.IO.Directory.GetFiles(inputDirectoryPath, @"*.docx").Concat(System.IO.Directory.GetFiles(inputDirectoryPath, @"*.wpf"))).ToArray();
			
			int p = 0;
			foreach (string fileName in listOfTemps) {
				if (!(System.IO.File.Exists(System.IO.Path.ChangeExtension(fileName, null) + ".txt"))) {
					Console.WriteLine(fileName + " does not have corresponding text file"); p++;
				}
			}
			
			if (p == 0)
				Console.WriteLine("There are no files that don't have corresponding text files");
			
			Console.ReadLine();
		}
	}
}
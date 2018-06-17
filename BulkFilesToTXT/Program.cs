/////////////////////////////////////////////////////////////////////////////
// BulkFilesToTXT
// Part of https://github.com/UNLangAI/Dataset-Tools
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Office.Interop.Word;

namespace BulkFilesToTXT
{
class Program
{
    public static void Main(string[] args)
    {
        string inputDirectoryPath;
        string docxFilter = "*.docx", docFilter = "*.doc", wpfFilter = "*.wpf"; // NOTE: WORDPERFECT FORM CONVERSION WORKS ONLY ON WORD 15.0 ONWARDS
        int[] iCharStr = {1, 2, 3}; bool isWordVersionSupported = false;
        
        Console.WriteLine	(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>\n" +
                             ">>> BulkFilesToTXT\n>>> A bulk DOC(X) to TXT converter\n>>>\n>>> A small tool that is a part of https://github.com/UNLangAI\n" +
                             "<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<\nPlease wait for WordPerfect conversion compatibility check....");

        // Run a temporary instance just to check if version does allow for WordPerfect Conversions, and ask if users are interested?
        {
	        Microsoft.Office.Interop.Word.Application appVersion = new Microsoft.Office.Interop.Word.Application();
	        appVersion.Visible = false;
	        isWordVersionSupported = (Int32.Parse(new String(appVersion.Version.ToString().Where(Char.IsDigit).ToArray())) >= 160); // We mean 16.0 but Int32.Parse interprets it as 160
			((_Application)appVersion).Quit();
				
			if (isWordVersionSupported) {
				Console.WriteLine(">>> NOTE: WORDPERFECT (*.WPF) FILE CONVERSION IS SUPPORTED!\n");
				Console.WriteLine("Do you want to process WordPerfect Form files? [Y/N - def. N]: ");
				string Pr = Console.ReadLine().ToUpper();
				if (!(Pr == "Y" || Pr == "N")) {
					Console.WriteLine("Illegal response, defaulting to not processing WordPerfect Form files"); isWordVersionSupported = false;
				} else isWordVersionSupported = (Pr == "Y");
			}
			else
				Console.WriteLine(">>> NOTE: WORDPERFECT (*.WPF) FILE CONVERSION IS NOT SUPPORTED!\n");
	    }
     
        // Get directory which has the files
        {
            Console.WriteLine("What is the location of source files? (e.g. E:\\Whatever\\Source Files): ");
            inputDirectoryPath = Console.ReadLine();
            if (!Directory.Exists(inputDirectoryPath)) {
                Console.WriteLine("Directory does not exist, illegal directory given, please close program and try again!");
                string r = Console.ReadLine();
                return;
            }
        }

        Parallel.ForEach(iCharStr, new ParallelOptions { MaxDegreeOfParallelism = 3 }, (x) =>
        {
           	string[] inputFilePaths = {}, rawFilePaths = {};
            string savePath = "";

            // Get rid of "temporary" files from bad Word launches
            string[] listOfTemps = Directory.GetFiles(inputDirectoryPath, @"~*.doc").Concat(Directory.GetFiles(inputDirectoryPath, @"~*.docx").Concat(Directory.GetFiles(inputDirectoryPath, @"~*.wpf").Concat(Directory.GetFiles(inputDirectoryPath, @"~*.txt").Concat(Directory.GetFiles(inputDirectoryPath, @"~*.tmp"))))).ToArray();
            foreach (string delStr in listOfTemps) { try { File.Delete(delStr); } catch { /* ... */} } // Simply delete them, no questions asked
            
            // Make the program do different work as per loops, and add directory information to our orig array
            if (x == 1) {
                rawFilePaths = Directory.GetFiles(inputDirectoryPath, docFilter);
                savePath = inputDirectoryPath;
            } else if (x == 2) {
                rawFilePaths = Directory.GetFiles(inputDirectoryPath, docxFilter);
                savePath = inputDirectoryPath;
            } else if (x == 3 && isWordVersionSupported) {
            	rawFilePaths = Directory.GetFiles(inputDirectoryPath, wpfFilter);
            	savePath = inputDirectoryPath;
            }

            // First get *rid* of processed files in the to-be-processed array by filtering them out
            List<String> finalPaths = inputFilePaths.ToList(); // inputFilePaths has nothing in it (yet), we just use this to initialise the list
            foreach (string locStr in rawFilePaths) {
            	if (!(File.Exists(Path.Combine(savePath, (Path.GetFileNameWithoutExtension(locStr)+".txt"))))) {
            		finalPaths.Add(locStr);
                }
            }
            
            // Add that to processing list array
            inputFilePaths = finalPaths.ToArray();
            
            Parallel.ForEach(inputFilePaths, new ParallelOptions { MaxDegreeOfParallelism = 8 }, (inputFilePath) =>
            {
             	try {
	                bool isItProcessed = true; // Assume it is
	
	                Console.WriteLine("Processing {0} on thread {1}", inputFilePath, Thread.CurrentThread.ManagedThreadId);
	               
	                Microsoft.Office.Interop.Word.Application application = null;
	                
	                // TODO: DO THIS MORE ELEGANTLY
	                if (x == 3 && isWordVersionSupported) {
	                	
	                	// If we have a DOCX file already existing, get rid of it
	                	if (File.Exists(Path.Combine(savePath, (Path.GetFileNameWithoutExtension(inputFilePath)+".docx")))) {
	                		File.Delete(Path.Combine(savePath, (Path.GetFileNameWithoutExtension(inputFilePath)+".docx")));
	                	}
	                	
	                	try {
	                		application = new Microsoft.Office.Interop.Word.Application(); // new attempt
		                    Document document = application.Documents.Open(inputFilePath);
		                    application.ActiveDocument.SaveAs(Path.Combine(savePath, (Path.GetFileNameWithoutExtension(inputFilePath)+".txt")), WdSaveFormat.wdFormatText);
						} catch {
	                		Console.WriteLine("Unable to process the file {0}", inputFilePath);
	                		return;
	                	} finally {
	                		((_Application)application).Quit(); // Kill this instance
	                	}
	                	
	                	Console.WriteLine("The file {0} has been processed.", inputFilePath);
	                	return;
	                }
	
	                try {
	                	application = new Microsoft.Office.Interop.Word.Application(); // Start new instance, as new attempt
	                    Document document = application.Documents.Open(inputFilePath);
	                    application.ActiveDocument.SaveAs(Path.Combine(savePath, (Path.GetFileNameWithoutExtension(inputFilePath)+".txt")), WdSaveFormat.wdFormatText);
	                } catch (System.Runtime.InteropServices.COMException e) {
	                    isItProcessed = false; // The file extension for file saved is WRONG, not use .docx --> .doc or vice-versa
	                } finally { 
	                	((_Application)application).Quit();
	                }
	
	                if(isItProcessed)
	                    Console.WriteLine("The file {0} has been processed.", inputFilePath);
	                else {
	                    if (Path.GetExtension(inputFilePath) == ".doc") {
	                        File.Copy(inputFilePath, Path.Combine(savePath, (Path.GetFileNameWithoutExtension(inputFilePath)+".docx")));
	                        File.Delete(inputFilePath);
	                        inputFilePath = Path.Combine(savePath, (Path.GetFileNameWithoutExtension(inputFilePath)+".docx"));
	                        return;
	                    } else if (Path.GetExtension(inputFilePath) == ".docx") {
	                        File.Copy(inputFilePath, Path.Combine(savePath, (Path.GetFileNameWithoutExtension(inputFilePath)+".doc")));
	                        File.Delete(inputFilePath);
	                        inputFilePath = Path.Combine(savePath, (Path.GetFileNameWithoutExtension(inputFilePath)+".doc"));
	                        return;
	                    }
	
	                    try {
	                		application = new Microsoft.Office.Interop.Word.Application();
	                        Document document = application.Documents.Open(inputFilePath);
	                        application.ActiveDocument.SaveAs(Path.Combine(savePath, (Path.GetFileNameWithoutExtension(inputFilePath)+".txt")), WdSaveFormat.wdFormatText);
	                    } catch (System.Runtime.InteropServices.COMException e) {
	                        Console.WriteLine("Unable to process the file {0}", inputFilePath);
	                	} finally {
	                		((_Application)application).Quit();
	                	}
	                }
                } catch { Console.WriteLine("Unexpected error occured! Program will continue as if nothing happened"); return; } // Skip ahead! 
            });
        });
    }
}
}

/////////////////////////////////////////////////////////////////////////////
// ResolutonFetcher
// Part of https://github.com/UNLangAI/Dataset-Tools
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Threading;

///
/// CookieAwareWebClient
///
/// Courtesy: https://stackoverflow.com/questions/14551345/accept-cookies-in-webclient
///

public class CookieAwareWebClient : WebClient
{
    public CookieContainer CookieContainer {
        get;
        set;
    }
    public Uri Uri {
        get;
        set;
    }

    public CookieAwareWebClient()
    : this(new CookieContainer())
    {
    }

    public CookieAwareWebClient(CookieContainer cookies)
    {
        this.CookieContainer = cookies;
    }

    protected override WebRequest GetWebRequest(Uri address)
    {
        WebRequest request = base.GetWebRequest(address);
        if (request is HttpWebRequest)
        {
            (request as HttpWebRequest).CookieContainer = this.CookieContainer;
        }
        HttpWebRequest httpRequest = (HttpWebRequest)request;
        httpRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        return httpRequest;
    }

    protected override WebResponse GetWebResponse(WebRequest request)
    {
        WebResponse response = base.GetWebResponse(request);
        String setCookieHeader = response.Headers[HttpResponseHeader.SetCookie];

        //do something if needed to parse out the cookie.
        if (setCookieHeader != null)
        {
            Cookie cookie = new Cookie(); //create cookie
            this.CookieContainer.Add(cookie);
        }

        return response;
    }
}

namespace ResolutonFetcher
{
public class Program
{
    ///
    /// Checks the file exists or not.
    ///
    /// The URL of the remote file.
    /// True : If the file exits, False if file not exists
    ///
    /// Courtesy: https://stackoverflow.com/a/3808841
    ///

    public static bool RemoteFileExists(string url)
    {
        try
        {
            // Creating the HttpWebRequest
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            // Setting the Request method HEAD, you can also use GET too.
            request.Method = "HEAD";
            // Getting the Web Response.
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            // Returns TRUE if the Status code == 200
            response.Close();
            return (response.StatusCode == HttpStatusCode.OK);
        }
        catch
        {
            //Any exception will returns false.
            return false;
        }
    }

    ///
    /// Attempts to retrieve the final redirected URL of a given URL
    ///
    /// Courtesy: https://stackoverflow.com/a/28424940
    ///

    public static string GetFinalRedirect(string url)
    {
        if(string.IsNullOrWhiteSpace(url))
            return url;

        int maxRedirCount = 8;  // prevent infinite loops
        string newUrl = url;
        do
        {
            HttpWebRequest req = null;
            HttpWebResponse resp = null;
            try
            {
                req = (HttpWebRequest) HttpWebRequest.Create(url);
                req.Method = "HEAD";
                req.AllowAutoRedirect = false;
                resp = (HttpWebResponse)req.GetResponse();
                switch (resp.StatusCode)
                {
                case HttpStatusCode.OK:
                    return newUrl;
                case HttpStatusCode.Redirect:
                case HttpStatusCode.MovedPermanently:
                case HttpStatusCode.RedirectKeepVerb:
                case HttpStatusCode.RedirectMethod:
                    newUrl = resp.Headers["Location"];
                    if (newUrl == null)
                        return url;

                    if (newUrl.IndexOf("://", System.StringComparison.Ordinal) == -1)
                    {
                        // Doesn't have a URL Schema, meaning it's a relative or absolute URL
                        Uri u = new Uri(new Uri(url), newUrl);
                        newUrl = u.ToString();
                    }
                    break;
                default:
                    return newUrl;
                }
                url = newUrl;
            }
            catch (WebException)
            {
                // Return the last known good URL
                return newUrl;
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
                if (resp != null)
                    resp.Close();
            }
        } while (maxRedirCount-- > 0);

        return newUrl;
    }

    ///
    /// ProcessDocument
    ///
    /// Fetches document from brute-forced link and attempts to download it
    ///
    public static void ProcessDocument(string processStr, string fileName, string saveLocation, string[] alreadyDownloadedFilesList) {

        fileName = fileName.Replace("/","-"); // Because "/" is not part of a valid Windows filename

        // If file already exists, why bother? (NOTE: THIS IS PLACED ABOVE AS TO PREVENT INTERNET INTERACTION TO EXPEDITE PROCESS)
        foreach (string possibleSavedFile in alreadyDownloadedFilesList) {
        	Console.WriteLine("Checking if possiblity can be " + possibleSavedFile);
        	if (saveLocation + "\\" + fileName == System.IO.Path.ChangeExtension(possibleSavedFile, null)) {
        		Console.WriteLine("[Fail] Attempting to download "+ fileName +" which is already existing (estimation)");
        		return;
        	}
        }

        Console.WriteLine("[Processing] Using download link for Resolution " + fileName +" as " + processStr);

        string fileExtension = Path.GetExtension(GetFinalRedirect(processStr)).ToLower(); // We can't use this redirect link directly as cookie is not generated, thus - prohibiting download
        string saveFileName = saveLocation + "\\" + fileName + fileExtension; // Location of (soon to be) saved file
        
        // We don't work with links whose file extension we can't use
        if (!(fileExtension == ".docx" || fileExtension == ".doc" || fileExtension == ".wpf")) {
            Console.WriteLine("[Fail] Attempting to download " + fileName + fileExtension + " which we can't process");
            return;
        }

        Console.WriteLine("[Processing] Saving processed file to " + saveFileName);

        CookieContainer cookieJar = new CookieContainer();
        cookieJar.Add(new Cookie("noCookie", "helloWorld", "/", "noSite"));

        using (WebClient myWebClient = new CookieAwareWebClient(cookieJar))
        {
            // Download the Web resource and save it into the current filesystem folder.
            try {
                myWebClient.DownloadFile(processStr, saveFileName);
                Console.WriteLine("[Processing] Saved file to " + saveFileName);
            } catch {
                return;
            }
        }

        Console.WriteLine("[Success] File Processed as " + saveFileName);
    }

    public static void Main(string[] args)
    {
        // Initialise variables
        int numOfThreads = 0, assemblyNo = 0, resolutionNo = 0;
        string storageDir;
        bool generalAssembly = true;

        Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>\n" +
                          ">>> ResolutionFetcher\n>>> A Resolution Fetching Tool\n>>>\n>>> A small tool that is a part of https://github.com/UNLangAI\n" +
                          "<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<\n");

        {
            Console.WriteLine("Should we fetch UNGA or UNSC Documents? [default: UNGA, options: UNGA, UNSC]: ");
            string result = Console.ReadLine();
            if (result == "UNGA")
                generalAssembly = true;
            else if (result == "UNSC")
                generalAssembly = false;
            else
                Console.WriteLine("Illegal response given, defaulting for UNGA!");
        }

        // Get number of threads to run when running loop
        {
            Console.WriteLine("How many simultaneous threads would you like to run [min. 1, max. 64]: ");

            try {
                numOfThreads = Int32.Parse(new String(Console.ReadLine().Where(Char.IsDigit).ToArray()));
            } catch {
                Console.WriteLine("ERROR: Illegal number provided, defaulting to 1 thread!");
                numOfThreads = 1;
            }

            if (numOfThreads < 1) {
                Console.WriteLine("Defaulting to 1 thread as number of threads illegal!\n");
                numOfThreads = 1;
            } else if (numOfThreads > 64) {
                Console.WriteLine("Defaulting to 64 threads as number of threads illegal!\n");
                numOfThreads = 64;
            }
        }

        // Get storage location for saving files
        {
            Console.WriteLine("Where do you wish to store the fetched documents?: [e.g. D:\\Resolutions]: ");
            storageDir = Console.ReadLine();

            if (!Directory.Exists(storageDir)) {
                Console.WriteLine("Attempting to create new directory as given directory does not exist");
                try {
                    Directory.CreateDirectory(storageDir);
                    if (Directory.Exists(storageDir)) {
                        Console.WriteLine("Directory created successfully");
                    }
                }
                catch {
                    Console.WriteLine("ERROR: Could not create new directory, restart program, give new directory and try again!");
                    string noUseStr = Console.ReadLine();
                    return;
                }
            }
        }

        // Get Resolutions Per Session (r/s)
        if (generalAssembly)
        {
            // If working to retrieve UNGA files
            Console.WriteLine("What should be the maximum resolutions per session?: [default: 300]: ");

            try {
                resolutionNo = Int32.Parse(new String(Console.ReadLine().Where(Char.IsDigit).ToArray()));
            }
            catch {
                Console.WriteLine("Illegal number provided, defaulting to 300!");
                resolutionNo = 300;
            }

            if (resolutionNo < 1) {
                Console.WriteLine("Defaulting to 300 resolution per session as number provided is illegal!\n");
                resolutionNo = 300;
            }
        } else {
            // If working to retrieve UNSC files
            Console.WriteLine("Till which maximum resolution number should brute-force be applied per session?: [default: 2417, min. 822]: ");

            try {
                resolutionNo = Int32.Parse(new String(Console.ReadLine().Where(Char.IsDigit).ToArray()));
            }
            catch {
                Console.WriteLine("Illegal number provided, defaulting to 2417!");
                resolutionNo = 2417;
            }

            if (resolutionNo < 822) {
                Console.WriteLine("Defaulting to 2417 resolutions per session as number provided is illegal!\n");
                resolutionNo = 2417;
            }
        }

        // Get Session Numbers
        if (generalAssembly) {
            Console.WriteLine("Till which UNGA Session should files be retrieved?: [min. 48, default: 72]: ");

            try {
                assemblyNo = Int32.Parse(new String(Console.ReadLine().Where(Char.IsDigit).ToArray()));
            }
            catch {
                Console.WriteLine("Illegal number provided, defaulting to 72nd session!");
                assemblyNo = 72;
            }

            if (resolutionNo < 48) {
                Console.WriteLine("Defaulting to max 72nd session as number provided is illegal!\n");
                assemblyNo = 72;
            }
        } else {
            Console.WriteLine("Till which UNSC Session should files be retrieved?: [min. 1993, default: 2018]: ");

            try {
                assemblyNo = Int32.Parse(new String(Console.ReadLine().Where(Char.IsDigit).ToArray()));
            }
            catch {
                Console.WriteLine("Illegal number provided, defaulting to the 2018 session!");
                assemblyNo = 2018;
            }

            if (resolutionNo < 1993) {
                Console.WriteLine("Defaulting to the 2018 session as number provided is illegal!\n");
                assemblyNo = 2018;
            }
        }

        // Start using loops to fetch files
        {
            int minAsmNo = 48, minResNo = 1;

            if (!generalAssembly) {
                minAsmNo = 1993;
                minResNo = 822;
            }

            // First, get a list of files that are present in that format
            string[] alreadyDownloadedFilesList = 	Directory.GetFiles(storageDir, "*.doc").Concat(Directory.GetFiles(storageDir, "*.docx").Concat(Directory.GetFiles(storageDir, "*.wpf"))).ToArray();
            
            for (int loopAsmNo = minAsmNo; loopAsmNo <= assemblyNo; loopAsmNo++) {
                for (int loopResNo = minResNo; loopResNo <= resolutionNo; loopResNo = loopResNo+numOfThreads) {
                    WaitHandle[] waitHandles = new WaitHandle[numOfThreads];
                    for (int i = 0; i < numOfThreads; i++)
                    {
                        var j = i;
                        var handle = new EventWaitHandle(false, EventResetMode.ManualReset);
                        var thread = new Thread(() =>
                        {
                            string stringUN;
                            // Access string is different for both cases
                            if (generalAssembly) {
                                stringUN = "http://daccess-ods.un.org/access.nsf/GetFile?OpenAgent&DS=A/RES/" + loopAsmNo +"/" + (loopResNo+j) + "&Lang=E&Type=DOC";
                            } else {
                                stringUN = "http://daccess-ods.un.org/access.nsf/GetFile?OpenAgent&DS=S/RES/" + (loopResNo+j) +"(" + loopAsmNo + ")&Lang=E&Type=DOC";
                            }

                            Console.WriteLine("[Attempt] Processing brute-force generated link: " + stringUN);

                            if (RemoteFileExists(stringUN)) {
                                Console.WriteLine("[Processing] Initialise usage of link " + stringUN);
                                // Name difference of "A" and "S"
                                if (generalAssembly) {
                                    ProcessDocument(stringUN, "A/RES/" + loopAsmNo +"/" + (loopResNo+j), storageDir, alreadyDownloadedFilesList);
                                } else {
                                    ProcessDocument(stringUN, "S/RES/" + loopAsmNo +"/" + (loopResNo+j), storageDir, alreadyDownloadedFilesList);
                                }
                            } else Console.WriteLine("[Processing] Cannot process " + stringUN);

                            handle.Set();
                        });
                        waitHandles[j] = handle;
                        thread.Start();
                    }
                    WaitHandle.WaitAll(waitHandles);
                }
            }
        }
    }
}
}
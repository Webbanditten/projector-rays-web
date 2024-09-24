using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ProjectorRaysWeb.CloudflareTurnstile;
using ProjectorRaysWeb.Models;

namespace ProjectorRaysWeb.Controllers;

public class HomeController(CloudflareTurnstileProvider cloudflareTurnstileProvider) : Controller
{
    public IActionResult Index()
    {
        return View();
    }
        
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadFile(UploadFileModel model,
        [FromForm(Name = "cf-turnstile-response")] string turnstileToken)
    {
        // Validate asp.net core token
        if (!ModelState.IsValid)
            return Content("Invalid token");
        
        
        // if cloudflare header ip is not present, use the user's ip
        var userIpAddress = HttpContext.Connection.RemoteIpAddress;
        if (Request.Headers.ContainsKey("CF-Connecting-IP"))
        {
            userIpAddress = IPAddress.Parse(Request.Headers["CF-Connecting-IP"]);
        }

        // verify token
        CloudflareTurnstileVerifyResult cftResult = await cloudflareTurnstileProvider
            .Verify(turnstileToken, userIpAddress: userIpAddress);
      
        if(!cftResult.Success)
            return Content("Invalid token from Cloudflare Turnstile");
        
        
        var files = model.Files;
        
        if (files == null || !files.Any())
            return Content("No files selected");

        // Create new folder with GUID in uploads folder
        var folderName = Guid.NewGuid().ToString();
        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "uploads", folderName);

        if (!Directory.Exists(uploads))
            Directory.CreateDirectory(uploads);

        if (!Directory.Exists(Path.Combine(uploads, "output")))
            Directory.CreateDirectory(Path.Combine(uploads, "output"));

        var pathToExe = Path.Combine(Directory.GetCurrentDirectory(), "executables", "projector_rays.exe");

        foreach (var file in files)
        {
            // Check file extension
            var extension = Path.GetExtension(file.FileName);
            if (extension != ".cct" && extension != ".dcr")
                return Content($"Invalid file extension for {file.FileName}");

            // Save file to folder
            var filePath = Path.Combine(uploads, file.FileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Run exe with parameter of file path and folder name
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pathToExe,
                    Arguments = $"decompile {filePath} -o {Path.Combine(uploads, "output")}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
        }
        
        if (!Directory.EnumerateFiles(Path.Combine(uploads, "output")).Any()) {
            Directory.Delete(uploads, true);
            return View("Error", new UploadErrorViewModel { Message = "Your file seems to not generate any output" });
        }
        
        return RedirectToAction("Result", new { folderName });
    }
    
    public IActionResult Result(string folderName)
    {
        return View(folderName);
    }
    
    // Show result page
    public async Task<IActionResult> Download(string folderName)
    {
        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "uploads", folderName);
        

        // Zip the output folder
        var zipPath = Path.Combine(uploads, "output.zip");
        ZipFile.CreateFromDirectory(Path.Combine(uploads, "output"), zipPath);

        // Read the zip file
        var memory = new MemoryStream();
        using (var stream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            await stream.CopyToAsync(memory);
        }
        memory.Position = 0;

        // Delete the output folder
        Directory.Delete(uploads, true);
        
        // Return the file then redirect to result page
        
        return File(memory, "application/zip", "output.zip");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
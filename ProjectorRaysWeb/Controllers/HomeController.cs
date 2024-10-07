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
      // Try parse ip
      if (IPAddress.TryParse(Request.Headers["CF-Connecting-IP"], out var cfIp))
      {
        userIpAddress = cfIp;
      }
    }

    // verify token
    CloudflareTurnstileVerifyResult cftResult = await cloudflareTurnstileProvider
        .Verify(turnstileToken, userIpAddress: userIpAddress);

    if (!cftResult.Success)
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
          Arguments = $"decompile \"{filePath}\" -o {Path.Combine(uploads, "output")}",
          RedirectStandardOutput = true,   // Capture standard output
          RedirectStandardError = true,    // Optionally capture error output
          UseShellExecute = false,
          CreateNoWindow = true
        }
      };

      // Start the process
      process.Start();

      // Read the standard output
      string output = await process.StandardOutput.ReadToEndAsync();

      // Optionally read standard error
      string errorOutput = await process.StandardError.ReadToEndAsync();

      // Wait for process to exit
      await process.WaitForExitAsync();

      if (!string.IsNullOrEmpty(errorOutput))
      {
        Console.WriteLine("Error running ProjectorRays: " + errorOutput);
      }
    }

    if (model.ExportAssets)
    {
      Console.WriteLine("Exporting assets for " + folderName);
      var pathToDirectorCastRipper = Path.Combine(Directory.GetCurrentDirectory(), "executables", "DirectorCastRipper", "DirectorCastRipper.exe");

      foreach (var file in files)
      {
        try
        {
          var filePath = Path.Combine(uploads, file.FileName);

          // Run exe with parameter of file path and folder name
          var process = new Process
          {
            StartInfo = new ProcessStartInfo
            {
              FileName = pathToDirectorCastRipper,
              Arguments = $"--cli --dismiss-dialogs --include-names --member-types image sound text --output-folder {Path.Combine(uploads, "output")} --files \"{filePath}\"",
              RedirectStandardOutput = true,   // Capture standard output
              RedirectStandardError = true,    // Optionally capture error output
              RedirectStandardInput = true,
              UseShellExecute = false,
              CreateNoWindow = false,

            }
          };

          // Start the process
          process.Start();

          // Send an "enter" or line break to finish the program
          await process.StandardInput.WriteLineAsync();
          await process.StandardInput.FlushAsync();
          process.StandardInput.Close();  // Close input stream after sending the line break

          // Read the standard output and error concurrently
          var outputTask = process.StandardOutput.ReadToEndAsync();
          var errorTask = process.StandardError.ReadToEndAsync();

          // Wait for both tasks to complete
          await Task.WhenAll(outputTask, errorTask);

          // Wait for the process to exit
          await process.WaitForExitAsync();

          // Get the results
          var output = await outputTask;
          var errorOutput = await errorTask;

          if (!string.IsNullOrEmpty(errorOutput))
          {
            //return Content($"Error: {errorOutput}");
            throw new Exception(errorOutput);
          }

          // Handle or log the output if needed
          if (!string.IsNullOrEmpty(output))
          {
            // Optionally handle the output (log it, save it, etc.)
          }

          Console.WriteLine("Output of asset extraction: " + folderName + "\n\n" + output);
        }
        catch (Exception e)
        {
          Console.WriteLine("Error exporting assets for " + folderName + ": " + e.Message);
        }
      }

    }

    if (!Directory.EnumerateFiles(Path.Combine(uploads, "output")).Any())
    {
      Directory.Delete(uploads, true);
      return View("Error", new UploadErrorViewModel { Message = "The files uploaded does not seem to not generate any output" });
    }

    return RedirectToAction("Result", new { folderName });
  }

  public IActionResult Result(string folderName)
  {
    return View(new ResultPageViewModel { FolderName = folderName });
  }

  // Show result page
  public async Task<IActionResult> Download(string folderName)
  {
    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "uploads", folderName);
    // If folder exists
    if (!Directory.Exists(uploads))
    {
      return View("Error", new UploadErrorViewModel { Message = "Output folder does not exist. This means you've probably already downloaded the files once." });
    }

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
    // Make the guid/foldername short for the user
    var shortFolderName = folderName[..6];

    return File(memory, "application/zip", shortFolderName + "-output.zip");
  }

  [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
  public IActionResult Error()
  {
    return View("SysError", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
  }
}

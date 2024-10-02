namespace ProjectorRaysWeb.Models;

public class UploadFileModel
{
    public List<IFormFile> Files { get; set; }
    public bool ExportImages { get; set; }
}
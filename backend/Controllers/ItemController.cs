
using CloudCore.Models;
using DotNetEnv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
namespace CloudCore.Controllers
{
    [ApiController]
    [Route("user/{userid}/mydrive")]
    public class ItemController : ControllerBase
    {

        private readonly CloudCoreDbContext _context;


        public ItemController(CloudCoreDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Item>>> GetItems(int userId, int? parentId)
        {
            var userFiles = await _context.Items
                .Where(item => item.UserId == userId && item.IsDeleted == false && item.ParentId == parentId)
                .ToListAsync();

            if (userFiles == null || !userFiles.Any())
                return NotFound("No files found for this user.");

            return Ok(userFiles);
        }


        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadFile(int id)
        {

            var item = await _context.Items
        .Where(i => i.Id == id && i.IsDeleted == false && i.Type == "file")
        .FirstOrDefaultAsync();

            if (item == null)
                return NotFound("File not found or is not a file.");

            if (string.IsNullOrEmpty(item.FilePath))
                return NotFound("File path is empty.");

            var filePath = Path.Combine(Environment.GetEnvironmentVariable("FileStorage__BasePath"), item.FilePath);

            var directory = Path.GetDirectoryName(filePath);


            if (!System.IO.File.Exists(filePath))
                return Ok($"file not found. {filePath}");

            return PhysicalFile(filePath, item.MimeType ?? "application/octet-stream", item.Name);
        }

        
    }
}
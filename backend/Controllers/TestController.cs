using Microsoft.AspNetCore.Mvc;

namespace CloudCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { 
                message = "Backend работает!", 
                timestamp = DateTime.UtcNow,
                status = "OK"
            });
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { 
                status = "healthy",
                service = "FileStorage API",
                version = "1.0.0"
            });
        }

        [HttpPost("echo")]
        public IActionResult Echo([FromBody] object data)
        {
            return Ok(new { 
                received = data,
                echo = "Получено успешно",
                timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("files")]
        public IActionResult GetFiles()
        {
            var fakeFiles = new[]
            {
                new { id = 1, name = "document.pdf", type = "file", size = 1024 },
                new { id = 2, name = "My Folder", type = "folder", size = 0 },
                new { id = 3, name = "photo.jpg", type = "file", size = 2048 }
            };

            return Ok(new { 
                files = fakeFiles,
                count = fakeFiles.Length,
                message = "Тестовые данные"
            });
        }
    }
}
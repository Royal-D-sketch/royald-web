import re
import os

# Update DebtorController
with open('Controllers/DebtorController.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Add IWebHostEnvironment
content = content.replace('private readonly DebtorService _svc;', 'private readonly DebtorService _svc;\n        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;')
content = content.replace('public DebtorController(AppDbContext db, DebtorService svc)', 'public DebtorController(AppDbContext db, DebtorService svc, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)')
content = content.replace('_svc = svc;', '_svc = svc;\n            _env = env;')

# Add File Upload Helper
helper = '''
        private async Task<string> UploadFileAsync(Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0) return null;
            var uploads = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploads, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return "/uploads/" + fileName;
        }
'''
if 'UploadFileAsync' not in content:
    content = content.replace('public async Task<IActionResult> UpdateStatus', helper + '\n        [HttpPost, ValidateAntiForgeryToken]\n        public async Task<IActionResult> UpdateStatus')

# Save back
with open('Controllers/DebtorController.cs', 'w', encoding='utf-8') as f:
    f.write(content)

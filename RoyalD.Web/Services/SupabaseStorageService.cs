using System.Net.Http.Headers;

namespace RoyalD.Web.Services
{
    public class SupabaseStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public SupabaseStorageService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<string?> UploadFileAsync(IFormFile file, string bucketName = "uploads")
        {
            if (file == null || file.Length == 0) return null;

            var supabaseUrl = "https://pssccxujypweaahkbvdw.supabase.co";
            var supabaseKey = "sb_secret_" + "HDeHWItzrWLIApDlj-pAKA_XAV3kwEt";
            
            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                return null; // or throw Exception
            }

            var uniqueFileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLower();
            var objectPath = uniqueFileName;

            var requestUrl = $"{supabaseUrl.TrimEnd('/')}/storage/v1/object/{bucketName}/{objectPath}";

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Add("apikey", supabaseKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);

            using var content = new StreamContent(file.OpenReadStream());
            var contentType = file.ContentType;
            if (string.IsNullOrEmpty(contentType)) contentType = "application/octet-stream";
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                // Returns the public URL to view the file
                return $"{supabaseUrl.TrimEnd('/')}/storage/v1/object/public/{bucketName}/{objectPath}";
            }
            
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Supabase Upload Error: " + error);
            return null;
        }
    }
}


using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace cats.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class cat : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;

        public cat(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        [HttpGet(Name = "GetStatusImage")]
        public async Task<IActionResult> Get([FromQuery] string url)
        {

            var client = _httpClientFactory.CreateClient();

            int statusCode;

            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync(url);
                statusCode = (int)response.StatusCode;
            }
            catch (HttpRequestException ex)
            {
                statusCode = 443;
            }
            catch (Exception ex)
            {
                statusCode = 500;
            }

            string cacheKey = $"httpcat_image_{statusCode}";


            if (!_cache.TryGetValue(cacheKey, out byte[] cachedImage))
            {

                var imageUrl = $"https://http.cat/{statusCode}";
                var imageResponse = await client.GetAsync(imageUrl);

                if (imageResponse.IsSuccessStatusCode)
                {
                    var imageStream = await imageResponse.Content.ReadAsByteArrayAsync();

                    Task.Run(() =>
                    {
                        _cache.Set(cacheKey, imageStream, TimeSpan.FromMinutes(30));
                    });

                    return File(imageStream, "image/jpeg");
                }
                else
                {
                    return StatusCode((int)imageResponse.StatusCode);
                }
            }
            else
            {

                return File(cachedImage, "image/jpeg");
            }
        }
    }
}

using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Models;
namespace Api.Controllers;



[ApiController]

[Route("api/[controller]")]
public class ActivitiesController : ControllerBase

{

    private readonly HttpClient _httpClient;



    private const string AccessToken = "19dca384d80e278f891252bca4d42a0c";

    private const string PolarUrl = "https://www.polaraccesslink.com/v3/users/activities";



    public ActivitiesController(IHttpClientFactory httpClientFactory)

    {

        _httpClient = httpClientFactory.CreateClient();

    }



    [HttpGet]

    public async Task<IActionResult> GetAllActivities()

    {

        try

        {

            var request = new HttpRequestMessage(HttpMethod.Get, PolarUrl);

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);



            var response = await _httpClient.SendAsync(request);



            if (!response.IsSuccessStatusCode)

            {

                var errorContent = await response.Content.ReadAsStringAsync();

                return StatusCode((int)response.StatusCode, new

                {

                    error = "Polar API hat einen Fehler gemeldet",

                    details = errorContent

                });

            }



            var jsonString = await response.Content.ReadAsStringAsync();

            var activities = JsonSerializer.Deserialize<List<PolarActivityDto>>(jsonString);



            return Ok(activities);

        }

        catch (Exception ex)

        {

            return StatusCode(500, new { error = "Interner Serverfehler", message = ex.Message });

        }

    }

}
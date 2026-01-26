// See https://aka.ms/new-console-template for more information
using BrightonBins;
using BrightonBins.Converter;
using BrightonBins.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

Console.WriteLine("Hello, World!");

const string postCode = "BN1 8NT";
var serialiserSettings = new JsonSerializerOptions(JsonSerializerDefaults.Web);
serialiserSettings.Converters.Add(new AutoNumberToStringConverter());
serialiserSettings.PropertyNamingPolicy= JsonNamingPolicy.CamelCase;
serialiserSettings.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;

var BASE_URL = "https://enviroservices.brighton-hove.gov.uk/";
var INIT_URL = $"{BASE_URL}link/collections";
var API_URL = $"{BASE_URL}xas/";
var operations_url = $"{BASE_URL}pages/en_GB/BartecCollective/Jobs_Get_Combined.page.xml";

var httpClient = new HttpClient();

var initResponse = await httpClient.GetAsync(INIT_URL);
initResponse.EnsureSuccessStatusCode();

var initPostResponse = await httpClient.PostAsJsonAsync(API_URL, new Dictionary<string, object>() {
    { "action", "get_session_data" },
    {"params", new Dictionary<string, object>() {
        { "hybrid", false},
        { "offline", false},
        { "referrer", null},
        { "profile", ""},
        { "timezoneoffset", 0},
        { "timezoneId", "Europe/London"},
        { "preferredLanguages", new[] {"en-GB", "en-US", "en" } },
        { "version", 2}
    } }
});
initPostResponse.EnsureSuccessStatusCode();
var initPostDto = await initPostResponse.Content.ReadFromJsonAsync<ResponseDto>(serialiserSettings);
var cookiesFromInit = initPostResponse.Headers.Single(a => a.Key == "Set-Cookie").Value.Select(a => a.Split("; ").First());

httpClient.DefaultRequestHeaders.Add("x-csrf-token", initPostDto.CsrfToken); // cookies issue?
var finalCookies = cookiesFromInit.Concat([$"__Host-XASID={cookiesFromInit.Single(a => a.StartsWith("xasid=")).Split("=").ElementAt(1)}"]);
httpClient.DefaultRequestHeaders.Add("Cookie", finalCookies);

var operationsResponse = await httpClient.GetAsync(operations_url);
operationsResponse.EnsureSuccessStatusCode();

var operationsResponseStr = await operationsResponse.Content.ReadAsStringAsync();

var postcodeOperationId = RegexTools.GetPostCodeOperationId(operationsResponseStr);


var addressGuid = initPostDto.Objects.Where(a => a.ObjectType == "BHCCTheme.Address").Select(a => a.Guid).Single();
var newChangesSet = initPostDto.Changes;
newChangesSet[long.Parse(addressGuid)].Add("SearchString", new HashValue() { Value = postCode });

var req = new RequestDto()
{
    OperationId = postcodeOperationId,
    Changes = newChangesSet,
    Objects = initPostDto.Objects.Where(a => a.ObjectType != "DeepLink.DeepLink").OrderBy(a => a.ObjectType).ToArray(),
    Params = new() { { "Address", new() { { "guid", addressGuid } } } }
};

var postcodeLookUpResonse = await httpClient.PostAsJsonAsync(API_URL, req, serialiserSettings);

postcodeLookUpResonse.EnsureSuccessStatusCode();


Console.ReadLine();

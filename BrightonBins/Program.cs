// See https://aka.ms/new-console-template for more information
using BrightonBins;
using BrightonBins.Dtos;

// To run in browser, start here:
// https://enviroservices.brighton-hove.gov.uk/link/collections

Console.WriteLine("Hello, World!");

const string postCode = "BN1 8NT";
const long Uprn = 22058876;

var BASE_URL = "https://enviroservices.brighton-hove.gov.uk/";
var INIT_URL = $"{BASE_URL}link/collections";
var API_URL = $"{BASE_URL}xas/";
var operations_url = $"{BASE_URL}pages/en_GB/BartecCollective/Jobs_Get_Combined.page.xml";

var httpClient = new HttpClient();

var initResponse = await httpClient.GetAsync(INIT_URL);
initResponse.EnsureSuccessStatusCode();

var initPostDto = await httpClient.PostAsJsonTypedAsync<Dictionary<string, object>, ResponseDto>(API_URL, new Dictionary<string, object>() {
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

httpClient.DefaultRequestHeaders.Add("x-csrf-token", initPostDto.CsrfToken);

var operationsResponse = await httpClient.GetAsync(operations_url);
operationsResponse.EnsureSuccessStatusCode();

var operationsResponseStr = await operationsResponse.Content.ReadAsStringAsync();

var addressGuid = initPostDto.Objects.Where(a => a.ObjectType == "BHCCTheme.Address").Select(a => a.Guid).Single();
var newChangesSet = initPostDto.Changes;
newChangesSet[long.Parse(addressGuid)].Add("SearchString", new HashValue() { Value = postCode });


var postCodeLookupDto = await httpClient.PostAsJsonTypedAsync<RequestDto, ResponseDto>(API_URL, new RequestDto()
{
    OperationId = RegexTools.GetPostCodeOperationId(operationsResponseStr),
    Changes = newChangesSet,
    Objects = initPostDto.Objects.Where(a => a.ObjectType != "DeepLink.DeepLink").OrderBy(a => a.ObjectType).ToArray(),
    Params = new() { { "Address", new() { { "guid", addressGuid } } } }
});

var q = postCodeLookupDto.Changes.Where(a => a.Value.ContainsKey("uprn") && long.Parse(a.Value["uprn"].Value) == Uprn);

var uprnChangeElement = q.First();

var collectionGuid = postCodeLookupDto.Objects.Where(a => long.Parse(a.Guid) == uprnChangeElement.Key).Single();

var newNewChangesSet = postCodeLookupDto.Changes;
newNewChangesSet[uprnChangeElement.Key] = uprnChangeElement.Value;

var rdto = new RequestDto()
{
    OperationId = RegexTools.GetScheduleOperationId(operationsResponseStr),
    Changes = newNewChangesSet,
    Objects = postCodeLookupDto.Objects.Where(a => a.ObjectType != "DeepLink.DeepLink").OrderBy(a => a.ObjectType).ToArray(),
    Params = new() { { "Collection", new() { { "guid", collectionGuid.Guid } } } }
};

var scheduleResponse = await httpClient.PostAsJsonTypedAsync<RequestDto, ResponseDto>(API_URL, rdto);


Console.ReadLine();

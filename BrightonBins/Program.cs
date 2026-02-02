// See https://aka.ms/new-console-template for more information
using BrightonBins;
using BrightonBins.Dtos;
using System.Text.Json;

// To run in browser, start here:
// https://enviroservices.brighton-hove.gov.uk/link/collections

Console.WriteLine("Brighton Bins Collection Lookup");

const string postCode = "BN1 8NT";
const long Uprn = 22058876;

var BASE_URL = "https://enviroservices.brighton-hove.gov.uk/";
var INIT_URL = $"{BASE_URL}link/collections";
var API_URL = $"{BASE_URL}xas/";
var operations_url = $"{BASE_URL}pages/en_GB/BartecCollective/Jobs_Get_Combined.page.xml";

var httpClient = new HttpClient();

// Step 1: Initialize session
Console.WriteLine("Initializing session...");
var initResponse = await httpClient.GetAsync(INIT_URL);
initResponse.EnsureSuccessStatusCode();

// Step 2: Get session data
Console.WriteLine("Getting session data...");
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

Console.WriteLine("\n=== DEBUG: Initial Session Objects ===");
var collectionObject = initPostDto.Objects.FirstOrDefault(o => o.ObjectType == "Collections.Collection");
if (collectionObject != null)
{
    Console.WriteLine($"Found Collection object: {collectionObject.Guid}");
    if (collectionObject.Attributes != null)
    {
        foreach (var attr in collectionObject.Attributes)
        {
            Console.WriteLine($"  {attr.Key}: {attr.Value.Value}");
        }
    }
}
else
{
    Console.WriteLine("ERROR: No Collection object in initial session!");
    foreach (var obj in initPostDto.Objects)
    {
        Console.WriteLine($"  Type: {obj.ObjectType}, GUID: {obj.Guid}");
    }
    Console.ReadLine();
    return;
}

var collectionGuid = collectionObject.Guid;

// Set CSRF token for subsequent requests
httpClient.DefaultRequestHeaders.Add("x-csrf-token", initPostDto.CsrfToken);

// Step 3: Get operations XML
Console.WriteLine("\nFetching operations...");
var operationsResponse = await httpClient.GetAsync(operations_url);
operationsResponse.EnsureSuccessStatusCode();
var operationsResponseStr = await operationsResponse.Content.ReadAsStringAsync();

// Step 4: Look up postcode
Console.WriteLine($"Looking up postcode: {postCode}...");
var addressGuid = initPostDto.Objects.First(a => a.ObjectType == "BHCCTheme.Address").Guid;

// Create a deep copy of changes and add the search string
var postcodeLookupChanges = new Dictionary<long, Dictionary<string, HashValue>>(
    initPostDto.Changes.ToDictionary(
        kvp => kvp.Key,
        kvp => new Dictionary<string, HashValue>(kvp.Value)
    )
);
postcodeLookupChanges[long.Parse(addressGuid)]["SearchString"] = new HashValue() { Value = postCode };

var postCodeLookupDto = await httpClient.PostAsJsonTypedAsync<RequestDto, ResponseDto>(API_URL, new RequestDto()
{
    OperationId = RegexTools.GetPostCodeOperationId(operationsResponseStr),
    Changes = postcodeLookupChanges,
    Objects = initPostDto.Objects.Where(a => a.ObjectType != "DeepLink.DeepLink").OrderBy(a => a.ObjectType).ToArray(),
    Params = new() { { "Address", new() { { "guid", addressGuid } } } }
});

// Step 5: Find the UPRN in the results
Console.WriteLine($"\n=== Finding UPRN: {Uprn} ===");

// Find the change entry with matching UPRN
var uprnChangeElement = postCodeLookupDto.Changes
    .FirstOrDefault(a => a.Value.ContainsKey("uprn") && long.Parse(a.Value["uprn"].Value) == Uprn);

if (uprnChangeElement.Key == 0)
{
    Console.WriteLine($"ERROR: UPRN {Uprn} not found for postcode {postCode}");
    Console.WriteLine("\nAvailable UPRNs:");
    foreach (var change in postCodeLookupDto.Changes.Where(c => c.Value.ContainsKey("uprn")))
    {
        Console.WriteLine($"  UPRN: {change.Value["uprn"].Value} (Change ID: {change.Key})");
    }
    Console.ReadLine();
    return;
}

Console.WriteLine($"Found UPRN {Uprn} at Change ID: {uprnChangeElement.Key}");

// Step 6: Prepare schedule request
Console.WriteLine("\n=== Preparing collection schedule request ===");

// Merge changes from both responses
var scheduleChanges = new Dictionary<long, Dictionary<string, HashValue>>(
    postCodeLookupDto.Changes.ToDictionary(
        kvp => kvp.Key,
        kvp => new Dictionary<string, HashValue>(kvp.Value)
    )
);

// Set the Collection's selectedAddress to point to the selected UPRN change ID
var collectionChangeKey = long.Parse(collectionGuid);
if (!scheduleChanges.ContainsKey(collectionChangeKey))
{
    scheduleChanges[collectionChangeKey] = new Dictionary<string, HashValue>();
}
scheduleChanges[collectionChangeKey]["selectedAddress"] = new HashValue() { Value = uprnChangeElement.Key.ToString() };

Console.WriteLine($"Collection GUID: {collectionGuid}");
Console.WriteLine($"Setting selectedAddress to UPRN change ID: {uprnChangeElement.Key}");

// Merge objects - use postCodeLookupDto objects but ensure Collection object is included
var scheduleObjects = postCodeLookupDto.Objects
    .Where(a => a.ObjectType != "DeepLink.DeepLink")
    .ToList();

// Make sure Collection object is included (it should be from the postcode lookup, but add it if not)
if (!scheduleObjects.Any(o => o.ObjectType == "Collections.Collection"))
{
    scheduleObjects.Add(collectionObject);
}

Console.WriteLine("\n=== Sending Schedule Request ===");
var scheduleResponse = await httpClient.PostAsJsonTypedAsync<RequestDto, ResponseDto>(API_URL, new RequestDto()
{
    OperationId = RegexTools.GetScheduleOperationId(operationsResponseStr),
    Changes = scheduleChanges,
    Objects = scheduleObjects.OrderBy(a => a.ObjectType).ToArray(),
    Params = new() { { "Collection", new() { { "guid", collectionGuid } } } }
});

Console.WriteLine("\n=== SUCCESS! Collection Schedule Retrieved ===");
Console.WriteLine($"Objects returned: {scheduleResponse.Objects.Length}");
Console.WriteLine($"Changes returned: {scheduleResponse.Changes.Count}");

// Display collection information
foreach (var obj in scheduleResponse.Objects)
{
    Console.WriteLine($"\n{obj.ObjectType}: {obj.Guid}");
    if (obj.Attributes != null)
    {
        foreach (var attr in obj.Attributes)
        {
            Console.WriteLine($"  {attr.Key}: {attr.Value.Value}");
        }
    }
}

Console.WriteLine("\nPress Enter to exit...");
Console.ReadLine();

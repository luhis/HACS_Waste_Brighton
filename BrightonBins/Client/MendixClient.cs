using BrightonBins.Dtos;

namespace BrightonBins.Client;

public class MendixClient(HttpClient httpClient) : IMendixClient
{
    const string BASE_URL = "https://enviroservices.brighton-hove.gov.uk/";
    const string INIT_URL = $"{BASE_URL}link/collections";
    const string API_URL = $"{BASE_URL}xas/";
    const string operations_url = $"{BASE_URL}pages/en_GB/BartecCollective/Jobs_Get_Combined.page.xml";
    private readonly IReadOnlyDictionary<string, object> GetSessionDataRequest = new Dictionary<string, object>() {
            { "action", "get_session_data" },
            {"params", new Dictionary<string, object?>() {
                { "hybrid", false},
                { "offline", false},
                { "referrer", null},
                { "profile", ""},
                { "timezoneoffset", 0},
                { "timezoneId", "Europe/London"},
                { "preferredLanguages", new[] {"en-GB", "en-US", "en" } },
                { "version", 2}
            } }
        };

    async Task<IReadOnlyList<ObjectDto>> IMendixClient.GetSchedule(string postCode, long uprn)
    {
        // Step 1: Initialize session
        Console.WriteLine("Initializing session...");
        var initResponse = await httpClient.GetStringAsync(INIT_URL);

        // Step 2: Get session data
        Console.WriteLine("Getting session data...");
        var sessionDataDto = await httpClient.PostAsJsonTypedAsync<IReadOnlyDictionary<string, object>, ResponseDto>(API_URL, GetSessionDataRequest);

        Console.WriteLine("\n=== DEBUG: Initial Session Objects ===");
        var collectionObject = sessionDataDto.Objects.FirstOrDefault(o => o.ObjectType == "Collections.Collection");
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
            foreach (var obj in sessionDataDto.Objects)
            {
                Console.WriteLine($"  Type: {obj.ObjectType}, GUID: {obj.Guid}");
            }
            Console.ReadLine();
            throw new Exception("Fail");
        }

        var collectionGuid = collectionObject.Guid;

        // Set CSRF token for subsequent requests
        httpClient.DefaultRequestHeaders.Add("x-csrf-token", sessionDataDto.CsrfToken);

        // Step 3: Get operations XML
        Console.WriteLine("\nFetching operations...");
        var operationsResponse = await httpClient.GetAsString(operations_url);

        // Step 4: Look up postcode
        Console.WriteLine($"Looking up postcode: {postCode}...");
        var addressGuid = sessionDataDto.Objects.First(a => a.ObjectType == "BHCCTheme.Address").Guid;

        // Create a deep copy of changes and add the search string
        var postcodeLookupChanges = new Dictionary<long, Dictionary<string, HashValue>>(
            sessionDataDto.Changes.ToDictionary(
                kvp => kvp.Key,
                kvp => new Dictionary<string, HashValue>(kvp.Value)
            )
        );
        postcodeLookupChanges[long.Parse(addressGuid)]["SearchString"] = new HashValue() { Value = postCode };

        var postCodeLookupDto = await httpClient.PostAsJsonTypedAsync<RequestDto, ResponseDto>(API_URL, new RequestDto()
        {
            OperationId = RegexTools.GetPostCodeOperationId(operationsResponse),
            Changes = postcodeLookupChanges,
            Objects = sessionDataDto.Objects.Where(a => a.ObjectType != "DeepLink.DeepLink").OrderBy(a => a.ObjectType).ToArray(),
            Params = new() { { "Address", new() { { "guid", addressGuid } } } }
        });

        // Step 5: Find the UPRN in the results
        Console.WriteLine($"\n=== Finding UPRN: {uprn} ===");

        // Find the change entry with matching UPRN
        var uprnChangeElement = postCodeLookupDto.Changes
            .FirstOrDefault(a => a.Value.ContainsKey("uprn") && long.Parse(a.Value["uprn"].Value) == uprn);

        if (uprnChangeElement.Key == 0)
        {
            Console.WriteLine($"ERROR: UPRN {uprn} not found for postcode {postCode}");
            Console.WriteLine("\nAvailable UPRNs:");
            foreach (var change in postCodeLookupDto.Changes.Where(c => c.Value.ContainsKey("uprn")))
            {
                Console.WriteLine($"  UPRN: {change.Value["uprn"].Value} (Change ID: {change.Key})");
            }
            Console.ReadLine();
            throw new Exception("Fail");
        }

        Console.WriteLine($"Found UPRN {uprn} at Change ID: {uprnChangeElement.Key}");

        // Step 6: Prepare schedule request
        Console.WriteLine("\n=== Preparing collection schedule request ===");

        // Merge changes from both responses
        var scheduleChanges = new Dictionary<long, Dictionary<string, HashValue>>(
            postCodeLookupDto.Changes.Concat(sessionDataDto.Changes).DistinctBy(a => a.Key).ToDictionary(
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
        // use changes from get session data

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
            OperationId = RegexTools.GetScheduleOperationId(operationsResponse),
            Changes = scheduleChanges,
            Objects = scheduleObjects.OrderBy(a => a.ObjectType).ToArray(),
            Params = new() { { "Collection", new() { { "guid", collectionGuid } } } }
        });

        Console.WriteLine("\n=== SUCCESS! Collection Schedule Retrieved ===");
        Console.WriteLine($"Objects returned: {scheduleResponse.Objects.Length}");
        Console.WriteLine($"Changes returned: {scheduleResponse.Changes.Count}");

        return scheduleResponse.Objects;
    }
}

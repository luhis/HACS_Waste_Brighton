// See https://aka.ms/new-console-template for more information
using BrightonBins.Client;

// To run in browser, start here:
// https://enviroservices.brighton-hove.gov.uk/link/collections
// 3208814
// Python Mendix example: https://github.com/mampfes/hacs_waste_collection_schedule/blob/master/custom_components/waste_collection_schedule/waste_collection_schedule/source/knowsley_gov_uk.py

Console.WriteLine("Brighton Bins Collection Lookup");

const string postCode = "BN1 8NT";
const long Uprn = 22058876;

var cookieContainer = new System.Net.CookieContainer();
var handler = new HttpClientHandler
{
    UseCookies = true,
    CookieContainer = cookieContainer
};

IMendixClient client = new MendixClient(new HttpClient(handler));

await client.GetSchedule(postCode, Uprn);

Console.WriteLine("\nPress Enter to exit...");
Console.ReadLine();

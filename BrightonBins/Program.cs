// See https://aka.ms/new-console-template for more information
using BrightonBins.Client;

// To run in browser, start here:
// https://enviroservices.brighton-hove.gov.uk/link/collections
// 3208814

Console.WriteLine("Brighton Bins Collection Lookup");

const string postCode = "BN1 8NT";
const long Uprn = 22058876;

IMendixClient client = new MendixClient(new HttpClient());

await client.GetSchedule(postCode, Uprn);

Console.WriteLine("\nPress Enter to exit...");
Console.ReadLine();

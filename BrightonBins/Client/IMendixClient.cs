using BrightonBins.Dtos;

namespace BrightonBins.Client
{
    public interface IMendixClient
    {
        Task<IReadOnlyList<ObjectDto>> GetSchedule(string postCode, long uprn);
    }
}
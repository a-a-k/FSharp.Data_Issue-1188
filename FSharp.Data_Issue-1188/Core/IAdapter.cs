namespace MinEnvironment
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IAdapter
    {
        Task<(List<string> pages, List<ItemPrice> items, string zoneName)> SearchAndParse(string zoneId, string query);

        Task<(List<ItemPrice> items, string zoneName)> Parse(IEnumerable<string> pages, string query);
    }
}

using Application.Common.Pagination;
using Application.Services.Pagination;
using Discord;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace Infrastructure.Services.Discord
{
    public class PaginationService : IPaginationService
    {
        private readonly IMemoryCache _cache;

        public PaginationService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public string CreatePaginationSession(string header, string fullText, int charsPerPage = 3500, ButtonBuilder? customButton = null)
        {
            var sessionId = Guid.NewGuid().ToString("N");
            var pages = new List<string>();

            for (int i = 0; i < fullText.Length; i += charsPerPage)
            {
                pages.Add(fullText.Substring(i, Math.Min(charsPerPage, fullText.Length - i)));
            }

            var sessionData = new PaginationSessionData
            {
                Header = header,
                Pages = pages,
                CustomButton = customButton
            };

            _cache.Set(sessionId, sessionData, TimeSpan.FromMinutes(15));
            return sessionId;
        }

        public string CreatePaginationSession(string header, IEnumerable<string> items, int charsPerPage = 3500, string separator = "\n", ButtonBuilder? customButton = null)
        {
            var sessionId = Guid.NewGuid().ToString("N");
            var pages = new List<string>();
            var currentPage = new StringBuilder();

            foreach (var item in items)
            {
                if (item.Length > charsPerPage)
                {
                    if (currentPage.Length > 0)
                    {
                        pages.Add(currentPage.ToString());
                        currentPage.Clear();
                    }

                    // Tniemy giganta
                    for (int i = 0; i < item.Length; i += charsPerPage)
                    {
                        pages.Add(item.Substring(i, Math.Min(charsPerPage, item.Length - i)));
                    }
                    continue;
                }

                int addedLength = currentPage.Length == 0 ? item.Length : separator.Length + item.Length;

                if (currentPage.Length > 0 && currentPage.Length + addedLength > charsPerPage)
                {
                    pages.Add(currentPage.ToString());
                    currentPage.Clear();
                }

                if (currentPage.Length > 0)
                {
                    currentPage.Append(separator);
                }
                currentPage.Append(item);
            }

            if (currentPage.Length > 0)
            {
                pages.Add(currentPage.ToString());
            }

            if (pages.Count == 0)
            {
                pages.Add("No data to display.");
            }

            var sessionData = new PaginationSessionData
            {
                Header = header,
                Pages = pages,
                CustomButton = customButton
            };

            _cache.Set(sessionId, sessionData, TimeSpan.FromMinutes(15));
            return sessionId;
        }

        public PaginationSessionData? GetSessionData(string sessionId)
        {
            _cache.TryGetValue(sessionId, out PaginationSessionData? sessionData);
            return sessionData;
        }
    }
}

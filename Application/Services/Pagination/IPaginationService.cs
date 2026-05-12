using Application.Common.Pagination;
using Discord;

namespace Application.Services.Pagination
{
    public interface IPaginationService
    {
        string CreatePaginationSession(string header, string fullText, int charsPerPage = 3500, ButtonBuilder? customButton = null);
        string CreatePaginationSession(string header, IEnumerable<string> items, int charsPerPage = 3500, string separator = "\n", ButtonBuilder? customButton = null);
        PaginationSessionData? GetSessionData(string sessionId);
    }
}

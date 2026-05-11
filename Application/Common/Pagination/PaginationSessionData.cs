using Discord;

namespace Application.Common.Pagination
{
    public class PaginationSessionData
    {
        public string Header { get; set; } = string.Empty;
        public List<string> Pages { get; set; } = new();
        public ButtonBuilder? CustomButton { get; set; }
    }
}

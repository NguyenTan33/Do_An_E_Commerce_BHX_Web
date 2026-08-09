using System.Collections.Generic;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces
{
    public interface IAdminQuestionService
    {
        Task<(List<Question> Questions, Dictionary<int, Product> Products, Dictionary<string, string> Users, int CountUnanswered, int CountAnswered, int CountAll)> GetQuestionListAsync(string filter, string search);
        Task<bool> ReplyQuestionAsync(int questionId, string answer);
        Task<bool> DeleteQuestionAsync(int id);
    }
}

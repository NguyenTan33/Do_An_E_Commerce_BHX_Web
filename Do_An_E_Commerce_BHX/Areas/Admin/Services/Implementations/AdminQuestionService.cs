using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Services.Implementations
{
    public class AdminQuestionService : IAdminQuestionService
    {
        private readonly ApplicationDbContext _dbContext;

        public AdminQuestionService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? new ApplicationDbContext();
        }

        public async Task<(List<Question> Questions, Dictionary<int, Product> Products, Dictionary<string, string> Users, int CountUnanswered, int CountAnswered, int CountAll)> GetQuestionListAsync(string filter, string search)
        {
            var query = _dbContext.Question.AsQueryable();

            if (filter == "unanswered")
            {
                query = query.Where(q => q.Answer == null || q.Answer.Trim() == "");
            }
            else if (filter == "answered")
            {
                query = query.Where(q => q.Answer != null && q.Answer.Trim() != "");
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(q => q.Content.Contains(s));
            }

            var listQuestions = await query.OrderByDescending(q => q.CreatedDate).ToListAsync();

            var productIds = listQuestions.Select(q => q.ProductId).Distinct().ToList();
            var products = await _dbContext.Product.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p);

            var userIds = listQuestions.Where(q => q.UserId > 0).Select(q => q.UserId.ToString()).Distinct().ToList();
            var usersList = await _dbContext.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            var users = usersList.ToDictionary(u => u.Id, u => !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.UserName);

            int countUnanswered = await _dbContext.Question.CountAsync(q => q.Answer == null || q.Answer.Trim() == "");
            int countAnswered = await _dbContext.Question.CountAsync(q => q.Answer != null && q.Answer.Trim() != "");
            int countAll = await _dbContext.Question.CountAsync();

            return (listQuestions, products, users, countUnanswered, countAnswered, countAll);
        }

        public async Task<bool> ReplyQuestionAsync(int questionId, string answer)
        {
            if (string.IsNullOrWhiteSpace(answer)) return false;

            var question = await _dbContext.Question.FirstOrDefaultAsync(q => q.Id == questionId);
            if (question == null) return false;

            question.Answer = answer.Trim();
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteQuestionAsync(int id)
        {
            var question = await _dbContext.Question.FirstOrDefaultAsync(q => q.Id == id);
            if (question == null) return false;

            _dbContext.Question.Remove(question);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}

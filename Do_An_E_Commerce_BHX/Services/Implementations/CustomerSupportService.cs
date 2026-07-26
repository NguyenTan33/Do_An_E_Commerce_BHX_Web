using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Do_An_E_Commerce_BHX.Services.Implementations
{
    public class CustomerSupportService
    {
        ApplicationDbContext dbContext;
        public CustomerSupportService(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public void AddReview(int productId,string userId , int rating , string comment)
        {
            var product = dbContext.Product.FirstOrDefault(p => p.Id == productId);
            if (product == null || rating >5 || rating < 1) return;
            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Comment = comment,
                CreatedDate = DateTime.Now,
            };
            dbContext.Review.Add(review);
            dbContext.SaveChanges();

        }

        public List<Review> GetAllReviewsByProductID(int productId)
        {
            return dbContext.Review
                .Where(r => r.ProductId == productId).ToList(); 
        } 

        //--------------------------//

        public void AddQuestion(int productId , string userId , string content )
        {
            var product = dbContext.Product.FirstOrDefault( p => p.Id == productId);

            var question = new Question
            {
                ProductId= productId,
                UserId = userId,
                Content = content,

                CreatedDate = DateTime.Now,
            };
            dbContext.Question.Add(question);
            dbContext.SaveChanges();
        }
        public void AddAnswer(int questionId , string answer)
        {
            var question = dbContext.Question.FirstOrDefault(p => p.Id == questionId);
            question.Answer = answer;
            dbContext.SaveChanges();
        }

        public void RemoveQuestion(int questionId)
        {
            var question = dbContext.Question.FirstOrDefault(p => p.Id == questionId);
            dbContext.Question.Remove(question);
            dbContext.SaveChanges() ;
        }

        public List<Question> GetAllQuestionsByProductId(int productId)
        {
            return dbContext.Question
                .Where(question => question.ProductId == productId).ToList();
        }
        public List<Question> GetAllQuestionsHaveNotAnswerdYet()
        {
            return dbContext.Question
                .Where(question => question.Answer == null
                ).OrderByDescending(question => question.CreatedDate)
                .ToList();
        }

    }
}
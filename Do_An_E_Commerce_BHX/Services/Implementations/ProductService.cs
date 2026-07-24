using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Do_An_E_Commerce_BHX.Services.Implementations
{
    public class ProductService
    {
        public SearchHandler searchHandler { get; set; }
        public ApplicationDbContext appDBContext { get; set; }
        public ProductService(ApplicationDbContext appDBContext, SearchHandler searchHandler)
        {
            this.appDBContext = appDBContext;
            this.searchHandler = searchHandler;
        }

        //this method only for Controller calling
        public List<Product> Search(ProductType inputData)
        {
            var productQuery = searchHandler.Execute(inputData).ToList();
            return productQuery;
        }
        public void AddNewProduct(string name, decimal cost, string Description, Category category)
        {
            Product product = new Product();
            product.Name = name;
            product.Price = cost;
            product.Description = Description;
            product.Category = category;

            appDBContext.Product.Add(product);
            appDBContext.SaveChanges();
        }

        public void DeleteProduct(int id)
        {
            var product = appDBContext.Product.FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                appDBContext.Product.Remove(product);
                appDBContext.SaveChanges();
            }
        }

        public void AdjustProduct(int id, ProductType product)
        {
            var productObject = appDBContext.Product.FirstOrDefault(p => p.Id == id);
            if (productObject == null) return;

            if (product.name != null) productObject.Name = product.name;

            appDBContext.SaveChanges();
        }



    }
    public abstract class Search
    {

        public ApplicationDbContext dBContext;
        public Search(ApplicationDbContext dbContext) { this.dBContext = dbContext; }
        public abstract IQueryable<Product> SearchProduct(IQueryable<Product> query, ProductType x);
    }
    public class SearchByName : Search
    {
        public SearchByName(ApplicationDbContext dbContext) : base(dbContext) { }
        public override IQueryable<Product> SearchProduct(IQueryable<Product> query, ProductType x)
        {

            if (!string.IsNullOrEmpty(x.name))
            {
                query = query.Where(p => p.Name.Contains(x.name));
            }
            return query;
        }
    }
    public class SearchByCategory : Search
    {
        public SearchByCategory(ApplicationDbContext dbContext) : base(dbContext) { }
        public override IQueryable<Product> SearchProduct(IQueryable<Product> query, ProductType x)
        {
            if (x.category != null)
            {
                query = query.Where(p => p.CategoryId == x.category.Id);
            }
            return query;
        }
    }
    public class SearchHandler
    {
        private readonly Search _searchByName;
        private readonly Search _searchByCategory;
        ApplicationDbContext dbContext;
        public SearchHandler(ApplicationDbContext dbContext)
        {
            _searchByName = new SearchByName(dbContext);
            _searchByCategory = new SearchByCategory(dbContext);
            this.dbContext = dbContext;
        }

        // 3. Hàm thực thi (Nhận tham số x vào đây chứ không nhận ở tên Class)
        public IQueryable<Product> Execute(ProductType x)
        {
            IQueryable<Product> products = dbContext.Product;

            products = _searchByName.SearchProduct(products, x);

            products = _searchByCategory.SearchProduct(products, x);

            return products;
        }

    }
    public class ProductType
    {
        public string name;
        public Category category;
    }
}
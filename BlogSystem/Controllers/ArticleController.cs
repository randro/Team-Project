using BlogSystem.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace BlogSystem.Controllers
{
    public class ArticleController : Controller
    {
        //
        // GET: Article
        [HttpGet]
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        //
        // Get: Article/List
        [HttpGet]
        public ActionResult List()
        {
            using (var database = new BlogDbContext())
            {
                // Get article from database
                var articles = database.Articles.Include(a => a.Author).ToList();

                return View(articles);
            }
        }

        //
        // Get: Article/AllArticles
        [HttpGet]
        public ActionResult AllArticles()
        {
            using (var database = new BlogDbContext())
            {
                // Get author id
                var authorId = database.Users.Where(u => u.UserName == this.User.Identity.Name).First().Id;

                // Get article from database
                var articles = database.Articles.Where(u => u.AuthorId == authorId).ToList();

                return View(articles);
            }
        }

        //
        // Get: Article/IFeelLucky
        [HttpGet]
        public ActionResult IFeelLucky()
        {
            using (var database = new BlogDbContext())
            {
                // Get count articles from database
                var countArticles = database.Articles.Count();

                // Get random article from article list
                Random random = new Random();
                int randomNum = random.Next(1, countArticles - 2);
                var articles = database.Articles.Include(a => a.Author).OrderBy(a => a.Id).Skip(randomNum).Take(1).ToList();

                return View(articles);
            }
        }

        //
        // GET: Article/Details
        [HttpGet]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var database = new BlogDbContext())
            {
                // Get the article from database
                var article = database.Articles.Where(a => a.Id == id).Include(a => a.Author).First();

                // Check if article exists
                if (article == null)
                {
                    return HttpNotFound();
                }

                // Pass article to view
                return View(article);
            }
        }

        //
        // GET: Article/Create
        [Authorize]
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: Article/Create
        [Authorize]
        [HttpPost]
        public ActionResult Create(Article article, HttpPostedFileBase image)
        {
            if (ModelState.IsValid)
            {
                // Insert article in database
                using (var database = new BlogDbContext())
                {
                    // Get author id
                    var authorId = database.Users.Where(u => u.UserName == this.User.Identity.Name).First().Id;

                    // Set articlea author
                    article.AuthorId = authorId;

                    // Set datetime
                    article.DatePost = DateTime.Now;

                    // Upload image. Check allowed types.
                    if (image != null)
                    {
                        var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/tif" };

                        if (allowedContentTypes.Contains(image.ContentType))
                        {
                            var imagesPath = "/Content/Images/";

                            var filename = image.FileName;

                            var uploadPath = imagesPath + filename;

                            var physicalPath = Server.MapPath(uploadPath);

                            image.SaveAs(physicalPath);

                            article.ImagePath = uploadPath;
                        }
                    }

                    // Save article in DB
                    database.Articles.Add(article);
                    database.SaveChanges();

                    return RedirectToAction("Index");
                }
            }

            return View(article);
        }

        //
        // GET: Article/Delete
        [Authorize]
        [HttpGet]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var database = new BlogDbContext())
            {
                // Get the article from database
                var article = database.Articles.Where(a => a.Id == id).Include(a => a.Author).First();

                if (!IsUserAuthorizedToEdit(article))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                // Check if article exists
                if (article == null)
                {
                    return HttpNotFound();
                }

                // Pass article to view
                return View(article);
            }
        }

        //
        // POST: Article/Delete
        [Authorize]
        [HttpPost]
        [ActionName("Delete")]
        public ActionResult DeleteConfirmed(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var database = new BlogDbContext())
            {
                // Get the article from database
                var article = database.Articles.Where(a => a.Id == id).Include(a => a.Author).First();

                // Check if article exists
                if (article == null)
                {
                    return HttpNotFound();
                }

                // Delete article from database
                database.Articles.Remove(article);
                database.SaveChanges();

                // Redirect to index page
                return RedirectToAction("Index");
            }
        }

        //
        // GET: Article/Edit
        [Authorize]
        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var database = new BlogDbContext())
            {
                // Get the article from database
                var article = database.Articles.Where(a => a.Id == id).First();

                if (!IsUserAuthorizedToEdit(article))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                // Check if article exists
                if (article == null)
                {
                    return HttpNotFound();
                }

                // Create the view model
                var model = new ArticleViewModel();
                model.Id = article.Id;
                model.Title = article.Title;
                model.Content = article.Content;

                // Pass the view model to view
                return View(model);
            }
        }

        //
        // POST: Article/Edit
        [Authorize]
        [HttpPost]
        public ActionResult Edit(ArticleViewModel model)
        {
            // Check if model state is valid
            if (ModelState.IsValid)
            {
                using (var database = new BlogDbContext())
                {
                    // Get article from DB
                    var article = database.Articles.FirstOrDefault(a => a.Id == model.Id);

                    // Set article properties
                    article.Title = model.Title;
                    article.Content = model.Content;

                    // Save article state in DB
                    database.Entry(article).State = EntityState.Modified;
                    database.SaveChanges();

                    // Redirect to the index page
                    return RedirectToAction("Index");
                }
            }

            // If model state is invlalid, return the same view
            return View(model);
        }

        private bool IsUserAuthorizedToEdit(Article article)
        {
            bool isAdmin = this.User.IsInRole("Admin");
            bool isAuthor = article.IsAuthor(this.User.Identity.Name);

            return isAdmin || isAuthor;
        }
    }
}
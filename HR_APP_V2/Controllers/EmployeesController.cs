using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using HR_APP_V2.Models;
using PagedList;

namespace HR_APP_V2.Controllers
{
    public class EmployeesController : Controller
    {
        private Human_ResourcesEntities1 db = new Human_ResourcesEntities1();

        // GET: Employees
        public ActionResult Index(string sortOrder, string currentFilter, string searchString, int? page)
        {
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.lNameSortParm = sortOrder == "lName" ? "lName_desc" : "lName";

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            var employees = from s in db.Employees
                           select s;

            if (!String.IsNullOrEmpty(searchString))
            {
                employees = employees.Where(s => s.Last_Name.Contains(searchString)
                                       || s.First_Name.Contains(searchString)
                                       || s.SSN.ToString().Contains(searchString));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    employees = employees.OrderByDescending(s => s.First_Name);
                    break;
                case "lName":
                    employees = employees.OrderBy(s => s.Last_Name);
                    break;
                case "lName_desc":
                    employees = employees.OrderByDescending(s => s.Last_Name);
                    break;
                default:
                    employees = employees.OrderBy(s => s.First_Name);
                    break;
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(employees.ToPagedList(pageNumber, pageSize));
        }

        // GET: Employees/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Employee employee = db.Employees.Find(id);
            if (employee == null)
            {
                return HttpNotFound();
            }
            return View(employee);
        }

        // GET: Employees/Create
        public ActionResult Create(int? from)
        {
            ViewBag.from = from;
            return View(new Employee());
        }

        // POST: Employees/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,First_Name,Last_Name,Gender,Marital_Status,SSN,Address,Phone_Number,Add_User,Date_Added")] Employee employee, int? from)
        {
            if (ModelState.IsValid)
            {
                employee.Add_User = User.Identity.Name;
                employee.Date_Added = DateTime.Now;
                try
                {
                    db.Employees.Add(employee);
                    db.SaveChanges();
                }
                catch (DbEntityValidationException ex)
                {
                    var err = new StringBuilder();
                    foreach (var entityValidationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in entityValidationErrors.ValidationErrors)
                        {
                            Response.Write("Property: " + validationError.PropertyName + " Error: " + validationError.ErrorMessage);
                            err.AppendLine(validationError.ErrorMessage);
                            System.Diagnostics.Debug.WriteLine("Property: " + validationError.PropertyName + " Error: " + validationError.ErrorMessage);
                            System.Diagnostics.Debug.WriteLine("err: " + err);
                            return RedirectToAction("ErrorMessage", new { message = err.ToString() } );
                        }
                    }
                }
                if(employee.Phone_Number.Length != 12)
                {
                    return RedirectToAction("ErrorMessage", new { message = "Phone number must be a 10 digit number seperated by hyphens ( - )." });
                }
                Regex rx = new Regex(@"^[0-9\-]*$");
                if (!rx.IsMatch(employee.Phone_Number))
                {
                    return RedirectToAction("ErrorMessage", new { message = "Phone number must be a 10 digit number seperated by hyphens ( - )." });
                }
                if (from == 1)
                {
                    int lastAddedID = db.Employees.Max(item => item.ID);
                    return RedirectToAction("Create", "WC_Inbox", new { id = lastAddedID });
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }

            return View(employee);
        }

        // GET: Employees/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Employee employee = db.Employees.Find(id);
            if (employee == null)
            {
                return HttpNotFound();
            }
            return View(employee);
        }

        // POST: Employees/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,First_Name,Last_Name,Gender,Marital_Status,SSN,Address,Phone_Number")] Employee employee)
        {
            if (ModelState.IsValid)
            {
                employee.Add_User = "Cole";
                employee.Date_Added = DateTime.Today;
                db.Entry(employee).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(employee);
        }

        // GET: Employees/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Employee employee = db.Employees.Find(id);
            if (employee == null)
            {
                return HttpNotFound();
            }
            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Employee employee = db.Employees.Find(id);
            db.Employees.Remove(employee);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult ErrorMessage(string message)
        {
            ViewBag.ErrorMessage = message;
            System.Diagnostics.Debug.WriteLine("Error message: " + message);
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

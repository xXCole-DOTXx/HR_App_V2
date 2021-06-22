using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using HR_APP_V2.Models;
using PagedList;

namespace HR_APP_V2.Controllers
{
    public class WC_InboxController : Controller
    {
        private Human_ResourcesEntities1 db = new Human_ResourcesEntities1();

        // GET: WC_Inbox
        public ActionResult Index(string searchString, string sortOrder, string currentFilter, int? page)
        {
            ViewBag.IDSortParm = String.IsNullOrEmpty(sortOrder) ? "id_desc" : "";

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            var wC_Inbox = db.WC_Inbox.Include(w => w.Employee);

            if (!String.IsNullOrEmpty(searchString))
            {
                wC_Inbox = wC_Inbox.Where(s => s.EncovaID.Contains(searchString)
                                       || s.Org_Number.ToString().Contains(searchString)
                                       || s.Employee.First_Name.Contains(searchString)
                                       || s.Employee.Last_Name.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "id_desc":
                    wC_Inbox = wC_Inbox.OrderByDescending(s => s.ID);
                    break;
                default:
                    wC_Inbox = wC_Inbox.OrderBy(s => s.ID);
                    break;
            }

            int pageSize = 5;
            int pageNumber = (page ?? 1);
            return View(wC_Inbox.ToPagedList(pageNumber, pageSize));
        }

        // GET: WC_Inbox
        public ActionResult Archive(string searchString, string sortOrder, string currentFilter, int? page)
        {
            ViewBag.IDSortParm = String.IsNullOrEmpty(sortOrder) ? "id_desc" : "";

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            var wC_Inbox = db.WC_Inbox.Include(w => w.Employee);
            wC_Inbox = wC_Inbox.Where(s => s.Status == "Archived");

            switch (sortOrder)
            {
                case "id_desc":
                    wC_Inbox = wC_Inbox.OrderByDescending(s => s.ID);
                    break;
                default:
                    wC_Inbox = wC_Inbox.OrderBy(s => s.ID);
                    break;
            }

            int pageSize = 5;
            int pageNumber = (page ?? 1);
            return View(wC_Inbox.ToPagedList(pageNumber, pageSize));
        }

        // GET: Employees
        public ActionResult EmployeeSelect(string sortOrder, string currentFilter, string searchString, int? page)
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

        // GET: WC_Inbox/Details/5
        public ActionResult Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            WC_Inbox wC_Inbox = db.WC_Inbox.Find(id);
            if (wC_Inbox == null)
            {
                return HttpNotFound();
            }

            Employee employee = db.Employees.Find(wC_Inbox.EmployeeID);
            string fullName = employee.First_Name + " " + employee.Last_Name;
            System.Diagnostics.Debug.WriteLine("Employee full name: " + fullName);
            ViewBag.EmployeeID = id;
            ViewBag.Name = fullName;
            return View(wC_Inbox);
        }

        // GET: WC_Inbox/Create
        public ActionResult Create(int? id)
        {
            System.Diagnostics.Debug.WriteLine("Employee ID was: " + id);
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Employee employee = db.Employees.Find(id);
            if (employee == null)
            {
                return HttpNotFound();
            }
            string fullName = employee.First_Name + " " + employee.Last_Name;
            System.Diagnostics.Debug.WriteLine("Employee full name: " + fullName);
            ViewBag.EmployeeID = id;
            ViewBag.Name = fullName;
            ViewBag.Status = "Pending";
            return View();
        }

        // POST: WC_Inbox/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,InboxID,EmployeeID,Org_Number,Hire_Date,Job_Title,Work_Schedule,Injury_Date,Injury_Time,DOT_12,Start_Time,Injured_Body_Part,Side,Missing_Work,Return_to_Work_Date,Doctors_Release,Treatment,Injury_Description,Equipment,Witness,Questioned,Medical_History,Inbox_Submitted,Comments,User_Email,Contact_Email,Specialist_Email,Optional_Email,Optional_Email2,Optional_Email3,Optional_Email4,Status,Add_User,Date_Added")] WC_Inbox wC_Inbox)
        {
            if (ModelState.IsValid)
            {
                //Get logged in windows user, get the date right now, and create the wcInbox unique ID
                var userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                wC_Inbox.Add_User = userName;
                wC_Inbox.Date_Added = DateTime.Today;
                db.WC_Inbox.Add(wC_Inbox);
                db.SaveChanges();
                SendEmailIQ();
                return RedirectToAction("Index");
            }

            ViewBag.EmployeeID = new SelectList(db.Employees, "ID", "First_Name", wC_Inbox.EmployeeID);
            return View(wC_Inbox);
        }

        // GET: WC_Inbox/Edit/5
        public ActionResult Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            WC_Inbox wC_Inbox = db.WC_Inbox.Find(id);
            if (wC_Inbox == null)
            {
                return HttpNotFound();
            }

            Employee employee = db.Employees.Find(wC_Inbox.EmployeeID);
            string fullName = employee.First_Name + " " + employee.Last_Name;
            System.Diagnostics.Debug.WriteLine("Employee full name: " + fullName);
            ViewBag.EmployeeID = id;
            ViewBag.Name = fullName;

            return View(wC_Inbox);
        }

        // POST: WC_Inbox/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,InboxID,EmployeeID,Org_Number,Hire_Date,Job_Title,Work_Schedule,Injury_Date,Injury_Time,DOT_12,Start_Time,Injured_Body_Part,Side,Missing_Work,Return_to_Work_Date,Doctors_Release,Treatment,Injury_Description,Equipment,Witness,Questioned,Medical_History,Inbox_Submitted,Comments,User_Email,Contact_Email,Specialist_Email,Optional_Email,Optional_Email2,Optional_Email3,Optional_Email4,Add_User,Date_Added")] WC_Inbox wC_Inbox)
        {
            if (ModelState.IsValid)
            {
                db.Entry(wC_Inbox).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.EmployeeID = new SelectList(db.Employees, "ID", "First_Name", wC_Inbox.EmployeeID);
            return View(wC_Inbox);
        }

        // GET: WC_Inbox/Work/5
        public ActionResult Work(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            WC_Inbox wC_Inbox = db.WC_Inbox.Find(id);
            if (wC_Inbox == null)
            {
                return HttpNotFound();
            }

            Employee employee = db.Employees.Find(wC_Inbox.EmployeeID);
            string fullName = employee.First_Name + " " + employee.Last_Name;
            System.Diagnostics.Debug.WriteLine("Employee full name: " + fullName);
            ViewBag.EmployeeID = id;
            ViewBag.Name = fullName;

            return View(wC_Inbox);
        }

        // POST: WC_Inbox/Work/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Work([Bind(Include = "ID,EmployeeID,Org_Number,Hire_Date,Job_Title,Work_Schedule,Injury_Date,Injury_Time,DOT_12,Start_Time,Injured_Body_Part,Side,Missing_Work,Return_to_Work_Date,Doctors_Release,Treatment,Injury_Description,Equipment,Witness,Questioned,Medical_History,Inbox_Submitted,Comments,User_Email,Contact_Email,Specialist_Email,Optional_Email,Optional_Email2,Optional_Email3,Optional_Email4,Add_User,Date_Added,TX_EROI_lag,Claim_Ruling,Injury_Type,TTD_Onset_Date,Restricted_RTW,Full_Duty_RTW,TTD_Award_notice,RTW_Notice_Carrier,Lost_Time_Start1,Lost_Time_End1,Lost_Time_Start2,Lost_Time_End2,Lost_Time_Start3,Lost_Time_End3,Status,HR_Comments,EncovaID,HR_User,Date_Modified")] WC_Inbox wC_Inbox)
        {
            if (ModelState.IsValid)
            {
                wC_Inbox.HR_User = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                wC_Inbox.Date_Modified = DateTime.Today;
                db.Entry(wC_Inbox).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.EmployeeID = new SelectList(db.Employees, "ID", "First_Name", wC_Inbox.EmployeeID);
            return View(wC_Inbox);
        }

        // GET: WC_Inbox/Delete/5
        public ActionResult Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            WC_Inbox wC_Inbox = db.WC_Inbox.Find(id);
            if (wC_Inbox == null)
            {
                return HttpNotFound();
            }
            return View(wC_Inbox);
        }

        // POST: WC_Inbox/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(long id)
        {
            WC_Inbox wC_Inbox = db.WC_Inbox.Find(id);
            db.WC_Inbox.Remove(wC_Inbox);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public void SendEmailIQ() //send email to the applicable district
        {
            string dEmail = "Cole.K.Perry@wv.gov"; //Recipient 
            MailMessage mail = new MailMessage("DOHRoadwayHistorySrv@wv.gov", dEmail);
            mail.IsBodyHtml = true;
            mail.Subject = "New WC Inbox item";
            mail.Body = "What up?";
            SmtpClient client = new SmtpClient("smtp.office365.com");
            client.Port = 587;
            client.EnableSsl = true;
            client.UseDefaultCredentials = false; // Important: This line of code must be executed before setting the NetworkCredentials object, otherwise the setting will be reset (a bug in .NET)
            NetworkCredential cred = new System.Net.NetworkCredential("DOHRoadwayHistorySrv@wv.gov", "5FZScjR/"); client.Credentials = cred;
            client.Send(mail);
        }

    }
}

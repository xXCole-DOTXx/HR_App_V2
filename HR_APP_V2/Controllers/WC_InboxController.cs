using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
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

            wC_Inbox = wC_Inbox.Where(s => s.Status != "Archived");

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
            ViewBag.Now = DateTime.Today;
            return View(new WC_Inbox());
        }

        // POST: WC_Inbox/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,InboxID,EmployeeID,District,Org_Number,Hire_Date,Job_Title,Work_Schedule,Injury_Date,Injury_Time,DOT_12,Start_Time,Injured_Body_Part,Side,Missing_Work,Return_to_Work_Date,Doctors_Release,Treatment,Injury_Description,Equipment,Witness,Questioned,Medical_History,Inbox_Submitted,Inbox_Reason,Comments,User_Email,Contact_Email,Specialist_Email,Optional_Email,Optional_Email2,Optional_Email3,Optional_Email4,Status,Add_User,Date_Added")] WC_Inbox wC_Inbox, string fullName)
        {
            if (ModelState.IsValid)
            {
                wC_Inbox.Add_User = User.Identity.Name;
                wC_Inbox.Date_Added = DateTime.Now;

                //Check data before submitting
                Regex rx = new Regex(@"[^0-9]"); //Everything that isn't a number
                if (rx.IsMatch(wC_Inbox.Org_Number))
                {
                    return RedirectToAction("ErrorMessage", new { message = "Org Number must be a 4 digit number." });
                }
                Regex timeRx = new Regex(@"^(1[0-2]|0?[1-9]):([0-5]\d)\s?((?:A|P)\.?M\.?)$", RegexOptions.IgnoreCase);
                if (wC_Inbox.Injury_Time.Length <= 7 && wC_Inbox.Injury_Time.Length >= 6)
                {
                    if (!timeRx.IsMatch(wC_Inbox.Injury_Time))
                    {
                        return RedirectToAction("ErrorMessage", new { message = "Injury Time must be in the proper time format: 9:30am." });
                    }
                }
                else
                {
                    return RedirectToAction("ErrorMessage", new { message = "Injury Time must be in the proper time format: 9:30am. The data you entered included either too many or too few characters." });
                }

                if (wC_Inbox.Start_Time.Length <= 7 && wC_Inbox.Start_Time.Length >= 6)
                {
                    if (!timeRx.IsMatch(wC_Inbox.Start_Time))
                    {
                        return RedirectToAction("ErrorMessage", new { message = "Start Time must be in the proper time format: 9:30am." });
                    }
                }
                else
                {
                    return RedirectToAction("ErrorMessage", new { message = "Start Time must be in the proper time format: 9:30am. The data you entered included either too many or too few characters." });
                }
                //Checking end

                try
                {
                    db.WC_Inbox.Add(wC_Inbox);
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
                            return RedirectToAction("ErrorMessage", new { message = err.ToString() });
                        }
                    }
                }
                string[] recipients = { "E096752@wv.gov", "Lydia.j.bunner@wv.gov", "Tina.m.huffman@wv.gov", "jonathan.w.schaffer@wv.gov", "kathryn.l.hill@wv.gov", "brandon.j.cook@wv.gov", "kristen.m.shrewsbury@wv.gov", "debra.k.davis@wv.gov" };
                for (int i = 0; i < recipients.Length; i++)
                {
                    SendEmail(wC_Inbox.Org_Number, wC_Inbox.District, recipients[i]);
                }
                
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
            string addUser = wC_Inbox.Add_User;
            ViewBag.EmployeeID = id;
            ViewBag.Name = fullName;
            ViewBag.Add_User = addUser;

            return View(wC_Inbox);
        }

        // POST: WC_Inbox/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,InboxID,EmployeeID,District,Org_Number,Hire_Date,Job_Title,Work_Schedule,Injury_Date,Injury_Time,DOT_12,Start_Time,Injured_Body_Part,Side,Missing_Work,Return_to_Work_Date,Doctors_Release,Treatment,Injury_Description,Equipment,Witness,Questioned,Medical_History,Inbox_Submitted,Inbox_Reason,Comments,User_Email,Contact_Email,Specialist_Email,Optional_Email,Optional_Email2,Optional_Email3,Optional_Email4, Add_User")] WC_Inbox wC_Inbox)
        {
            if (ModelState.IsValid)
            {

                
                db.Entry(wC_Inbox).State = EntityState.Modified;
                try
                {
                    db.SaveChanges();
                }
                catch (DbEntityValidationException ex)
                {
                    foreach (var errors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in errors.ValidationErrors)
                        {
                            // get the error message 
                            string errorMessage = validationError.ErrorMessage;
                            System.Diagnostics.Debug.WriteLine(errorMessage);

                        }
                    }
                }
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
            ViewBag.EmployeeID = id;
            ViewBag.Name = fullName;
            var user = wC_Inbox.Add_User;
            var addDate = wC_Inbox.Date_Added;
            ViewBag.user = user;
            ViewBag.date = addDate;

            return View(wC_Inbox);
        }

        // POST: WC_Inbox/Work/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Work([Bind(Include = "ID,EncovaID,EmployeeID,District,Org_Number,Hire_Date,Job_Title,Work_Schedule,Injury_Date,Injury_Time,DOT_12,Start_Time,Injured_Body_Part,Side,Missing_Work,Return_to_Work_Date,Doctors_Release,Treatment,Injury_Description,Equipment,Witness,Questioned,Medical_History,Inbox_Submitted,Comments,User_Email,Contact_Email,Specialist_Email,Optional_Email,Optional_Email2,Optional_Email3,Optional_Email4,TX_EROI_lag,Claim_Ruling,Injury_Type,TTD_Onset_Date,Restricted_RTW,Full_Duty_RTW,TTD_Award_notice,RTW_Notice_Carrier,Lost_Time_Start1,Lost_Time_End1,Lost_Time_Start2,Lost_Time_End2,Lost_Time_Start3,Lost_Time_End3,Status,HR_Comments,Add_User,Date_Added,HR_User,Date_Modified")] WC_Inbox wC_Inbox)
        {
            if (ModelState.IsValid)
            {
                wC_Inbox.HR_User = User.Identity.Name;
                wC_Inbox.Date_Modified = DateTime.Now;
                db.Entry(wC_Inbox).State = EntityState.Modified;
                try
                {
                    db.SaveChanges();
                }
                catch (DbEntityValidationException dbEx)
                {
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            Trace.TraceInformation("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                        }
                    }
                }
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
            try
            {
                db.WC_Inbox.Remove(wC_Inbox);
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
                        return RedirectToAction("ErrorMessage", new { message = err.ToString() });
                    }
                }
            }
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

        public void SendEmail(string orgNum, int? district, string recipient) 
        {
            //send an e-mail to procuremnt to let them know an invalid e-mail was provided, and that the software in question is expiring.  
            string emailText = "<html><body><div><br>A new workers comp form has been created for an employee in district " + district +  " org " + orgNum + ".</ div ></ body ></ html >";
            MailMessage myMail = new MailMessage("DOHRoadwayHistorySrv@wv.gov", recipient);
            myMail.IsBodyHtml = true;
            myMail.Subject = "New WC Inbox Form";
            myMail.Body = emailText;
            SmtpClient client1 = new SmtpClient("relay.wv.gov");
            client1.Port = 25;
            client1.EnableSsl = false;
            client1.UseDefaultCredentials = false; // Important: This line of code must be executed before setting the NetworkCredentials object, otherwise the setting will be reset (a bug in .NET)
            NetworkCredential cred1 = new System.Net.NetworkCredential("DOTHumanResourcesSrv@wv.gov", "wnC6W6?C"); client1.Credentials = cred1;
            client1.Send(myMail);
        }

        public ActionResult ErrorMessage(string message)
        {
            ViewBag.ErrorMessage = message;
            System.Diagnostics.Debug.WriteLine("Error message: " + message);
            return View();
        }

    }
}

// using System.Data.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PBL3_Course.Models;
using PBL3_Course.Services;

namespace PBL3_Course.Controllers;
[Authorize]
public class CourseController : Controller
{
    private readonly IWebHostEnvironment _environment;

    private readonly AppDbContext _context;
    private readonly IVnPayServices _vnPayServices;
    private readonly ILogger<CourseController> _logger;

    public CourseController(ILogger<CourseController> logger,AppDbContext context,IWebHostEnvironment environment,IVnPayServices vnPayServices)
    {
        _logger = logger;
        _context=context;
        _environment=environment;
        _vnPayServices=vnPayServices;
        
    }
    //Quản lí khóa học(admin)
    [Authorize(Roles ="Admin")]
    public IActionResult Index()
    {
        var allCourse=_context.courses.Where(c=>c.IsActive==1).Include(c=>c.Category).ToList();
        return View(allCourse);
    }
    [Authorize(Roles ="Admin")]
    public IActionResult IndexForCourseNotActive()
    {
        var allCourse=_context.courses.Where(c=>c.IsActive==0).Include(c=>c.Category).ToList();
        return View("Index",allCourse);
    }
    [HttpGet]
    public IActionResult Create()
    {
        SelectList categoryList=new SelectList(_context.categories,"Id","CategoryName");
        ViewData["categoryList"]=categoryList;
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> Create([Bind("CourseName,Description,ImageFile,status,Price,CategoryId")] Course course)
    {
        if(!ModelState.IsValid)
        {
            return View();
        }
        if(course.ImageFile!=null)
        {
            var filepath=Path.Combine(_environment.WebRootPath,"uploads",course.ImageFile.FileName);
            if(!System.IO.File.Exists(filepath))
            {
                using FileStream fileStream=new FileStream(filepath,FileMode.Create);
                course.ImageFile.CopyTo(fileStream);
            }
            course.CourseImageLink=$"uploads/{course.ImageFile.FileName}";
        }
        if(User.IsInRole("Admin"))
        {
            course.IsActive=1;
        }
        var UserId=User.Claims.FirstOrDefault(c=>c.Type=="Id")?.Value;
        course.TeacherId=int.Parse(UserId);
        course.DateCreated=DateTime.Now;
        await _context.courses.AddAsync(course);
        await _context.SaveChangesAsync();
        if(User.IsInRole("Admin")==false)
        {
            return RedirectToAction("Index","Home");
        }
        return RedirectToAction("Index");
    }
    [AllowAnonymous]
    public IActionResult Detail(int? id)
    {
        if(id==null)
        {
            return Content("Không tìm thấy khóa học này");
        }
        var kq=_context.courses.Where(c=>c.Id==id).FirstOrDefault();
        if(kq==null)
        {
            return Content("Không tìm thấy khóa học này");
        }
        if(kq.IsActive==0)
        {
            return NotFound();
        }
        _context.Entry(kq).Collection(c=>c.chapters).Load();
        if(kq.chapters!=null)
        {
            foreach(var item in kq.chapters)
            {
                _context.Entry(item).Collection(i=>i.lessons).Load();
            }
        }
        
        return View(kq);
    }
    [HttpGet]
    public IActionResult Edit(int? id)
    {
        if(id==null)
        {
            return NotFound();
        }

        SelectList categoryList=new SelectList(_context.categories,"Id","CategoryName");
        ViewData["categoryList"]=categoryList;
        var kq=_context.courses.Where(c=>c.Id==id).FirstOrDefault();
        if(kq==null)
        {
            return Content("Không tìm thấy Course");
        }
        int userId=Int32.Parse(User.Claims.First(c => c.Type == "Id").Value);
        if((User.IsInRole("Teacher")&&kq.TeacherId==userId)==false)
        {
            return NotFound();
        }
        return View(kq);
    }
    [HttpPost]
    public async Task<IActionResult> Edit(int? id,[Bind("CourseName,Description,ImageFile,status,Price,CategoryId")] Course course)
    {
        if(!ModelState.IsValid)
        {
            return View();
        }
        var kq=_context.courses.Where(c=>c.Id==id).FirstOrDefault();
        
        if(kq==null)
        {
            return Content("Khong tim thay");
        }
        int userId=Int32.Parse(User.Claims.First(c => c.Type == "Id").Value);
        if((User.IsInRole("Teacher")&&kq.TeacherId==userId)==false)
        {
            return NotFound();
        }
        // Xoa file cu di
            // if(string.IsNullOrEmpty(kq.CourseImageLink)==false)
            // {
            //     Console.WriteLine("Hello go to here");
            //     var FolderName="wwwroot/uploads";
            //     string fileNameDelete=kq.CourseImageLink.Substring(8);
            //     Console.WriteLine(FolderName+ fileNameDelete);
            //     DirectoryInfo dir = new DirectoryInfo(FolderName);

            //     foreach(FileInfo fi in dir.GetFiles())
            //     {
            //         if(fi.Name==fileNameDelete)
            //         {
            //             fi.Delete();
            //             break;
            //         }
            //     }
            // }
        // end
        if(course.ImageFile!=null)
        {
            var filepath=Path.Combine(_environment.WebRootPath,"uploads",course.ImageFile.FileName);
            if(!System.IO.File.Exists(filepath))
            {
                using FileStream fileStream=new FileStream(filepath,FileMode.Create);
                course.ImageFile.CopyTo(fileStream);
            }

            course.CourseImageLink=$"uploads/{course.ImageFile.FileName}";
        }
        kq.CategoryId=course.CategoryId;
        kq.CourseName=course.CourseName;
        kq.Price=course.Price;
        kq.DateEdited=DateTime.Now;
        kq.Description=course.Description;
        kq.status=course.status;
        kq.CourseImageLink=course.CourseImageLink;
        _context.Entry(kq).State=EntityState.Modified;
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
    [HttpPost]
    [AllowAnonymous]
    public IActionResult Search(string courseName)
    {
        var kq=_context.courses.Where(c=>c.CourseName.Contains(courseName)).ToList();
        return View("AllCourse",kq);
    }
    [Authorize(Roles ="Admin")]
    public IActionResult SearchForAdmin(string courseName)
    {
         var kq=_context.courses.Where(c=>c.CourseName.Contains(courseName)).ToList();
        return View("Index",kq);
    }
    public IActionResult Delete(int? id)
    {
        if(id==null)
        {
            return Content("Khong tim thay khoa hoc");
        }
        var kq=_context.courses.Where(c=>c.Id==id).FirstOrDefault();
        if(kq==null)
        {
            return Content("Khong tim thay khoa hoc");
        }

        int userId=Int32.Parse(User.Claims.First(c => c.Type == "Id").Value);
        if((User.IsInRole("Teacher")&&kq.TeacherId==userId)==false)
        {
            return NotFound();
        }
        _context.courses.Remove(kq);
        _context.SaveChanges();
        //Kiem tra xem user còn đang dạy khóa nào hay không
        bool checkExistCourseWithTeacherId=_context.courses.Any(c=>c.TeacherId==kq.TeacherId);
        //Lấy ID của teacher trong database
        int TeacherId=_context.roles.Where(r=>r.RoleName=="Teacher").Select(r=>r.Id).FirstOrDefault();
        if(checkExistCourseWithTeacherId==false)
        {
            var deleteRoleTeacher=_context.usersRoles.Where(c=>c.UsersId==kq.TeacherId&&c.RoleId==TeacherId).FirstOrDefault();
            //Neu ton tai thi xoa role teacher cho user do
            if(deleteRoleTeacher!=null)
            {
                _context.usersRoles.Remove(deleteRoleTeacher);
            }
        }
        _context.SaveChanges();
        return RedirectToAction("Index");
    }
    [AllowAnonymous]
    public IActionResult AllCourse(int?id,string sortBy)
    {
        List<Course> allCourse=new List<Course>();
        if(id==null)
        {
            allCourse=_context.courses.ToList();
            //sort
            if(sortBy=="price1")
            {
                allCourse=_context.courses.OrderByDescending(c=>c.Price).ToList();
            }
            else if(sortBy=="price2")
            {
                allCourse=_context.courses.OrderBy(c=>c.Price).ToList();
            }
            allCourse=allCourse.Where(c=>c.IsActive==1).ToList();
            return View(allCourse);
        }
        //Neu id khac null
        allCourse=_context.courses.Where(c=>c.CategoryId==id).ToList();
        //sort
        if(sortBy=="name")
        {
            allCourse=_context.courses.OrderBy(c=>c.CourseName).ToList();
        }
        allCourse=allCourse.Where(c=>c.IsActive==1).ToList();
        return View(allCourse);
    }
    public IActionResult CourseActivate(int id)
    {
        var kq=_context.courses.Where(c=>c.Id==id).FirstOrDefault();
        if(kq==null)
        {
            return Content("Khong tim thay khoa hoc");
        }
        _context.Entry(kq).State=EntityState.Modified;
        kq.IsActive=1;
        //Lay ra id cua user hien tai
        int UserId=kq.TeacherId;
        //Tim Teacher Id Trong role
        UsersRole usersRole=new UsersRole();
        var RoleTeacherID=_context.roles.Where(r=>r.RoleName=="Teacher").Select(r=>r.Id).FirstOrDefault();
        
        //Kiem tra nguoi dung hien tai da co role teacher hay chua
        bool checkAddUserRole=_context.usersRoles.Any(u=>u.RoleId==RoleTeacherID&& u.UsersId==UserId);
        if(checkAddUserRole==false)
        {
            usersRole.UsersId=UserId;
            usersRole.RoleId=RoleTeacherID;
            _context.usersRoles.Add(usersRole);
        }
        _context.SaveChanges();
        return RedirectToAction("Index");
    }
    public IActionResult Checkout(int? courseId)
    {
        if(courseId==null)
        {
            return NotFound();
        }
        bool checkCourse=_context.courses.Any(c => c.Id==courseId);
        if(checkCourse==false)
        {
            return Content("Khong tim thay khoa hoc");
        }
        ViewData["courseId"]=courseId;
        return View();
    }
    public IActionResult CourseEnroll(int courseId)
    {
        if(User.Identity.IsAuthenticated==false)
        {
            return RedirectToAction("Login","User");
        }
        bool checkCourse=_context.courses.Any(c => c.Id==courseId);
        if(checkCourse==false)
        {
            return Content("Khong tim thay khoa hoc");
        }
        int userId=Int32.Parse(User.Claims.First(c => c.Type == "Id").Value);
        if(_context.usersCourses.Any(u=>u.UsersId==userId&&u.CourseId==courseId))
        {
            return Content("Ban da dang ki khoa hoc nay roi");
        }
        UsersCourse usersCourse=new UsersCourse();
        usersCourse.CourseId=courseId;
        usersCourse.UsersId=userId;
        _context.usersCourses.Add(usersCourse);
        _context.SaveChanges();
        return RedirectToAction("Index","Home");
    }
    public IActionResult CheckoutVNPay(int courseId)
    {
        if(User.Identity.IsAuthenticated==false)
        {
            return RedirectToAction("Login","User");
        }
        var checkCourse=_context.courses.Where(c => c.Id==courseId).FirstOrDefault();
        if(checkCourse==null)
        {
            return Content("Khong tim thay khoa hoc");
        }
        int userId=Int32.Parse(User.Claims.First(c => c.Type == "Id").Value);
        if(_context.usersCourses.Any(u=>u.UsersId==userId&&u.CourseId==courseId))
        {
            return Content("Ban da dang ki khoa hoc nay roi");
        }

        var vnPayModel = new VnPaymentRequestModel
					{
						Amount = (double)checkCourse.Price*23000,
						CreatedDate = DateTime.Now,
						Description = $"",
						FullName = "",
						OrderId = new Random().Next(1000, 100000),
                        courseId=courseId,
					};
        // Serialize vnPayModel thành chuỗi JSON
        var vnPayModelJson = JsonConvert.SerializeObject(vnPayModel);

    // Lưu chuỗi JSON trong TempData
        TempData["vnPayModel"] = vnPayModelJson;

        return Redirect(_vnPayServices.CreatePaymentUrl(HttpContext,vnPayModel));
    }
    public IActionResult PaymentCallBack()
		{
			var response = _vnPayServices.PaymentExecute(Request.Query);

			if (response == null || response.VnPayResponseCode != "00")
			{
				TempData["Message"] = $"Lỗi thanh toán VN Pay: {response.VnPayResponseCode}";
				return View("PaymentFail");
			}

            var vnPayModelJson = TempData["vnPayModel"] as string;
            var vnPayModel = JsonConvert.DeserializeObject<VnPaymentRequestModel>(vnPayModelJson);


            //User cua id hien tai dang dang nhap
            int userId=Int32.Parse(User.Claims.First(c => c.Type == "Id").Value);

            if (vnPayModel != null)
            {
                // Lưu đơn hàng vô database
                Order order=new Order();
                order.UserId=userId;
                order.courseId=vnPayModel.courseId;
                order.TotalMoney=vnPayModel.Amount;
                order.DateCreated=vnPayModel.CreatedDate;
                _context.orders.Add(order);


                //Kich hoat khoa hoc cho user o day
                UsersCourse usersCourse=new UsersCourse();
                usersCourse.CourseId=vnPayModel.courseId;
                usersCourse.UsersId=userId;
                _context.usersCourses.Add(usersCourse);
            }
            
            _context.SaveChanges();
			

			TempData["Message"] = $"Thanh toán VNPay thành công";
			return RedirectToAction("Index","Home");
		}
}

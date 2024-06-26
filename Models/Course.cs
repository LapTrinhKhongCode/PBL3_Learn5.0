using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PBL3_Course
{
    [Table("Course")]
    public class Course
    {
        [Key]
        public int Id{set;get;}
        [Required]
        [Display(Name ="Course name")]
        public string CourseName{set;get;}
        [Required]
        [Display(Name ="Price")]

        public decimal Price{set;get;}
        [DataType(DataType.Text)]
        [Display(Name ="Course description")]

        public string? Description{set;get;}
        public ICollection<Chapter>? chapters{set;get;}
        public DateTime DateCreated{set;get;}
        public DateTime? DateEdited{set;get;}
        public int status {set;get;}
        [Display(Name ="Photo representing the course")]
        [NotMapped]
        public IFormFile? ImageFile{set;get;}
        public string? CourseImageLink{set;get;}
        public int IsActive{set;get;}

        public ICollection<UsersCourse>? usersCourses{set;get;}
        // Khoa ngoai lien ket den bang category
        [ForeignKey("CategoryId")]
        public int CategoryId{set;get;}
        public Category? Category{set;get;}

        //Teacher ID
         [ForeignKey("TeacherId")]
        public int TeacherId{set;get;}
        public Users? Teacher{set;get;}

        public ICollection<Test>? Tests{set;get;}
        public ICollection<Comment>? comments{set;get;}
       

    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PBL3_Course
{
    [Table("Question")]
    public class Question
    {
        [Key]
        public int Id{set;get;}
        [Required(ErrorMessage ="You have not entered content for the question")]
        public string QuestionName{set;get;}
        [ForeignKey("TestId")]
        public Test? Test{set;get;}
        public int TestId{set;get;}
        public List<Answer>? Answers{set;get;}
    }
}
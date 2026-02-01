using System.ComponentModel.DataAnnotations;

namespace Kursserver.Models
{
    public class NoClass
    {
        [Key]
        public int Id { get; set; }

        public DateTime Date { get; set; }
    }

}

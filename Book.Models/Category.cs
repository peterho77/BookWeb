using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Book.Models
{
	public class Category
	{
		[Key]
		public int Id {  get; set; }
		[Required]
		[MaxLength(30)]
		[DisplayName("Name")]
		public string Name { get; set; }
		[Range(1,100)]
		[DisplayName("Display order")]
		public int DisplayOrder {  get; set; }

	}
}

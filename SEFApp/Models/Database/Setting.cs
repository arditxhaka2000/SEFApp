using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEFApp.Models.Database
{
    [Table("Settings")]
    public class Setting
    {
        [PrimaryKey]
        public string Key { get; set; }

        public string Value { get; set; }
        public string Description { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}

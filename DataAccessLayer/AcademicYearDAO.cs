using BussinessObject.Models;
using DataAccessLayer.BaseDAO;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer
{
    public class AcademicYearDAO : BaseDAO<AcademicYear>
    {
        private readonly ASDPRSContext _context;
        public AcademicYearDAO(ASDPRSContext context) : base(context)
        {
            _context = context;
        }
    }
}

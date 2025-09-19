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
    public class CampusDAO : BaseDAO<Campus>
    {
        private readonly ASDPRSContext _context;
        public CampusDAO(ASDPRSContext context) : base(context)
        {
            _context = context;
        }
    }
}

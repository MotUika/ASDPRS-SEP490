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
    public class SystemConfigDAO : BaseDAO<SystemConfig>
    {
        private readonly ASDPRSContext _context;
        public SystemConfigDAO(ASDPRSContext context) : base(context)
        {
            _context = context;
        }
    }
}

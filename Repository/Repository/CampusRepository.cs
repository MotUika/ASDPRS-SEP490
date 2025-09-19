using BussinessObject.Models;
using DataAccessLayer;
using DataAccessLayer.BaseDAO;
using Microsoft.EntityFrameworkCore;
using Repository.BaseRepository;
using Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Repository
{
    public class CampusRepository : BaseRepository<Campus>, ICampusRepository
    {
        private readonly ASDPRSContext _context;

        public CampusRepository(BaseDAO<Campus> baseDao, ASDPRSContext context) : base(baseDao)
        {
            _context = context;
        }

        // No additional methods needed since BaseRepository provides all basic CRUD operations
    }
}

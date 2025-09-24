using BussinessObject.Models;
using DataAccessLayer.BaseDAO;
using DataAccessLayer.IBaseDAO;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DAO
{
    public class CriteriaDAO : BaseDAO<Criteria>, IBaseDAO<Criteria>
    {
        public CriteriaDAO(ASDPRSContext context) : base(context) { }
    }
}

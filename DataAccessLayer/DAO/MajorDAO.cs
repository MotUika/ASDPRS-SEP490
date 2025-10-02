using BussinessObject.Models;
using DataAccessLayer.BaseDAO;
using DataAccessLayer.IBaseDAO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DAO
{
    public class MajorDAO : BaseDAO<Major>, IBaseDAO<Major>
    {
        public MajorDAO(ASDPRSContext context) : base(context) { }
    }
}

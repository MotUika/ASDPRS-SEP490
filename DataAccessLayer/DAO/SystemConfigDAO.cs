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
    public class SystemConfigDAO : BaseDAO<SystemConfig>, IBaseDAO<SystemConfig>
    {
        public SystemConfigDAO(ASDPRSContext context) : base(context) { }
    }
}

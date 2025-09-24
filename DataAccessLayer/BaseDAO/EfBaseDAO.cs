namespace DataAccessLayer.BaseDAO
{
    public class EfBaseDAO<T> : BaseDAO<T> where T : class
    {
        public EfBaseDAO(ASDPRSContext context) : base(context)
        {
        }
    }
}

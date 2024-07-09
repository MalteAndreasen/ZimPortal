using ZimPortal.Models;

namespace ZimPortal.Data
{
    public class CachingContext
    {
        private List<Outlet> _cache = new List<Outlet>();

        public void AssignCache(List<Outlet> newOutlets)
        {
            if (newOutlets.Count > 0)
                _cache = newOutlets;
            else throw new ArgumentException("Retrieved 0 outlets.");
        }

        public Outlet GetIndex(int i)
        {
            return _cache[i];
        }

        public List<Outlet> GetCache()
        {
            return _cache;
        }
    }
}
